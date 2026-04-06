using System.Collections.Generic;
using Godot;

public class FairwayPathBuilder
{
    public List<Vector2> BuildCenterLine(
        Vector2 teePosition,
        Vector2 cupPosition,
        int controlPointCount,
        float maxLateralJitter,
        Rect2 allowedBounds,
        RandomNumberGenerator rng)
    {
        var points = new List<Vector2> { teePosition };

        var direction = (cupPosition - teePosition).Normalized();
        if (direction == Vector2.Zero)
        {
            direction = Vector2.Right;
        }

        var normal = direction.Orthogonal().Normalized();
        var safeBounds = allowedBounds.Grow(-10.0f);
        var primaryCurveSign = rng.Randf() < 0.5f ? -1.0f : 1.0f;
        var sCurveBlend = rng.RandfRange(0.25f, 0.75f);

        var clampedControlCount = Mathf.Clamp(controlPointCount, 1, 6);
        for (var i = 1; i <= clampedControlCount; i += 1)
        {
            var t = i / (float)(clampedControlCount + 1);
            var basePoint = teePosition.Lerp(cupPosition, t);
            var falloff = 0.4f + 0.6f * Mathf.Sin(Mathf.Pi * t);
            var along = rng.RandfRange(-20.0f, 20.0f);
            var curve = primaryCurveSign * maxLateralJitter * Mathf.Sin(Mathf.Pi * t) * sCurveBlend;
            var randomLateral = rng.RandfRange(-maxLateralJitter * 0.55f, maxLateralJitter * 0.55f);
            var alternatingBend = ((i & 1) == 0 ? -1.0f : 1.0f) * maxLateralJitter * 0.20f * (1.0f - sCurveBlend);
            var lateral = (curve + randomLateral + alternatingBend) * falloff;

            var candidate = basePoint + direction * along + normal * lateral;
            points.Add(ClampToRect(candidate, safeBounds));
        }

        points.Add(cupPosition);
        return points;
    }

    public Vector2[] BuildCorridorPolygon(List<Vector2> centerLine, float startWidth, float endWidth)
    {
        if (centerLine.Count < 2)
        {
            return System.Array.Empty<Vector2>();
        }

        var leftPoints = new List<Vector2>(centerLine.Count);
        var rightPoints = new List<Vector2>(centerLine.Count);

        for (var i = 0; i < centerLine.Count; i += 1)
        {
            var t = centerLine.Count == 1 ? 0.0f : i / (float)(centerLine.Count - 1);
            var width = Mathf.Lerp(startWidth, endWidth, t);
            var halfWidth = Mathf.Max(6.0f, width * 0.5f);

            var normal = ComputeSmoothedNormal(centerLine, i);
            leftPoints.Add(centerLine[i] + normal * halfWidth);
            rightPoints.Add(centerLine[i] - normal * halfWidth);
        }

        var polygon = new List<Vector2>(leftPoints.Count + rightPoints.Count);
        for (var i = 0; i < leftPoints.Count; i += 1)
        {
            polygon.Add(leftPoints[i]);
        }

        for (var i = rightPoints.Count - 1; i >= 0; i -= 1)
        {
            polygon.Add(rightPoints[i]);
        }

        return polygon.ToArray();
    }

    private static Vector2 ComputeSmoothedNormal(IReadOnlyList<Vector2> points, int index)
    {
        var previousIndex = Mathf.Max(0, index - 1);
        var nextIndex = Mathf.Min(points.Count - 1, index + 1);
        var tangent = (points[nextIndex] - points[previousIndex]).Normalized();
        if (tangent == Vector2.Zero)
        {
            tangent = Vector2.Right;
        }

        return tangent.Orthogonal().Normalized();
    }

    private static Vector2 ClampToRect(Vector2 point, Rect2 bounds)
    {
        return new Vector2(
            Mathf.Clamp(point.X, bounds.Position.X, bounds.End.X),
            Mathf.Clamp(point.Y, bounds.Position.Y, bounds.End.Y));
    }
}
