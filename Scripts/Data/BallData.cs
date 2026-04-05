using System;

[Serializable]
public class BallData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Ball";
    public string Description { get; set; } = string.Empty;

    // Higher distance multiplier increases launch power for all clubs.
    public float DistanceMultiplier { get; set; } = 1.0f;

    // Higher control multiplier increases effective friction for tighter rollout control.
    public float ControlFrictionMultiplier { get; set; } = 1.0f;

    public BallData Clone()
    {
        return new BallData
        {
            Id = Id,
            Name = Name,
            Description = Description,
            DistanceMultiplier = DistanceMultiplier,
            ControlFrictionMultiplier = ControlFrictionMultiplier
        };
    }
}
