using System;
using Godot;

public partial class ClubController : Node
{
    public readonly struct ClubDefinition
    {
        public ClubDefinition(string name, float minPower, float maxPower, float frictionMultiplier, float aimGuideScale)
        {
            Name = name;
            MinPower = minPower;
            MaxPower = maxPower;
            FrictionMultiplier = frictionMultiplier;
            AimGuideScale = aimGuideScale;
        }

        public string Name { get; }
        public float MinPower { get; }
        public float MaxPower { get; }
        public float FrictionMultiplier { get; }
        public float AimGuideScale { get; }
    }

    // Core tuning values for club identity. Keep these simple and easy to tweak.
    private static readonly ClubDefinition[] Clubs =
    {
        new ClubDefinition("Driver", minPower: 220.0f, maxPower: 620.0f, frictionMultiplier: 0.95f, aimGuideScale: 0.42f),
        new ClubDefinition("Iron", minPower: 170.0f, maxPower: 470.0f, frictionMultiplier: 1.00f, aimGuideScale: 0.38f),
        new ClubDefinition("Wedge", minPower: 120.0f, maxPower: 320.0f, frictionMultiplier: 1.08f, aimGuideScale: 0.34f),
        new ClubDefinition("Putter", minPower: 60.0f, maxPower: 170.0f, frictionMultiplier: 1.18f, aimGuideScale: 0.28f)
    };

    public int SelectedClubIndex { get; private set; }
    public ClubDefinition CurrentClub => Clubs[SelectedClubIndex];

    public event Action<ClubDefinition>? ClubChanged;

    public override void _Ready()
    {
        EmitClubChanged();
    }

    public ClubDefinition SelectNextClub()
    {
        SelectedClubIndex = (SelectedClubIndex + 1) % Clubs.Length;
        EmitClubChanged();
        return CurrentClub;
    }

    public ClubDefinition SelectPreviousClub()
    {
        SelectedClubIndex = (SelectedClubIndex - 1 + Clubs.Length) % Clubs.Length;
        EmitClubChanged();
        return CurrentClub;
    }

    private void EmitClubChanged()
    {
        ClubChanged?.Invoke(CurrentClub);
    }
}
