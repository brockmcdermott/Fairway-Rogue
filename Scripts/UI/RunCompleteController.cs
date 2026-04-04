using Godot;

public partial class RunCompleteController : Control
{
    private Label? _summaryLabel;
    private Label? _historyLabel;
    private Button? _newRunButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");
    private SaveManager? SaveManagerSingleton => GetNodeOrNull<SaveManager>("/root/SaveManager");

    public override void _Ready()
    {
        _summaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SummaryLabel");
        _historyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HistoryLabel");
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

        var hasHighScore = SaveManagerSingleton?.HasHighScoreSave() ?? false;
        var bestScore = hasHighScore ? (SaveManagerSingleton?.LoadHighScore() ?? relative) : relative;
        var isNewBest = !hasHighScore || relative < bestScore;
        if (isNewBest)
        {
            bestScore = relative;
            SaveManagerSingleton?.SaveHighScore(bestScore);
        }

        _summaryLabel.Text =
            "Run Complete\n" +
            $"Holes: {run.HolesCompleted}/{run.TotalHoles}\n" +
            $"Total Strokes: {run.TotalStrokes}\n" +
            $"Total Par: {run.TotalPar}\n" +
            $"Final Score: {relativeText} ({label})\n" +
            $"Best Score: {HoleResultData.FormatRelativeScore(bestScore)}" +
            (isNewBest ? " (New Best)" : string.Empty);

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
        GameManagerSingleton?.StartNewRun();
    }

    private void OnMainMenuPressed()
    {
        GameManagerSingleton?.GoToMainMenu();
    }
}
