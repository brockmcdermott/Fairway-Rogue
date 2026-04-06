using System;
using System.Collections.Generic;
using Godot;

public partial class LieEvaluator : Node
{
    private const string TreeObstacleGroup = "tree_obstacle";

    [ExportGroup("Tree Obstruction")]
    [Export] public float TreeLineBlockPadding { get; set; } = 6.0f;
    [Export] public float MinimumLineCheckDistance { get; set; } = 20.0f;

    public TerrainType CurrentLie { get; private set; } = TerrainType.Tee;

    public event Action<TerrainType>? LieChanged;

    private readonly List<TerrainRegion> _regions = new List<TerrainRegion>();
    private readonly List<CollisionObject2D> _treeObstacles = new List<CollisionObject2D>();
    private Rect2 _playableBounds = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));

    public void Configure(Node terrainRoot, Rect2 playableBounds)
    {
        _regions.Clear();
        _treeObstacles.Clear();
        _playableBounds = playableBounds;

        RegisterTerrainNodesRecursive(terrainRoot);
        PurgeInvalidReferences();
        _regions.Sort((left, right) => right.Priority.CompareTo(left.Priority));
    }

    public TerrainType EvaluateLie(Vector2 ballGlobalPosition)
    {
        var lie = EvaluateTerrainAtPosition(ballGlobalPosition);

        if (lie != CurrentLie)
        {
            CurrentLie = lie;
            LieChanged?.Invoke(lie);
        }

        return CurrentLie;
    }

    public TerrainType EvaluateTerrainAtPosition(Vector2 globalPosition)
    {
        PurgeInvalidReferences();

        if (!_playableBounds.HasPoint(globalPosition))
        {
            return TerrainType.OutOfBounds;
        }

        var lie = TerrainType.Rough;
        for (var i = 0; i < _regions.Count;)
        {
            var region = _regions[i];
            if (!IsNodeUsable(region))
            {
                _regions.RemoveAt(i);
                continue;
            }

            try
            {
                if (region.ContainsGlobalPoint(globalPosition))
                {
                    lie = region.RegionTerrainType;
                    break;
                }
            }
            catch (ObjectDisposedException)
            {
                _regions.RemoveAt(i);
                continue;
            }

            i += 1;
        }

        return lie;
    }

    public bool IsObstructedTreeLie(Vector2 ballGlobalPosition, Vector2 targetGlobalPosition)
    {
        var lie = EvaluateTerrainAtPosition(ballGlobalPosition);
        if (lie == TerrainType.Trees)
        {
            return true;
        }

        if (ballGlobalPosition.DistanceTo(targetGlobalPosition) < MinimumLineCheckDistance)
        {
            return false;
        }

        return IsShotLineBlockedByTrees(ballGlobalPosition, targetGlobalPosition);
    }

    private void RegisterTerrainNodesRecursive(Node node)
    {
        if (!IsNodeUsable(node))
        {
            return;
        }

        if (node is TerrainRegion terrainRegion)
        {
            _regions.Add(terrainRegion);
        }

        if (node is CollisionObject2D collisionObject && collisionObject.IsInGroup(TreeObstacleGroup))
        {
            _treeObstacles.Add(collisionObject);
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                RegisterTerrainNodesRecursive(childNode);
            }
        }
    }

    private bool IsShotLineBlockedByTrees(Vector2 fromPosition, Vector2 toPosition)
    {
        for (var i = 0; i < _treeObstacles.Count; i += 1)
        {
            var obstacle = _treeObstacles[i];
            if (!IsNodeUsable(obstacle) || !obstacle.IsInsideTree())
            {
                continue;
            }

            foreach (var child in obstacle.GetChildren())
            {
                if (child is not CollisionShape2D collisionShape || collisionShape.Disabled || collisionShape.Shape is not CircleShape2D circleShape)
                {
                    continue;
                }

                var center = collisionShape.GlobalPosition;
                var maxScale = GetMaxScaleMagnitude(collisionShape.GlobalTransform);
                var radius = circleShape.Radius * maxScale + TreeLineBlockPadding;

                if (DistanceFromPointToSegment(center, fromPosition, toPosition) <= radius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void PurgeInvalidReferences()
    {
        for (var i = _regions.Count - 1; i >= 0; i -= 1)
        {
            if (!IsNodeUsable(_regions[i]))
            {
                _regions.RemoveAt(i);
            }
        }

        for (var i = _treeObstacles.Count - 1; i >= 0; i -= 1)
        {
            if (!IsNodeUsable(_treeObstacles[i]))
            {
                _treeObstacles.RemoveAt(i);
            }
        }
    }

    private static bool IsNodeUsable(Node? node)
    {
        return node != null &&
               GodotObject.IsInstanceValid(node) &&
               !node.IsQueuedForDeletion();
    }

    private static float GetMaxScaleMagnitude(Transform2D transform)
    {
        return Mathf.Max(transform.X.Length(), transform.Y.Length());
    }

    private static float DistanceFromPointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        var segment = segmentEnd - segmentStart;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= Mathf.Epsilon)
        {
            return point.DistanceTo(segmentStart);
        }

        var projection = (point - segmentStart).Dot(segment) / lengthSquared;
        var t = Mathf.Clamp(projection, 0.0f, 1.0f);
        var closest = segmentStart + segment * t;
        return point.DistanceTo(closest);
    }
}
