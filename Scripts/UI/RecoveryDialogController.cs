using Godot;

public partial class RecoveryDialogController : Control
{
    private const string DefaultBodyText = "Choose a recovery option. Taking a drop adds 1 penalty stroke.";

    public delegate void DropChosenHandler();
    public event DropChosenHandler? DropChosen;

    public delegate void PlayFromLieChosenHandler();
    public event PlayFromLieChosenHandler? PlayFromLieChosen;

    private Button? _dropButton;
    private Button? _playFromLieButton;
    private Label? _bodyLabel;
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        _dropButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/DropButton");
        _playFromLieButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/PlayFromLieButton");
        _bodyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/BodyLabel");

        if (_dropButton != null)
        {
            _dropButton.Pressed += OnDropPressed;
        }

        if (_playFromLieButton != null)
        {
            _playFromLieButton.Pressed += OnPlayFromLiePressed;
        }

        Visible = false;
    }

    public void ShowDialog(string? bodyText = null)
    {
        if (_bodyLabel != null)
        {
            _bodyLabel.Text = string.IsNullOrWhiteSpace(bodyText) ? DefaultBodyText : bodyText;
        }

        Visible = true;
        _dropButton?.GrabFocus();
    }

    public void HideDialog()
    {
        Visible = false;
    }

    private void OnDropPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        HideDialog();
        DropChosen?.Invoke();
    }

    private void OnPlayFromLiePressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        HideDialog();
        PlayFromLieChosen?.Invoke();
    }
}
