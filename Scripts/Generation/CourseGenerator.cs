using System.Collections.Generic;
using Godot;

public partial class CourseGenerator : Node
{
    [ExportGroup("Course Layout")]
    [Export] public Vector2 BaseHoleBoundsSize { get; set; } = new Vector2(1900.0f, 1120.0f);
    [Export] public Vector2 HoleSpacing { get; set; } = new Vector2(1820.0f, 1180.0f);
    [Export] public Vector2 CenterJitter { get; set; } = new Vector2(360.0f, 260.0f);
    [Export] public float WorldPadding { get; set; } = 520.0f;
    [Export] public float MinimumCenterDistanceScale { get; set; } = 0.62f;

    [ExportGroup("Routing")]
    [Export] public bool UseSerpentineRouting { get; set; } = true;
    [Export] public float RouteTurnJitterDegrees { get; set; } = 20.0f;

    public CourseLayout GenerateCourseLayout(int runSeed, int totalHoles, HoleGenerator holeGenerator)
    {
        var clampedHoles = Mathf.Clamp(totalHoles, 1, 36);
        var layout = new CourseLayout
        {
            Seed = runSeed,
            TotalHoles = clampedHoles,
            Holes = new List<HoleLayout>(clampedHoles)
        };

        var rng = new RandomNumberGenerator
        {
            Seed = ComposeCourseSeed(runSeed, clampedHoles)
        };

        var centers = BuildHoleCenters(clampedHoles, rng);
        var worldBounds = new Rect2(centers[0], Vector2.Zero);

        for (var i = 0; i < centers.Count; i += 1)
        {
            var holeNumber = i + 1;
            var difficulty = clampedHoles <= 1 ? 0.0f : i / (float)(clampedHoles - 1);
            var holeSizeScale = Mathf.Lerp(1.0f, 1.16f, difficulty);
            var holeSize = BaseHoleBoundsSize * holeSizeScale;
            var holeBounds = new Rect2(centers[i] - holeSize * 0.5f, holeSize);
            worldBounds = worldBounds.Merge(holeBounds);

            var preferredDirection = GetPreferredDirection(centers, i, rng);
            var holeLayout = holeGenerator.GenerateLayoutForBounds(runSeed, holeNumber, clampedHoles, holeBounds, preferredDirection);
            layout.Holes.Add(holeLayout);
        }

        layout.WorldBounds = worldBounds.Grow(Mathf.Max(120.0f, WorldPadding));
        return layout;
    }

    private List<Vector2> BuildHoleCenters(int totalHoles, RandomNumberGenerator rng)
    {
        var centers = new List<Vector2>(totalHoles);
        var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(totalHoles * 1.38f)), 2, 7);
        var rows = Mathf.CeilToInt(totalHoles / (float)columns);

        for (var row = 0; row < rows; row += 1)
        {
            var leftToRight = !UseSerpentineRouting || (row & 1) == 0;
            for (var colStep = 0; colStep < columns; colStep += 1)
            {
                if (centers.Count >= totalHoles)
                {
                    break;
                }

                var col = leftToRight ? colStep : columns - 1 - colStep;
                var basePos = new Vector2(col * HoleSpacing.X, row * HoleSpacing.Y);
                var jitter = new Vector2(
                    rng.RandfRange(-CenterJitter.X, CenterJitter.X),
                    rng.RandfRange(-CenterJitter.Y, CenterJitter.Y));
                var candidate = basePos + jitter;
                candidate = PushAwayFromNeighbors(candidate, centers, rng);
                centers.Add(candidate);
            }
        }

        var centroid = Vector2.Zero;
        for (var i = 0; i < centers.Count; i += 1)
        {
            centroid += centers[i];
        }

        centroid /= Mathf.Max(1, centers.Count);
        for (var i = 0; i < centers.Count; i += 1)
        {
            centers[i] -= centroid;
        }

        return centers;
    }

    private Vector2 PushAwayFromNeighbors(Vector2 candidate, IReadOnlyList<Vector2> existingCenters, RandomNumberGenerator rng)
    {
        var minDistance = Mathf.Min(HoleSpacing.X, HoleSpacing.Y) * Mathf.Clamp(MinimumCenterDistanceScale, 0.30f, 1.0f);
        if (existingCenters.Count == 0)
        {
            return candidate;
        }

        for (var pass = 0; pass < 6; pass += 1)
        {
            var wasAdjusted = false;
            for (var i = 0; i < existingCenters.Count; i += 1)
            {
                var existing = existingCenters[i];
                var delta = candidate - existing;
                var distance = delta.Length();
                if (distance >= minDistance)
                {
                    continue;
                }

                var pushDir = distance <= 0.001f
                    ? Vector2.Right.Rotated(rng.RandfRange(-Mathf.Pi, Mathf.Pi))
                    : delta / distance;
                candidate = existing + pushDir * minDistance;
                wasAdjusted = true;
            }

            if (!wasAdjusted)
            {
                break;
            }
        }

        return candidate;
    }

    private Vector2 GetPreferredDirection(IReadOnlyList<Vector2> centers, int index, RandomNumberGenerator rng)
    {
        Vector2 direction;
        if (index < centers.Count - 1)
        {
            direction = (centers[index + 1] - centers[index]).Normalized();
        }
        else if (index > 0)
        {
            direction = (centers[index] - centers[index - 1]).Normalized();
        }
        else
        {
            direction = Vector2.Right;
        }

        if (direction == Vector2.Zero)
        {
            direction = Vector2.Right;
        }

        var jitterRadians = Mathf.DegToRad(rng.RandfRange(-RouteTurnJitterDegrees, RouteTurnJitterDegrees));
        return direction.Rotated(jitterRadians).Normalized();
    }

    private static ulong ComposeCourseSeed(int runSeed, int holeCount)
    {
        unchecked
        {
            var mixed = ((ulong)(uint)runSeed * 11400714819323198485UL) ^ ((ulong)(uint)holeCount * 14029467366897019727UL);
            return mixed == 0UL ? 1UL : mixed;
        }
    }
}
