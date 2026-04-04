public readonly struct TerrainProperties
{
    public TerrainProperties(float frictionMultiplier, float powerMultiplier, float accuracyPenaltyDegrees, bool isHazard)
    {
        FrictionMultiplier = frictionMultiplier;
        PowerMultiplier = powerMultiplier;
        AccuracyPenaltyDegrees = accuracyPenaltyDegrees;
        IsHazard = isHazard;
    }

    public float FrictionMultiplier { get; }
    public float PowerMultiplier { get; }
    public float AccuracyPenaltyDegrees { get; }
    public bool IsHazard { get; }
}

public static class TerrainDatabase
{
    public static TerrainProperties GetProperties(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Tee:
                return new TerrainProperties(frictionMultiplier: 1.00f, powerMultiplier: 1.00f, accuracyPenaltyDegrees: 0.0f, isHazard: false);
            case TerrainType.Fairway:
                return new TerrainProperties(frictionMultiplier: 1.00f, powerMultiplier: 1.00f, accuracyPenaltyDegrees: 0.0f, isHazard: false);
            case TerrainType.Rough:
                return new TerrainProperties(frictionMultiplier: 1.35f, powerMultiplier: 0.90f, accuracyPenaltyDegrees: 3.0f, isHazard: false);
            case TerrainType.Sand:
                return new TerrainProperties(frictionMultiplier: 1.85f, powerMultiplier: 0.70f, accuracyPenaltyDegrees: 6.0f, isHazard: false);
            case TerrainType.Green:
                return new TerrainProperties(frictionMultiplier: 0.88f, powerMultiplier: 0.78f, accuracyPenaltyDegrees: 0.0f, isHazard: false);
            case TerrainType.Water:
                return new TerrainProperties(frictionMultiplier: 1.00f, powerMultiplier: 0.0f, accuracyPenaltyDegrees: 0.0f, isHazard: true);
            case TerrainType.Trees:
                return new TerrainProperties(frictionMultiplier: 1.15f, powerMultiplier: 0.80f, accuracyPenaltyDegrees: 10.0f, isHazard: true);
            case TerrainType.OutOfBounds:
                return new TerrainProperties(frictionMultiplier: 1.00f, powerMultiplier: 0.0f, accuracyPenaltyDegrees: 0.0f, isHazard: true);
            default:
                return new TerrainProperties(frictionMultiplier: 1.00f, powerMultiplier: 1.00f, accuracyPenaltyDegrees: 0.0f, isHazard: false);
        }
    }
}
