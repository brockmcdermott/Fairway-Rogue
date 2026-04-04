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
    private Label? _windLabel;
    private Button? _returnToMenuButton;
    private Button? _recoveryTestButton;

    public override void _Ready()
    {
        _holeLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/HoleLabel");
        _parLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/ParLabel");
        _strokeLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StrokeLabel");
        _clubLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/ClubLabel");
        _windLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/WindLabel");
        _returnToMenuButton = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ButtonRow/ReturnToMenuButton");
        _recoveryTestButton = GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ButtonRow/RecoveryPromptButton");

        if (_returnToMenuButton != null)
        {
            _returnToMenuButton.Pressed += () => MainMenuRequested?.Invoke();
        }

        if (_recoveryTestButton != null)
        {
            _recoveryTestButton.Pressed += () => RecoveryPromptRequested?.Invoke();
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
}
