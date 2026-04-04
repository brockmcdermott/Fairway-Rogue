using Godot;

public partial class GameplaySceneController : Node2D
{
    private HUDController? _hud;
    private HoleController? _holeController;
    private BallController? _ballController;
    private ClubController? _clubController;
    private ShotController? _shotController;
    private LieEvaluator? _lieEvaluator;
    private HazardResolver? _hazardResolver;
    private RecoveryDialogController? _recoveryDialog;
    private Control? _holeCompletePanel;
    private Label? _holeCompleteLabel;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");

    public override void _Ready()
    {
        _hud = GetNodeOrNull<HUDController>("HUD");
        _holeController = GetNodeOrNull<HoleController>("HoleRoot/StaticPracticeHole");
        _ballController = GetNodeOrNull<BallController>("Ball");
        _clubController = GetNodeOrNull<ClubController>("ClubController");
        _shotController = GetNodeOrNull<ShotController>("ShotController");
        _lieEvaluator = GetNodeOrNull<LieEvaluator>("LieEvaluator");
        _hazardResolver = GetNodeOrNull<HazardResolver>("HazardResolver");
        _recoveryDialog = GetNodeOrNull<RecoveryDialogController>("RecoveryDialog");
        _holeCompletePanel = GetNodeOrNull<Control>("HoleCompleteOverlay/PanelContainer");
        _holeCompleteLabel = GetNodeOrNull<Label>("HoleCompleteOverlay/PanelContainer/MarginContainer/VBoxContainer/HoleCompleteLabel");

        if (_hud != null)
        {
            _hud.MainMenuRequested += OnMainMenuRequested;
            _hud.SetRecoveryPromptVisible(false);
        }

        if (_holeController != null)
        {
            _holeController.HoleCompleted += OnHoleCompleted;
        }

        if (_recoveryDialog != null)
        {
            _recoveryDialog.DropChosen += OnTakeDropChosen;
            _recoveryDialog.PlayFromLieChosen += OnPlayFromLieChosen;
            _recoveryDialog.HideDialog();
        }

        if (_holeController != null && _ballController != null)
        {
            _holeController.BindBall(_ballController);
            _ballController.SetPlayableBounds(_holeController.GetCourseBounds());
        }

        if (_lieEvaluator != null && _holeController != null)
        {
            var terrainRoot = _holeController.GetNodeOrNull<Node>("Visuals");
            if (terrainRoot != null)
            {
                _lieEvaluator.Configure(terrainRoot, _holeController.GetCourseBounds());
            }
        }

        if (_shotController != null && _ballController != null && _holeController != null && _hud != null && _clubController != null && _lieEvaluator != null)
        {
            _shotController.Configure(_ballController, _holeController, _hud, _clubController, _lieEvaluator);
        }

        if (_hazardResolver != null && _ballController != null && _holeController != null && _shotController != null && _hud != null && _lieEvaluator != null)
        {
            _hazardResolver.Configure(_ballController, _holeController, _shotController, _hud, _lieEvaluator);
            _hazardResolver.TreeRecoveryPromptRequested += OnTreeRecoveryPromptRequested;
        }

        InitializeHud();
        HideHoleCompleteMessage();

        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    public override void _Process(double delta)
    {
        if (_hud == null || _holeController == null || _ballController == null || _holeController.IsHoleComplete)
        {
            return;
        }

        if (_lieEvaluator != null)
        {
            var lie = _lieEvaluator.EvaluateLie(_ballController.GlobalPosition);
            _ballController.SetTerrain(lie);
            _hud.SetLieType(lie);
        }

        _hud.SetDistanceToCup(_holeController.GetDistanceToCup(_ballController.GlobalPosition));
    }

    private void InitializeHud()
    {
        if (_hud == null || _holeController == null || _clubController == null)
        {
            return;
        }

        _hud.SetTopLevelData(
            _holeController.HoleNumber,
            _holeController.Par,
            _holeController.LocalStrokeCount,
            clubName: _clubController.CurrentClub.Name,
            windText: "Aim: Left/Right | Club: Up/Down | Hold Space: Power"
        );
        _hud.SetStatusMessage("Ready for next shot.");

        if (_lieEvaluator != null && _ballController != null)
        {
            var initialLie = _lieEvaluator.EvaluateLie(_ballController.GlobalPosition);
            _ballController.SetTerrain(initialLie);
            _hud.SetLieType(initialLie);
        }
    }

    private void OnMainMenuRequested()
    {
        GameManagerSingleton?.GoToMainMenu();
    }

    private void OnTreeRecoveryPromptRequested()
    {
        _recoveryDialog?.ShowDialog("Ball is obstructed in the trees.\nTake a drop for +1 stroke or play from lie.");
    }

    private void OnTakeDropChosen()
    {
        _hazardResolver?.ResolveTakeDropChoice();
    }

    private void OnPlayFromLieChosen()
    {
        _hazardResolver?.ResolvePlayFromLieChoice();
    }

    private void OnHoleCompleted(int strokes)
    {
        _shotController?.SetInputEnabled(false);
        _ballController?.SetMovementEnabled(false);
        _recoveryDialog?.HideDialog();

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
        _hud?.SetStatusMessage("Hole complete.");
        GameManagerSingleton?.ChangeState(GameState.HoleComplete);
    }

    private void HideHoleCompleteMessage()
    {
        if (_holeCompletePanel != null)
        {
            _holeCompletePanel.Visible = false;
        }

        if (_holeCompleteLabel != null)
        {
            _holeCompleteLabel.Visible = false;
        }
    }

    public override void _ExitTree()
    {
        if (_holeController != null)
        {
            _holeController.HoleCompleted -= OnHoleCompleted;
        }

        if (_recoveryDialog != null)
        {
            _recoveryDialog.DropChosen -= OnTakeDropChosen;
            _recoveryDialog.PlayFromLieChosen -= OnPlayFromLieChosen;
        }

        if (_hazardResolver != null)
        {
            _hazardResolver.TreeRecoveryPromptRequested -= OnTreeRecoveryPromptRequested;
        }

        base._ExitTree();
    }
}
