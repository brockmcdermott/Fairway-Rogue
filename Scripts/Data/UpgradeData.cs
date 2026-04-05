using System;
using System.Collections.Generic;

public enum UpgradeKind
{
    ClubModifier,
    BallUnlock
}

[Serializable]
public class UpgradeData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Upgrade";
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; } = 10;
    public UpgradeKind Kind { get; set; } = UpgradeKind.ClubModifier;

    public ClubType TargetClubType { get; set; } = ClubType.Driver;
    public float MinPowerDelta { get; set; }
    public float MaxPowerDelta { get; set; }
    public float FrictionMultiplierDelta { get; set; }

    public string UnlockBallId { get; set; } = string.Empty;
    public List<string> RequiredUpgradeIds { get; set; } = new List<string>();

    public bool AffectsClub(ClubType clubType)
    {
        return Kind == UpgradeKind.ClubModifier && TargetClubType == clubType;
    }

    public ClubData ApplyToClub(ClubData source)
    {
        if (!AffectsClub(source.ClubType))
        {
            return source.Clone();
        }

        var upgraded = source.Clone();
        upgraded.MinPower += MinPowerDelta;
        upgraded.MaxPower += MaxPowerDelta;
        upgraded.FrictionMultiplier = MathF.Max(0.1f, upgraded.FrictionMultiplier + FrictionMultiplierDelta);
        return upgraded;
    }
}
