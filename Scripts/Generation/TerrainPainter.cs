using System.Collections.Generic;
using Godot;

public partial class TerrainPainter : Node
{
    [ExportGroup("Terrain Colors")]
    [Export] public Color RoughColor { get; set; } = new Color(0.255f, 0.498f, 0.188f);
    [Export] public Color FairwayColor { get; set; } = new Color(0.400f, 0.694f, 0.286f);
    [Export] public Color TeeColor { get; set; } = new Color(0.498f, 0.749f, 0.325f);
    [Export] public Color GreenColor { get; set; } = new Color(0.576f, 0.824f, 0.400f);
    [Export] public Color SandColor { get; set; } = new Color(0.835f, 0.745f, 0.475f);
    [Export] public Color WaterColor { get; set; } = new Color(0.188f, 0.525f, 0.780f);
    [Export] public Color TreesColor { get; set; } = new Color(0.243f, 0.400f, 0.196f);
    [Export] public Color TreeCanopyColor { get; set; } = new Color(0.157f, 0.329f, 0.141f);
    [Export] public Color OutOfBoundsLineColor { get; set; } = new Color(0.93f, 0.22f, 0.22f);

    public void PaintLayout(HoleController holeController, HoleLayout layout)
    {
        var visualsRoot = holeController.GetNodeOrNull<Node2D>("Visuals");
        if (visualsRoot == null)
        {
            visualsRoot = new Node2D
            {
                Name = "Visuals"
            };
            holeController.AddChild(visualsRoot);
        }

        ClearChildren(visualsRoot);

        AddTerrainRegion(visualsRoot, "Rough", layout.RoughPolygon, TerrainType.Rough, RoughColor, 20);
        AddTerrainRegion(visualsRoot, "Fairway", layout.FairwayPolygon, TerrainType.Fairway, FairwayColor, 30);
        AddTerrainRegion(visualsRoot, "TeeArea", layout.TeePolygon, TerrainType.Tee, TeeColor, 55);
        AddTerrainRegion(visualsRoot, "Green", layout.GreenPolygon, TerrainType.Green, GreenColor, 50);

        for (var i = 0; i < layout.SandPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"Sand{i + 1}", layout.SandPolygons[i], TerrainType.Sand, SandColor, 40);
        }

        for (var i = 0; i < layout.WaterPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"Water{i + 1}", layout.WaterPolygons[i], TerrainType.Water, WaterColor, 70);
        }

        for (var i = 0; i < layout.TreeAreaPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"TreesCluster{i + 1}", layout.TreeAreaPolygons[i], TerrainType.Trees, TreesColor, 65);
        }

        AddOutOfBoundsLine(visualsRoot, layout.Bounds);
        AddTreeVisuals(visualsRoot, layout.TreeObstacles);
        AddTreeObstacles(visualsRoot, layout.TreeObstacles);
    }

    private void AddTerrainRegion(Node2D root, string name, Vector2[] polygon, TerrainType terrainType, Color color, int priority)
    {
        if (polygon.Length < 3)
        {
            return;
        }

        var region = new TerrainRegion
        {
            Name = name,
            Polygon = polygon,
            RegionTerrainType = terrainType,
            Priority = priority,
            Color = color
        };

        root.AddChild(region);
    }

    private void AddOutOfBoundsLine(Node2D root, Rect2 bounds)
    {
        var line = new Line2D
        {
            Name = "OutOfBoundsLine",
            DefaultColor = OutOfBoundsLineColor,
            Width = 4.0f,
            Closed = true
        };

        line.Points = new[]
        {
            bounds.Position,
            new Vector2(bounds.End.X, bounds.Position.Y),
            bounds.End,
            new Vector2(bounds.Position.X, bounds.End.Y)
        };

        root.AddChild(line);
    }

    private void AddTreeVisuals(Node2D root, IReadOnlyList<TreeObstacleLayout> treeObstacles)
    {
        var canopiesRoot = new Node2D
        {
            Name = "TreeCanopies"
        };
        root.AddChild(canopiesRoot);

        for (var i = 0; i < treeObstacles.Count; i += 1)
        {
            var obstacle = treeObstacles[i];
            var canopy = new Polygon2D
            {
                Name = $"Tree{i + 1}",
                Position = obstacle.Position,
                Color = TreeCanopyColor,
                Polygon = BuildCanopyPolygon(obstacle.Radius)
            };

            canopiesRoot.AddChild(canopy);
        }
    }

    private void AddTreeObstacles(Node2D root, IReadOnlyList<TreeObstacleLayout> treeObstacles)
    {
        var obstacleRoot = new Node2D
        {
            Name = "TreeObstacles"
        };
        root.AddChild(obstacleRoot);

        for (var i = 0; i < treeObstacles.Count; i += 1)
        {
            var obstacleData = treeObstacles[i];

            var obstacle = new StaticBody2D
            {
                Name = $"TreeObstacle{i + 1}",
                Position = obstacleData.Position,
                CollisionLayer = 1u << 1,
                CollisionMask = 0
            };
            obstacle.AddToGroup("tree_obstacle");

            var shape = new CollisionShape2D
            {
                Shape = new CircleShape2D
                {
                    Radius = obstacleData.Radius
                }
            };

            obstacle.AddChild(shape);
            obstacleRoot.AddChild(obstacle);
        }
    }

    private static Vector2[] BuildCanopyPolygon(float radius)
    {
        var canopyRadius = Mathf.Max(10.0f, radius + 4.0f);
        return new[]
        {
            new Vector2(0, -canopyRadius),
            new Vector2(canopyRadius * 0.66f, -canopyRadius * 0.66f),
            new Vector2(canopyRadius, 0),
            new Vector2(canopyRadius * 0.66f, canopyRadius * 0.66f),
            new Vector2(0, canopyRadius),
            new Vector2(-canopyRadius * 0.66f, canopyRadius * 0.66f),
            new Vector2(-canopyRadius, 0),
            new Vector2(-canopyRadius * 0.66f, -canopyRadius * 0.66f)
        };
    }

    private static void ClearChildren(Node node)
    {
        var children = node.GetChildren();
        for (var i = children.Count - 1; i >= 0; i -= 1)
        {
            if (children[i] is Node child)
            {
                // Avoid removing collision objects during physics callbacks.
                child.CallDeferred(Node.MethodName.QueueFree);
            }
        }
    }
}
