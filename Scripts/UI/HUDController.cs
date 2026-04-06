using Godot;

public partial class HUDController : Control
{
    public delegate void MainMenuRequestedHandler();
    public event MainMenuRequestedHandler? MainMenuRequested;

    public delegate void RecoveryPromptRequestedHandler();
    public event RecoveryPromptRequestedHandler? RecoveryPromptRequested;

    private Label? _holeLabel;
    private Label? _parLabel;
    private Label? _strokeLabel;
    private Label? _clubLabel;
    private Label? _powerLabel;
    private Label? _lieLabel;
    private Label? _distanceLabel;
    private Label? _windLabel;
    private Label? _statusLabel;
    private Button? _returnToMenuButton;
    private Button? _recoveryTestButton;
    private AudioManager? AudioManagerSingleton => GetNodeOrNull<AudioManager>("/root/AudioManager");

    public override void _Ready()
    {
        _holeLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/HoleLabel");
        _parLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/ParLabel");
        _strokeLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StrokeLabel");
        _clubLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/ClubLabel");
        _powerLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/PowerLabel");
        _lieLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/LieLabel");
        _distanceLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/DistanceLabel");
        _windLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/WindLabel");
        _statusLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StatusLabel");
        _returnToMenuButton = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ButtonRow/ReturnToMenuButton");
        _recoveryTestButton = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ButtonRow/RecoveryPromptButton");

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
        if (_powerLabel != null)
        {
            var percentage = Mathf.RoundToInt(chargeRatio * 100.0f);
            _powerLabel.Text = $"Power: {percentage}% ({powerValue:0})";
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
}
