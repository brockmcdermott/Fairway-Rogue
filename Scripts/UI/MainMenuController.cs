using Godot;

public partial class MainMenuController : Control
{
    private Button? _newRunButton;
    private Button? _continueButton;
    private Button? _settingsButton;
    private Button? _highScoresButton;
    private Button? _quitButton;
    private AcceptDialog? _highScoresDialog;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private SaveManager? SaveManagerSingleton => GetNodeOrNull<SaveManager>("/root/SaveManager");
    private SceneRouter? SceneRouterSingleton => GetNodeOrNull<SceneRouter>("/root/SceneRouter");

    public override void _Ready()
    {
        _newRunButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/NewRunButton");
        _continueButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ContinueButton");
        _settingsButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SettingsButton");
        _highScoresButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HighScoresButton");
        _quitButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/QuitButton");
        _highScoresDialog = GetNodeOrNull<AcceptDialog>("HighScoresDialog");

        if (_newRunButton != null)
        {
            _newRunButton.Pressed += OnNewRunPressed;
        }

        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }

        if (_settingsButton != null)
        {
            _settingsButton.Pressed += OnSettingsPressed;
        }

        if (_highScoresButton != null)
        {
            _highScoresButton.Pressed += OnHighScoresPressed;
        }

        if (_quitButton != null)
        {
            _quitButton.Pressed += OnQuitPressed;
        }

        RefreshContinueButton();
        GameManagerSingleton?.ChangeState(GameState.MainMenu);
    }

    private void RefreshContinueButton()
    {
        if (_continueButton == null)
        {
            return;
        }

        var hasSave = SaveManagerSingleton?.HasRunSave() ?? false;
        _continueButton.Visible = hasSave;
        _continueButton.Disabled = !hasSave;
    }

    private void OnNewRunPressed()
    {
        GameManagerSingleton?.StartNewRun();
    }

    private void OnContinuePressed()
    {
        GameManagerSingleton?.ResumeRun();
    }

    private void OnSettingsPressed()
    {
        SceneRouterSingleton?.GoToSettings();
    }

    private void OnHighScoresPressed()
    {
        if (_highScoresDialog == null)
        {
            return;
        }

        var highScore = SaveManagerSingleton?.LoadHighScore() ?? 0;
        _highScoresDialog.DialogText = "High Scores are a placeholder for now.\n\n" +
                                 $"Best Score (relative to par): {highScore}";
        _highScoresDialog.PopupCentered();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
