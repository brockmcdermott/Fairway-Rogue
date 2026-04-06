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
    [Export] public Rect2 GenerationBounds { get; set; } = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));
    [Export] public float BoundsMargin { get; set; } = 70.0f;

    [ExportGroup("Generation Tuning")]
    [Export] public int MaxGenerationAttempts { get; set; } = 10;
    [Export] public float MinHoleLength { get; set; } = 520.0f;
    [Export] public float MaxHoleLength { get; set; } = 890.0f;
    [Export] public float LengthJitter { get; set; } = 44.0f;
    [Export] public float MaxFairwayWidth { get; set; } = 176.0f;
    [Export] public float MinFairwayWidth { get; set; } = 108.0f;
    [Export] public bool UseFallbackTemplate { get; set; } = true;

    [ExportGroup("Route Variety")]
    [Export] public float DirectionalCenterJitterX { get; set; } = 120.0f;
    [Export] public float DirectionalCenterJitterY { get; set; } = 90.0f;
    [Export] public float DirectionalCrossAxisJitter { get; set; } = 150.0f;
    [Export] public float OrientationJitterDegrees { get; set; } = 15.0f;

    [ExportGroup("Wind Tuning")]
    [Export] public float MinWindStrength { get; set; } = 0.03f;
    [Export] public float MaxWindStrength { get; set; } = 0.34f;

    private readonly FairwayPathBuilder _fairwayPathBuilder = new FairwayPathBuilder();
    private readonly HazardPlacer _hazardPlacer = new HazardPlacer();
    private readonly HoleValidator _holeValidator = new HoleValidator();

    public HoleLayout GenerateLayout(int runSeed, int holeNumber, int totalHoles)
    {
        var clampedHole = Mathf.Max(1, holeNumber);
        var clampedTotalHoles = Mathf.Max(1, totalHoles);
        var baseSeed = ComposeBaseSeed(runSeed, clampedHole);
        var attemptLimit = Mathf.Max(1, MaxGenerationAttempts);

        for (var attempt = 0; attempt < attemptLimit; attempt += 1)
        {
            var attemptSeed = baseSeed + (ulong)(attempt * 7919);
            var rng = new RandomNumberGenerator
            {
                Seed = attemptSeed
            };

            var candidate = BuildCandidateLayout((int)(attemptSeed & 0x7fffffff), clampedHole, clampedTotalHoles, rng);
            if (_holeValidator.Validate(candidate, out _))
            {
                return candidate;
            }
        }

        GD.PushWarning($"[HoleGenerator] Failed validation after {attemptLimit} attempts. Using fallback template for hole {clampedHole}.");
        return BuildFallbackLayout((int)(baseSeed & 0x7fffffff), clampedHole, clampedTotalHoles);
    }

    private HoleLayout BuildCandidateLayout(int seed, int holeNumber, int totalHoles, RandomNumberGenerator rng)
    {
        var difficulty = totalHoles <= 1 ? 0.0f : (holeNumber - 1) / (float)(totalHoles - 1);
        var safeBounds = GenerationBounds.Grow(-BoundsMargin);

        var targetLength = Mathf.Lerp(MinHoleLength, MaxHoleLength, difficulty) + rng.RandfRange(-LengthJitter, LengthJitter);
        var heading = BuildHoleDirection(holeNumber, rng);
        var headingNormal = heading.Orthogonal().Normalized();

        var center = safeBounds.GetCenter() + new Vector2(
            rng.RandfRange(-DirectionalCenterJitterX, DirectionalCenterJitterX),
            rng.RandfRange(-DirectionalCenterJitterY, DirectionalCenterJitterY));
        center = ClampPointToRect(center, safeBounds);

        var halfLength = Mathf.Max(260.0f, targetLength * 0.5f);
        var teeOffset = rng.RandfRange(-DirectionalCrossAxisJitter, DirectionalCrossAxisJitter);
        var cupOffset = rng.RandfRange(-DirectionalCrossAxisJitter, DirectionalCrossAxisJitter);

        var teePosition = ClampPointToRect(center - heading * halfLength + headingNormal * teeOffset, safeBounds);
        var cupPosition = ClampPointToRect(center + heading * halfLength + headingNormal * cupOffset, safeBounds);

        if (teePosition.DistanceTo(cupPosition) < Mathf.Max(320.0f, MinHoleLength * 0.6f))
        {
            teePosition = ClampPointToRect(center - heading * (halfLength + 80.0f), safeBounds);
            cupPosition = ClampPointToRect(center + heading * (halfLength + 80.0f), safeBounds);
        }

        var fairwayWidth = Mathf.Lerp(MaxFairwayWidth, MinFairwayWidth, difficulty) + rng.RandfRange(-8.0f, 8.0f);
        fairwayWidth = Mathf.Clamp(fairwayWidth, MinFairwayWidth - 8.0f, MaxFairwayWidth + 8.0f);
        var controlPoints = 2 + Mathf.RoundToInt(Mathf.Lerp(1.0f, 3.0f, difficulty));
        var lateralJitter = Mathf.Lerp(60.0f, 170.0f, difficulty);

        var centerLine = _fairwayPathBuilder.BuildCenterLine(
            teePosition,
            cupPosition,
            controlPoints,
            lateralJitter,
            safeBounds,
            rng);

        var fairwayPolygon = _fairwayPathBuilder.BuildCorridorPolygon(
            centerLine,
            fairwayWidth * 1.15f,
            fairwayWidth * 0.72f);

        var greenRadius = Mathf.Lerp(46.0f, 34.0f, difficulty) + rng.RandfRange(-3.0f, 3.0f);
        var greenPolygon = BuildCirclePolygon(cupPosition, greenRadius, 20);
        var teePolygon = BuildCirclePolygon(teePosition, 28.0f, 12);
        var roughPolygon = BuildRectPolygon(GenerationBounds);

        var holeLength = teePosition.DistanceTo(cupPosition);
        var par = holeLength < 570.0f ? 3 : holeLength < 770.0f ? 4 : 5;
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
            Bounds = GenerationBounds,
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

    private HoleLayout BuildFallbackLayout(int seed, int holeNumber, int totalHoles)
    {
        if (!UseFallbackTemplate)
        {
            var fallbackRng = new RandomNumberGenerator
            {
                Seed = ComposeBaseSeed(seed, holeNumber)
            };

            return BuildCandidateLayout(seed, holeNumber, totalHoles, fallbackRng);
        }

        var rng = new RandomNumberGenerator { Seed = ComposeBaseSeed(seed, holeNumber) };
        var direction = BuildHoleDirection(holeNumber, rng);
        var normal = direction.Orthogonal().Normalized();
        var center = GenerationBounds.GetCenter();
        var tee = ClampPointToRect(center - direction * 360.0f - normal * 20.0f, GenerationBounds.Grow(-34.0f));
        var cup = ClampPointToRect(center + direction * 360.0f + normal * 22.0f, GenerationBounds.Grow(-34.0f));
        var fairwayCenterLine = new List<Vector2>
        {
            tee,
            tee.Lerp(cup, 0.35f) + normal * 56.0f,
            tee.Lerp(cup, 0.65f) - normal * 52.0f,
            cup
        };

        return new HoleLayout
        {
            HoleNumber = holeNumber,
            Seed = seed,
            Par = 4,
            Bounds = GenerationBounds,
            TeePosition = tee,
            CupPosition = cup,
            WindDirection = direction,
            WindStrength = MinWindStrength,
            FairwayPathPoints = fairwayCenterLine,
            RoughPolygon = BuildRectPolygon(GenerationBounds),
            FairwayPolygon = _fairwayPathBuilder.BuildCorridorPolygon(fairwayCenterLine, 155.0f, 115.0f),
            GreenPolygon = BuildCirclePolygon(cup, 42.0f, 20),
            TeePolygon = BuildCirclePolygon(tee, 28.0f, 12),
            SandPolygons = new List<Vector2[]>
            {
                BuildEllipsePolygon(new Vector2(cup.X - 95.0f, cup.Y + 46.0f), 34.0f, 24.0f, 0.25f, 12)
            },
            WaterPolygons = new List<Vector2[]>(),
            TreeAreaPolygons = new List<Vector2[]>
            {
                BuildEllipsePolygon(tee.Lerp(cup, 0.58f) + normal * 100.0f, 70.0f, 42.0f, 0.2f, 14)
            },
            TreeObstacles = new List<TreeObstacleLayout>
            {
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.57f) + normal * 92.0f, Radius = 18.0f },
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.61f) + normal * 86.0f, Radius = 16.0f },
                new TreeObstacleLayout { Position = tee.Lerp(cup, 0.65f) + normal * 102.0f, Radius = 17.0f }
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

    private static Vector2[] BuildCirclePolygon(Vector2 center, float radius, int segments)
    {
        var clampedSegments = Mathf.Max(8, segments);
        var polygon = new Vector2[clampedSegments];

        for (var i = 0; i < clampedSegments; i += 1)
        {
            var angle = Mathf.Tau * i / clampedSegments;
            polygon[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
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

    private Vector2 BuildHoleDirection(int holeNumber, RandomNumberGenerator rng)
    {
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
