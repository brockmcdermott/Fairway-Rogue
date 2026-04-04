using System;
using System.Collections.Generic;
using Godot;

[Serializable]
public class HoleLayout
{
    public int HoleNumber { get; set; } = 1;
    public int Seed { get; set; }
    public int Par { get; set; } = 4;

    public Rect2 Bounds { get; set; } = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));
    public Vector2 TeePosition { get; set; } = new Vector2(180, 350);
    public Vector2 CupPosition { get; set; } = new Vector2(1060, 350);

    public Vector2 WindDirection { get; set; } = Vector2.Right;
    public float WindStrength { get; set; } = 0.05f;

    public List<Vector2> FairwayPathPoints { get; set; } = new List<Vector2>();

    public Vector2[] RoughPolygon { get; set; } = System.Array.Empty<Vector2>();
    public Vector2[] FairwayPolygon { get; set; } = System.Array.Empty<Vector2>();
    public Vector2[] GreenPolygon { get; set; } = System.Array.Empty<Vector2>();
    public Vector2[] TeePolygon { get; set; } = System.Array.Empty<Vector2>();

    public List<Vector2[]> SandPolygons { get; set; } = new List<Vector2[]>();
    public List<Vector2[]> WaterPolygons { get; set; } = new List<Vector2[]>();
    public List<Vector2[]> TreeAreaPolygons { get; set; } = new List<Vector2[]>();
    public List<TreeObstacleLayout> TreeObstacles { get; set; } = new List<TreeObstacleLayout>();
}

[Serializable]
public class TreeObstacleLayout
{
    public Vector2 Position { get; set; }
    public float Radius { get; set; } = 18.0f;
}
