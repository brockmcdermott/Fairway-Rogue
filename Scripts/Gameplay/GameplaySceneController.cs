using Godot;

public partial class GameplaySceneController : Node2D
{
    private HUDController? _hud;
    private RecoveryDialogController? _recoveryDialog;
    private HoleController? _holeController;
    private BallController? _ballController;
    private Control? _holeCompletePanel;
    private Label? _holeCompleteLabel;

    private bool _shotFlowActive = true;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");

    public override void _Ready()
    {
        _hud = GetNodeOrNull<HUDController>("HUD");
        _recoveryDialog = GetNodeOrNull<RecoveryDialogController>("RecoveryDialog");
        _holeController = GetNodeOrNull<HoleController>("HoleRoot/StaticPracticeHole");
        _ballController = GetNodeOrNull<BallController>("Ball");
        _holeCompletePanel = GetNodeOrNull<Control>("HoleCompleteOverlay/PanelContainer");
        _holeCompleteLabel = GetNodeOrNull<Label>("HoleCompleteOverlay/PanelContainer/MarginContainer/VBoxContainer/HoleCompleteLabel");

        if (_hud != null)
        {
            _hud.MainMenuRequested += OnMainMenuRequested;
            _hud.RecoveryPromptRequested += OnRecoveryPromptRequested;
        }

        if (_recoveryDialog != null)
        {
            _recoveryDialog.DropChosen += OnDropChosen;
            _recoveryDialog.PlayFromLieChosen += OnPlayFromLieChosen;
        }

        if (_holeController != null)
        {
            _holeController.HoleCompleted += OnHoleCompleted;
        }

        if (_holeController != null && _ballController != null)
        {
            _holeController.BindBall(_ballController);
        }

        InitializeHud();

        if (_holeCompletePanel != null)
        {
            _holeCompletePanel.Visible = false;
        }

        if (_holeCompleteLabel != null)
        {
            _holeCompleteLabel.Visible = false;
        }

        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    public override void _Process(double delta)
    {
        if (_hud == null || _holeController == null || _ballController == null)
        {
            return;
        }

        if (_holeController.IsHoleComplete)
        {
            return;
        }

        _hud.SetDistanceToCup(_holeController.GetDistanceToCup(_ballController.GlobalPosition));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_shotFlowActive)
        {
            return;
        }

        if (@event.IsActionPressed("ui_accept") && _holeController != null)
        {
            _holeController.RegisterStroke();
            _hud?.SetStrokeCount(_holeController.LocalStrokeCount);
            GetViewport().SetInputAsHandled();
        }
    }

    private void InitializeHud()
    {
        if (_hud == null || _holeController == null)
        {
            return;
        }

        _hud.SetTopLevelData(
            _holeController.HoleNumber,
            _holeController.Par,
            _holeController.LocalStrokeCount,
            clubName: "Practice",
            windText: "Arrow keys move ball | Space counts stroke"
        );
        _hud.SetRecoveryPromptVisible(false);

        if (_ballController != null)
        {
            _hud.SetDistanceToCup(_holeController.GetDistanceToCup(_ballController.GlobalPosition));
        }
    }

    private void OnMainMenuRequested()
    {
        GameManagerSingleton?.GoToMainMenu();
    }

    private void OnRecoveryPromptRequested()
    {
        GameManagerSingleton?.ChangeState(GameState.RecoveryPrompt);
        _recoveryDialog?.ShowDialog();
    }

    private void OnDropChosen()
    {
        _holeController?.RegisterStroke();
        _hud?.SetStrokeCount(_holeController?.LocalStrokeCount ?? 0);
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    private void OnPlayFromLieChosen()
    {
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    private void OnHoleCompleted(int strokes)
    {
        _shotFlowActive = false;
        _ballController?.SetMovementEnabled(false);

        if (_holeCompleteLabel != null)
        {
            _holeCompleteLabel.Text =
                "Hole Complete (Placeholder)\n" +
                $"Strokes: {strokes}\n" +
                "Return to menu to restart this practice hole.";
            _holeCompleteLabel.Visible = true;
        }

        if (_holeCompletePanel != null)
        {
            _holeCompletePanel.Visible = true;
        }

        _hud?.SetStrokeCount(strokes);
        GameManagerSingleton?.ChangeState(GameState.HoleComplete);
    }
}
