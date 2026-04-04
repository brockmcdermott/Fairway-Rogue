using Godot;

public partial class ScorecardController : Control
{
    private Label? _summaryLabel;
    private Button? _shopButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");
    private SceneRouter? SceneRouterSingleton => GetNodeOrNull<SceneRouter>("/root/SceneRouter");

    public override void _Ready()
    {
        _summaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SummaryLabel");
        _shopButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/ShopButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_shopButton != null)
        {
            _shopButton.Pressed += OnShopPressed;
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
        if (_summaryLabel == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        if (run == null)
        {
            _summaryLabel.Text = "No run loaded.";
            return;
        }

        _summaryLabel.Text =
            $"Holes Recorded: {run.HoleResults.Count}\n" +
            $"Total Strokes: {run.TotalStrokes}\n" +
            $"Relative to Par: {run.GetScoreRelativeToPar()}";
    }

    private void OnShopPressed()
    {
        GameManagerSingleton?.ChangeState(GameState.Shop);
        SceneRouterSingleton?.GoToShop();
    }

    private void OnMainMenuPressed()
    {
        GameManagerSingleton?.GoToMainMenu();
    }
}
