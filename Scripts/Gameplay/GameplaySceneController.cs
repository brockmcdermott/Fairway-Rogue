using System.Collections.Generic;
using Godot;

public partial class GameplaySceneController : Node2D
{
    [ExportGroup("Camera Framing")]
    [Export] public float CameraPaddingPixels { get; set; } = 14.0f;
    [Export] public float CameraMinZoom { get; set; } = 0.42f;
    [Export] public float CameraMaxZoom { get; set; } = 3.10f;
    [Export] public float CourseFillFactor { get; set; } = 0.52f;

    [ExportGroup("Camera Interaction")]
    [Export] public bool FollowBallEnabled { get; set; } = true;
    [Export] public float CameraFollowLerpSpeed { get; set; } = 10.0f;
    [Export] public float CameraDragSensitivity { get; set; } = 1.0f;
    [Export] public float CameraZoomStep { get; set; } = 0.10f;
    [Export] public float CameraUserZoomMin { get; set; } = 0.55f;
    [Export] public float CameraUserZoomMax { get; set; } = 2.8f;
    [Export] public MouseButton CameraPanButton { get; set; } = MouseButton.Right;

    [ExportGroup("Course Map")]
    [Export] public float InactiveHoleAlpha { get; set; } = 0.82f;

    [ExportGroup("Debug Hooks")]
    [Export] public bool UseProceduralGeneration { get; set; } = true;
    [Export] public int DebugSeedOverride { get; set; }
    [Export] public bool EnableDebugOverlay { get; set; }

    private Camera2D? _camera;
    private Viewport? _viewport;
    private Node2D? _holeRoot;
    private HUDController? _hud;
    private HoleController? _holeController;
    private BallController? _ballController;
    private ClubController? _clubController;
    private ShotController? _shotController;
    private LieEvaluator? _lieEvaluator;
    private HazardResolver? _hazardResolver;
    private HoleGenerator? _holeGenerator;
    private CourseGenerator? _courseGenerator;
    private TerrainPainter? _terrainPainter;
    private RecoveryDialogController? _recoveryDialog;
    private CourseLayout? _courseLayout;
    private HoleLayout? _activeLayout;
    private PackedScene? _holeTemplateScene;
    private readonly List<HoleController> _courseHoles = new List<HoleController>();
    private Rect2 _cameraPanBounds = new Rect2(Vector2.Zero, new Vector2(1280, 720));
    private Rect2 _activeHoleBounds = new Rect2(Vector2.Zero, new Vector2(1280, 720));
    private float _cameraBaseZoom = 1.0f;
    private float _cameraUserZoom = 1.0f;
    private Vector2 _cameraPanOffset = Vector2.Zero;
    private bool _isCameraDragging;
    private Vector2 _lastDragMousePosition = Vector2.Zero;

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

        _holeRoot = GetNodeOrNull<Node2D>("HoleRoot");
        _hud = GetNodeOrNull<HUDController>("UIRoot/HUD");
        _ballController = GetNodeOrNull<BallController>("Ball");
        _clubController = GetNodeOrNull<ClubController>("ClubController");
        _shotController = GetNodeOrNull<ShotController>("ShotController");
        _lieEvaluator = GetNodeOrNull<LieEvaluator>("LieEvaluator");
        _hazardResolver = GetNodeOrNull<HazardResolver>("HazardResolver");
        _holeGenerator = GetNodeOrNull<HoleGenerator>("HoleGenerator");
        _courseGenerator = GetNodeOrNull<CourseGenerator>("CourseGenerator");
        _terrainPainter = GetNodeOrNull<TerrainPainter>("TerrainPainter");
        _recoveryDialog = GetNodeOrNull<RecoveryDialogController>("UIRoot/RecoveryDialog");
        _holeTemplateScene = ResourceLoader.Load<PackedScene>("res://Scenes/Gameplay/StaticPracticeHole.tscn");

        if (_ballController != null)
        {
            _ballController.BallStartedMoving += OnBallStartedMoving;
        }

        if (_hud != null)
        {
            _hud.MainMenuRequested += OnMainMenuRequested;
            _hud.ClubSelectionRequested += OnClubSelectionRequested;
            _hud.SetRecoveryPromptVisible(false);
        }

        AudioManagerSingleton?.PlayMusic("gameplay");

        BuildOrLoadCourseMap();

        if (_recoveryDialog != null)
        {
            _recoveryDialog.DropChosen += OnTakeDropChosen;
            _recoveryDialog.PlayFromLieChosen += OnPlayFromLieChosen;
            _recoveryDialog.HideDialog();
        }

        if (_holeController != null && _ballController != null)
        {
            _holeController.BindBall(_ballController);
            _ballController.SetPlayableBounds(GetCurrentPlayableBounds());
            UpdateCameraFraming();
        }

        if (_lieEvaluator != null && _holeController != null)
        {
            var terrainRoot = _holeController.GetNodeOrNull<Node>("Visuals");
            if (terrainRoot != null)
            {
                _lieEvaluator.Configure(terrainRoot, GetCurrentPlayableBounds());
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
        if (HandleCameraInput(@event))
        {
            return;
        }

        if (!@event.IsActionPressed("ui_cancel"))
        {
            return;
        }

        if (_recoveryDialog != null && _recoveryDialog.Visible)
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
        UpdateCameraTracking((float)delta);

        if (_hud == null || _holeController == null || _ballController == null || _holeController.IsHoleComplete)
        {
            UpdateDebugOverlay();
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
        _hud.SetStatusMessage($"Ready. Ball: {activeBallName}. Scroll to zoom, {CameraPanButton} drag to pan, click to look.");

        if (_lieEvaluator != null && _ballController != null)
        {
            var initialLie = _lieEvaluator.EvaluateLie(_ballController.GlobalPosition);
            _ballController.SetTerrain(initialLie);
            _hud.SetLieType(initialLie);
        }

        _hud.SetDebugInfo(string.Empty, EnableDebugOverlay);
    }

    private void BuildOrLoadCourseMap()
    {
        DisconnectActiveHoleSignals();
        _courseHoles.Clear();
        _courseLayout = null;
        _activeLayout = null;
        _holeController = null;

        var run = RunManagerSingleton;
        var holeRoot = _holeRoot;
        if (holeRoot == null)
        {
            return;
        }

        if (!UseProceduralGeneration || _holeGenerator == null || _courseGenerator == null || run == null || !run.HasActiveRun)
        {
            _holeController = GetNodeOrNull<HoleController>("HoleRoot/StaticPracticeHole");
            if (_holeController != null)
            {
                _courseHoles.Add(_holeController);
                _holeController.SetCupEnabled(true);
                ConnectActiveHoleSignals();
            }

            _hud?.SetStatusMessage("Static hole debug mode.");
            UpdateCameraFraming();
            return;
        }

        var runSeed = DebugSeedOverride != 0 ? DebugSeedOverride : run.Seed;
        var totalHoles = Mathf.Max(1, run.TotalHoles);

        if (!run.HasCourseLayout)
        {
            var generatedCourse = _courseGenerator.GenerateCourseLayout(runSeed, totalHoles, _holeGenerator);
            run.SetCourseLayout(generatedCourse);
        }

        _courseLayout = run.CurrentCourseLayout;
        if (_courseLayout == null || _courseLayout.Holes.Count == 0)
        {
            GD.PushError("[GameplaySceneController] Course layout generation failed.");
            return;
        }

        BuildHoleNodes(_courseLayout);
        ActivateHole(Mathf.Clamp(run.CurrentHoleIndex, 1, totalHoles));
    }

    private void BuildHoleNodes(CourseLayout courseLayout)
    {
        if (_holeRoot == null)
        {
            return;
        }

        ClearHoleRoot();

        var template = _holeTemplateScene;
        if (template == null)
        {
            GD.PushError("[GameplaySceneController] Missing hole template scene.");
            return;
        }

        for (var i = 0; i < courseLayout.Holes.Count; i += 1)
        {
            var holeLayout = courseLayout.Holes[i];
            var instance = template.Instantiate();
            if (instance is not HoleController hole)
            {
                instance.QueueFree();
                continue;
            }

            hole.Name = $"Hole_{holeLayout.HoleNumber}";
            _holeRoot.AddChild(hole);
            hole.ApplyGeneratedLayout(holeLayout, _terrainPainter);
            hole.SetCupEnabled(false);
            hole.Modulate = new Color(0.86f, 0.90f, 0.86f, Mathf.Clamp(InactiveHoleAlpha, 0.35f, 1.0f));
            hole.ZIndex = 5;
            _courseHoles.Add(hole);
        }
    }

    private void ClearHoleRoot()
    {
        if (_holeRoot == null)
        {
            return;
        }

        var children = _holeRoot.GetChildren();
        for (var i = children.Count - 1; i >= 0; i -= 1)
        {
            if (children[i] is Node child)
            {
                _holeRoot.RemoveChild(child);
                child.QueueFree();
            }
        }
    }

    private void ActivateHole(int holeNumber)
    {
        DisconnectActiveHoleSignals();

        HoleController? active = null;
        for (var i = 0; i < _courseHoles.Count; i += 1)
        {
            if (_courseHoles[i].HoleNumber == holeNumber)
            {
                active = _courseHoles[i];
                break;
            }
        }

        if (active == null && _courseHoles.Count > 0)
        {
            active = _courseHoles[0];
        }

        _holeController = active;
        if (_holeController == null)
        {
            return;
        }

        for (var i = 0; i < _courseHoles.Count; i += 1)
        {
            var hole = _courseHoles[i];
            var isActive = hole == _holeController;
            hole.SetCupEnabled(isActive);
            hole.ZIndex = isActive ? 20 : 5;
            hole.Modulate = isActive
                ? Colors.White
                : new Color(0.86f, 0.90f, 0.86f, Mathf.Clamp(InactiveHoleAlpha, 0.35f, 1.0f));
        }

        _activeLayout = _holeController.ActiveLayout;
        ConnectActiveHoleSignals();

        if (_ballController != null)
        {
            _holeController.BindBall(_ballController);
            _ballController.SetPlayableBounds(GetCurrentPlayableBounds());
        }

        if (_lieEvaluator != null)
        {
            var terrainRoot = _holeController.GetNodeOrNull<Node>("Visuals");
            if (terrainRoot != null)
            {
                _lieEvaluator.Configure(terrainRoot, GetCurrentPlayableBounds());
            }
        }

        UpdateCameraFraming();
        var direction = _activeLayout != null ? GetCompassDirection(_activeLayout.WindDirection) : "Calm";
        var wind = _activeLayout != null ? _activeLayout.WindStrength : 0.0f;
        _hud?.SetStatusMessage($"Hole {_holeController.HoleNumber} ready. Wind {direction} {wind:0.00}.");
    }

    private void ConnectActiveHoleSignals()
    {
        if (_holeController == null)
        {
            return;
        }

        _holeController.HoleCompleted += OnHoleCompleted;
        _holeController.CupRejectedBySpeed += OnCupRejectedBySpeed;
    }

    private void DisconnectActiveHoleSignals()
    {
        if (_holeController == null)
        {
            return;
        }

        _holeController.HoleCompleted -= OnHoleCompleted;
        _holeController.CupRejectedBySpeed -= OnCupRejectedBySpeed;
    }

    private void UpdateCameraFraming()
    {
        if (_camera == null || _holeController == null)
        {
            return;
        }

        var bounds = _holeController.GetCourseBounds();
        if (!IsValidRect(bounds))
        {
            return;
        }

        _activeHoleBounds = bounds;
        _cameraPanBounds = _courseLayout != null && IsValidRect(_courseLayout.WorldBounds)
            ? _courseLayout.WorldBounds
            : bounds.Grow(280.0f);

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
        var fill = Mathf.Clamp(CourseFillFactor, 0.35f, 1.20f);
        _cameraBaseZoom = Mathf.Clamp(
            Mathf.Max(zoomX, zoomY) * fill,
            Mathf.Min(CameraMinZoom, CameraMaxZoom),
            Mathf.Max(CameraMinZoom, CameraMaxZoom));

        _cameraUserZoom = Mathf.Clamp(_cameraUserZoom, CameraUserZoomMin, CameraUserZoomMax);
        ClampCameraPanOffset();
        ApplyCameraZoom();

        var center = ClampCameraPosition(GetCameraFollowAnchor() + _cameraPanOffset);
        _camera.GlobalPosition = center;
    }

    private Rect2 GetCurrentPlayableBounds()
    {
        if (_courseLayout != null && IsValidRect(_courseLayout.WorldBounds))
        {
            return _courseLayout.WorldBounds;
        }

        if (_holeController != null)
        {
            return _holeController.GetCourseBounds();
        }

        return new Rect2(new Vector2(-640.0f, -360.0f), new Vector2(1280.0f, 720.0f));
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
        var courseHoles = _courseLayout?.TotalHoles ?? RunManagerSingleton?.TotalHoles ?? 1;
        var activeHole = _holeController?.HoleNumber ?? 1;

        var text =
            $"Debug | H{activeHole}/{courseHoles} | {movingText} | Speed {speed:0.00}\n" +
            $"Terrain: {terrainText}\n" +
            $"Last Safe: ({safePos.X:0.0}, {safePos.Y:0.0}) | Prev Shot: ({previousPos.X:0.0}, {previousPos.Y:0.0})\n" +
            $"Last Hazard: {hazardText}";

        _hud.SetDebugInfo(text, true);
    }

    private void OnViewportSizeChanged()
    {
        UpdateCameraFraming();
    }

    private void OnBallStartedMoving()
    {
        _cameraPanOffset = Vector2.Zero;
        _isCameraDragging = false;
    }

    private void UpdateCameraTracking(float delta)
    {
        if (_camera == null)
        {
            return;
        }

        ApplyCameraZoom();
        ClampCameraPanOffset();

        var target = ClampCameraPosition(GetCameraFollowAnchor() + _cameraPanOffset);
        if (delta <= 0.0f)
        {
            _camera.GlobalPosition = target;
            return;
        }

        var lerpWeight = 1.0f - Mathf.Exp(-Mathf.Max(1.0f, CameraFollowLerpSpeed) * delta);
        _camera.GlobalPosition = _camera.GlobalPosition.Lerp(target, lerpWeight);
    }

    private Vector2 GetCameraFollowAnchor()
    {
        if (FollowBallEnabled && _ballController != null && IsInstanceValid(_ballController))
        {
            return _ballController.GlobalPosition;
        }

        return IsValidRect(_activeHoleBounds) ? _activeHoleBounds.GetCenter() : Vector2.Zero;
    }

    private bool HandleCameraInput(InputEvent @event)
    {
        if (_camera == null || _hud == null || _recoveryDialog != null && _recoveryDialog.Visible)
        {
            return false;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                AdjustCameraZoom(1.0f - CameraZoomStep);
                return true;
            }

            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                AdjustCameraZoom(1.0f + CameraZoomStep);
                return true;
            }

            if (mouseButton.ButtonIndex == CameraPanButton)
            {
                _isCameraDragging = mouseButton.Pressed;
                _lastDragMousePosition = mouseButton.Position;
                return true;
            }

            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                var worldPoint = GetGlobalMousePosition();
                _cameraPanOffset = worldPoint - GetCameraFollowAnchor();
                ClampCameraPanOffset();
                return true;
            }

            return false;
        }

        if (@event is InputEventMouseMotion mouseMotion && _isCameraDragging)
        {
            var pixelDelta = mouseMotion.Position - _lastDragMousePosition;
            _lastDragMousePosition = mouseMotion.Position;

            var worldDelta = pixelDelta * Mathf.Max(0.05f, _camera.Zoom.X) * Mathf.Max(0.01f, CameraDragSensitivity);
            _cameraPanOffset -= worldDelta;
            ClampCameraPanOffset();
            return true;
        }

        return false;
    }

    private void AdjustCameraZoom(float multiplier)
    {
        if (_camera == null)
        {
            return;
        }

        var safeMultiplier = Mathf.Clamp(multiplier, 0.2f, 5.0f);
        _cameraUserZoom = Mathf.Clamp(
            _cameraUserZoom * safeMultiplier,
            Mathf.Min(CameraUserZoomMin, CameraUserZoomMax),
            Mathf.Max(CameraUserZoomMin, CameraUserZoomMax));
        ClampCameraPanOffset();
        ApplyCameraZoom();
    }

    private void ApplyCameraZoom()
    {
        if (_camera == null)
        {
            return;
        }

        var absoluteZoom = Mathf.Clamp(
            _cameraBaseZoom * _cameraUserZoom,
            Mathf.Min(CameraMinZoom, CameraMaxZoom),
            Mathf.Max(CameraMinZoom, CameraMaxZoom));
        _camera.Zoom = new Vector2(absoluteZoom, absoluteZoom);
    }

    private void ClampCameraPanOffset()
    {
        var anchor = GetCameraFollowAnchor();
        var clamped = ClampCameraPosition(anchor + _cameraPanOffset);
        _cameraPanOffset = clamped - anchor;
    }

    private Vector2 ClampCameraPosition(Vector2 target)
    {
        if (_camera == null || !IsValidRect(_cameraPanBounds))
        {
            return target;
        }

        var viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 1.0f || viewportSize.Y <= 1.0f)
        {
            return target;
        }

        var halfExtents = viewportSize * _camera.Zoom * 0.5f;
        var min = _cameraPanBounds.Position + halfExtents;
        var max = _cameraPanBounds.End - halfExtents;

        if (min.X > max.X)
        {
            target.X = _cameraPanBounds.GetCenter().X;
        }
        else
        {
            target.X = Mathf.Clamp(target.X, min.X, max.X);
        }

        if (min.Y > max.Y)
        {
            target.Y = _cameraPanBounds.GetCenter().Y;
        }
        else
        {
            target.Y = Mathf.Clamp(target.Y, min.Y, max.Y);
        }

        return target;
    }

    private static bool IsValidRect(Rect2 rect)
    {
        return rect.Size.X > Mathf.Epsilon && rect.Size.Y > Mathf.Epsilon;
    }

    private string BuildHudWindAndControlsText()
    {
        if (_activeLayout == null)
        {
            return "Wind: Calm";
        }

        var direction = GetCompassDirection(_activeLayout.WindDirection);
        return $"Wind: {direction} {_activeLayout.WindStrength:0.00}";
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

    private void OnClubSelectionRequested(ClubType clubType)
    {
        if (_ballController != null && _ballController.IsMoving)
        {
            return;
        }

        _clubController?.SelectClubType(clubType);
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

    private void OnCupRejectedBySpeed(float incomingSpeed, float threshold)
    {
        _hud?.SetStatusMessage($"Lip-out: speed {incomingSpeed:0.0} exceeds cup capture {threshold:0.0}.");
    }

    public override void _ExitTree()
    {
        DisconnectActiveHoleSignals();

        if (_ballController != null)
        {
            _ballController.BallStartedMoving -= OnBallStartedMoving;
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

        if (_hud != null)
        {
            _hud.ClubSelectionRequested -= OnClubSelectionRequested;
            _hud.MainMenuRequested -= OnMainMenuRequested;
        }

        if (_viewport != null)
        {
            _viewport.SizeChanged -= OnViewportSizeChanged;
        }

        base._ExitTree();
    }
}
