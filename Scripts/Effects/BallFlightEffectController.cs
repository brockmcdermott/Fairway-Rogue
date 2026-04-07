using Godot;

public partial class BallFlightEffectController : Node2D
{
    [ExportGroup("Flight Visual")]
    [Export] public float MaxVisualLift { get; set; } = 11.0f;
    [Export] public float LiftLerpSpeed { get; set; } = 10.0f;
    [Export] public float BobAmplitude { get; set; } = 1.2f;
    [Export] public float BobSpeed { get; set; } = 12.0f;
    [Export] public float VisualLiftScale { get; set; } = 0.92f;

    [ExportGroup("Shadow")]
    [Export] public Vector2 ShadowOffsetScale { get; set; } = new Vector2(0.18f, 0.24f);
    [Export] public Color ShadowColor { get; set; } = new Color(0.04f, 0.06f, 0.06f, 0.38f);
    [Export] public float ShadowArcWidth { get; set; } = 2.6f;

    [ExportGroup("Ball")]
    [Export] public Color BallColor { get; set; } = new Color(0.97f, 0.98f, 0.99f);
    [Export] public Color BallOutlineColor { get; set; } = new Color(0.13f, 0.14f, 0.17f);
    [Export] public Color BallHighlightColor { get; set; } = new Color(1.0f, 1.0f, 1.0f, 0.70f);

    private BallController? _ball;
    private float _visualLift;
    private float _bobTime;

    public override void _Ready()
    {
        _ball = GetParentOrNull<BallController>();
        ZIndex = 5;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_ball == null)
        {
            return;
        }

        var dt = (float)delta;
        var airborneLift = _ball.VisualHeight;
        var groundLift = _ball.IsMoving && !_ball.IsAirborne ? MaxVisualLift * _ball.SpeedRatio * 0.22f : 0.0f;
        var targetLift = Mathf.Max(airborneLift, groundLift);
        var blend = 1.0f - Mathf.Exp(-LiftLerpSpeed * dt);
        _visualLift = Mathf.Lerp(_visualLift, targetLift, blend);

        if (_ball.IsMoving && !_ball.IsAirborne)
        {
            _bobTime += dt * BobSpeed;
        }
        else
        {
            _bobTime = 0.0f;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_ball == null)
        {
            return;
        }

        var speedRatio = _ball.SpeedRatio;
        var bob = _ball.IsMoving && !_ball.IsAirborne ? Mathf.Sin(_bobTime) * BobAmplitude * speedRatio : 0.0f;
        var totalLift = Mathf.Max(0.0f, (_visualLift + bob) * Mathf.Max(0.05f, VisualLiftScale));
        var radius = Mathf.Max(1.0f, _ball.Radius);

        var shadowOffset = new Vector2(totalLift * ShadowOffsetScale.X, totalLift * ShadowOffsetScale.Y);
        var shadowRadius = radius * Mathf.Lerp(1.0f, 0.80f, speedRatio);
        var shadowAlpha = ShadowColor.A * Mathf.Lerp(1.0f, 0.55f, speedRatio);
        var shadow = new Color(ShadowColor.R, ShadowColor.G, ShadowColor.B, shadowAlpha);
        var arcLiftFade = Mathf.Lerp(1.0f, 0.58f, Mathf.Clamp(totalLift / Mathf.Max(1.0f, _ball.AirborneMaxVisualHeight), 0.0f, 1.0f));
        DrawArc(
            shadowOffset + new Vector2(0.0f, radius * 0.15f),
            shadowRadius,
            Mathf.DegToRad(20.0f),
            Mathf.DegToRad(160.0f),
            24,
            new Color(shadow.R, shadow.G, shadow.B, shadow.A * arcLiftFade),
            ShadowArcWidth,
            true);

        var ballPosition = new Vector2(0.0f, -totalLift);
        DrawCircle(ballPosition, radius, BallColor);
        DrawArc(ballPosition, radius, 0.0f, Mathf.Tau, 24, BallOutlineColor, 2.0f);

        var highlightPos = ballPosition + new Vector2(-radius * 0.32f, -radius * 0.34f);
        DrawCircle(highlightPos, radius * 0.24f, BallHighlightColor);
    }
}
