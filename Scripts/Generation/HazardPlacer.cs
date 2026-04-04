using System.Collections.Generic;
using Godot;

public class HazardPlacer
{
    public void PopulateHazards(HoleLayout layout, float difficulty, float fairwayWidth, RandomNumberGenerator rng)
    {
        layout.SandPolygons.Clear();
        layout.WaterPolygons.Clear();
        layout.TreeAreaPolygons.Clear();
        layout.TreeObstacles.Clear();

        PlaceSand(layout, difficulty, fairwayWidth, rng);
        PlaceWater(layout, difficulty, fairwayWidth, rng);
        PlaceTrees(layout, difficulty, fairwayWidth, rng);
    }

    private void PlaceSand(HoleLayout layout, float difficulty, float fairwayWidth, RandomNumberGenerator rng)
    {
        var sandCount = 1 + Mathf.RoundToInt(difficulty * 2.0f);
        sandCount = Mathf.Clamp(sandCount, 1, 3);

        for (var i = 0; i < sandCount; i += 1)
        {
            var t = i == 0 ? rng.RandfRange(0.78f, 0.94f) : rng.RandfRange(0.35f, 0.85f);
            var center = BuildOffsetPointAlongPath(layout, t, fairwayWidth, rng, preferNearCenter: false);

            var radiusX = rng.RandfRange(26.0f, 44.0f);
            var radiusY = rng.RandfRange(20.0f, 34.0f);
            var rotation = rng.RandfRange(0.0f, Mathf.Tau);
            var polygon = BuildEllipsePolygon(center, radiusX, radiusY, rotation, 12);

            if (!IsHazardPolygonValid(layout, polygon))
            {
                continue;
            }

            layout.SandPolygons.Add(polygon);
        }
    }

    private void PlaceWater(HoleLayout layout, float difficulty, float fairwayWidth, RandomNumberGenerator rng)
    {
        var waterChance = 0.18f + difficulty * 0.42f;
        var waterCount = rng.Randf() < waterChance ? 1 : 0;
        if (difficulty > 0.72f && rng.Randf() < 0.35f)
        {
            waterCount += 1;
        }

        for (var i = 0; i < waterCount; i += 1)
        {
            var t = rng.RandfRange(0.35f, 0.72f);
            var center = BuildOffsetPointAlongPath(layout, t, fairwayWidth, rng, preferNearCenter: true);
            var radiusX = rng.RandfRange(48.0f, 82.0f);
            var radiusY = rng.RandfRange(24.0f, 42.0f);
            var pathDirection = GetDirectionAlongPath(layout.FairwayPathPoints, t);
            var rotation = pathDirection.Angle() + rng.RandfRange(-0.6f, 0.6f);
            var polygon = BuildEllipsePolygon(center, radiusX, radiusY, rotation, 14);

            if (!IsHazardPolygonValid(layout, polygon))
            {
                continue;
            }

            layout.WaterPolygons.Add(polygon);
        }
    }

    private void PlaceTrees(HoleLayout layout, float difficulty, float fairwayWidth, RandomNumberGenerator rng)
    {
        var clusterCount = 1 + Mathf.RoundToInt(difficulty * 3.0f);
        clusterCount = Mathf.Clamp(clusterCount, 1, 4);

        for (var clusterIndex = 0; clusterIndex < clusterCount; clusterIndex += 1)
        {
            var t = rng.RandfRange(0.20f, 0.90f);
            var center = BuildOffsetPointAlongPath(layout, t, fairwayWidth, rng, preferNearCenter: false, out var usedDirection);
            var radius = rng.RandfRange(54.0f, 86.0f);
            var rotation = usedDirection.Angle() + rng.RandfRange(-0.5f, 0.5f);
            var treeArea = BuildEllipsePolygon(center, radius * 1.2f, radius * 0.85f, rotation, 14);

            if (!IsHazardPolygonValid(layout, treeArea))
            {
                continue;
            }

            layout.TreeAreaPolygons.Add(treeArea);

            var treeCount = 3 + rng.RandiRange(0, 2) + (difficulty > 0.65f ? 1 : 0);
            for (var i = 0; i < treeCount; i += 1)
            {
                var angle = rng.RandfRange(0.0f, Mathf.Tau);
                var distance = rng.RandfRange(10.0f, radius * 0.58f);
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var treeRadius = rng.RandfRange(14.0f, 22.0f);

                if (!layout.Bounds.Grow(-18.0f).HasPoint(position))
                {
                    continue;
                }

                if (position.DistanceTo(layout.TeePosition) < 72.0f || position.DistanceTo(layout.CupPosition) < 64.0f)
                {
                    continue;
                }

                if (OverlapsExistingTree(layout.TreeObstacles, position, treeRadius))
                {
                    continue;
                }

                layout.TreeObstacles.Add(new TreeObstacleLayout
                {
                    Position = position,
                    Radius = treeRadius
                });
            }
        }
    }

    private static Vector2 BuildOffsetPointAlongPath(
        HoleLayout layout,
        float t,
        float fairwayWidth,
        RandomNumberGenerator rng,
        bool preferNearCenter)
    {
        return BuildOffsetPointAlongPath(layout, t, fairwayWidth, rng, preferNearCenter, out _);
    }

    private static Vector2 BuildOffsetPointAlongPath(
        HoleLayout layout,
        float t,
        float fairwayWidth,
        RandomNumberGenerator rng,
        bool preferNearCenter,
        out Vector2 usedDirection)
    {
        var pathPoint = GetPointAlongPath(layout.FairwayPathPoints, t);
        usedDirection = GetDirectionAlongPath(layout.FairwayPathPoints, t);
        var normal = usedDirection.Orthogonal().Normalized();
        var side = rng.Randf() < 0.5f ? -1.0f : 1.0f;

        float minOffset;
        float maxOffset;
        if (preferNearCenter)
        {
            minOffset = -fairwayWidth * 0.28f;
            maxOffset = fairwayWidth * 0.28f;
        }
        else
        {
            minOffset = fairwayWidth * 0.45f;
            maxOffset = fairwayWidth * 1.15f;
        }

        var lateralOffset = preferNearCenter
            ? rng.RandfRange(minOffset, maxOffset)
            : side * rng.RandfRange(minOffset, maxOffset);

        var candidate = pathPoint + normal * lateralOffset;
        var safeBounds = layout.Bounds.Grow(-18.0f);
        return new Vector2(
            Mathf.Clamp(candidate.X, safeBounds.Position.X, safeBounds.End.X),
            Mathf.Clamp(candidate.Y, safeBounds.Position.Y, safeBounds.End.Y));
    }

    private static Vector2 GetPointAlongPath(IReadOnlyList<Vector2> path, float t)
    {
        if (path.Count == 0)
        {
            return Vector2.Zero;
        }

        if (path.Count == 1)
        {
            return path[0];
        }

        var clampedT = Mathf.Clamp(t, 0.0f, 1.0f);
        var scaled = clampedT * (path.Count - 1);
        var index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, path.Count - 2);
        var localT = scaled - index;
        return path[index].Lerp(path[index + 1], localT);
    }

    private static Vector2 GetDirectionAlongPath(IReadOnlyList<Vector2> path, float t)
    {
        if (path.Count < 2)
        {
            return Vector2.Right;
        }

        var clampedT = Mathf.Clamp(t, 0.0f, 1.0f);
        var scaled = clampedT * (path.Count - 1);
        var index = Mathf.Clamp(Mathf.RoundToInt(scaled), 1, path.Count - 1);
        var previous = path[index - 1];
        var current = path[index];
        var direction = (current - previous).Normalized();
        return direction == Vector2.Zero ? Vector2.Right : direction;
    }

    private static bool IsHazardPolygonValid(HoleLayout layout, Vector2[] polygon)
    {
        if (polygon.Length < 3)
        {
            return false;
        }

        var safeBounds = layout.Bounds.Grow(-14.0f);
        for (var i = 0; i < polygon.Length; i += 1)
        {
            if (!safeBounds.HasPoint(polygon[i]))
            {
                return false;
            }
        }

        if (Geometry2D.IsPointInPolygon(layout.TeePosition, polygon) || Geometry2D.IsPointInPolygon(layout.CupPosition, polygon))
        {
            return false;
        }

        return true;
    }

    private static bool OverlapsExistingTree(IReadOnlyList<TreeObstacleLayout> existingTrees, Vector2 position, float radius)
    {
        for (var i = 0; i < existingTrees.Count; i += 1)
        {
            var tree = existingTrees[i];
            if (position.DistanceTo(tree.Position) < radius + tree.Radius + 8.0f)
            {
                return true;
            }
        }

        return false;
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
