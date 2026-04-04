using System;
using System.Collections.Generic;
using Godot;

public partial class LieEvaluator : Node
{
    public TerrainType CurrentLie { get; private set; } = TerrainType.Tee;

    public event Action<TerrainType>? LieChanged;

    private readonly List<TerrainRegion> _regions = new List<TerrainRegion>();
    private Rect2 _playableBounds = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));

    public void Configure(Node terrainRoot, Rect2 playableBounds)
    {
        _regions.Clear();
        _playableBounds = playableBounds;

        RegisterRegionsRecursive(terrainRoot);
        _regions.Sort((left, right) => right.Priority.CompareTo(left.Priority));
    }

    public TerrainType EvaluateLie(Vector2 ballGlobalPosition)
    {
        TerrainType lie;

        if (!_playableBounds.HasPoint(ballGlobalPosition))
        {
            lie = TerrainType.OutOfBounds;
        }
        else
        {
            lie = TerrainType.Rough;
            for (var i = 0; i < _regions.Count; i += 1)
            {
                var region = _regions[i];
                if (region.ContainsGlobalPoint(ballGlobalPosition))
                {
                    lie = region.RegionTerrainType;
                    break;
                }
            }
        }

        if (lie != CurrentLie)
        {
            CurrentLie = lie;
            LieChanged?.Invoke(lie);
        }

        return CurrentLie;
    }

    private void RegisterRegionsRecursive(Node node)
    {
        if (node is TerrainRegion terrainRegion)
        {
            _regions.Add(terrainRegion);
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                RegisterRegionsRecursive(childNode);
            }
        }
    }
}
