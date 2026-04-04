using System;
using Godot;

public partial class HoleController : Node2D
{
    [Export] public int HoleNumber { get; set; } = 1;
    [Export] public int Par { get; set; } = 4;

    public int LocalStrokeCount { get; private set; }
    public bool IsHoleComplete { get; private set; }

    public Vector2 TeePosition => _teeMarker?.GlobalPosition ?? GlobalPosition;
    public Vector2 CupPosition => _cupArea?.GlobalPosition ?? GlobalPosition;

    public event Action<int>? HoleCompleted;

    private Marker2D? _teeMarker;
    private Area2D? _cupArea;
    private BallController? _trackedBall;

    public override void _Ready()
    {
        _teeMarker = GetNodeOrNull<Marker2D>("BallSpawn");
        _cupArea = GetNodeOrNull<Area2D>("Cup/CupArea");

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

    public void RegisterStroke()
    {
        if (IsHoleComplete)
        {
            return;
        }

        LocalStrokeCount += 1;
    }

    public float GetDistanceToCup(Vector2 fromPosition)
    {
        return DistanceCalculator.DistanceToCup(fromPosition, CupPosition);
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
        HoleCompleted?.Invoke(LocalStrokeCount);
        GD.Print($"[HoleController] Hole complete. Hole {HoleNumber}, strokes: {LocalStrokeCount}");
    }
}
