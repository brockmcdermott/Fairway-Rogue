using Godot;

public partial class GameplaySceneController : Node2D
{
    private static readonly int[] TemporaryParByHole =
    {
        4, 3, 5,
        4, 4, 3,
        5, 4, 4
    };

    private HUDController? _hud;
    private HoleController? _holeController;
    private BallController? _ballController;
    private ClubController? _clubController;
    private ShotController? _shotController;
    private LieEvaluator? _lieEvaluator;
    private HazardResolver? _hazardResolver;
    private RecoveryDialogController? _recoveryDialog;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");

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

        if (_hud != null)
        {
            _hud.MainMenuRequested += OnMainMenuRequested;
            _hud.SetRecoveryPromptVisible(false);
        }

        ApplyTemporaryHoleConfiguration();

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

    private void ApplyTemporaryHoleConfiguration()
    {
        if (_holeController == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        var holeIndex = run?.CurrentHoleIndex ?? _holeController.HoleNumber;
        var resolvedHoleIndex = Mathf.Max(1, holeIndex);

        _holeController.HoleNumber = resolvedHoleIndex;
        _holeController.Par = GetTemporaryParForHole(resolvedHoleIndex);
    }

    private static int GetTemporaryParForHole(int holeIndex)
    {
        if (holeIndex <= 0)
        {
            return 4;
        }

        var arrayIndex = (holeIndex - 1) % TemporaryParByHole.Length;
        return TemporaryParByHole[arrayIndex];
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

    private void OnHoleCompleted(HoleResultData holeResult)
    {
        _shotController?.SetInputEnabled(false);
        _ballController?.SetMovementEnabled(false);
        _recoveryDialog?.HideDialog();

        _hud?.SetStrokeCount(holeResult.Strokes);
        _hud?.SetStatusMessage($"Hole complete: {holeResult.Label} ({holeResult.RelativeScoreText}).");
        GameManagerSingleton?.SubmitHoleResult(holeResult);
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
