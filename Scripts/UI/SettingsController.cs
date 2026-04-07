using Godot;

public partial class SettingsController : Control
{
    private Label? _statusLabel;
    private Button? _backButton;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private SaveManager? SaveManagerSingleton => AutoloadLocator.Get<SaveManager>(this, nameof(SaveManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        PixelUiStyler.ApplyMenuStyle(this);

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
