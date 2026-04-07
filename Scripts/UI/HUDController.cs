using Godot;

public partial class HUDController : Control
{
    public delegate void MainMenuRequestedHandler();
    public event MainMenuRequestedHandler? MainMenuRequested;

    public delegate void RecoveryPromptRequestedHandler();
    public event RecoveryPromptRequestedHandler? RecoveryPromptRequested;

    public delegate void ClubSelectionRequestedHandler(ClubType clubType);
    public event ClubSelectionRequestedHandler? ClubSelectionRequested;

    private Label? _holeLabel;
    private Label? _parLabel;
    private Label? _strokeLabel;
    private Label? _clubLabel;
    private Label? _powerLabel;
    private Label? _shotProfileLabel;
    private Label? _lieLabel;
    private Label? _distanceLabel;
    private Label? _windLabel;
    private Label? _statusLabel;
    private Label? _debugLabel;
    private ProgressBar? _powerMeter;
    private Button? _returnToMenuButton;
    private Button? _recoveryTestButton;
    private Button? _driverButton;
    private Button? _ironButton;
    private Button? _wedgeButton;
    private Button? _putterButton;
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        PixelUiStyler.ApplyHudStyle(this);

        _holeLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/HoleLabel");
        _parLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/ParLabel");
        _strokeLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/StrokeLabel");
        _clubLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/ClubLabel");
        _powerLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/PowerLabel");
        _shotProfileLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/ShotProfileLabel");
        _lieLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/LieLabel");
        _distanceLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/DistanceLabel");
        _windLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/WindLabel");
        _statusLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/StatusLabel");
        _debugLabel = GetNodeOrNull<Label>("InfoPanel/MarginContainer/VBoxContainer/DebugLabel");
        _powerMeter = GetNodeOrNull<ProgressBar>("ClubSelectorRoot/PanelContainer/MarginContainer/VBoxContainer/PowerMeter");
        _returnToMenuButton = GetNodeOrNull<Button>("InfoPanel/MarginContainer/VBoxContainer/ButtonRow/ReturnToMenuButton");
        _recoveryTestButton = GetNodeOrNull<Button>("InfoPanel/MarginContainer/VBoxContainer/ButtonRow/RecoveryPromptButton");
        _driverButton = GetNodeOrNull<Button>("ClubSelectorRoot/PanelContainer/MarginContainer/VBoxContainer/ClubButtonRow/DriverButton");
        _ironButton = GetNodeOrNull<Button>("ClubSelectorRoot/PanelContainer/MarginContainer/VBoxContainer/ClubButtonRow/IronButton");
        _wedgeButton = GetNodeOrNull<Button>("ClubSelectorRoot/PanelContainer/MarginContainer/VBoxContainer/ClubButtonRow/WedgeButton");
        _putterButton = GetNodeOrNull<Button>("ClubSelectorRoot/PanelContainer/MarginContainer/VBoxContainer/ClubButtonRow/PutterButton");

        if (_powerMeter != null)
        {
            _powerMeter.MinValue = 0.0f;
            _powerMeter.MaxValue = 100.0f;
            _powerMeter.Value = 0.0f;
        }

        if (_returnToMenuButton != null)
        {
            _returnToMenuButton.Pressed += () =>
            {
                AudioManagerSingleton?.PlaySfx("ui_click");
                MainMenuRequested?.Invoke();
            };
        }

        if (_recoveryTestButton != null)
        {
            _recoveryTestButton.Pressed += () =>
            {
                AudioManagerSingleton?.PlaySfx("ui_click");
                RecoveryPromptRequested?.Invoke();
            };
        }

        BindClubButton(_driverButton, ClubType.Driver);
        BindClubButton(_ironButton, ClubType.Iron);
        BindClubButton(_wedgeButton, ClubType.Wedge);
        BindClubButton(_putterButton, ClubType.Putter);
    }

    public void SetTopLevelData(int holeNumber, int par, int strokes, string clubName, string windText)
    {
        if (_holeLabel != null)
        {
            _holeLabel.Text = $"Hole: {holeNumber}";
        }

        if (_parLabel != null)
        {
            _parLabel.Text = $"Par: {par}";
        }

        if (_strokeLabel != null)
        {
            _strokeLabel.Text = $"Strokes: {strokes}";
        }

        if (_clubLabel != null)
        {
            _clubLabel.Text = $"Club: {clubName}";
        }

        if (_windLabel != null)
        {
            _windLabel.Text = windText;
        }
    }

    public void SetStrokeCount(int strokes)
    {
        if (_strokeLabel != null)
        {
            _strokeLabel.Text = $"Strokes: {strokes}";
        }
    }

    public void SetDistanceToCup(float distance)
    {
        if (_distanceLabel != null)
        {
            _distanceLabel.Text = $"Distance to Cup: {distance:0.0}";
        }
    }

    public void SetClubName(string clubName)
    {
        if (_clubLabel != null)
        {
            _clubLabel.Text = $"Club: {clubName}";
        }
    }

    public void SetPower(float chargeRatio, float powerValue)
    {
        var percentage = Mathf.RoundToInt(chargeRatio * 100.0f);
        if (_powerLabel != null)
        {
            _powerLabel.Text = $"Power: {percentage}% ({powerValue:0})";
        }

        if (_powerMeter != null)
        {
            _powerMeter.Value = percentage;
        }
    }

    public void SetShotProfile(string profileText)
    {
        if (_shotProfileLabel != null)
        {
            _shotProfileLabel.Text = profileText;
        }
    }

    public void SetLieType(TerrainType terrainType)
    {
        if (_lieLabel != null)
        {
            _lieLabel.Text = $"Lie: {terrainType}";
        }
    }

    public void SetRecoveryPromptVisible(bool visible)
    {
        if (_recoveryTestButton != null)
        {
            _recoveryTestButton.Visible = visible;
            _recoveryTestButton.Disabled = !visible;
        }
    }

    public void SetStatusMessage(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Status: {message}";
        }
    }

    public void SetSelectedClub(ClubType clubType)
    {
        SetClubButtonState(_driverButton, clubType == ClubType.Driver);
        SetClubButtonState(_ironButton, clubType == ClubType.Iron);
        SetClubButtonState(_wedgeButton, clubType == ClubType.Wedge);
        SetClubButtonState(_putterButton, clubType == ClubType.Putter);
    }

    public void SetDebugInfo(string message, bool visible)
    {
        if (_debugLabel == null)
        {
            return;
        }

        _debugLabel.Visible = visible;
        if (visible)
        {
            _debugLabel.Text = message;
        }
    }

    private void BindClubButton(Button? button, ClubType clubType)
    {
        if (button == null)
        {
            return;
        }

        button.Pressed += () =>
        {
            AudioManagerSingleton?.PlaySfx("ui_click");
            ClubSelectionRequested?.Invoke(clubType);
        };
    }

    private static void SetClubButtonState(Button? button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        button.Disabled = selected;
    }
}
