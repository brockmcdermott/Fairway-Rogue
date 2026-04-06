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
    private AudioManager? AudioManagerSingleton => GetNodeOrNull<AudioManager>("/root/AudioManager");

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
        AudioManagerSingleton?.PlayMusic("menu");
    }

    private void RefreshContinueButton()
    {
        if (_continueButton == null)
        {
            return;
        }

        var hasSave = SaveManagerSingleton?.HasRunSave() ?? false;
        if (hasSave && !(SaveManagerSingleton?.TryLoadRun(out _) ?? false))
        {
            SaveManagerSingleton?.DeleteRunSave();
            hasSave = false;
        }

        _continueButton.Visible = hasSave;
        _continueButton.Disabled = !hasSave;
    }

    private void OnNewRunPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.StartNewRun();
    }

    private void OnContinuePressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.ResumeRun();
        RefreshContinueButton();
    }

    private void OnSettingsPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        SceneRouterSingleton?.GoToSettings();
    }

    private void OnHighScoresPressed()
    {
        if (_highScoresDialog == null)
        {
            return;
        }

        var highScore = SaveManagerSingleton?.LoadHighScore() ?? 0;
        var bestRun = SaveManagerSingleton?.LoadBestRun();
        var bestRunText = "No completed run saved yet.";
        if (bestRun != null)
        {
            var bestRelativeText = HoleResultData.FormatRelativeScore(bestRun.ScoreRelativeToPar);
            bestRunText =
                $"Best Run: {bestRelativeText}\n" +
                $"Strokes/Par: {bestRun.TotalStrokes}/{bestRun.TotalPar}\n" +
                $"Holes: {bestRun.HolesCompleted}/{bestRun.TotalHoles}\n" +
                $"Currency Earned: {bestRun.CurrencyEarned}";
        }

        _highScoresDialog.DialogText =
            "High Scores\n\n" +
            $"Best Score (relative to par): {highScore}\n\n" +
            bestRunText;
        AudioManagerSingleton?.PlaySfx("ui_click");
        _highScoresDialog.PopupCentered();
    }

    private void OnQuitPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        if (GameManagerSingleton != null)
        {
            GameManagerSingleton.QuitGame();
            return;
        }

        GetTree().Quit();
    }
}
