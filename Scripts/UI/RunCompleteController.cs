using Godot;

public partial class RunCompleteController : Control
{
    private Label? _summaryLabel;
    private Button? _newRunButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");
    private SaveManager? SaveManagerSingleton => GetNodeOrNull<SaveManager>("/root/SaveManager");

    public override void _Ready()
    {
        _summaryLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/SummaryLabel");
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
        _summaryLabel.Text =
            "Run Complete (Placeholder)\n" +
            $"Total Strokes: {run.TotalStrokes}\n" +
            $"Relative to Par: {relative}";

        var highScore = SaveManagerSingleton?.LoadHighScore() ?? int.MaxValue;
        if (relative < highScore)
        {
            SaveManagerSingleton?.SaveHighScore(relative);
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
