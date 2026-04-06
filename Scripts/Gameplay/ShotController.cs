using Godot;

public partial class ShotController : Node2D
{
    [ExportGroup("Aim Tuning")]
    [Export] public float AimRotateSpeedDegrees { get; set; } = 145.0f;

    [ExportGroup("Power Tuning")]
    [Export] public float MaxChargeSeconds { get; set; } = 1.2f;
    [Export] public float MinChargeRatio { get; set; } = 0.12f;
    [Export] public float AimLineWidth { get; set; } = 3.0f;

    private BallController? _ball;
    private HoleController? _hole;
    private HUDController? _hud;
    private ClubController? _clubController;
    private LieEvaluator? _lieEvaluator;
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    private bool _inputEnabled = true;
    private bool _isCharging;
    private float _chargeTime;
    private float _aimAngleRadians;

    public void Configure(
        BallController ball,
        HoleController hole,
        HUDController hud,
        ClubController clubController,
        LieEvaluator lieEvaluator)
    {
        _ball = ball;
        _hole = hole;
        _hud = hud;
        _clubController = clubController;
        _lieEvaluator = lieEvaluator;

        _clubController.ClubChanged += OnClubChanged;

        _aimAngleRadians = (hole.CupPosition - ball.GlobalPosition).Angle();
        OnClubChanged(_clubController.CurrentClub);

        UpdatePowerHud();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!HasCoreReferences())
        {
            return;
        }

        var dt = (float)delta;

        if (CanTakeShot())
        {
            HandleAimInput(dt);
            HandleClubSwitchInput();
            HandleChargeAndReleaseInput(dt);
        }
        else if (_isCharging)
        {
            CancelCharge();
        }

        UpdatePowerHud();
        QueueRedraw();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled)
        {
            CancelCharge();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!HasCoreReferences() || !CanTakeShot())
        {
            return;
        }

        var club = _clubController!.CurrentClub;
        var shotPower = GetCurrentShotPower(club);

        var currentLie = _lieEvaluator != null ? _lieEvaluator.CurrentLie : TerrainType.Fairway;
        var terrainProperties = TerrainDatabase.GetProperties(currentLie);
        shotPower *= terrainProperties.PowerMultiplier;

        var origin = _ball!.GlobalPosition;
        var direction = GetAimDirection();
        var lineLength = shotPower * club.AimGuideScale;
        var tip = origin + direction * lineLength;

        var chargeRatio = GetChargeRatio();
        var lineColor = new Color(0.95f, 0.95f - 0.35f * chargeRatio, 0.25f + 0.35f * chargeRatio);

        DrawLine(origin, tip, lineColor, AimLineWidth, true);

        var normal = direction.Orthogonal();
        DrawLine(tip, tip - direction * 16.0f + normal * 8.0f, lineColor, AimLineWidth, true);
        DrawLine(tip, tip - direction * 16.0f - normal * 8.0f, lineColor, AimLineWidth, true);
    }

    private bool HasCoreReferences()
    {
        return _ball != null && _hole != null && _hud != null && _clubController != null;
    }

    private bool CanTakeShot()
    {
        return _inputEnabled && _ball != null && !_ball.IsMoving && _hole != null && !_hole.IsHoleComplete;
    }

    private void HandleAimInput(float delta)
    {
        if (_isCharging)
        {
            return;
        }

        var rotateInput = 0.0f;
        if (Input.IsActionPressed("ui_left"))
        {
            rotateInput -= 1.0f;
        }

        if (Input.IsActionPressed("ui_right"))
        {
            rotateInput += 1.0f;
        }

        if (Mathf.IsZeroApprox(rotateInput))
        {
            return;
        }

        var radiansPerSecond = Mathf.DegToRad(AimRotateSpeedDegrees);
        _aimAngleRadians += rotateInput * radiansPerSecond * delta;
    }

    private void HandleClubSwitchInput()
    {
        if (_isCharging)
        {
            return;
        }

        if (Input.IsActionJustPressed("ui_up"))
        {
            _clubController!.SelectPreviousClub();
        }
        else if (Input.IsActionJustPressed("ui_down"))
        {
            _clubController!.SelectNextClub();
        }
    }

    private void HandleChargeAndReleaseInput(float delta)
    {
        if (!_isCharging && Input.IsActionJustPressed("ui_accept"))
        {
            _isCharging = true;
            _chargeTime = 0.0f;
            return;
        }

        if (!_isCharging)
        {
            return;
        }

        if (Input.IsActionPressed("ui_accept"))
        {
            _chargeTime = Mathf.Min(_chargeTime + delta, MaxChargeSeconds);
        }

        if (Input.IsActionJustReleased("ui_accept"))
        {
            FireShot();
        }
    }

    private void FireShot()
    {
        if (!CanTakeShot())
        {
            CancelCharge();
            return;
        }

        var club = _clubController!.CurrentClub;
        var shotPower = GetCurrentShotPower(club);
        var lieForShot = _lieEvaluator != null ? _lieEvaluator.CurrentLie : TerrainType.Fairway;
        var terrainProperties = TerrainDatabase.GetProperties(lieForShot);
        shotPower *= terrainProperties.PowerMultiplier;

        if (shotPower <= 0.01f)
        {
            _isCharging = false;
            _hud?.SetStatusMessage("Cannot hit from an invalid lie. Recover to a playable position first.");
            return;
        }

        _isCharging = false;

        _hole!.RegisterStroke();
        _hud!.SetStrokeCount(_hole.LocalStrokeCount);

        _ball!.Launch(GetAimDirection(), shotPower, club.FrictionMultiplier);
        AudioManagerSingleton?.PlaySfx("shot");
    }

    private Vector2 GetAimDirection()
    {
        return Vector2.Right.Rotated(_aimAngleRadians).Normalized();
    }

    private float GetCurrentShotPower(ClubData club)
    {
        var chargeRatio = GetChargeRatio();
        return Mathf.Lerp(club.MinPower, club.MaxPower, chargeRatio);
    }

    private float GetChargeRatio()
    {
        var ratio = Mathf.Clamp(_chargeTime / MaxChargeSeconds, 0.0f, 1.0f);

        if (_isCharging)
        {
            return Mathf.Max(ratio, MinChargeRatio);
        }

        return MinChargeRatio;
    }

    private void UpdatePowerHud()
    {
        if (_hud == null || _clubController == null)
        {
            return;
        }

        var club = _clubController.CurrentClub;
        var ratio = GetChargeRatio();
        var power = GetCurrentShotPower(club);
        var lieForHud = _lieEvaluator != null ? _lieEvaluator.CurrentLie : TerrainType.Fairway;
        var terrainProperties = TerrainDatabase.GetProperties(lieForHud);
        power *= terrainProperties.PowerMultiplier;

        _hud.SetPower(ratio, power);
    }

    private void CancelCharge()
    {
        _isCharging = false;
        _chargeTime = 0.0f;
    }

    private void OnClubChanged(ClubData club)
    {
        _hud?.SetClubName(club.Name);
        UpdatePowerHud();
    }
}
