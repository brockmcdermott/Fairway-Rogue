using System;
using Godot;

public partial class HoleController : Node2D
{
    [Export] public int HoleNumber { get; set; } = 1;
    [Export] public int Par { get; set; } = 4;
    [ExportGroup("Cup Capture")]
    [Export] public float CupCaptureMaxSpeed { get; set; } = 105.0f;
    [Export] public float CupLipOutSpeedScale { get; set; } = 0.76f;
    [Export] public float CupLipOutMinSpeed { get; set; } = 82.0f;
    [Export] public float CupLipOutAngleJitterDegrees { get; set; } = 17.0f;
    [Export] public float CupRejectOffsetDistance { get; set; } = 18.0f;
    [Export] public float CupRejectCooldownSeconds { get; set; } = 0.14f;
    [Export] public bool VerboseCupDebug { get; set; }

    public int LocalStrokeCount { get; private set; }
    public bool IsHoleComplete { get; private set; }
    public int ScoreRelativeToPar => LocalStrokeCount - Par;
    public HoleLayout? ActiveLayout { get; private set; }

    public Vector2 TeePosition => _teeMarker?.GlobalPosition ?? GlobalPosition;
    public Vector2 CupPosition => _cupArea?.GlobalPosition ?? GlobalPosition;

    public event Action<HoleResultData>? HoleCompleted;
    public event Action<float, float>? CupRejectedBySpeed;

    private Marker2D? _teeMarker;
    private Area2D? _cupArea;
    private BallController? _trackedBall;
    private Polygon2D? _roughPolygon;
    private float _cupRejectCooldownRemaining;

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

    public override void _PhysicsProcess(double delta)
    {
        if (_cupRejectCooldownRemaining <= 0.0f)
        {
            return;
        }

        _cupRejectCooldownRemaining = Mathf.Max(0.0f, _cupRejectCooldownRemaining - (float)delta);
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
        _cupRejectCooldownRemaining = 0.0f;
    }

    public void SetCupEnabled(bool enabled)
    {
        if (_cupArea == null)
        {
            return;
        }

        _cupArea.Monitoring = enabled;
        _cupArea.Monitorable = enabled;
        _cupArea.CollisionLayer = enabled ? 1u : 0u;
        _cupArea.CollisionMask = enabled ? 1u : 0u;
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

        var ballSpeed = _trackedBall.Velocity.Length();
        if (_trackedBall.IsAirborne || ballSpeed > CupCaptureMaxSpeed)
        {
            RejectCupEntry(ballSpeed);
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

    private void RejectCupEntry(float incomingSpeed)
    {
        if (_trackedBall == null || _cupRejectCooldownRemaining > 0.0f)
        {
            return;
        }

        _cupRejectCooldownRemaining = Mathf.Max(0.01f, CupRejectCooldownSeconds);

        var cupPosition = CupPosition;
        var incomingDirection = _trackedBall.Velocity.LengthSquared() > 0.001f
            ? _trackedBall.Velocity.Normalized()
            : (_trackedBall.GlobalPosition - cupPosition).Normalized();
        if (incomingDirection == Vector2.Zero)
        {
            incomingDirection = Vector2.Right;
        }

        var jitter = Mathf.DegToRad(GetDeterministicJitter(incomingSpeed));
        var outDirection = incomingDirection.Rotated(jitter).Normalized();
        var outSpeed = Mathf.Max(CupLipOutMinSpeed, incomingSpeed * CupLipOutSpeedScale);
        var pushDistance = Mathf.Max(CupRejectOffsetDistance, _trackedBall.Radius + 6.0f);

        _trackedBall.GlobalPosition = cupPosition + outDirection * pushDistance;
        _trackedBall.OverrideMotion(outDirection * outSpeed);

        CupRejectedBySpeed?.Invoke(incomingSpeed, CupCaptureMaxSpeed);
        if (VerboseCupDebug)
        {
            GD.Print($"[HoleController] Cup lip-out: incoming {incomingSpeed:0.0}, threshold {CupCaptureMaxSpeed:0.0}");
        }
    }

    private float GetDeterministicJitter(float incomingSpeed)
    {
        var sign = ((LocalStrokeCount + HoleNumber) & 1) == 0 ? 1.0f : -1.0f;
        var seeded = Mathf.Abs(Mathf.Sin((incomingSpeed + HoleNumber * 7.31f + LocalStrokeCount * 4.19f) * 0.061f));
        var jitter = Mathf.Lerp(4.0f, Mathf.Max(4.0f, CupLipOutAngleJitterDegrees), seeded);
        return jitter * sign;
    }
}
