using Godot;

public partial class BallController : Area2D
{
    [Export] public float DebugMoveSpeed { get; set; } = 240.0f;
    [Export] public float Radius { get; set; } = 10.0f;

    public bool MovementEnabled { get; private set; } = true;

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

    public override void _Process(double delta)
    {
        if (!MovementEnabled)
        {
            return;
        }

        var direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (direction == Vector2.Zero)
        {
            return;
        }

        GlobalPosition += direction.Normalized() * DebugMoveSpeed * (float)delta;
    }

    public void ResetAt(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
    }

    public void SetMovementEnabled(bool enabled)
    {
        MovementEnabled = enabled;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, new Color("f8f9fa"));
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 24, new Color("222222"), 2.0f);
    }
}
