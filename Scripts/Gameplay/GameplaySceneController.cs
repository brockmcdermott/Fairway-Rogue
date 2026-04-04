using Godot;

public partial class GameplaySceneController : Node2D
{
    private HUDController? _hud;
    private RecoveryDialogController? _recoveryDialog;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");

    public override void _Ready()
    {
        _hud = GetNodeOrNull<HUDController>("HUD");
        _recoveryDialog = GetNodeOrNull<RecoveryDialogController>("RecoveryDialog");

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

        InitializeHud();

        if (GameManagerSingleton?.CurrentState == GameState.LoadingHole)
        {
            GameManagerSingleton.ChangeState(GameState.InHole);
        }
    }

    private void InitializeHud()
    {
        if (_hud == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        var holeNumber = run?.CurrentHoleIndex ?? 1;
        var strokes = run?.TotalStrokes ?? 0;

        _hud.SetTopLevelData(
            holeNumber,
            par: 4,
            strokes,
            clubName: "Driver",
            windText: "Wind: --"
        );
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
        RunManagerSingleton?.AddStroke();
        _hud?.SetStrokeCount(RunManagerSingleton?.TotalStrokes ?? 0);
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    private void OnPlayFromLieChosen()
    {
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }
}
