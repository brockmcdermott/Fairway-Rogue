using System.Collections.Generic;
using Godot;

public partial class HoleGenerator : Node
{
    private static readonly Vector2[] DirectionPresets =
    {
        Vector2.Right,
        Vector2.Left,
        Vector2.Up,
        Vector2.Down,
        new Vector2(0.72f, -0.69f).Normalized(),
        new Vector2(0.68f, 0.74f).Normalized(),
        new Vector2(-0.70f, 0.71f).Normalized(),
        new Vector2(-0.74f, -0.67f).Normalized()
    };

    [ExportGroup("Generation Bounds")]
    [Export] public Rect2 GenerationBounds { get; set; } = new Rect2(new Vector2(-700, -420), new Vector2(1400, 840));
    [Export] public float BoundsMargin { get; set; } = 84.0f;

    [ExportGroup("Generation Tuning")]
    [Export] public int MaxGenerationAttempts { get; set; } = 12;
    [Export] public float MinHoleLength { get; set; } = 760.0f;
    [Export] public float MaxHoleLength { get; set; } = 1420.0f;
    [Export] public float LengthJitter { get; set; } = 96.0f;
    [Export] public float MaxFairwayWidth { get; set; } = 300.0f;
    [Export] public float MinFairwayWidth { get; set; } = 170.0f;
    [Export] public bool UseFallbackTemplate { get; set; } = true;

    [ExportGroup("Shape Quality")]
    [Export] public float TeeRadiusMin { get; set; } = 44.0f;
    [Export] public float TeeRadiusMax { get; set; } = 62.0f;
    [Export] public float GreenRadiusMin { get; set; } = 58.0f;
    [Export] public float GreenRadiusMax { get; set; } = 84.0f;
    [Export] public int FairwayControlPointMin { get; set; } = 4;
    [Export] public int FairwayControlPointMax { get; set; } = 7;
    [Export] public float RoughStartWidthScaleMin { get; set; } = 2.90f;
    [Export] public float RoughStartWidthScaleMax { get; set; } = 3.60f;
    [Export] public float RoughEndWidthScaleMin { get; set; } = 2.45f;
    [Export] public float RoughEndWidthScaleMax { get; set; } = 3.15f;

    [ExportGroup("Route Variety")]
    [Export] public float DirectionalCenterJitterX { get; set; } = 140.0f;
    [Export] public float DirectionalCenterJitterY { get; set; } = 110.0f;
    [Export] public float DirectionalCrossAxisJitter { get; set; } = 200.0f;
    [Export] public float OrientationJitterDegrees { get; set; } = 22.0f;

    [ExportGroup("Wind Tuning")]
    [Export] public float MinWindStrength { get; set; } = 0.03f;
    [Export] public float MaxWindStrength { get; set; } = 0.34f;

    private readonly FairwayPathBuilder _fairwayPathBuilder = new FairwayPathBuilder();
    private readonly HazardPlacer _hazardPlacer = new HazardPlacer();
    private readonly HoleValidator _holeValidator = new HoleValidator();

    public HoleLayout GenerateLayout(int runSeed, int holeNumber, int totalHoles)
    {
        return GenerateLayoutForBounds(runSeed, holeNumber, totalHoles, GenerationBounds);
    }

    public HoleLayout GenerateLayoutForBounds(int runSeed, int holeNumber, int totalHoles, Rect2 generationBounds, Vector2? preferredDirection = null)
    {
        var clampedHole = Mathf.Max(1, holeNumber);
        var clampedTotalHoles = Mathf.Max(1, totalHoles);
        var bounds = EnsureValidBounds(generationBounds);
        var baseSeed = ComposeBaseSeed(runSeed, clampedHole);
        var attemptLimit = Mathf.Max(1, MaxGenerationAttempts);

        for (var attempt = 0; attempt < attemptLimit; attempt += 1)
        {
            var attemptSeed = baseSeed + (ulong)(attempt * 7919);
            var rng = new RandomNumberGenerator
            {
                Seed = attemptSeed
            };

            var candidate = BuildCandidateLayout(
                (int)(attemptSeed & 0x7fffffff),
                clampedHole,
                clampedTotalHoles,
                bounds,
                rng,
                preferredDirection);

            if (_holeValidator.Validate(candidate, out _))
            {
                return candidate;
            }
        }

        GD.PushWarning($"[HoleGenerator] Failed validation after {attemptLimit} attempts. Using fallback template for hole {clampedHole}.");
        return BuildFallbackLayout((int)(baseSeed & 0x7fffffff), clampedHole, clampedTotalHoles, bounds, preferredDirection);
    }

    private HoleLayout BuildCandidateLayout(
        int seed,
        int holeNumber,
        int totalHoles,
        Rect2 bounds,
        RandomNumberGenerator rng,
        Vector2? preferredDirection)
    {
        var difficulty = totalHoles <= 1 ? 0.0f : (holeNumber - 1) / (float)(totalHoles - 1);
        var safeBounds = bounds.Grow(-Mathf.Clamp(BoundsMargin, 20.0f, 220.0f));

        var targetLength = Mathf.Lerp(MinHoleLength, MaxHoleLength, difficulty) + rng.RandfRange(-LengthJitter, LengthJitter);
        var maxLengthInBounds = Mathf.Max(360.0f, safeBounds.Size.Length() * 0.80f);
        targetLength = Mathf.Clamp(targetLength, 360.0f, maxLengthInBounds);

        var heading = BuildHoleDirection(holeNumber, rng, preferredDirection);
        var headingNormal = heading.Orthogonal().Normalized();

        var center = safeBounds.GetCenter() + new Vector2(
            rng.RandfRange(-DirectionalCenterJitterX, DirectionalCenterJitterX),
            rng.RandfRange(-DirectionalCenterJitterY, DirectionalCenterJitterY));
        center = ClampPointToRect(center, safeBounds);

        var halfLength = Mathf.Max(200.0f, targetLength * 0.5f);
        var maxCrossJitter = Mathf.Min(DirectionalCrossAxisJitter, Mathf.Min(safeBounds.Size.X, safeBounds.Size.Y) * 0.22f);
        var teeOffset = rng.RandfRange(-maxCrossJitter, maxCrossJitter);
        var cupOffset = rng.RandfRange(-maxCrossJitter, maxCrossJitter);

        var teePosition = ClampPointToRect(center - heading * halfLength + headingNormal * teeOffset, safeBounds);
        var cupPosition = ClampPointToRect(center + heading * halfLength + headingNormal * cupOffset, safeBounds);

        if (teePosition.DistanceTo(cupPosition) < Mathf.Max(400.0f, MinHoleLength * 0.55f))
        {
            var extra = Mathf.Min(120.0f, safeBounds.Size.Length() * 0.08f);
            teePosition = ClampPointToRect(center - heading * (halfLength + extra), safeBounds);
            cupPosition = ClampPointToRect(center + heading * (halfLength + extra), safeBounds);
        }

        var fairwayWidth = Mathf.Lerp(MaxFairwayWidth, MinFairwayWidth, difficulty) + rng.RandfRange(-16.0f, 16.0f);
        fairwayWidth = Mathf.Clamp(fairwayWidth, MinFairwayWidth - 20.0f, MaxFairwayWidth + 24.0f);
        var controlPoints = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(FairwayControlPointMin, FairwayControlPointMax, difficulty + rng.RandfRange(-0.15f, 0.15f))),
            Mathf.Min(FairwayControlPointMin, FairwayControlPointMax),
            Mathf.Max(FairwayControlPointMin, FairwayControlPointMax));

        var lateralJitter = Mathf.Lerp(fairwayWidth * 0.55f, fairwayWidth * 1.08f, difficulty);

        var centerLine = _fairwayPathBuilder.BuildCenterLine(
            teePosition,
            cupPosition,
            controlPoints,
            lateralJitter,
            safeBounds,
            rng);

        var fairwayPolygon = _fairwayPathBuilder.BuildCorridorPolygon(
            centerLine,
            fairwayWidth * 1.30f,
            fairwayWidth * 0.92f);

        var greenRadius = Mathf.Lerp(GreenRadiusMax, GreenRadiusMin, difficulty) + rng.RandfRange(-5.0f, 5.0f);
        var teeRadius = Mathf.Lerp(TeeRadiusMax, TeeRadiusMin, difficulty) + rng.RandfRange(-3.0f, 3.0f);

        var greenPolygon = BuildOrganicBlob(
            cupPosition,
            Mathf.Max(32.0f, greenRadius),
            24,
            0.10f,
            rng,
            heading.Angle());

        var teePolygon = BuildOrganicBlob(
            teePosition,
            Mathf.Max(28.0f, teeRadius),
            20,
            0.06f,
            rng,
            heading.Angle() + Mathf.Pi * 0.5f);

        var roughPolygon = BuildRoughEnvelope(centerLine, fairwayWidth, bounds, rng);

        var holeLength = teePosition.DistanceTo(cupPosition);
        var par = holeLength < 700.0f ? 3 : holeLength < 1030.0f ? 4 : 5;
        var windDirection = Vector2.Right.Rotated(rng.RandfRange(-Mathf.Pi, Mathf.Pi)).Normalized();
        var windStrength = Mathf.Clamp(
            Mathf.Lerp(MinWindStrength, MaxWindStrength, difficulty) + rng.RandfRange(-0.02f, 0.02f),
            MinWindStrength,
            MaxWindStrength);

        var layout = new HoleLayout
        {
            HoleNumber = holeNumber,
            Seed = seed,
            Par = par,
            Bounds = bounds,
            TeePosition = teePosition,
            CupPosition = cupPosition,
            WindDirection = windDirection,
            WindStrength = windStrength,
            FairwayPathPoints = centerLine,
            RoughPolygon = roughPolygon,
            FairwayPolygon = fairwayPolygon,
            GreenPolygon = greenPolygon,
            TeePolygon = teePolygon,
            SandPolygons = new List<Vector2[]>(),
            WaterPolygons = new List<Vector2[]>(),
            TreeAreaPolygons = new List<Vector2[]>(),
            TreeObstacles = new List<TreeObstacleLayout>()
        };

        _hazardPlacer.PopulateHazards(layout, difficulty, fairwayWidth, rng);
        return layout;
    }

    private HoleLayout BuildFallbackLayout(
        int seed,
        int holeNumber,
        int totalHoles,
        Rect2 bounds,
        Vector2? preferredDirection)
    {
        if (!UseFallbackTemplate)
        {
            var fallbackRng = new RandomNumberGenerator
            {
                Seed = ComposeBaseSeed(seed, holeNumber)
            };

            return BuildCandidateLayout(seed, holeNumber, totalHoles, bounds, fallbackRng, preferredDirection);
        }

        var rng = new RandomNumberGenerator { Seed = ComposeBaseSeed(seed, holeNumber) };
        var safeBounds = bounds.Grow(-Mathf.Clamp(BoundsMargin, 20.0f, 220.0f));
        var direction = BuildHoleDirection(holeNumber, rng, preferredDirection);
        var normal = direction.Orthogonal().Normalized();
        var center = safeBounds.GetCenter();
        var tee = ClampPointToRect(center - direction * 440.0f - normal * 24.0f, safeBounds);
        var cup = ClampPointToRect(center + direction * 440.0f + normal * 26.0f, safeBounds);
        var fairwayCenterLine = new List<Vector2>
        {
            tee,
            tee.Lerp(cup, 0.25f) + normal * 80.0f,
            tee.Lerp(cup, 0.50f) - normal * 75.0f,
            tee.Lerp(cup, 0.75f) + normal * 60.0f,
            cup
        };

        return new HoleLayout
        {
            HoleNumber = holeNumber,
            Seed = seed,
            Par = 4,
            Bounds = bounds,
            TeePosition = tee,
            CupPosition = cup,
            WindDirection = direction,
            WindStrength = MinWindStrength,
            FairwayPathPoints = _fairwayPathBuilder.BuildCenterLine(tee, cup, 4, 110.0f, safeBounds, rng),
            RoughPolygon = BuildRoughEnvelope(fairwayCenterLine, 220.0f, bounds, rng),
            FairwayPolygon = _fairwayPathBuilder.BuildCorridorPolygon(fairwayCenterLine, 260.0f, 190.0f),
            GreenPolygon = BuildOrganicBlob(cup, 72.0f, 24, 0.06f, rng, direction.Angle()),
            TeePolygon = BuildOrganicBlob(tee, 54.0f, 20, 0.04f, rng, direction.Angle() + Mathf.Pi * 0.5f),
            SandPolygons = new List<Vector2[]>
            {
                BuildEllipsePolygon(new Vector2(cup.X - 130.0f, cup.Y + 70.0f), 58.0f, 38.0f, 0.25f, 16)
            },
            WaterPolygons = new List<Vector2[]>(),
            TreeAreaPolygons = new List<Vector2[]>
            {
                BuildEllipsePolygon(tee.Lerp(cup, 0.58f) + normal * 140.0f, 118.0f, 78.0f, 0.2f, 16)
            },
            TreeObstacles = new List<TreeObstacleLayout>
            {
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.57f) + normal * 132.0f, Radius = 20.0f },
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.61f) + normal * 126.0f, Radius = 18.0f },
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.65f) + normal * 142.0f, Radius = 19.0f }
            }
        };
    }

    private static ulong ComposeBaseSeed(int runSeed, int holeNumber)
    {
        unchecked
        {
            var mixed = ((ulong)(uint)runSeed * 73856093UL) ^
                        ((ulong)(uint)holeNumber * 19349663UL) ^
                        0x9e3779b97f4a7c15UL;
            return mixed == 0UL ? 1UL : mixed;
        }
    }

    private static Rect2 EnsureValidBounds(Rect2 bounds)
    {
        var size = new Vector2(
            Mathf.Max(560.0f, Mathf.Abs(bounds.Size.X)),
            Mathf.Max(420.0f, Mathf.Abs(bounds.Size.Y)));
        var position = bounds.Size.X >= 0.0f && bounds.Size.Y >= 0.0f
            ? bounds.Position
            : new Vector2(
                Mathf.Min(bounds.Position.X, bounds.Position.X + bounds.Size.X),
                Mathf.Min(bounds.Position.Y, bounds.Position.Y + bounds.Size.Y));
        return new Rect2(position, size);
    }

    private static Vector2[] BuildRectPolygon(Rect2 bounds)
    {
        return new[]
        {
            bounds.Position,
            new Vector2(bounds.End.X, bounds.Position.Y),
            bounds.End,
            new Vector2(bounds.Position.X, bounds.End.Y)
        };
    }

    private Vector2[] BuildRoughEnvelope(List<Vector2> centerLine, float fairwayWidth, Rect2 bounds, RandomNumberGenerator rng)
    {
        if (centerLine.Count < 2)
        {
            return BuildRectPolygon(bounds);
        }

        var startScale = rng.RandfRange(
            Mathf.Min(RoughStartWidthScaleMin, RoughStartWidthScaleMax),
            Mathf.Max(RoughStartWidthScaleMin, RoughStartWidthScaleMax));
        var endScale = rng.RandfRange(
            Mathf.Min(RoughEndWidthScaleMin, RoughEndWidthScaleMax),
            Mathf.Max(RoughEndWidthScaleMin, RoughEndWidthScaleMax));

        var startWidth = Mathf.Max(220.0f, fairwayWidth * startScale);
        var endWidth = Mathf.Max(180.0f, fairwayWidth * endScale);
        var polygon = _fairwayPathBuilder.BuildCorridorPolygon(centerLine, startWidth, endWidth);
        if (polygon.Length < 6)
        {
            return BuildRectPolygon(bounds);
        }

        var safeBounds = bounds.Grow(-Mathf.Clamp(BoundsMargin * 0.35f, 10.0f, 48.0f));
        for (var i = 0; i < polygon.Length; i += 1)
        {
            polygon[i] = ClampPointToRect(polygon[i], safeBounds);
        }

        return polygon;
    }

    private static Vector2[] BuildEllipsePolygon(Vector2 center, float radiusX, float radiusY, float rotation, int segments)
    {
        var clampedSegments = Mathf.Max(8, segments);
        var polygon = new Vector2[clampedSegments];

        for (var i = 0; i < clampedSegments; i += 1)
        {
            var angle = Mathf.Tau * i / clampedSegments;
            var local = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY).Rotated(rotation);
            polygon[i] = center + local;
        }

        return polygon;
    }

    private static Vector2[] BuildOrganicBlob(
        Vector2 center,
        float radius,
        int segments,
        float radialVariance,
        RandomNumberGenerator rng,
        float rotation)
    {
        var clampedSegments = Mathf.Clamp(segments, 10, 40);
        var clampedVariance = Mathf.Clamp(radialVariance, 0.0f, 0.25f);
        var points = new Vector2[clampedSegments];

        for (var i = 0; i < clampedSegments; i += 1)
        {
            var t = i / (float)clampedSegments;
            var angle = Mathf.Tau * t;
            var noise = Mathf.Sin(angle * 2.0f + rng.RandfRange(-0.6f, 0.6f)) * 0.5f +
                        Mathf.Sin(angle * 3.0f + rng.RandfRange(-0.6f, 0.6f)) * 0.3f;
            var adjustedRadius = radius * (1.0f + noise * clampedVariance);
            var local = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * adjustedRadius;
            points[i] = center + local.Rotated(rotation);
        }

        return points;
    }

    private Vector2 BuildHoleDirection(int holeNumber, RandomNumberGenerator rng, Vector2? preferredDirection)
    {
        if (preferredDirection.HasValue && preferredDirection.Value != Vector2.Zero)
        {
            var bias = preferredDirection.Value.Normalized();
            var biasJitter = Mathf.DegToRad(rng.RandfRange(-OrientationJitterDegrees * 0.65f, OrientationJitterDegrees * 0.65f));
            var rotated = bias.Rotated(biasJitter).Normalized();
            return rotated == Vector2.Zero ? Vector2.Right : rotated;
        }

        var baseIndex = Mathf.Abs(holeNumber - 1) % DirectionPresets.Length;
        var variantOffset = rng.RandiRange(0, 2);
        var direction = DirectionPresets[(baseIndex + variantOffset) % DirectionPresets.Length];
        var jitterRadians = Mathf.DegToRad(rng.RandfRange(-OrientationJitterDegrees, OrientationJitterDegrees));
        direction = direction.Rotated(jitterRadians).Normalized();
        return direction == Vector2.Zero ? Vector2.Right : direction;
    }

    private static Vector2 ClampPointToRect(Vector2 point, Rect2 bounds)
    {
        return new Vector2(
            Mathf.Clamp(point.X, bounds.Position.X, bounds.End.X),
            Mathf.Clamp(point.Y, bounds.Position.Y, bounds.End.Y));
    }
}
