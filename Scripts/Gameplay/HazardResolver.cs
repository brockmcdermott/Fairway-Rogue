using Godot;
using Godot.Collections;

public partial class HazardResolver : Node
{
    [ExportGroup("Recovery Search")]
    [Export] public float RecoverySearchStep { get; set; } = 24.0f;
    [Export] public float RecoverySearchMaxRadius { get; set; } = 220.0f;
    [Export] public int RecoverySearchAngleSamples { get; set; } = 20;
    [Export] public float RecoveryCollisionRadiusScale { get; set; } = 0.9f;

    private BallController? _ball;
    private HoleController? _hole;
    private ShotController? _shotController;
    private HUDController? _hud;
    private LieEvaluator? _lieEvaluator;

    private bool _isResolvingHazard;
    private Vector2 _previousShotPosition = Vector2.Zero;
    private Vector2 _lastSafePosition = Vector2.Zero;

    public Vector2 PreviousShotPosition => _previousShotPosition;
    public Vector2 LastSafePosition => _lastSafePosition;

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

        ball.ShotLaunched += OnShotLaunched;
        ball.TerrainChanged += OnTerrainChanged;
        ball.HazardEntered += OnHazardEntered;
    }

    public override void _ExitTree()
    {
        DisconnectBallSignals();
        base._ExitTree();
    }

    private void DisconnectBallSignals()
    {
        if (_ball == null)
        {
            return;
        }

        _ball.ShotLaunched -= OnShotLaunched;
        _ball.TerrainChanged -= OnTerrainChanged;
        _ball.HazardEntered -= OnHazardEntered;
    }

    private void OnShotLaunched(Vector2 shotStartPosition)
    {
        _previousShotPosition = shotStartPosition;
        _lastSafePosition = shotStartPosition;
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
        if (_isResolvingHazard || _ball == null || _hole == null || _shotController == null || _hud == null || _lieEvaluator == null || _hole.IsHoleComplete)
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

    private bool TrySearchAround(Vector2 center, out Vector2 safePoint)
    {
        safePoint = center;

        if (IsSafeRecoveryPoint(center))
        {
            safePoint = center;
            return true;
        }

        var sampleCount = Mathf.Max(8, RecoverySearchAngleSamples);
        var radiusStep = Mathf.Max(8.0f, RecoverySearchStep);
        var maxRadius = Mathf.Max(radiusStep, RecoverySearchMaxRadius);

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
