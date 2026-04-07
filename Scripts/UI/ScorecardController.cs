using Godot;

public partial class ScorecardController : Control
{
    private Label? _holeSummaryLabel;
    private Label? _totalsLabel;
    private Label? _historyLabel;
    private Button? _nextHoleButton;
    private Button? _shopButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private RunManager? RunManagerSingleton => AutoloadLocator.Get<RunManager>(this, nameof(RunManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        PixelUiStyler.ApplyMenuStyle(this);

        _holeSummaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HoleSummaryLabel");
        _totalsLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/TotalsLabel");
        _historyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/HistoryScroll/HistoryLabel");
        _nextHoleButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/NextHoleButton");
        _shopButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/ShopButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_nextHoleButton != null)
        {
            _nextHoleButton.Pressed += OnNextHolePressed;
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += OnMainMenuPressed;
        }

        if (_shopButton != null)
        {
            _shopButton.Pressed += OnShopPressed;
        }

        GameManagerSingleton?.ChangeState(GameState.Scorecard);
        AudioManagerSingleton?.PlayMusic("menu");
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

        if (_shopButton != null)
        {
            var canUseShop = latest != null && !run.IsFinalHole();
            _shopButton.Disabled = !canUseShop;
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
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.GoToMainMenu();
    }

    private void OnNextHolePressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.ContinueRunToNextHole();
    }

    private void OnShopPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.GoToShop();
    }
}
