using Godot;

public partial class RecoveryDialogController : Control
{
    public delegate void DropChosenHandler();
    public event DropChosenHandler? DropChosen;

    public delegate void PlayFromLieChosenHandler();
    public event PlayFromLieChosenHandler? PlayFromLieChosen;

    private Button? _dropButton;
    private Button? _playFromLieButton;

    public override void _Ready()
    {
        _dropButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/DropButton");
        _playFromLieButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/PlayFromLieButton");

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

    public void ShowDialog()
    {
        Visible = true;
    }

    public void HideDialog()
    {
        Visible = false;
    }

    private void OnDropPressed()
    {
        HideDialog();
        DropChosen?.Invoke();
    }

    private void OnPlayFromLiePressed()
    {
        HideDialog();
        PlayFromLieChosen?.Invoke();
    }
}
