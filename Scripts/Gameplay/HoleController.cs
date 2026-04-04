using System;
using Godot;

public partial class HoleController : Node2D
{
    [Export] public int HoleNumber { get; set; } = 1;
    [Export] public int Par { get; set; } = 4;

    public int LocalStrokeCount { get; private set; }
    public bool IsHoleComplete { get; private set; }
    public int ScoreRelativeToPar => LocalStrokeCount - Par;
    public HoleLayout? ActiveLayout { get; private set; }

    public Vector2 TeePosition => _teeMarker?.GlobalPosition ?? GlobalPosition;
    public Vector2 CupPosition => _cupArea?.GlobalPosition ?? GlobalPosition;

    public event Action<HoleResultData>? HoleCompleted;

    private Marker2D? _teeMarker;
    private Area2D? _cupArea;
    private BallController? _trackedBall;
    private Polygon2D? _roughPolygon;

    public override void _Ready()
    {
        _teeMarker = GetNodeOrNull<Marker2D>("BallSpawn");
        _cupArea = GetNodeOrNull<Area2D>("Cup/CupArea");
        _roughPolygon = GetNodeOrNull<Polygon2D>("Visuals/Rough");

        if (_cupArea != null)
        {
            _cupArea.AreaEntered += OnCupAreaEntered;
        }

        ResetHoleState();
    }

    public void BindBall(BallController ball)
    {
        _trackedBall = ball;
        _trackedBall.ResetAt(TeePosition);
    }

    public void ApplyGeneratedLayout(HoleLayout layout, TerrainPainter? terrainPainter)
    {
        ActiveLayout = layout;
        HoleNumber = Mathf.Max(1, layout.HoleNumber);
        Par = Mathf.Max(1, layout.Par);

        if (_teeMarker != null)
        {
            _teeMarker.Position = ToLocal(layout.TeePosition);
        }

        var cupRoot = _cupArea?.GetParentOrNull<Node2D>();
        if (cupRoot != null)
        {
            cupRoot.Position = ToLocal(layout.CupPosition);
        }

        terrainPainter?.PaintLayout(this, layout);
        _roughPolygon = GetNodeOrNull<Polygon2D>("Visuals/Rough");

        ResetHoleState();
        _trackedBall?.ResetAt(TeePosition);
    }

    public void RegisterStroke()
    {
        if (IsHoleComplete)
        {
            return;
        }

        LocalStrokeCount += 1;
    }

    public void AddPenaltyStroke(int penaltyStrokes = 1)
    {
        if (IsHoleComplete)
        {
            return;
        }

        LocalStrokeCount += Mathf.Max(0, penaltyStrokes);
    }

    public HoleResultData BuildHoleResult()
    {
        return new HoleResultData
        {
            HoleNumber = Mathf.Max(1, HoleNumber),
            Par = Mathf.Max(1, Par),
            Strokes = Mathf.Max(0, LocalStrokeCount)
        };
    }

    public float GetDistanceToCup(Vector2 fromPosition)
    {
        return DistanceCalculator.DistanceToCup(fromPosition, CupPosition);
    }

    public Rect2 GetCourseBounds()
    {
        if (ActiveLayout != null)
        {
            return ActiveLayout.Bounds;
        }

        if (_roughPolygon == null || _roughPolygon.Polygon.Length == 0)
        {
            return new Rect2(new Vector2(100, 100), new Vector2(1080, 520));
        }

        var points = _roughPolygon.Polygon;

        var min = points[0];
        var max = points[0];

        for (var i = 1; i < points.Length; i += 1)
        {
            var point = points[i];
            min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
            max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
        }

        var globalOffset = _roughPolygon.GlobalPosition;
        return new Rect2(min + globalOffset, max - min);
    }

    public void ResetHoleState()
    {
        LocalStrokeCount = 0;
        IsHoleComplete = false;
    }

    private void OnCupAreaEntered(Area2D area)
    {
        if (IsHoleComplete)
        {
            return;
        }

        if (_trackedBall == null)
        {
            return;
        }

        if (area != _trackedBall && !area.IsInGroup("golf_ball"))
        {
            return;
        }

        CompleteHole();
    }

    private void CompleteHole()
    {
        IsHoleComplete = true;
        var result = BuildHoleResult();
        HoleCompleted?.Invoke(result);
        GD.Print($"[HoleController] Hole complete. Hole {HoleNumber}, strokes: {LocalStrokeCount}");
    }
}
