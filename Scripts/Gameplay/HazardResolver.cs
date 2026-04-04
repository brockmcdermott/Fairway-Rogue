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

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");

    public Vector2 PreviousShotPosition => _previousShotPosition;
    public Vector2 LastSafePosition => _lastSafePosition;
    public bool IsRecoveryPromptActive => _recoveryPromptActive;

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

        _hud.SetStatusMessage("Play from lie selected. No penalty.");
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
        var dropPosition = FindTreeDropPosition(_ball.GlobalPosition);
        _ball.ResetAt(dropPosition);

        var droppedLie = _lieEvaluator.EvaluateLie(dropPosition);
        _ball.SetTerrain(droppedLie);
        _lastSafePosition = dropPosition;

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

        if (_lieEvaluator.IsObstructedTreeLie(_ball.GlobalPosition, _hole.CupPosition))
        {
            BeginTreeRecoveryPrompt();
            return;
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

        if (terrainType != TerrainType.Water && terrainType != TerrainType.OutOfBounds)
        {
            return;
        }

        _isResolvingHazard = true;
        _shotController.SetInputEnabled(false);

        _ball.StopBall();
        _hole.AddPenaltyStroke(1);

        var recoveryPosition = FindRecoveryPosition(entryPosition);
        _ball.ResetAt(recoveryPosition);

        var recoveredLie = _lieEvaluator.EvaluateLie(recoveryPosition);
        _ball.SetTerrain(recoveredLie);
        _lastSafePosition = recoveryPosition;

        _hud.SetStrokeCount(_hole.LocalStrokeCount);

        var hazardLabel = terrainType == TerrainType.Water ? "Water" : "Out of Bounds";
        _hud.SetStatusMessage($"{hazardLabel}: +1 penalty stroke. Repositioned to a safe lie.");

        _isResolvingHazard = false;
        _shotController.SetInputEnabled(true);
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

    private bool TrySearchForTreeDropAround(Vector2 center, out Vector2 dropPoint)
    {
        dropPoint = center;

        if (IsValidTreeDropPoint(center))
        {
            dropPoint = center;
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
                var candidate = center + offset;

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
        safePoint = center;

        if (IsSafeRecoveryPoint(center))
        {
            safePoint = center;
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
                var candidate = center + offset;

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
}
