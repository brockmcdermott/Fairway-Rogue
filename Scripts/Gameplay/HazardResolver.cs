using System;
using Godot;
using Godot.Collections;

public partial class HazardResolver : Node
{
    [ExportGroup("Recovery Search")]
    [Export] public float RecoverySearchStep { get; set; } = 24.0f;
    [Export] public float RecoverySearchMaxRadius { get; set; } = 220.0f;
    [Export] public int RecoverySearchAngleSamples { get; set; } = 20;
    [Export] public float RecoveryCollisionRadiusScale { get; set; } = 0.9f;
    [Export] public float TreeDropMaxRadius { get; set; } = 150.0f;
    [Export] public float TreeDropBackStep { get; set; } = 22.0f;
    [Export] public float TreeDropBackMaxDistance { get; set; } = 180.0f;

    [ExportGroup("Safety")]
    [Export] public float HazardReentryCooldownSeconds { get; set; } = 0.15f;
    [Export] public bool VerboseDebugLogging { get; set; }

    public event Action? TreeRecoveryPromptRequested;

    private BallController? _ball;
    private HoleController? _hole;
    private ShotController? _shotController;
    private HUDController? _hud;
    private LieEvaluator? _lieEvaluator;

    private bool _isResolvingHazard;
    private bool _recoveryPromptActive;
    private Vector2 _previousShotPosition = Vector2.Zero;
    private Vector2 _lastSafePosition = Vector2.Zero;
    private TerrainType _lastResolvedHazard = TerrainType.Tee;
    private double _lastHazardResolveTimeSeconds = -9999.0;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public Vector2 PreviousShotPosition => _previousShotPosition;
    public Vector2 LastSafePosition => _lastSafePosition;
    public bool IsRecoveryPromptActive => _recoveryPromptActive;
    public string LastHazardDebugText { get; private set; } = "none";

    public void Configure(
        BallController ball,
        HoleController hole,
        ShotController shotController,
        HUDController hud,
        LieEvaluator lieEvaluator)
    {
        DisconnectBallSignals();

        _ball = ball;
        _hole = hole;
        _shotController = shotController;
        _hud = hud;
        _lieEvaluator = lieEvaluator;

        _previousShotPosition = ball.GlobalPosition;
        _lastSafePosition = ball.GlobalPosition;
        _isResolvingHazard = false;
        _recoveryPromptActive = false;
        _lastResolvedHazard = TerrainType.Tee;
        _lastHazardResolveTimeSeconds = -9999.0;
        LastHazardDebugText = "none";

        ball.ShotLaunched += OnShotLaunched;
        ball.BallStopped += OnBallStopped;
        ball.TerrainChanged += OnTerrainChanged;
        ball.HazardEntered += OnHazardEntered;
    }

    public override void _ExitTree()
    {
        DisconnectBallSignals();
        base._ExitTree();
    }

    public void ResolvePlayFromLieChoice()
    {
        if (!_recoveryPromptActive || _shotController == null || _hud == null)
        {
            return;
        }

        _hud.SetStatusMessage("Play from lie selected. Tree obstruction penalties remain active.");
        EndRecoveryPrompt();
    }

    public void ResolveTakeDropChoice()
    {
        if (!_recoveryPromptActive || _ball == null || _hole == null || _shotController == null || _hud == null || _lieEvaluator == null)
        {
            return;
        }

        _isResolvingHazard = true;

        _hole.AddPenaltyStroke(1);
        var dropPosition = EnsureSafeRecoveryPosition(FindTreeDropPosition(_ball.GlobalPosition), _ball.GlobalPosition);
        _ball.ResetAt(dropPosition);

        var droppedLie = _lieEvaluator.EvaluateLie(dropPosition);
        if (!IsSafeTerrain(droppedLie))
        {
            dropPosition = EnsureSafeRecoveryPosition(_hole.TeePosition, _ball.GlobalPosition);
            _ball.ResetAt(dropPosition);
            droppedLie = _lieEvaluator.EvaluateLie(dropPosition);
        }

        _ball.SetTerrain(droppedLie);
        _lastSafePosition = dropPosition;
        LastHazardDebugText = $"Tree drop -> ({dropPosition.X:0.0}, {dropPosition.Y:0.0})";

        _hud.SetStrokeCount(_hole.LocalStrokeCount);
        _hud.SetStatusMessage("Take a drop selected: +1 penalty stroke.");

        _isResolvingHazard = false;
        EndRecoveryPrompt();
    }

    private void DisconnectBallSignals()
    {
        if (_ball == null)
        {
            return;
        }

        _ball.ShotLaunched -= OnShotLaunched;
        _ball.BallStopped -= OnBallStopped;
        _ball.TerrainChanged -= OnTerrainChanged;
        _ball.HazardEntered -= OnHazardEntered;
    }

    private void OnShotLaunched(Vector2 shotStartPosition)
    {
        _previousShotPosition = shotStartPosition;
        _lastSafePosition = shotStartPosition;
    }

    private void OnBallStopped()
    {
        if (_isResolvingHazard || _recoveryPromptActive || _ball == null || _hole == null || _shotController == null || _hud == null || _lieEvaluator == null)
        {
            return;
        }

        if (_hole.IsHoleComplete || _ball.IsMoving)
        {
            return;
        }

        var lieAtRest = _lieEvaluator.EvaluateTerrainAtPosition(_ball.GlobalPosition);
        _ball.SetTerrain(lieAtRest);

        if (lieAtRest == TerrainType.Water || lieAtRest == TerrainType.OutOfBounds)
        {
            OnHazardEntered(lieAtRest, _ball.GlobalPosition);
            return;
        }

        if (_lieEvaluator.IsObstructedTreeLie(_ball.GlobalPosition, _hole.CupPosition))
        {
            BeginTreeRecoveryPrompt();
            return;
        }

        if (_ball.CurrentTerrainType == TerrainType.Sand)
        {
            AudioManagerSingleton?.PlaySfx("sand_impact");
        }

        _hud.SetStatusMessage("Ready for next shot.");
    }

    private void OnTerrainChanged(TerrainType terrainType)
    {
        if (_ball == null || _lieEvaluator == null)
        {
            return;
        }

        if (!IsSafeTerrain(terrainType))
        {
            return;
        }

        var terrainAtBall = _lieEvaluator.EvaluateTerrainAtPosition(_ball.GlobalPosition);
        if (IsSafeTerrain(terrainAtBall))
        {
            _lastSafePosition = _ball.GlobalPosition;
        }
    }

    private void OnHazardEntered(TerrainType terrainType, Vector2 entryPosition)
    {
        if (_isResolvingHazard || _recoveryPromptActive || _ball == null || _hole == null || _shotController == null || _hud == null || _lieEvaluator == null || _hole.IsHoleComplete)
        {
            return;
        }

        if (_ball.IsAirborne)
        {
            LastHazardDebugText = $"{terrainType} ignored while airborne @ ({entryPosition.X:0.0}, {entryPosition.Y:0.0})";
            return;
        }

        if (terrainType != TerrainType.Water && terrainType != TerrainType.OutOfBounds)
        {
            return;
        }

        var nowSeconds = Time.GetTicksMsec() / 1000.0;
        if (terrainType == _lastResolvedHazard &&
            nowSeconds - _lastHazardResolveTimeSeconds <= Mathf.Max(0.0f, HazardReentryCooldownSeconds) &&
            entryPosition.DistanceTo(_lastSafePosition) <= Mathf.Max(RecoverySearchStep, 12.0f))
        {
            return;
        }

        _isResolvingHazard = true;
        _shotController.SetInputEnabled(false);

        try
        {
            _ball.StopBall();
            _hole.AddPenaltyStroke(1);
            if (terrainType == TerrainType.Water)
            {
                AudioManagerSingleton?.PlaySfx("splash");
            }

            var recoveryPosition = EnsureSafeRecoveryPosition(FindRecoveryPosition(entryPosition), entryPosition);
            _ball.ResetAt(recoveryPosition);

            var recoveredLie = _lieEvaluator.EvaluateLie(recoveryPosition);
            if (!IsSafeTerrain(recoveredLie))
            {
                recoveryPosition = EnsureSafeRecoveryPosition(_hole.TeePosition, entryPosition);
                _ball.ResetAt(recoveryPosition);
                recoveredLie = _lieEvaluator.EvaluateLie(recoveryPosition);
            }

            if (!IsSafeTerrain(recoveredLie))
            {
                recoveredLie = TerrainType.Tee;
            }

            _ball.SetTerrain(recoveredLie);
            _lastSafePosition = recoveryPosition;

            _hud.SetStrokeCount(_hole.LocalStrokeCount);

            var hazardLabel = terrainType == TerrainType.Water ? "Water" : "Out of Bounds";
            _hud.SetStatusMessage($"{hazardLabel}: +1 penalty stroke. Repositioned to a safe lie.");
            LastHazardDebugText =
                $"{hazardLabel} @ ({entryPosition.X:0.0}, {entryPosition.Y:0.0}) -> ({recoveryPosition.X:0.0}, {recoveryPosition.Y:0.0})";

            _lastResolvedHazard = terrainType;
            _lastHazardResolveTimeSeconds = nowSeconds;

            if (VerboseDebugLogging)
            {
                GD.Print($"[HazardResolver] {LastHazardDebugText}");
            }
        }
        finally
        {
            _isResolvingHazard = false;
            _ball.SetMovementEnabled(true);
            _shotController.SetInputEnabled(true);
            GameManagerSingleton?.ChangeState(GameState.InHole);
        }
    }

    private void BeginTreeRecoveryPrompt()
    {
        if (_shotController == null || _hud == null)
        {
            return;
        }

        _recoveryPromptActive = true;
        _shotController.SetInputEnabled(false);
        _hud.SetStatusMessage("Obstructed lie in trees. Choose recovery.");
        AudioManagerSingleton?.PlaySfx("recovery_prompt");
        GameManagerSingleton?.ChangeState(GameState.RecoveryPrompt);
        TreeRecoveryPromptRequested?.Invoke();
    }

    private void EndRecoveryPrompt()
    {
        if (_shotController == null)
        {
            return;
        }

        _recoveryPromptActive = false;
        _shotController.SetInputEnabled(true);
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    private Vector2 FindTreeDropPosition(Vector2 obstructedPosition)
    {
        if (_hole == null)
        {
            return obstructedPosition;
        }

        if (TryFindBackwardTreeDrop(obstructedPosition, out var backwardDrop))
        {
            return backwardDrop;
        }

        if (TrySearchForTreeDropAround(obstructedPosition, out var nearObstructedLie))
        {
            return nearObstructedLie;
        }

        if (TrySearchForTreeDropAround(_lastSafePosition, out var nearLastSafe))
        {
            return nearLastSafe;
        }

        if (TrySearchForTreeDropAround(_previousShotPosition, out var nearPrevious))
        {
            return nearPrevious;
        }

        if (IsSafeRecoveryPoint(_lastSafePosition))
        {
            return _lastSafePosition;
        }

        return FindRecoveryPosition(obstructedPosition);
    }

    private bool TryFindBackwardTreeDrop(Vector2 obstructedPosition, out Vector2 dropPoint)
    {
        dropPoint = obstructedPosition;
        if (_hole == null)
        {
            return false;
        }

        var awayFromCup = (obstructedPosition - _hole.CupPosition).Normalized();
        if (awayFromCup == Vector2.Zero)
        {
            awayFromCup = (_lastSafePosition - obstructedPosition).Normalized();
        }

        if (awayFromCup == Vector2.Zero)
        {
            awayFromCup = Vector2.Left;
        }

        var step = Mathf.Max(8.0f, TreeDropBackStep);
        var maxDistance = Mathf.Max(step, TreeDropBackMaxDistance);

        for (var distance = step; distance <= maxDistance; distance += step)
        {
            var candidate = ClampInsidePlayableBounds(obstructedPosition + awayFromCup * distance);
            if (!IsValidTreeDropPoint(candidate))
            {
                continue;
            }

            dropPoint = candidate;
            return true;
        }

        return false;
    }

    private bool TrySearchForTreeDropAround(Vector2 center, out Vector2 dropPoint)
    {
        dropPoint = ClampInsidePlayableBounds(center);

        var clampedCenter = ClampInsidePlayableBounds(center);
        if (IsValidTreeDropPoint(clampedCenter))
        {
            dropPoint = clampedCenter;
            return true;
        }

        var sampleCount = Mathf.Max(8, RecoverySearchAngleSamples);
        var radiusStep = Mathf.Max(8.0f, RecoverySearchStep);
        var maxRadius = Mathf.Max(radiusStep, TreeDropMaxRadius);

        for (var radius = radiusStep; radius <= maxRadius; radius += radiusStep)
        {
            for (var i = 0; i < sampleCount; i += 1)
            {
                var angle = Mathf.Tau * i / sampleCount;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var candidate = ClampInsidePlayableBounds(center + offset);

                if (!IsValidTreeDropPoint(candidate))
                {
                    continue;
                }

                dropPoint = candidate;
                return true;
            }
        }

        return false;
    }

    private Vector2 FindRecoveryPosition(Vector2 hazardEntryPosition)
    {
        if (_ball == null || _hole == null)
        {
            return hazardEntryPosition;
        }

        if (IsSafeRecoveryPoint(_previousShotPosition))
        {
            return _previousShotPosition;
        }

        if (IsSafeRecoveryPoint(_lastSafePosition))
        {
            return _lastSafePosition;
        }

        if (TrySearchAround(_previousShotPosition, out var nearPrevious))
        {
            return nearPrevious;
        }

        if (TrySearchAround(_lastSafePosition, out var nearLastSafe))
        {
            return nearLastSafe;
        }

        if (TrySearchAround(hazardEntryPosition, out var nearEntry))
        {
            return nearEntry;
        }

        if (TrySearchAround(_hole.TeePosition, out var nearTee))
        {
            return nearTee;
        }

        return _hole.TeePosition;
    }

    private bool TrySearchAround(Vector2 center, out Vector2 safePoint, float maxRadiusOverride = -1.0f)
    {
        safePoint = ClampInsidePlayableBounds(center);

        var clampedCenter = ClampInsidePlayableBounds(center);
        if (IsSafeRecoveryPoint(clampedCenter))
        {
            safePoint = clampedCenter;
            return true;
        }

        var sampleCount = Mathf.Max(8, RecoverySearchAngleSamples);
        var radiusStep = Mathf.Max(8.0f, RecoverySearchStep);
        var configuredMaxRadius = maxRadiusOverride > 0.0f ? maxRadiusOverride : RecoverySearchMaxRadius;
        var maxRadius = Mathf.Max(radiusStep, configuredMaxRadius);

        for (var radius = radiusStep; radius <= maxRadius; radius += radiusStep)
        {
            for (var i = 0; i < sampleCount; i += 1)
            {
                var angle = Mathf.Tau * i / sampleCount;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var candidate = ClampInsidePlayableBounds(center + offset);

                if (!IsSafeRecoveryPoint(candidate))
                {
                    continue;
                }

                safePoint = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsSafeRecoveryPoint(Vector2 point)
    {
        if (_lieEvaluator == null)
        {
            return false;
        }

        point = ClampInsidePlayableBounds(point);

        var terrain = _lieEvaluator.EvaluateTerrainAtPosition(point);
        if (!IsSafeTerrain(terrain))
        {
            return false;
        }

        return !IsBlockedByCollision(point);
    }

    private bool IsValidTreeDropPoint(Vector2 point)
    {
        if (_lieEvaluator == null || _hole == null)
        {
            return false;
        }

        point = ClampInsidePlayableBounds(point);

        if (!IsSafeRecoveryPoint(point))
        {
            return false;
        }

        return !_lieEvaluator.IsObstructedTreeLie(point, _hole.CupPosition);
    }

    private bool IsSafeTerrain(TerrainType terrainType)
    {
        if (terrainType == TerrainType.Water || terrainType == TerrainType.OutOfBounds)
        {
            return false;
        }

        return !TerrainDatabase.GetProperties(terrainType).IsHazard;
    }

    private bool IsBlockedByCollision(Vector2 point)
    {
        if (_ball == null)
        {
            return true;
        }

        var world2D = _ball.GetWorld2D();
        var space = world2D?.DirectSpaceState;
        if (space == null)
        {
            return false;
        }

        var shape = new CircleShape2D();
        shape.Radius = Mathf.Max(1.0f, _ball.Radius * RecoveryCollisionRadiusScale);

        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0.0f, point),
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = uint.MaxValue
        };

        query.Exclude = new Array<Rid> { _ball.GetRid() };

        var results = space.IntersectShape(query, 8);
        return results.Count > 0;
    }

    private Vector2 EnsureSafeRecoveryPosition(Vector2 preferredPoint, Vector2 hazardEntryPosition)
    {
        var clampedPreferred = ClampInsidePlayableBounds(preferredPoint);
        if (IsSafeRecoveryPoint(clampedPreferred))
        {
            return clampedPreferred;
        }

        if (TrySearchAround(clampedPreferred, out var nearPreferred, RecoverySearchMaxRadius * 1.5f))
        {
            return nearPreferred;
        }

        var clampedPrevious = ClampInsidePlayableBounds(_previousShotPosition);
        if (TrySearchAround(clampedPrevious, out var nearPrevious, RecoverySearchMaxRadius * 1.5f))
        {
            return nearPrevious;
        }

        var clampedLastSafe = ClampInsidePlayableBounds(_lastSafePosition);
        if (TrySearchAround(clampedLastSafe, out var nearLastSafe, RecoverySearchMaxRadius * 1.5f))
        {
            return nearLastSafe;
        }

        var clampedEntry = ClampInsidePlayableBounds(hazardEntryPosition);
        if (TrySearchAround(clampedEntry, out var nearEntry, RecoverySearchMaxRadius * 1.5f))
        {
            return nearEntry;
        }

        if (_hole != null)
        {
            var tee = ClampInsidePlayableBounds(_hole.TeePosition);
            if (TrySearchAround(tee, out var nearTee, RecoverySearchMaxRadius * 2.0f))
            {
                return nearTee;
            }

            return tee;
        }

        return clampedPreferred;
    }

    private Vector2 ClampInsidePlayableBounds(Vector2 point)
    {
        if (_hole == null || _ball == null)
        {
            return point;
        }

        var bounds = _hole.GetCourseBounds();
        var inset = Mathf.Max(2.0f, _ball.Radius + 1.0f);
        var min = bounds.Position + new Vector2(inset, inset);
        var max = bounds.End - new Vector2(inset, inset);

        if (max.X < min.X || max.Y < min.Y)
        {
            return point;
        }

        return new Vector2(
            Mathf.Clamp(point.X, min.X, max.X),
            Mathf.Clamp(point.Y, min.Y, max.Y));
    }
}
