using System;
using Godot;

public partial class BallController : Area2D
{
    [ExportGroup("Ball Tuning")]
    [Export] public float Radius { get; set; } = 10.0f;
    [Export] public float MaxLaunchSpeed { get; set; } = 720.0f;
    [Export] public float BaseFrictionPerSecond { get; set; } = 420.0f;
    [Export] public float StopSpeedThreshold { get; set; } = 8.0f;
    [Export] public float StopSettleTime { get; set; } = 0.08f;
    [Export] public float HardSnapStopSpeed { get; set; } = 0.35f;
    [Export] public float BoundaryBounceDamping { get; set; } = 0.35f;
    [Export] public bool EnableBoundsBounce { get; set; }
    [Export] public bool DrawDebugBall { get; set; }

    [ExportGroup("Playable Bounds")]
    [Export] public Rect2 PlayableBounds { get; set; } = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));

    [ExportGroup("Movement Safety")]
    [Export] public float MaxMovementSeconds { get; set; } = 10.0f;
    [Export] public float StallDetectSeconds { get; set; } = 0.65f;
    [Export] public float StallDistanceThreshold { get; set; } = 0.6f;
    [Export] public bool VerboseDebugLogging { get; set; }

    [ExportGroup("Pseudo-3D Carry")]
    [Export] public float AirborneMinSeconds { get; set; } = 0.05f;
    [Export] public float AirborneMaxSeconds { get; set; } = 2.20f;
    [Export] public float AirborneDragPerSecond { get; set; } = 28.0f;
    [Export] public float AirborneMaxVisualHeight { get; set; } = 58.0f;
    [Export] public float CarryMinimumBlend { get; set; } = 0.10f;
    [Export] public float CarryLoftExponent { get; set; } = 0.82f;
    [Export] public float CarryPowerExponent { get; set; } = 0.86f;
    [Export] public float ArcPowerHeightScale { get; set; } = 1.35f;
    [Export] public bool IgnoreTerrainWhileAirborne { get; set; } = true;

    public Vector2 Velocity { get; private set; } = Vector2.Zero;
    public bool IsMoving { get; private set; }
    public bool IsAirborne { get; private set; }
    public float VisualHeight { get; private set; }
    public bool MovementEnabled { get; private set; } = true;
    public float SpeedRatio => MaxLaunchSpeed <= 0.0f ? 0.0f : Mathf.Clamp(Velocity.Length() / MaxLaunchSpeed, 0.0f, 1.0f);
    public TerrainType CurrentTerrainType { get; private set; } = TerrainType.Tee;
    public TerrainProperties CurrentTerrainProperties { get; private set; } = TerrainDatabase.GetProperties(TerrainType.Tee);

    public event Action? BallStartedMoving;
    public event Action? BallStopped;
    public event Action<Vector2>? ShotLaunched;
    public event Action<TerrainType, Vector2>? HazardEntered;
    public event Action<TerrainType>? TerrainChanged;

    private float _activeFrictionMultiplier = 1.0f;
    private float _settleTimer;
    private float _movementElapsed;
    private float _stallElapsed;
    private Vector2 _lastMovementPosition = Vector2.Zero;
    private float _airborneDuration;
    private float _airborneElapsed;
    private float _shotLoftFactor;
    private float _launchSpeedRatio;

    public override void _Ready()
    {
        AddToGroup("golf_ball");

        Monitoring = true;
        Monitorable = true;

        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision?.Shape is CircleShape2D circle)
        {
            circle.Radius = Radius;
        }

        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!MovementEnabled || !IsMoving)
        {
            return;
        }

        var dt = (float)delta;

        if (!IsVectorFinite(Velocity))
        {
            if (VerboseDebugLogging)
            {
                GD.PushWarning("[BallController] Non-finite velocity detected. Forcing stop.");
            }

            StopBall();
            return;
        }

        GlobalPosition += Velocity * dt;
        if (EnableBoundsBounce)
        {
            ApplyBoundsBounce();
        }

        _movementElapsed += dt;
        if (_movementElapsed >= Mathf.Max(0.1f, MaxMovementSeconds))
        {
            if (VerboseDebugLogging)
            {
                GD.Print($"[BallController] Safety stop: exceeded max movement time ({_movementElapsed:0.00}s).");
            }

            StopBall();
            return;
        }

        var speed = Velocity.Length();
        var deceleration = IsAirborne
            ? AirborneDragPerSecond * dt
            : BaseFrictionPerSecond * _activeFrictionMultiplier * CurrentTerrainProperties.FrictionMultiplier * dt;
        var nextSpeed = Mathf.Max(speed - deceleration, 0.0f);

        if (nextSpeed <= Mathf.Max(0.0f, HardSnapStopSpeed))
        {
            Velocity = Vector2.Zero;
        }
        else
        {
            Velocity = Velocity.Normalized() * nextSpeed;
        }

        UpdateAirborneState(dt);

        var travelled = GlobalPosition.DistanceTo(_lastMovementPosition);
        _lastMovementPosition = GlobalPosition;
        if (!IsAirborne && travelled <= Mathf.Max(0.0f, StallDistanceThreshold))
        {
            _stallElapsed += dt;
            if (_stallElapsed >= Mathf.Max(0.1f, StallDetectSeconds))
            {
                if (VerboseDebugLogging)
                {
                    GD.Print($"[BallController] Safety stop: stall detected at speed {Velocity.Length():0.00}.");
                }

                StopBall();
                return;
            }
        }
        else
        {
            _stallElapsed = 0.0f;
        }

        if (Velocity.Length() <= StopSpeedThreshold && !IsAirborne)
        {
            _settleTimer += dt;
            if (_settleTimer >= StopSettleTime)
            {
                StopBall();
            }
        }
        else
        {
            _settleTimer = 0.0f;
        }
    }

    public void Launch(Vector2 direction, float launchSpeed, float frictionMultiplier, float loftFactor = 0.0f)
    {
        if (!MovementEnabled || direction == Vector2.Zero)
        {
            return;
        }

        var clampedSpeed = Mathf.Clamp(launchSpeed, 0.0f, MaxLaunchSpeed);
        if (clampedSpeed <= 0.0f)
        {
            return;
        }

        var shotStartPosition = GlobalPosition;

        _activeFrictionMultiplier = Mathf.Max(0.1f, frictionMultiplier);
        Velocity = direction.Normalized() * clampedSpeed;
        IsMoving = true;
        _shotLoftFactor = Mathf.Clamp(loftFactor, 0.0f, 1.0f);
        _launchSpeedRatio = MaxLaunchSpeed <= 0.0f ? 0.0f : Mathf.Clamp(clampedSpeed / MaxLaunchSpeed, 0.0f, 1.0f);
        _airborneDuration = EstimateCarrySeconds(_shotLoftFactor, _launchSpeedRatio);
        _airborneElapsed = 0.0f;
        IsAirborne = _airborneDuration > 0.01f;
        VisualHeight = 0.0f;
        _settleTimer = 0.0f;
        _movementElapsed = 0.0f;
        _stallElapsed = 0.0f;
        _lastMovementPosition = GlobalPosition;

        ShotLaunched?.Invoke(shotStartPosition);
        BallStartedMoving?.Invoke();
    }

    public void ResetAt(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
        StopBall();
    }

    public void SetMovementEnabled(bool enabled)
    {
        MovementEnabled = enabled;
        if (!enabled)
        {
            StopBall();
        }
    }

    public void SetPlayableBounds(Rect2 bounds)
    {
        PlayableBounds = bounds;
    }

    public void SetTerrain(TerrainType terrainType)
    {
        if (IgnoreTerrainWhileAirborne && IsAirborne)
        {
            return;
        }

        if (terrainType == CurrentTerrainType)
        {
            return;
        }

        CurrentTerrainType = terrainType;
        CurrentTerrainProperties = TerrainDatabase.GetProperties(terrainType);
        TerrainChanged?.Invoke(terrainType);

        if (terrainType == TerrainType.Water || terrainType == TerrainType.OutOfBounds)
        {
            HazardEntered?.Invoke(terrainType, GlobalPosition);
        }
    }

    public void StopBall()
    {
        if (!IsMoving && Velocity == Vector2.Zero)
        {
            return;
        }

        Velocity = Vector2.Zero;
        IsMoving = false;
        _settleTimer = 0.0f;
        _movementElapsed = 0.0f;
        _stallElapsed = 0.0f;
        _lastMovementPosition = GlobalPosition;
        _airborneDuration = 0.0f;
        _airborneElapsed = 0.0f;
        _shotLoftFactor = 0.0f;
        _launchSpeedRatio = 0.0f;
        IsAirborne = false;
        VisualHeight = 0.0f;

        BallStopped?.Invoke();
    }

    public void OverrideMotion(Vector2 velocity)
    {
        Velocity = velocity;
        IsMoving = velocity.Length() > HardSnapStopSpeed;
        IsAirborne = false;
        VisualHeight = 0.0f;
        _airborneDuration = 0.0f;
        _airborneElapsed = 0.0f;
        _shotLoftFactor = 0.0f;
        _launchSpeedRatio = MaxLaunchSpeed <= 0.0f ? 0.0f : Mathf.Clamp(velocity.Length() / MaxLaunchSpeed, 0.0f, 1.0f);
        _settleTimer = 0.0f;
        _movementElapsed = 0.0f;
        _stallElapsed = 0.0f;
        _lastMovementPosition = GlobalPosition;
    }

    public float EstimateCarrySeconds(float loftFactor, float normalizedPower)
    {
        var loft = Mathf.Pow(
            Mathf.Clamp(loftFactor, 0.0f, 1.0f),
            Mathf.Max(0.10f, CarryLoftExponent));
        var power = Mathf.Pow(
            Mathf.Clamp(normalizedPower, 0.0f, 1.0f),
            Mathf.Max(0.10f, CarryPowerExponent));
        var carryBlend = loft * power;
        carryBlend = Mathf.Clamp(carryBlend, 0.0f, 1.0f);
        if (carryBlend > 0.001f)
        {
            carryBlend = Mathf.Lerp(Mathf.Clamp(CarryMinimumBlend, 0.0f, 0.95f), 1.0f, carryBlend);
        }

        return Mathf.Lerp(0.0f, Mathf.Max(AirborneMinSeconds, AirborneMaxSeconds), carryBlend);
    }

    public override void _Draw()
    {
        if (!DrawDebugBall)
        {
            return;
        }

        DrawCircle(Vector2.Zero, Radius, new Color("f8f9fa"));
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 24, new Color("222222"), 2.0f);
    }

    private void ApplyBoundsBounce()
    {
        var min = PlayableBounds.Position + new Vector2(Radius, Radius);
        var max = PlayableBounds.End - new Vector2(Radius, Radius);

        var pos = GlobalPosition;

        if (pos.X < min.X)
        {
            pos.X = min.X;
            Velocity = new Vector2(Mathf.Abs(Velocity.X) * BoundaryBounceDamping, Velocity.Y);
        }
        else if (pos.X > max.X)
        {
            pos.X = max.X;
            Velocity = new Vector2(-Mathf.Abs(Velocity.X) * BoundaryBounceDamping, Velocity.Y);
        }

        if (pos.Y < min.Y)
        {
            pos.Y = min.Y;
            Velocity = new Vector2(Velocity.X, Mathf.Abs(Velocity.Y) * BoundaryBounceDamping);
        }
        else if (pos.Y > max.Y)
        {
            pos.Y = max.Y;
            Velocity = new Vector2(Velocity.X, -Mathf.Abs(Velocity.Y) * BoundaryBounceDamping);
        }

        GlobalPosition = pos;
    }

    private void UpdateAirborneState(float dt)
    {
        if (!IsAirborne)
        {
            VisualHeight = 0.0f;
            return;
        }

        _airborneElapsed += dt;
        var progress = _airborneDuration <= 0.001f
            ? 1.0f
            : Mathf.Clamp(_airborneElapsed / _airborneDuration, 0.0f, 1.0f);
        var arc = 4.0f * progress * (1.0f - progress);
        var launchScale = Mathf.Lerp(0.35f, ArcPowerHeightScale, Mathf.Pow(_launchSpeedRatio, 0.86f));
        VisualHeight = Mathf.Max(0.0f, AirborneMaxVisualHeight * _shotLoftFactor * launchScale * arc);

        if (progress < 1.0f)
        {
            return;
        }

        IsAirborne = false;
        VisualHeight = 0.0f;
        _airborneDuration = 0.0f;
        _airborneElapsed = 0.0f;
    }

    private static bool IsVectorFinite(Vector2 vector)
    {
        return !float.IsNaN(vector.X) &&
               !float.IsNaN(vector.Y) &&
               !float.IsInfinity(vector.X) &&
               !float.IsInfinity(vector.Y);
    }
}
