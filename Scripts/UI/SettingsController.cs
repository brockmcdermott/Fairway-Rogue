using Godot;

public partial class SettingsController : Control
{
    private Label? _statusLabel;
    private Button? _backButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private SaveManager? SaveManagerSingleton => GetNodeOrNull<SaveManager>("/root/SaveManager");
    private AudioManager? AudioManagerSingleton => GetNodeOrNull<AudioManager>("/root/AudioManager");

    public override void _Ready()
    {
        _statusLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/StatusLabel");
        _backButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/BackButton");

        if (_backButton != null)
        {
            _backButton.Pressed += OnBackPressed;
        }

        var settings = SaveManagerSingleton?.LoadSettings() ?? new SettingsData();
        SaveManagerSingleton?.SaveSettings(settings);
        if (_statusLabel != null)
        {
            _statusLabel.Text =
                "Settings Placeholder\n" +
                $"Master Volume: {settings.MasterVolume:0.00}\n" +
                $"Fullscreen: {settings.Fullscreen}";
        }

        AudioManagerSingleton?.PlayMusic("menu");
    }

    private void OnBackPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.GoToMainMenu();
    }
}
