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

    [ExportGroup("Pixel Detail")]
    [Export] public int TerrainPatternSize { get; set; } = 32;
    [Export] public float TerrainOutlineWidth { get; set; } = 2.0f;
    [Export] public float BoundaryTickSpacing { get; set; } = 84.0f;
    [Export] public float SurroundingBackdropPadding { get; set; } = 760.0f;
    [Export] public float NeighborCourseAlpha { get; set; } = 0.58f;
    [Export] public bool DrawPerHoleBoundaryLines { get; set; } = false;
    [Export] public bool DrawPerHoleBoundaryTicks { get; set; } = false;

    private readonly Dictionary<TerrainType, Texture2D> _terrainTextures = new Dictionary<TerrainType, Texture2D>();
    private Texture2D? _treeCanopyTexture;
    private Texture2D? _worldBackdropTexture;
    private Texture2D? _neighborFairwayTexture;
    private Texture2D? _neighborWaterTexture;

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
        AddSurroundingWorld(visualsRoot, layout);

        AddTerrainRegion(visualsRoot, "Rough", layout.RoughPolygon, TerrainType.Rough, 10);
        AddTerrainRegion(visualsRoot, "Fairway", layout.FairwayPolygon, TerrainType.Fairway, 20);
        AddTerrainRegion(visualsRoot, "TeeArea", layout.TeePolygon, TerrainType.Tee, 50);
        AddTerrainRegion(visualsRoot, "Green", layout.GreenPolygon, TerrainType.Green, 45);

        for (var i = 0; i < layout.SandPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"Sand{i + 1}", layout.SandPolygons[i], TerrainType.Sand, 36);
        }

        for (var i = 0; i < layout.WaterPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"Water{i + 1}", layout.WaterPolygons[i], TerrainType.Water, 40);
        }

        for (var i = 0; i < layout.TreeAreaPolygons.Count; i += 1)
        {
            AddTerrainRegion(visualsRoot, $"TreesCluster{i + 1}", layout.TreeAreaPolygons[i], TerrainType.Trees, 42);
        }

        if (DrawPerHoleBoundaryLines)
        {
            AddOutOfBoundsLine(visualsRoot, layout.Bounds);
        }
        AddTreeVisuals(visualsRoot, layout.TreeObstacles);
        AddTreeObstacles(visualsRoot, layout.TreeObstacles);
    }

    private void AddSurroundingWorld(Node2D root, HoleLayout layout)
    {
        var padding = Mathf.Max(240.0f, SurroundingBackdropPadding);
        var expandedBounds = layout.Bounds.Grow(padding);
        var backdrop = new Polygon2D
        {
            Name = "SurroundingWorldBackdrop",
            Polygon = BuildRectPolygon(expandedBounds.GetCenter(), expandedBounds.Size),
            Color = Colors.White,
            Texture = GetWorldBackdropTexture(),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            Antialiased = false,
            ZIndex = 0
        };

        root.AddChild(backdrop);
        AddNeighborCourseHints(root, layout);
    }

    private void AddNeighborCourseHints(Node2D root, HoleLayout layout)
    {
        var center = layout.Bounds.GetCenter();
        var size = layout.Bounds.Size;
        var baseAngle = Mathf.DegToRad((layout.Seed % 360 + 360) % 360);
        var alpha = Mathf.Clamp(NeighborCourseAlpha, 0.25f, 0.95f);

        var offsets = new[]
        {
            new Vector2(-size.X * 0.92f, -size.Y * 0.70f),
            new Vector2(size.X * 0.96f, -size.Y * 0.68f),
            new Vector2(-size.X * 0.98f, size.Y * 0.72f),
            new Vector2(size.X * 0.95f, size.Y * 0.76f)
        };

        for (var i = 0; i < offsets.Length; i += 1)
        {
            var angle = baseAngle + i * 0.72f;
            var courseCenter = center + offsets[i].Rotated(baseAngle * 0.18f);

            var rough = new Polygon2D
            {
                Name = $"NeighborCourseRough{i + 1}",
                Polygon = BuildEllipsePolygon(
                    courseCenter,
                    size.X * 0.50f,
                    size.Y * 0.36f,
                    angle,
                    22),
                Color = new Color(TreesColor.R * 0.90f, TreesColor.G * 0.96f, TreesColor.B * 0.92f, 0.96f),
                Texture = GetWorldBackdropTexture(),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                Antialiased = false,
                ZIndex = 2
            };
            root.AddChild(rough);

            var fairway = new Polygon2D
            {
                Name = $"NeighborCourseFairway{i + 1}",
                Polygon = BuildEllipsePolygon(
                    courseCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 34.0f,
                    size.X * 0.34f,
                    size.Y * 0.20f,
                    angle + 0.12f,
                    18),
                Color = new Color(1.0f, 1.0f, 1.0f, alpha),
                Texture = GetNeighborFairwayTexture(),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                Antialiased = false,
                ZIndex = 3
            };
            root.AddChild(fairway);

            if ((i & 1) == 0)
            {
                var water = new Polygon2D
                {
                    Name = $"NeighborCourseWater{i + 1}",
                    Polygon = BuildEllipsePolygon(
                        courseCenter + new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * 90.0f,
                        size.X * 0.12f,
                        size.Y * 0.08f,
                        angle * 0.8f,
                        14),
                    Color = new Color(1.0f, 1.0f, 1.0f, alpha * 0.9f),
                    Texture = GetNeighborWaterTexture(),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                    Antialiased = false,
                    ZIndex = 3
                };
                root.AddChild(water);
            }
        }
    }

    private void AddTerrainRegion(Node2D root, string name, Vector2[] polygon, TerrainType terrainType, int priority)
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
            Color = Colors.White,
            Texture = GetTerrainTexture(terrainType),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            Antialiased = false,
            ZIndex = priority
        };

        root.AddChild(region);

        var outlineColor = GetTerrainOutlineColor(terrainType);
        if (outlineColor.A > 0.01f)
        {
            AddTerrainOutline(root, $"{name}Outline", polygon, outlineColor, TerrainOutlineWidth, priority + 1);
        }

        if (terrainType == TerrainType.Water)
        {
            AddWaterShimmer(root, $"{name}Foam", polygon, priority + 2);
        }
    }

    private static void AddTerrainOutline(Node2D root, string name, Vector2[] polygon, Color color, float width, int zIndex)
    {
        var outline = new Line2D
        {
            Name = name,
            DefaultColor = color,
            Width = width,
            Closed = true,
            Antialiased = false,
            JointMode = Line2D.LineJointMode.Sharp,
            BeginCapMode = Line2D.LineCapMode.None,
            EndCapMode = Line2D.LineCapMode.None,
            Points = polygon,
            ZIndex = zIndex
        };

        root.AddChild(outline);
    }

    private void AddWaterShimmer(Node2D root, string name, Vector2[] polygon, int zIndex)
    {
        var shimmer = new Polygon2D
        {
            Name = name,
            Polygon = polygon,
            Color = new Color(0.72f, 0.92f, 1.0f, 0.32f),
            Texture = BuildPatternTexture(
                TerrainPatternSize,
                new Color(1, 1, 1, 0.0f),
                new Color(1, 1, 1, 0.7f),
                (x, y, size) => ((y + x / 4) & 7) == 0),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            Antialiased = false,
            ZIndex = zIndex
        };

        root.AddChild(shimmer);
    }

    private void AddOutOfBoundsLine(Node2D root, Rect2 bounds)
    {
        var points = new[]
        {
            bounds.Position,
            new Vector2(bounds.End.X, bounds.Position.Y),
            bounds.End,
            new Vector2(bounds.Position.X, bounds.End.Y)
        };

        var shadow = new Line2D
        {
            Name = "OutOfBoundsShadow",
            DefaultColor = new Color(0.06f, 0.04f, 0.04f, 0.85f),
            Width = 7.0f,
            Closed = true,
            Antialiased = false,
            JointMode = Line2D.LineJointMode.Sharp,
            Points = points,
            ZIndex = 90
        };
        root.AddChild(shadow);

        var primary = new Line2D
        {
            Name = "OutOfBoundsLine",
            DefaultColor = OutOfBoundsLineColor,
            Width = 3.0f,
            Closed = true,
            Antialiased = false,
            JointMode = Line2D.LineJointMode.Sharp,
            Points = points,
            ZIndex = 91
        };
        root.AddChild(primary);

        var warning = new Line2D
        {
            Name = "OutOfBoundsWarning",
            DefaultColor = new Color(1.0f, 0.85f, 0.52f, 0.95f),
            Width = 1.0f,
            Closed = true,
            Antialiased = false,
            JointMode = Line2D.LineJointMode.Sharp,
            Points = points,
            ZIndex = 92
        };
        root.AddChild(warning);

        if (DrawPerHoleBoundaryTicks)
        {
            AddBoundaryTicks(root, bounds);
        }
    }

    private void AddBoundaryTicks(Node2D root, Rect2 bounds)
    {
        var spacing = Mathf.Max(32.0f, BoundaryTickSpacing);
        var tickSize = new Vector2(7.0f, 7.0f);
        var tickColor = new Color(1.0f, 0.74f, 0.22f, 0.88f);

        var perimeter = new List<Vector2>();
        for (var x = bounds.Position.X; x <= bounds.End.X; x += spacing)
        {
            perimeter.Add(new Vector2(x, bounds.Position.Y));
            perimeter.Add(new Vector2(x, bounds.End.Y));
        }

        for (var y = bounds.Position.Y + spacing; y < bounds.End.Y; y += spacing)
        {
            perimeter.Add(new Vector2(bounds.Position.X, y));
            perimeter.Add(new Vector2(bounds.End.X, y));
        }

        for (var i = 0; i < perimeter.Count; i += 1)
        {
            var tick = new Polygon2D
            {
                Name = $"OobTick{i + 1}",
                Polygon = BuildRectPolygon(perimeter[i], tickSize),
                Color = tickColor,
                ZIndex = 93
            };

            root.AddChild(tick);
        }
    }

    private void AddTreeVisuals(Node2D root, IReadOnlyList<TreeObstacleLayout> treeObstacles)
    {
        var canopiesRoot = new Node2D
        {
            Name = "TreeCanopies",
            ZIndex = 70
        };
        root.AddChild(canopiesRoot);

        for (var i = 0; i < treeObstacles.Count; i += 1)
        {
            var obstacle = treeObstacles[i];
            var treeRoot = new Node2D
            {
                Name = $"Tree{i + 1}",
                Position = obstacle.Position
            };
            canopiesRoot.AddChild(treeRoot);

            var trunk = new Polygon2D
            {
                Name = "Trunk",
                Polygon = BuildRectPolygon(new Vector2(0, obstacle.Radius * 0.70f), new Vector2(obstacle.Radius * 0.34f, obstacle.Radius * 0.76f)),
                Color = new Color(0.42f, 0.25f, 0.10f),
                ZIndex = 1
            };
            treeRoot.AddChild(trunk);

            var canopyShadow = new Polygon2D
            {
                Name = "CanopyShadow",
                Position = new Vector2(0, obstacle.Radius * 0.25f),
                Polygon = BuildCanopyPolygon(obstacle.Radius * 0.98f),
                Color = new Color(0.08f, 0.12f, 0.07f, 0.55f),
                ZIndex = 2
            };
            treeRoot.AddChild(canopyShadow);

            var canopy = new Polygon2D
            {
                Name = "Canopy",
                Polygon = BuildCanopyPolygon(obstacle.Radius),
                Color = TreeCanopyColor,
                Texture = GetTreeCanopyTexture(),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                Antialiased = false,
                ZIndex = 3
            };
            treeRoot.AddChild(canopy);

            var highlight = new Polygon2D
            {
                Name = "CanopyHighlight",
                Position = new Vector2(-obstacle.Radius * 0.20f, -obstacle.Radius * 0.24f),
                Polygon = BuildCanopyPolygon(obstacle.Radius * 0.64f),
                Color = new Color(0.60f, 0.82f, 0.40f, 0.65f),
                ZIndex = 4
            };
            treeRoot.AddChild(highlight);
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

    private Texture2D GetTerrainTexture(TerrainType terrainType)
    {
        if (_terrainTextures.TryGetValue(terrainType, out var cached))
        {
            return cached;
        }

        var texture = BuildTerrainTexture(terrainType);
        _terrainTextures[terrainType] = texture;
        return texture;
    }

    private Texture2D BuildTerrainTexture(TerrainType terrainType)
    {
        var size = Mathf.Max(8, TerrainPatternSize);
        return terrainType switch
        {
            TerrainType.Fairway => BuildPatternTexture(size, FairwayColor, Shift(FairwayColor, 0.07f), (x, y, _) => (x & 7) < 3),
            TerrainType.Tee => BuildPatternTexture(size, TeeColor, Shift(TeeColor, 0.08f), (x, y, _) => ((x + y) & 7) < 3),
            TerrainType.Green => BuildPatternTexture(size, GreenColor, Shift(GreenColor, 0.10f), (x, y, patternSize) =>
            {
                var center = patternSize * 0.5f;
                var dist = new Vector2(x - center, y - center).Length();
                return Mathf.FloorToInt(dist) % 6 == 0;
            }),
            TerrainType.Sand => BuildPatternTexture(size, SandColor, Shift(SandColor, -0.08f), (x, y, _) => ((x * 3 + y * 5) & 15) == 0),
            TerrainType.Water => BuildPatternTexture(size, WaterColor, Shift(WaterColor, 0.10f), (x, y, _) => ((y + (x / 4)) & 7) < 2),
            TerrainType.Trees => BuildPatternTexture(size, TreesColor, Shift(TreesColor, -0.06f), (x, y, _) => ((x * 7 + y * 3) & 11) < 3),
            _ => BuildPatternTexture(size, RoughColor, Shift(RoughColor, -0.06f), (x, y, _) => ((x + y * 3) & 9) < 2)
        };
    }

    private Texture2D GetTreeCanopyTexture()
    {
        if (_treeCanopyTexture != null)
        {
            return _treeCanopyTexture;
        }

        _treeCanopyTexture = BuildPatternTexture(
            24,
            TreeCanopyColor,
            Shift(TreeCanopyColor, 0.08f),
            (x, y, _) => ((x * 5 + y * 7) & 15) < 4);

        return _treeCanopyTexture;
    }

    private Texture2D GetWorldBackdropTexture()
    {
        if (_worldBackdropTexture != null)
        {
            return _worldBackdropTexture;
        }

        var baseColor = new Color(RoughColor.R * 0.72f, RoughColor.G * 0.76f, RoughColor.B * 0.70f);
        var accent = Shift(baseColor, 0.05f);
        _worldBackdropTexture = BuildPatternTexture(
            52,
            baseColor,
            accent,
            (x, y, _) => ((x * 5 + y * 7) & 15) < 3);
        return _worldBackdropTexture;
    }

    private Texture2D GetNeighborFairwayTexture()
    {
        if (_neighborFairwayTexture != null)
        {
            return _neighborFairwayTexture;
        }

        _neighborFairwayTexture = BuildPatternTexture(
            Mathf.Max(20, TerrainPatternSize),
            Shift(FairwayColor, -0.03f),
            Shift(FairwayColor, 0.07f),
            (x, y, _) => ((x + y) & 6) < 3);
        return _neighborFairwayTexture;
    }

    private Texture2D GetNeighborWaterTexture()
    {
        if (_neighborWaterTexture != null)
        {
            return _neighborWaterTexture;
        }

        _neighborWaterTexture = BuildPatternTexture(
            Mathf.Max(18, TerrainPatternSize - 6),
            Shift(WaterColor, -0.06f),
            Shift(WaterColor, 0.05f),
            (x, y, _) => ((y + (x / 3)) & 5) < 2);
        return _neighborWaterTexture;
    }

    private static Color GetTerrainOutlineColor(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Fairway => new Color(0.19f, 0.38f, 0.16f, 0.90f),
            TerrainType.Green => new Color(0.31f, 0.58f, 0.23f, 0.95f),
            TerrainType.Sand => new Color(0.58f, 0.48f, 0.24f, 0.95f),
            TerrainType.Water => new Color(0.10f, 0.30f, 0.53f, 0.95f),
            TerrainType.Trees => new Color(0.08f, 0.17f, 0.10f, 0.95f),
            TerrainType.Tee => new Color(0.23f, 0.47f, 0.19f, 0.95f),
            _ => new Color(0, 0, 0, 0)
        };
    }

    private static Texture2D BuildPatternTexture(int size, Color baseColor, Color accentColor, System.Func<int, int, int, bool> useAccentPredicate)
    {
        var clamped = Mathf.Max(8, size);
        var image = Image.CreateEmpty(clamped, clamped, false, Image.Format.Rgba8);

        for (var y = 0; y < clamped; y += 1)
        {
            for (var x = 0; x < clamped; x += 1)
            {
                image.SetPixel(x, y, useAccentPredicate(x, y, clamped) ? accentColor : baseColor);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static Color Shift(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp(color.R + amount, 0.0f, 1.0f),
            Mathf.Clamp(color.G + amount, 0.0f, 1.0f),
            Mathf.Clamp(color.B + amount, 0.0f, 1.0f),
            color.A);
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

    private static Vector2[] BuildRectPolygon(Vector2 center, Vector2 size)
    {
        var half = size * 0.5f;
        return new[]
        {
            new Vector2(center.X - half.X, center.Y - half.Y),
            new Vector2(center.X + half.X, center.Y - half.Y),
            new Vector2(center.X + half.X, center.Y + half.Y),
            new Vector2(center.X - half.X, center.Y + half.Y)
        };
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
