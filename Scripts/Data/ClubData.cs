using System;

public enum ClubType
{
    Driver,
    Iron,
    Wedge,
    Putter
}

[Serializable]
public class ClubData
{
    public string Id { get; set; } = string.Empty;
    public ClubType ClubType { get; set; } = ClubType.Driver;
    public string Name { get; set; } = "Club";
    public float MinPower { get; set; } = 100.0f;
    public float MaxPower { get; set; } = 300.0f;
    public float FrictionMultiplier { get; set; } = 1.0f;
    public float AimGuideScale { get; set; } = 0.35f;
    public float LoftFactor { get; set; } = 0.45f;
    public float ShotDispersionDegrees { get; set; } = 1.2f;

    public ClubData Clone()
    {
        return new ClubData
        {
            Id = Id,
            ClubType = ClubType,
            Name = Name,
            MinPower = MinPower,
            MaxPower = MaxPower,
            FrictionMultiplier = FrictionMultiplier,
            AimGuideScale = AimGuideScale,
            LoftFactor = LoftFactor,
            ShotDispersionDegrees = ShotDispersionDegrees
        };
    }
}
