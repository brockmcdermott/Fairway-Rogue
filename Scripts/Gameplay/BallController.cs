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
    [Export] public float BoundaryBounceDamping { get; set; } = 0.35f;

    [ExportGroup("Playable Bounds")]
    [Export] public Rect2 PlayableBounds { get; set; } = new Rect2(new Vector2(100, 100), new Vector2(1080, 520));

    public Vector2 Velocity { get; private set; } = Vector2.Zero;
    public bool IsMoving { get; private set; }
    public bool MovementEnabled { get; private set; } = true;

    public event Action? BallStartedMoving;
    public event Action? BallStopped;

    private float _activeFrictionMultiplier = 1.0f;
    private float _settleTimer;

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

        GlobalPosition += Velocity * dt;
        ApplyBoundsBounce();

        var speed = Velocity.Length();
        var deceleration = BaseFrictionPerSecond * _activeFrictionMultiplier * dt;
        var nextSpeed = Mathf.Max(speed - deceleration, 0.0f);

        if (nextSpeed <= 0.0f)
        {
            Velocity = Vector2.Zero;
        }
        else
        {
            Velocity = Velocity.Normalized() * nextSpeed;
        }

        if (Velocity.Length() <= StopSpeedThreshold)
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

    public void Launch(Vector2 direction, float launchSpeed, float frictionMultiplier)
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

        _activeFrictionMultiplier = Mathf.Max(0.1f, frictionMultiplier);
        Velocity = direction.Normalized() * clampedSpeed;
        IsMoving = true;
        _settleTimer = 0.0f;

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

    public void StopBall()
    {
        if (!IsMoving && Velocity == Vector2.Zero)
        {
            return;
        }

        Velocity = Vector2.Zero;
        IsMoving = false;
        _settleTimer = 0.0f;

        BallStopped?.Invoke();
    }

    public override void _Draw()
    {
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
}
