using Godot;

public partial class MainMenuController : Control
{
    private Button? _newRunButton;
    private Button? _continueButton;
    private Button? _settingsButton;
    private Button? _highScoresButton;
    private Button? _quitButton;
    private AcceptDialog? _highScoresDialog;
    private OptionButton? _highScoreLengthSelect;
    private Control? _newRunSelectionOverlay;
    private Button? _threeHoleButton;
    private Button? _nineHoleButton;
    private Button? _eighteenHoleButton;
    private Button? _cancelNewRunButton;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private SaveManager? SaveManagerSingleton => AutoloadLocator.Get<SaveManager>(this, nameof(SaveManager));
    private SceneRouter? SceneRouterSingleton => AutoloadLocator.Get<SceneRouter>(this, nameof(SceneRouter));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        PixelUiStyler.ApplyMenuStyle(this);

        _newRunButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/NewRunButton");
        _continueButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ContinueButton");
        _settingsButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SettingsButton");
        _highScoresButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HighScoresButton");
        _quitButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/QuitButton");
        _highScoresDialog = GetNodeOrNull<AcceptDialog>("HighScoresDialog");
        _highScoreLengthSelect = GetNodeOrNull<OptionButton>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HighScoreFilterRow/HighScoreLengthSelect");
        _newRunSelectionOverlay = GetNodeOrNull<Control>("NewRunSelectionOverlay");
        _threeHoleButton = GetNodeOrNull<Button>("NewRunSelectionOverlay/CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ThreeHoleButton");
        _nineHoleButton = GetNodeOrNull<Button>("NewRunSelectionOverlay/CenterContainer/PanelContainer/MarginContainer/VBoxContainer/NineHoleButton");
        _eighteenHoleButton = GetNodeOrNull<Button>("NewRunSelectionOverlay/CenterContainer/PanelContainer/MarginContainer/VBoxContainer/EighteenHoleButton");
        _cancelNewRunButton = GetNodeOrNull<Button>("NewRunSelectionOverlay/CenterContainer/PanelContainer/MarginContainer/VBoxContainer/CancelButton");

        if (_newRunSelectionOverlay != null)
        {
            PixelUiStyler.ApplyOverlayStyle(_newRunSelectionOverlay);
            _newRunSelectionOverlay.Visible = false;
        }

        ConfigureRunLengthSelector();
        if (_highScoreLengthSelect != null)
        {
            _highScoreLengthSelect.ItemSelected += _ => RefreshHighScoreButtonLabel();
        }

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

        if (_threeHoleButton != null)
        {
            _threeHoleButton.Pressed += () => OnRunLengthSelected(3);
        }

        if (_nineHoleButton != null)
        {
            _nineHoleButton.Pressed += () => OnRunLengthSelected(9);
        }

        if (_eighteenHoleButton != null)
        {
            _eighteenHoleButton.Pressed += () => OnRunLengthSelected(18);
        }

        if (_cancelNewRunButton != null)
        {
            _cancelNewRunButton.Pressed += HideRunLengthOverlay;
        }

        RefreshContinueButton();
        RefreshHighScoreButtonLabel();
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
        ShowRunLengthOverlay();
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

        var selectedRunLength = GetSelectedRunLength();
        var highScore = SaveManagerSingleton?.LoadHighScore(selectedRunLength) ?? 0;
        var bestRun = SaveManagerSingleton?.LoadBestRun(selectedRunLength);
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
            $"High Scores ({selectedRunLength} Holes)\n\n" +
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

    private void ConfigureRunLengthSelector()
    {
        if (_highScoreLengthSelect == null)
        {
            return;
        }

        _highScoreLengthSelect.Clear();
        for (var i = 0; i < RunManager.SupportedRunLengths.Length; i += 1)
        {
            var runLength = RunManager.SupportedRunLengths[i];
            _highScoreLengthSelect.AddItem($"{runLength} Holes", runLength);
            if (runLength == RunManager.DefaultTotalHoles)
            {
                _highScoreLengthSelect.Select(i);
            }
        }
    }

    private int GetSelectedRunLength()
    {
        if (_highScoreLengthSelect == null || _highScoreLengthSelect.GetSelectedId() <= 0)
        {
            return RunManager.DefaultTotalHoles;
        }

        return _highScoreLengthSelect.GetSelectedId();
    }

    private void ShowRunLengthOverlay()
    {
        if (_newRunSelectionOverlay == null)
        {
            GameManagerSingleton?.StartNewRunWithHoleCount(RunManager.DefaultTotalHoles);
            return;
        }

        _newRunSelectionOverlay.Visible = true;
        _nineHoleButton?.GrabFocus();
    }

    private void HideRunLengthOverlay()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        if (_newRunSelectionOverlay != null)
        {
            _newRunSelectionOverlay.Visible = false;
        }
    }

    private void OnRunLengthSelected(int holeCount)
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        if (_newRunSelectionOverlay != null)
        {
            _newRunSelectionOverlay.Visible = false;
        }

        GameManagerSingleton?.StartNewRunWithHoleCount(holeCount);
    }

    private void RefreshHighScoreButtonLabel()
    {
        if (_highScoresButton == null)
        {
            return;
        }

        var runLength = GetSelectedRunLength();
        _highScoresButton.Text = $"High Scores ({runLength}h)";
    }
}
