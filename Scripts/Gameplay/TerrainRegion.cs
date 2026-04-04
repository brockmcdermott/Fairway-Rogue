using Godot;

public partial class TerrainRegion : Polygon2D
{
    [Export] public TerrainType RegionTerrainType { get; set; } = TerrainType.Fairway;
    [Export] public int Priority { get; set; }

    public override void _Ready()
    {
        InferDefaultsFromName();
    }

    public bool ContainsGlobalPoint(Vector2 globalPoint)
    {
        if (Polygon.Length < 3)
        {
            return false;
        }

        var localPoint = ToLocal(globalPoint);
        return Geometry2D.IsPointInPolygon(localPoint, Polygon);
    }

    private void InferDefaultsFromName()
    {
        var lowered = Name.ToString().ToLowerInvariant();

        if (lowered.Contains("tee"))
        {
            RegionTerrainType = TerrainType.Tee;
        }
        else if (lowered.Contains("fairway"))
        {
            RegionTerrainType = TerrainType.Fairway;
        }
        else if (lowered.Contains("rough"))
        {
            RegionTerrainType = TerrainType.Rough;
        }
        else if (lowered.Contains("sand"))
        {
            RegionTerrainType = TerrainType.Sand;
        }
        else if (lowered.Contains("green"))
        {
            RegionTerrainType = TerrainType.Green;
        }
        else if (lowered.Contains("water"))
        {
            RegionTerrainType = TerrainType.Water;
        }
        else if (lowered.Contains("tree"))
        {
            RegionTerrainType = TerrainType.Trees;
        }
        else if (lowered.Contains("oob") || lowered.Contains("outofbounds"))
        {
            RegionTerrainType = TerrainType.OutOfBounds;
        }

        if (Priority != 0)
        {
            return;
        }

        switch (RegionTerrainType)
        {
            case TerrainType.Tee:
                Priority = 50;
                break;
            case TerrainType.Green:
                Priority = 45;
                break;
            case TerrainType.Sand:
                Priority = 40;
                break;
            case TerrainType.Fairway:
                Priority = 30;
                break;
            case TerrainType.Rough:
                Priority = 20;
                break;
            case TerrainType.Water:
                Priority = 60;
                break;
            case TerrainType.Trees:
                Priority = 55;
                break;
            case TerrainType.OutOfBounds:
                Priority = 65;
                break;
            default:
                Priority = 10;
                break;
        }
    }
}
