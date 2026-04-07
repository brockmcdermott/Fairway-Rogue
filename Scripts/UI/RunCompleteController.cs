using Godot;

public partial class RunCompleteController : Control
{
    private Label? _summaryLabel;
    private Label? _historyLabel;
    private Button? _newRunButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private RunManager? RunManagerSingleton => AutoloadLocator.Get<RunManager>(this, nameof(RunManager));
    private SaveManager? SaveManagerSingleton => AutoloadLocator.Get<SaveManager>(this, nameof(SaveManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        PixelUiStyler.ApplyMenuStyle(this);

        _summaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SummaryLabel");
        _historyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HistoryScroll/HistoryLabel");
        _newRunButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/NewRunButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_newRunButton != null)
        {
            _newRunButton.Pressed += OnNewRunPressed;
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += OnMainMenuPressed;
        }

        GameManagerSingleton?.ChangeState(GameState.RunComplete);
        AudioManagerSingleton?.PlayMusic("menu");
        SaveManagerSingleton?.DeleteRunSave();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        if (_summaryLabel == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        if (run == null)
        {
            _summaryLabel.Text = "Run complete.";
            return;
        }

        var relative = run.GetScoreRelativeToPar();
        var relativeText = HoleResultData.FormatRelativeScore(relative);
        var label = HoleResultData.GetScoreLabel(relative);
        var runLength = Mathf.Clamp(run.TotalHoles, 1, 36);

        var saveManager = SaveManagerSingleton;
        var hasHighScore = saveManager?.HasHighScoreSave(runLength) ?? false;
        var bestScore = hasHighScore ? (saveManager?.LoadHighScore(runLength) ?? relative) : relative;

        var previousBestRun = saveManager?.LoadBestRun(runLength);
        var currentBestRunData = new BestRunData
        {
            ScoreRelativeToPar = relative,
            TotalStrokes = run.TotalStrokes,
            TotalPar = run.TotalPar,
            HolesCompleted = run.HolesCompleted,
            TotalHoles = run.TotalHoles,
            CurrencyEarned = run.Currency,
            CompletedAtUnixSeconds = (long)Time.GetUnixTimeFromSystem()
        };

        var isNewBest = !hasHighScore || relative < bestScore;
        var isNewBestRun = previousBestRun == null || IsBetterRun(currentBestRunData, previousBestRun);
        if (isNewBest)
        {
            bestScore = relative;
            saveManager?.SaveHighScore(bestScore, runLength);
        }

        if (isNewBestRun)
        {
            saveManager?.SaveBestRun(currentBestRunData, runLength);
        }

        _summaryLabel.Text =
            "Run Complete\n" +
            $"Holes: {run.HolesCompleted}/{run.TotalHoles}\n" +
            $"Total Strokes: {run.TotalStrokes}\n" +
            $"Total Par: {run.TotalPar}\n" +
            $"Currency Earned: {run.Currency}\n" +
            $"Final Score: {relativeText} ({label})\n" +
            $"Best Score ({runLength}h): {HoleResultData.FormatRelativeScore(bestScore)}" +
            (isNewBest ? " (New Best)" : string.Empty) +
            (isNewBestRun ? "\nBest Run Record Updated" : string.Empty);

        if (_historyLabel != null)
        {
            if (run.HoleResults.Count == 0)
            {
                _historyLabel.Text = "No hole history available.";
            }
            else
            {
                var lines = "";
                for (var i = 0; i < run.HoleResults.Count; i += 1)
                {
                    var result = run.HoleResults[i];
                    lines += $"H{result.HoleNumber}: {result.Strokes} on Par {result.Par} ({result.RelativeScoreText}, {result.Label})\n";
                }

                _historyLabel.Text = lines.TrimEnd('\n');
            }
        }
    }

    private void OnNewRunPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.StartNewRun();
    }

    private void OnMainMenuPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.GoToMainMenu();
    }

    private static bool IsBetterRun(BestRunData current, BestRunData previous)
    {
        if (current.ScoreRelativeToPar != previous.ScoreRelativeToPar)
        {
            return current.ScoreRelativeToPar < previous.ScoreRelativeToPar;
        }

        if (current.TotalStrokes != previous.TotalStrokes)
        {
            return current.TotalStrokes < previous.TotalStrokes;
        }

        return current.CurrencyEarned > previous.CurrencyEarned;
    }
}
