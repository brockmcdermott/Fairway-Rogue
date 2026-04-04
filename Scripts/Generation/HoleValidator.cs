using System.Collections.Generic;
using Godot;

public class HoleValidator
{
    public int RouteSampleCount { get; set; } = 28;
    public float MinimumTeeCupDistance { get; set; } = 340.0f;
    public float MaxBlockedRouteRatio { get; set; } = 0.45f;
    public int MaxBlockedConsecutiveSamples { get; set; } = 5;

    public bool Validate(HoleLayout layout, out string reason)
    {
        reason = string.Empty;

        if (layout.FairwayPathPoints.Count < 2)
        {
            reason = "Fairway path has too few points.";
            return false;
        }

        if (layout.FairwayPolygon.Length < 6)
        {
            reason = "Fairway polygon is missing or too small.";
            return false;
        }

        if (!layout.Bounds.Grow(-14.0f).HasPoint(layout.TeePosition))
        {
            reason = "Tee is outside safe bounds.";
            return false;
        }

        if (!layout.Bounds.Grow(-14.0f).HasPoint(layout.CupPosition))
        {
            reason = "Cup is outside safe bounds.";
            return false;
        }

        if (layout.TeePosition.DistanceTo(layout.CupPosition) < MinimumTeeCupDistance)
        {
            reason = "Hole is too short for readability.";
            return false;
        }

        if (!IsSafeStartingPoint(layout, layout.TeePosition))
        {
            reason = "Tee landed in hazard/obstruction.";
            return false;
        }

        if (!IsSafeStartingPoint(layout, layout.CupPosition))
        {
            reason = "Cup landed in hazard/obstruction.";
            return false;
        }

        if (!HasPlayableRoute(layout, out reason))
        {
            return false;
        }

        return true;
    }

    private bool HasPlayableRoute(HoleLayout layout, out string reason)
    {
        reason = string.Empty;

        var samples = Mathf.Max(12, RouteSampleCount);
        var blocked = 0;
        var blockedConsecutive = 0;
        var fairwayCoverageSamples = 0;
        var middleSafeSamples = 0;

        for (var i = 0; i <= samples; i += 1)
        {
            var t = i / (float)samples;
            var sample = layout.TeePosition.Lerp(layout.CupPosition, t);

            if (!layout.Bounds.HasPoint(sample))
            {
                reason = "Route leaves playable bounds.";
                return false;
            }

            var inWater = IsPointInAnyPolygon(sample, layout.WaterPolygons);
            var inTrees = IsPointInAnyPolygon(sample, layout.TreeAreaPolygons);
            var obstructed = IsNearTreeObstacle(sample, layout.TreeObstacles, 8.0f);
            var blockedSample = inWater || inTrees || obstructed;

            if (blockedSample)
            {
                blocked += 1;
                blockedConsecutive += 1;
            }
            else
            {
                blockedConsecutive = 0;

                if (t is >= 0.30f and <= 0.70f)
                {
                    middleSafeSamples += 1;
                }
            }

            if (blockedConsecutive >= MaxBlockedConsecutiveSamples)
            {
                reason = "Route is blocked by consecutive hazards.";
                return false;
            }

            if (Geometry2D.IsPointInPolygon(sample, layout.FairwayPolygon) ||
                Geometry2D.IsPointInPolygon(sample, layout.GreenPolygon) ||
                Geometry2D.IsPointInPolygon(sample, layout.TeePolygon))
            {
                fairwayCoverageSamples += 1;
            }
        }

        if (blocked / (float)(samples + 1) > MaxBlockedRouteRatio)
        {
            reason = "Hazards block too much of the route.";
            return false;
        }

        if (fairwayCoverageSamples < Mathf.RoundToInt((samples + 1) * 0.40f))
        {
            reason = "Fairway corridor is not readable enough.";
            return false;
        }

        if (middleSafeSamples <= 0)
        {
            reason = "No safe route through middle section.";
            return false;
        }

        return true;
    }

    private static bool IsSafeStartingPoint(HoleLayout layout, Vector2 point)
    {
        if (IsPointInAnyPolygon(point, layout.WaterPolygons))
        {
            return false;
        }

        if (IsPointInAnyPolygon(point, layout.TreeAreaPolygons))
        {
            return false;
        }

        if (IsPointInAnyPolygon(point, layout.SandPolygons))
        {
            return false;
        }

        if (IsNearTreeObstacle(point, layout.TreeObstacles, 4.0f))
        {
            return false;
        }

        return true;
    }

    private static bool IsPointInAnyPolygon(Vector2 point, IReadOnlyList<Vector2[]> polygons)
    {
        for (var i = 0; i < polygons.Count; i += 1)
        {
            var polygon = polygons[i];
            if (polygon.Length >= 3 && Geometry2D.IsPointInPolygon(point, polygon))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearTreeObstacle(Vector2 point, IReadOnlyList<TreeObstacleLayout> obstacles, float padding)
    {
        for (var i = 0; i < obstacles.Count; i += 1)
        {
            var obstacle = obstacles[i];
            if (point.DistanceTo(obstacle.Position) <= obstacle.Radius + padding)
            {
                return true;
            }
        }

        return false;
    }
}
