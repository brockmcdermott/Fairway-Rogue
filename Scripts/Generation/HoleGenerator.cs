using System.Collections.Generic;
using Godot;

public partial class HoleGenerator : Node
{
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

        var teePosition = new Vector2(
            safeBounds.Position.X,
            rng.RandfRange(safeBounds.Position.Y + 28.0f, safeBounds.End.Y - 28.0f));

        var targetLength = Mathf.Lerp(MinHoleLength, MaxHoleLength, difficulty) + rng.RandfRange(-LengthJitter, LengthJitter);
        var cupX = Mathf.Clamp(
            teePosition.X + targetLength,
            teePosition.X + 320.0f,
            safeBounds.End.X);
        var cupY = Mathf.Clamp(
            teePosition.Y + rng.RandfRange(-160.0f, 160.0f),
            safeBounds.Position.Y,
            safeBounds.End.Y);
        var cupPosition = new Vector2(cupX, cupY);

        var fairwayWidth = Mathf.Lerp(MaxFairwayWidth, MinFairwayWidth, difficulty) + rng.RandfRange(-8.0f, 8.0f);
        fairwayWidth = Mathf.Clamp(fairwayWidth, MinFairwayWidth - 8.0f, MaxFairwayWidth + 8.0f);
        var controlPoints = 2 + Mathf.RoundToInt(Mathf.Lerp(1.0f, 3.0f, difficulty));
        var lateralJitter = Mathf.Lerp(40.0f, 140.0f, difficulty);

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

        var centerY = GenerationBounds.Position.Y + GenerationBounds.Size.Y * 0.5f;
        var tee = new Vector2(GenerationBounds.Position.X + 100.0f, centerY);
        var cup = new Vector2(GenerationBounds.End.X - 120.0f, centerY);
        var fairwayCenterLine = new List<Vector2>
        {
            tee,
            new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.35f, centerY - 35.0f),
            new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.65f, centerY + 35.0f),
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
            WindDirection = Vector2.Right,
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
                BuildEllipsePolygon(new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.62f, centerY - 90.0f), 70.0f, 42.0f, 0.2f, 14)
            },
            TreeObstacles = new List<TreeObstacleLayout>
            {
                new TreeObstacleLayout { Position = new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.60f, centerY - 92.0f), Radius = 18.0f },
                new TreeObstacleLayout { Position = new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.64f, centerY - 85.0f), Radius = 16.0f },
                new TreeObstacleLayout { Position = new Vector2(GenerationBounds.Position.X + GenerationBounds.Size.X * 0.66f, centerY - 104.0f), Radius = 17.0f }
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
}
