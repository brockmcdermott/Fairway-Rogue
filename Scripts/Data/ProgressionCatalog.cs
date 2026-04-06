using System.Collections.Generic;

public static class ProgressionCatalog
{
    public const string DefaultBallId = "ball_all_around";

    // Keep progression tuning centralized here so iteration is fast and safe.
    public static readonly IReadOnlyList<ClubData> BaseClubs = new List<ClubData>
    {
        new ClubData
        {
            Id = "club_driver",
            ClubType = ClubType.Driver,
            Name = "Driver",
            MinPower = 220.0f,
            MaxPower = 620.0f,
            FrictionMultiplier = 0.95f,
            AimGuideScale = 0.42f,
            LoftFactor = 0.32f,
            ShotDispersionDegrees = 2.4f
        },
        new ClubData
        {
            Id = "club_iron",
            ClubType = ClubType.Iron,
            Name = "Iron",
            MinPower = 170.0f,
            MaxPower = 470.0f,
            FrictionMultiplier = 1.00f,
            AimGuideScale = 0.38f,
            LoftFactor = 0.56f,
            ShotDispersionDegrees = 1.7f
        },
        new ClubData
        {
            Id = "club_wedge",
            ClubType = ClubType.Wedge,
            Name = "Wedge",
            MinPower = 120.0f,
            MaxPower = 320.0f,
            FrictionMultiplier = 1.08f,
            AimGuideScale = 0.34f,
            LoftFactor = 0.84f,
            ShotDispersionDegrees = 1.4f
        },
        new ClubData
        {
            Id = "club_putter",
            ClubType = ClubType.Putter,
            Name = "Putter",
            MinPower = 60.0f,
            MaxPower = 170.0f,
            FrictionMultiplier = 1.18f,
            AimGuideScale = 0.28f,
            LoftFactor = 0.04f,
            ShotDispersionDegrees = 0.25f
        }
    };

    public static readonly IReadOnlyList<BallData> BallVariants = new List<BallData>
    {
        new BallData
        {
            Id = "ball_all_around",
            Name = "All-Around Ball",
            Description = "Balanced distance and control.",
            DistanceMultiplier = 1.00f,
            ControlFrictionMultiplier = 1.00f
        },
        new BallData
        {
            Id = "ball_distance",
            Name = "Distance Ball",
            Description = "Longer shots with more rollout and less control.",
            DistanceMultiplier = 1.12f,
            ControlFrictionMultiplier = 0.90f
        },
        new BallData
        {
            Id = "ball_control",
            Name = "Control Ball",
            Description = "Shorter shots with tighter stopping control.",
            DistanceMultiplier = 0.92f,
            ControlFrictionMultiplier = 1.15f
        }
    };

    public static readonly IReadOnlyList<UpgradeData> Upgrades = new List<UpgradeData>
    {
        new UpgradeData
        {
            Id = "driver_power_1",
            Name = "Driver Power I",
            Description = "Increase driver launch range.",
            Cost = 20,
            Kind = UpgradeKind.ClubModifier,
            TargetClubType = ClubType.Driver,
            MinPowerDelta = 22.0f,
            MaxPowerDelta = 58.0f
        },
        new UpgradeData
        {
            Id = "driver_power_2",
            Name = "Driver Power II",
            Description = "Further boost driver launch range.",
            Cost = 34,
            Kind = UpgradeKind.ClubModifier,
            TargetClubType = ClubType.Driver,
            MinPowerDelta = 26.0f,
            MaxPowerDelta = 64.0f,
            RequiredUpgradeIds = new List<string> { "driver_power_1" }
        },
        new UpgradeData
        {
            Id = "iron_power_1",
            Name = "Iron Power I",
            Description = "Increase iron shot distance.",
            Cost = 18,
            Kind = UpgradeKind.ClubModifier,
            TargetClubType = ClubType.Iron,
            MinPowerDelta = 18.0f,
            MaxPowerDelta = 40.0f
        },
        new UpgradeData
        {
            Id = "wedge_precision_1",
            Name = "Wedge Control I",
            Description = "Improve wedge control and stop speed.",
            Cost = 16,
            Kind = UpgradeKind.ClubModifier,
            TargetClubType = ClubType.Wedge,
            FrictionMultiplierDelta = 0.08f
        },
        new UpgradeData
        {
            Id = "putter_touch_1",
            Name = "Putter Touch I",
            Description = "Improve putter pace control and consistency.",
            Cost = 15,
            Kind = UpgradeKind.ClubModifier,
            TargetClubType = ClubType.Putter,
            MinPowerDelta = 8.0f,
            MaxPowerDelta = 16.0f,
            FrictionMultiplierDelta = 0.06f
        },
        new UpgradeData
        {
            Id = "unlock_ball_distance",
            Name = "Unlock Distance Ball",
            Description = "Unlock and equip the high-distance ball.",
            Cost = 24,
            Kind = UpgradeKind.BallUnlock,
            UnlockBallId = "ball_distance"
        },
        new UpgradeData
        {
            Id = "unlock_ball_control",
            Name = "Unlock Control Ball",
            Description = "Unlock and equip the high-control ball.",
            Cost = 24,
            Kind = UpgradeKind.BallUnlock,
            UnlockBallId = "ball_control"
        }
    };

    public static ClubData GetBaseClub(ClubType clubType)
    {
        for (var i = 0; i < BaseClubs.Count; i += 1)
        {
            if (BaseClubs[i].ClubType == clubType)
            {
                return BaseClubs[i].Clone();
            }
        }

        return BaseClubs[0].Clone();
    }

    public static BallData GetBall(string ballId)
    {
        for (var i = 0; i < BallVariants.Count; i += 1)
        {
            if (BallVariants[i].Id == ballId)
            {
                return BallVariants[i].Clone();
            }
        }

        return BallVariants[0].Clone();
    }

    public static UpgradeData? GetUpgrade(string upgradeId)
    {
        for (var i = 0; i < Upgrades.Count; i += 1)
        {
            if (Upgrades[i].Id == upgradeId)
            {
                return Upgrades[i];
            }
        }

        return null;
    }
}
