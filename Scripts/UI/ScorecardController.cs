using Godot;

public partial class ScorecardController : Control
{
    private Label? _holeSummaryLabel;
    private Label? _totalsLabel;
    private Label? _historyLabel;
    private Button? _nextHoleButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");

    public override void _Ready()
    {
        _holeSummaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HoleSummaryLabel");
        _totalsLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/TotalsLabel");
        _historyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HistoryLabel");
        _nextHoleButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/NextHoleButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_nextHoleButton != null)
        {
            _nextHoleButton.Pressed += OnNextHolePressed;
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += OnMainMenuPressed;
        }

        GameManagerSingleton?.ChangeState(GameState.Scorecard);
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        var run = RunManagerSingleton;
        if (run == null)
        {
            if (_holeSummaryLabel != null)
            {
                _holeSummaryLabel.Text = "No run loaded.";
            }

            return;
        }

        var latest = run.GetLatestHoleResult();
        if (_nextHoleButton != null)
        {
            var canContinue = latest != null && !run.IsFinalHole();
            _nextHoleButton.Disabled = !canContinue;
        }

        if (_holeSummaryLabel != null)
        {
            if (latest == null)
            {
                _holeSummaryLabel.Text = "No hole result recorded yet.";
            }
            else
            {
                _holeSummaryLabel.Text =
                    $"Hole {latest.HoleNumber}\n" +
                    $"Par {latest.Par} | Strokes {latest.Strokes}\n" +
                    $"Result: {latest.RelativeScoreText} ({latest.Label})";
            }
        }

        if (_totalsLabel != null)
        {
            var runRelative = run.GetScoreRelativeToPar();
            _totalsLabel.Text =
                $"Totals: {run.TotalStrokes} strokes / {run.TotalPar} par\n" +
                $"Run Relative: {HoleResultData.FormatRelativeScore(runRelative)} ({HoleResultData.GetScoreLabel(runRelative)})\n" +
                $"Currency: {run.Currency} | Holes Complete: {run.HolesCompleted}/{run.TotalHoles}";
        }

        if (_historyLabel != null)
        {
            if (run.HoleResults.Count == 0)
            {
                _historyLabel.Text = "No hole history yet.";
            }
            else
            {
                var lines = "";
                for (var i = 0; i < run.HoleResults.Count; i += 1)
                {
                    var result = run.HoleResults[i];
                    lines += $"H{result.HoleNumber}: Par {result.Par}, Strokes {result.Strokes}, {result.RelativeScoreText} ({result.Label})\n";
                }

                _historyLabel.Text = lines.TrimEnd('\n');
            }
        }
    }

    private void OnMainMenuPressed()
    {
        GameManagerSingleton?.GoToMainMenu();
    }

    private void OnNextHolePressed()
    {
        GameManagerSingleton?.ContinueRunToNextHole();
    }
}
