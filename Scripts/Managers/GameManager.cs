using Godot;

public enum GameState
{
    MainMenu,
    LoadingHole,
    InHole,
    RecoveryPrompt,
    HoleComplete,
    Shop,
    Scorecard,
    RunComplete,
    Paused
}

public partial class GameManager : Node
{
    public delegate void GameStateChangedHandler(GameState previousState, GameState newState);
    public event GameStateChangedHandler? GameStateChanged;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private GameState _stateBeforePause = GameState.InHole;
    private Window? _rootWindow;

    private RunManager? RunManagerSingleton => AutoloadLocator.Get<RunManager>(this, nameof(RunManager));
    private SaveManager? SaveManagerSingleton => AutoloadLocator.Get<SaveManager>(this, nameof(SaveManager));
    private SceneRouter? SceneRouterSingleton => AutoloadLocator.Get<SceneRouter>(this, nameof(SceneRouter));

    public override void _Ready()
    {
        _rootWindow = GetTree().Root;
        if (_rootWindow != null)
        {
            _rootWindow.CloseRequested += OnRootCloseRequested;
        }

        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        var previous = CurrentState;
        CurrentState = newState;
        GameStateChanged?.Invoke(previous, newState);
        GD.Print($"[GameManager] State changed: {previous} -> {newState}");
    }

    public void StartNewRun()
    {
        var runManager = RunManagerSingleton;
        var sceneRouter = SceneRouterSingleton;

        if (runManager == null || sceneRouter == null)
        {
            GD.PushError("[GameManager] Unable to start run. Missing RunManager or SceneRouter autoload.");
            return;
        }

        var seed = (int)Time.GetUnixTimeFromSystem();
        runManager.StartRun(seed, RunManager.DefaultTotalHoles);
        SaveManagerSingleton?.SaveRun(runManager.BuildSaveData());

        ChangeState(GameState.LoadingHole);
        sceneRouter.GoToGameplay();
        ChangeState(GameState.InHole);
    }

    public void ResumeRun()
    {
        var runManager = RunManagerSingleton;
        var saveManager = SaveManagerSingleton;
        var sceneRouter = SceneRouterSingleton;

        if (runManager == null || saveManager == null || sceneRouter == null)
        {
            GD.PushError("[GameManager] Unable to resume run. Missing required autoload(s).");
            return;
        }

        if (!saveManager.HasRunSave())
        {
            GD.Print("[GameManager] Resume requested with no save present.");
            return;
        }

        if (!saveManager.TryLoadRun(out var saveData) || saveData == null)
        {
            GD.PushWarning("[GameManager] Save exists but could not be loaded. Staying in menu.");
            saveManager.DeleteRunSave();
            return;
        }

        runManager.LoadFromSave(saveData);

        ChangeState(GameState.LoadingHole);
        sceneRouter.GoToGameplay();
        ChangeState(GameState.InHole);
    }

    public void CompleteHole()
    {
        var runManager = RunManagerSingleton;
        var sceneRouter = SceneRouterSingleton;

        if (runManager == null || sceneRouter == null)
        {
            GD.PushError("[GameManager] Unable to complete hole. Missing RunManager or SceneRouter.");
            return;
        }

        ChangeState(GameState.HoleComplete);

        if (runManager.IsFinalHole())
        {
            CompleteRun();
            return;
        }

        sceneRouter.GoToScorecard();
        ChangeState(GameState.Scorecard);
    }

    public void SubmitHoleResult(HoleResultData holeResult)
    {
        var runManager = RunManagerSingleton;
        if (runManager == null)
        {
            GD.PushError("[GameManager] Cannot submit hole result. Missing RunManager.");
            return;
        }

        runManager.RecordHoleResult(holeResult);
        var reward = runManager.CalculateHoleReward(holeResult);
        runManager.AwardCurrency(reward);
        SaveCurrentRunIfAvailable();

        CompleteHole();
    }

    public void CompleteRun()
    {
        SceneRouterSingleton?.GoToRunComplete();
        ChangeState(GameState.RunComplete);
    }

    public void ContinueRunToNextHole()
    {
        var runManager = RunManagerSingleton;
        var sceneRouter = SceneRouterSingleton;
        if (runManager == null || sceneRouter == null)
        {
            GD.PushError("[GameManager] Unable to continue run. Missing RunManager or SceneRouter.");
            return;
        }

        if (runManager.IsFinalHole())
        {
            CompleteRun();
            return;
        }

        runManager.AdvanceToNextHole();
        SaveCurrentRunIfAvailable();

        ChangeState(GameState.LoadingHole);
        sceneRouter.GoToGameplay();
        ChangeState(GameState.InHole);
    }

    public void GoToShop()
    {
        var runManager = RunManagerSingleton;
        var sceneRouter = SceneRouterSingleton;
        if (runManager == null || sceneRouter == null)
        {
            GD.PushError("[GameManager] Unable to open shop. Missing RunManager or SceneRouter.");
            return;
        }

        if (runManager.IsFinalHole())
        {
            CompleteRun();
            return;
        }

        SaveCurrentRunIfAvailable();
        sceneRouter.GoToShop();
        ChangeState(GameState.Shop);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Paused)
        {
            return;
        }

        _stateBeforePause = CurrentState;
        SaveCurrentRunIfAvailable();
        ChangeState(GameState.Paused);
        GetTree().Paused = true;
    }

    public void UnpauseGame()
    {
        if (CurrentState != GameState.Paused)
        {
            return;
        }

        GetTree().Paused = false;
        ChangeState(_stateBeforePause);
    }

    public void SaveCurrentRunIfAvailable()
    {
        var runManager = RunManagerSingleton;
        if (runManager == null || !runManager.HasActiveRun)
        {
            return;
        }

        SaveManagerSingleton?.SaveRun(runManager.BuildSaveData());
    }

    public void GoToMainMenu()
    {
        if (CurrentState == GameState.RunComplete)
        {
            SaveManagerSingleton?.DeleteRunSave();
        }
        else
        {
            SaveCurrentRunIfAvailable();
        }

        SceneRouterSingleton?.GoToMainMenu();
        ChangeState(GameState.MainMenu);
        GetTree().Paused = false;
    }

    public void QuitGame()
    {
        if (CurrentState == GameState.RunComplete)
        {
            SaveManagerSingleton?.DeleteRunSave();
        }
        else
        {
            SaveCurrentRunIfAvailable();
        }

        GetTree().Quit();
    }

    public override void _ExitTree()
    {
        if (_rootWindow != null)
        {
            _rootWindow.CloseRequested -= OnRootCloseRequested;
        }

        base._ExitTree();
    }

    private void OnRootCloseRequested()
    {
        QuitGame();
    }
}
