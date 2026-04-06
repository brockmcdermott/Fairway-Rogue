using Godot;

public partial class GameplaySceneController : Node2D
{
    [ExportGroup("Camera Framing")]
    [Export] public float CameraPaddingPixels { get; set; } = 120.0f;
    [Export] public float CameraMinZoom { get; set; } = 0.55f;
    [Export] public float CameraMaxZoom { get; set; } = 2.20f;

    [ExportGroup("Debug Hooks")]
    [Export] public bool UseProceduralGeneration { get; set; } = true;
    [Export] public int DebugSeedOverride { get; set; }
    [Export] public bool EnableDebugOverlay { get; set; }

    private Camera2D? _camera;
    private Viewport? _viewport;
    private HUDController? _hud;
    private HoleController? _holeController;
    private BallController? _ballController;
    private ClubController? _clubController;
    private ShotController? _shotController;
    private LieEvaluator? _lieEvaluator;
    private HazardResolver? _hazardResolver;
    private HoleGenerator? _holeGenerator;
    private TerrainPainter? _terrainPainter;
    private RecoveryDialogController? _recoveryDialog;
    private HoleLayout? _activeLayout;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private RunManager? RunManagerSingleton => AutoloadLocator.Get<RunManager>(this, nameof(RunManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        _viewport = GetViewport();
        if (_viewport != null)
        {
            _viewport.SizeChanged += OnViewportSizeChanged;
        }

        _hud = GetNodeOrNull<HUDController>("UIRoot/HUD");
        _holeController = GetNodeOrNull<HoleController>("HoleRoot/StaticPracticeHole");
        _ballController = GetNodeOrNull<BallController>("Ball");
        _clubController = GetNodeOrNull<ClubController>("ClubController");
        _shotController = GetNodeOrNull<ShotController>("ShotController");
        _lieEvaluator = GetNodeOrNull<LieEvaluator>("LieEvaluator");
        _hazardResolver = GetNodeOrNull<HazardResolver>("HazardResolver");
        _holeGenerator = GetNodeOrNull<HoleGenerator>("HoleGenerator");
        _terrainPainter = GetNodeOrNull<TerrainPainter>("TerrainPainter");
        _recoveryDialog = GetNodeOrNull<RecoveryDialogController>("UIRoot/RecoveryDialog");

        if (_hud != null)
        {
            _hud.MainMenuRequested += OnMainMenuRequested;
            _hud.SetRecoveryPromptVisible(false);
        }

        AudioManagerSingleton?.PlayMusic("gameplay");

        GenerateAndApplyHoleLayout();

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
            UpdateCameraFraming();
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
        UpdateDebugOverlay();

        GameManagerSingleton?.ChangeState(GameState.InHole);
        GameManagerSingleton?.SaveCurrentRunIfAvailable();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel"))
        {
            return;
        }

        var manager = GameManagerSingleton;
        if (manager == null)
        {
            return;
        }

        if (manager.CurrentState == GameState.Paused)
        {
            manager.UnpauseGame();
            _hud?.SetStatusMessage("Resumed.");
            return;
        }

        if (manager.CurrentState == GameState.InHole || manager.CurrentState == GameState.RecoveryPrompt)
        {
            manager.PauseGame();
            _hud?.SetStatusMessage("Paused. Press Esc to resume.");
        }
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
        UpdateDebugOverlay();
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
            windText: BuildHudWindAndControlsText()
        );
        var activeBallName = RunManagerSingleton?.GetActiveBallData().Name ?? "All-Around Ball";
        _hud.SetStatusMessage($"Ready for next shot. Ball: {activeBallName}.");

        if (_lieEvaluator != null && _ballController != null)
        {
            var initialLie = _lieEvaluator.EvaluateLie(_ballController.GlobalPosition);
            _ballController.SetTerrain(initialLie);
            _hud.SetLieType(initialLie);
        }

        _hud.SetDebugInfo(string.Empty, EnableDebugOverlay);
    }

    private void GenerateAndApplyHoleLayout()
    {
        if (_holeController == null || _holeGenerator == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        var runSeed = DebugSeedOverride != 0 ? DebugSeedOverride : run?.Seed ?? (int)Time.GetUnixTimeFromSystem();
        var holeIndex = run?.CurrentHoleIndex ?? _holeController.HoleNumber;
        if (!UseProceduralGeneration)
        {
            _activeLayout = null;
            _holeController.HoleNumber = holeIndex;
            _hud?.SetStatusMessage("Debug static practice hole enabled.");
            UpdateCameraFraming();
            return;
        }

        var totalHoles = run?.TotalHoles ?? RunManager.DefaultTotalHoles;
        _activeLayout = _holeGenerator.GenerateLayout(runSeed, holeIndex, totalHoles);
        _holeController.ApplyGeneratedLayout(_activeLayout, _terrainPainter);
        if (_activeLayout == null)
        {
            return;
        }

        var direction = GetCompassDirection(_activeLayout.WindDirection);
        _hud?.SetStatusMessage($"Generated hole {_activeLayout.HoleNumber} (Seed {_activeLayout.Seed}) | Wind {direction} {_activeLayout.WindStrength:0.00}");
        UpdateCameraFraming();
    }

    private void UpdateCameraFraming()
    {
        if (_camera == null || _holeController == null)
        {
            return;
        }

        var bounds = _holeController.GetCourseBounds();
        if (bounds.Size.X <= Mathf.Epsilon || bounds.Size.Y <= Mathf.Epsilon)
        {
            return;
        }

        _camera.GlobalPosition = bounds.GetCenter();

        var viewportRect = GetViewportRect();
        if (viewportRect.Size.X <= 1.0f || viewportRect.Size.Y <= 1.0f)
        {
            return;
        }

        var safePadding = Mathf.Max(0.0f, CameraPaddingPixels);
        var targetWidth = Mathf.Max(1.0f, bounds.Size.X + safePadding * 2.0f);
        var targetHeight = Mathf.Max(1.0f, bounds.Size.Y + safePadding * 2.0f);

        var zoomX = targetWidth / viewportRect.Size.X;
        var zoomY = targetHeight / viewportRect.Size.Y;
        var targetZoom = Mathf.Clamp(Mathf.Max(zoomX, zoomY), Mathf.Min(CameraMinZoom, CameraMaxZoom), Mathf.Max(CameraMinZoom, CameraMaxZoom));
        _camera.Zoom = new Vector2(targetZoom, targetZoom);
    }

    private void UpdateDebugOverlay()
    {
        if (_hud == null)
        {
            return;
        }

        if (!EnableDebugOverlay || _ballController == null)
        {
            _hud.SetDebugInfo(string.Empty, false);
            return;
        }

        var speed = _ballController.Velocity.Length();
        var terrainText = _ballController.CurrentTerrainType.ToString();
        var movingText = _ballController.IsMoving ? "Moving" : "Stopped";
        var hazardText = _hazardResolver?.LastHazardDebugText ?? "none";
        var safePos = _hazardResolver?.LastSafePosition ?? Vector2.Zero;
        var previousPos = _hazardResolver?.PreviousShotPosition ?? Vector2.Zero;

        var text =
            $"Debug | {movingText} | Speed {speed:0.00}\n" +
            $"Terrain: {terrainText}\n" +
            $"Last Safe: ({safePos.X:0.0}, {safePos.Y:0.0}) | Prev Shot: ({previousPos.X:0.0}, {previousPos.Y:0.0})\n" +
            $"Last Hazard: {hazardText}";

        _hud.SetDebugInfo(text, true);
    }

    private void OnViewportSizeChanged()
    {
        UpdateCameraFraming();
    }

    private string BuildHudWindAndControlsText()
    {
        const string controls = "Aim: Left/Right | Club: Up/Down | Hold Space: Power";
        if (_activeLayout == null)
        {
            return controls;
        }

        var direction = GetCompassDirection(_activeLayout.WindDirection);
        return $"{controls} | Wind: {direction} {_activeLayout.WindStrength:0.00}";
    }

    private static string GetCompassDirection(Vector2 vector)
    {
        if (vector == Vector2.Zero)
        {
            return "Calm";
        }

        var angle = Mathf.RadToDeg(vector.Angle());
        if (angle < 0.0f)
        {
            angle += 360.0f;
        }

        if (angle is >= 337.5f or < 22.5f) return "E";
        if (angle < 67.5f) return "SE";
        if (angle < 112.5f) return "S";
        if (angle < 157.5f) return "SW";
        if (angle < 202.5f) return "W";
        if (angle < 247.5f) return "NW";
        if (angle < 292.5f) return "N";
        return "NE";
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
        AudioManagerSingleton?.PlaySfx("hole_complete");
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

        if (_viewport != null)
        {
            _viewport.SizeChanged -= OnViewportSizeChanged;
        }

        base._ExitTree();
    }
}
