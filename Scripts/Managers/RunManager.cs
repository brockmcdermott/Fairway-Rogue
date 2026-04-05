using System.Collections.Generic;
using Godot;

public partial class RunManager : Node
{
    public const int DefaultTotalHoles = 9;

    public int CurrentHoleIndex { get; private set; } = 1;
    public int TotalHoles { get; private set; } = DefaultTotalHoles;
    public int Currency { get; private set; }
    public List<HoleResultData> HoleResults { get; private set; } = new();
    public int TotalStrokes { get; private set; }
    public int TotalPar { get; private set; }
    public int Seed { get; private set; }
    public PlayerLoadout CurrentLoadout { get; private set; } = PlayerLoadout.CreateDefault();
    public int HolesCompleted => HoleResults.Count;

    public void StartRun(int seed, int totalHoles = DefaultTotalHoles)
    {
        Seed = seed;
        CurrentHoleIndex = 1;
        TotalHoles = Mathf.Max(1, totalHoles);
        Currency = 0;
        TotalStrokes = 0;
        TotalPar = 0;
        HoleResults = new List<HoleResultData>();
        CurrentLoadout = PlayerLoadout.CreateDefault();
    }

    public void AddStroke()
    {
        TotalStrokes += 1;
    }

    public void AwardCurrency(int amount)
    {
        Currency += Mathf.Max(amount, 0);
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= 0 || Currency < amount)
        {
            return false;
        }

        Currency -= amount;
        return true;
    }

    public int CalculateHoleReward(HoleResultData holeResult)
    {
        var par = Mathf.Max(1, holeResult.Par);
        var baseReward = 10 + par * 3;
        var performanceBonus = holeResult.ScoreRelativeToPar switch
        {
            <= -2 => 10,
            -1 => 6,
            0 => 3,
            1 => 1,
            _ => 0
        };

        var cleanHoleBonus = holeResult.Strokes <= holeResult.Par ? 2 : 0;
        var progressBonus = Mathf.Max(0, holeResult.HoleNumber - 1);
        return Mathf.Max(4, baseReward + performanceBonus + cleanHoleBonus + progressBonus);
    }

    public void RecordHoleResult(HoleResultData result)
    {
        if (result == null)
        {
            return;
        }

        var existingIndex = HoleResults.FindIndex(existing => existing.HoleNumber == result.HoleNumber);
        if (existingIndex >= 0)
        {
            HoleResults[existingIndex] = result;
        }
        else
        {
            HoleResults.Add(result);
        }

        HoleResults.Sort((left, right) => left.HoleNumber.CompareTo(right.HoleNumber));
        RecalculateTotals();
    }

    public bool IsFinalHole()
    {
        return CurrentHoleIndex >= TotalHoles;
    }

    public void AdvanceToNextHole()
    {
        if (CurrentHoleIndex < TotalHoles)
        {
            CurrentHoleIndex += 1;
        }
    }

    public int GetScoreRelativeToPar()
    {
        return TotalStrokes - TotalPar;
    }

    public BallData GetActiveBallData()
    {
        EnsureLoadoutIsValid();
        return ProgressionCatalog.GetBall(CurrentLoadout.ActiveBallId);
    }

    public IReadOnlyList<BallData> GetUnlockedBalls()
    {
        EnsureLoadoutIsValid();

        var unlocked = new List<BallData>();
        for (var i = 0; i < CurrentLoadout.UnlockedBallIds.Count; i += 1)
        {
            unlocked.Add(ProgressionCatalog.GetBall(CurrentLoadout.UnlockedBallIds[i]));
        }

        return unlocked;
    }

    public ClubData GetEffectiveClubData(ClubType clubType)
    {
        EnsureLoadoutIsValid();
        return BuildEffectiveClubData(clubType, additionalUpgrade: null, overrideBallId: null);
    }

    public IReadOnlyList<UpgradeData> GetAvailableUpgrades()
    {
        EnsureLoadoutIsValid();

        var available = new List<UpgradeData>();
        for (var i = 0; i < ProgressionCatalog.Upgrades.Count; i += 1)
        {
            var upgrade = ProgressionCatalog.Upgrades[i];
            if (IsUpgradePurchased(upgrade.Id))
            {
                continue;
            }

            if (!AreUpgradeRequirementsMet(upgrade))
            {
                continue;
            }

            available.Add(upgrade);
        }

        return available;
    }

    public bool IsUpgradePurchased(string upgradeId)
    {
        return CurrentLoadout.PurchasedUpgradeIds.Contains(upgradeId);
    }

    public bool SetActiveBall(string ballId, out string message)
    {
        EnsureLoadoutIsValid();

        if (!CurrentLoadout.UnlockedBallIds.Contains(ballId))
        {
            message = "That ball variant has not been unlocked yet.";
            return false;
        }

        var ball = ProgressionCatalog.GetBall(ballId);
        CurrentLoadout.ActiveBallId = ball.Id;
        message = $"Equipped {ball.Name}.";
        return true;
    }

    public bool TryPurchaseUpgrade(string upgradeId, out string message)
    {
        EnsureLoadoutIsValid();

        var upgrade = ProgressionCatalog.GetUpgrade(upgradeId);
        if (upgrade == null)
        {
            message = "Unknown upgrade.";
            return false;
        }

        if (IsUpgradePurchased(upgrade.Id))
        {
            message = "Upgrade already purchased.";
            return false;
        }

        if (!AreUpgradeRequirementsMet(upgrade))
        {
            message = "Upgrade requirement not met yet.";
            return false;
        }

        if (!SpendCurrency(upgrade.Cost))
        {
            message = "Not enough currency.";
            return false;
        }

        CurrentLoadout.PurchasedUpgradeIds.Add(upgrade.Id);

        if (upgrade.Kind == UpgradeKind.BallUnlock && !string.IsNullOrEmpty(upgrade.UnlockBallId))
        {
            if (!CurrentLoadout.UnlockedBallIds.Contains(upgrade.UnlockBallId))
            {
                CurrentLoadout.UnlockedBallIds.Add(upgrade.UnlockBallId);
            }

            CurrentLoadout.ActiveBallId = upgrade.UnlockBallId;
        }

        message = $"Purchased {upgrade.Name}.";
        return true;
    }

    public ClubData GetProjectedClubDataWithUpgrade(ClubType clubType, UpgradeData upgrade)
    {
        EnsureLoadoutIsValid();

        if (upgrade.Kind == UpgradeKind.ClubModifier)
        {
            return BuildEffectiveClubData(clubType, additionalUpgrade: upgrade, overrideBallId: null);
        }

        if (upgrade.Kind == UpgradeKind.BallUnlock && !string.IsNullOrEmpty(upgrade.UnlockBallId))
        {
            return BuildEffectiveClubData(clubType, additionalUpgrade: null, overrideBallId: upgrade.UnlockBallId);
        }

        return GetEffectiveClubData(clubType);
    }

    public RunSaveData BuildSaveData()
    {
        return new RunSaveData
        {
            Seed = Seed,
            CurrentHoleIndex = CurrentHoleIndex,
            TotalHoles = TotalHoles,
            Currency = Currency,
            TotalStrokes = TotalStrokes,
            TotalPar = TotalPar,
            HoleResults = new List<HoleResultData>(HoleResults),
            CurrentLoadout = CurrentLoadout.Clone()
        };
    }

    public void LoadFromSave(RunSaveData data)
    {
        Seed = data.Seed;
        CurrentHoleIndex = data.CurrentHoleIndex > 0 ? data.CurrentHoleIndex : 1;
        TotalHoles = data.TotalHoles > 0 ? data.TotalHoles : DefaultTotalHoles;
        Currency = Mathf.Max(data.Currency, 0);
        HoleResults = data.HoleResults ?? new List<HoleResultData>();
        HoleResults.Sort((left, right) => left.HoleNumber.CompareTo(right.HoleNumber));
        CurrentLoadout = data.CurrentLoadout?.Clone() ?? PlayerLoadout.CreateDefault();
        EnsureLoadoutIsValid();

        RecalculateTotals();
        CurrentHoleIndex = Mathf.Clamp(CurrentHoleIndex, 1, TotalHoles);
    }

    public HoleResultData? GetLatestHoleResult()
    {
        if (HoleResults.Count == 0)
        {
            return null;
        }

        return HoleResults[HoleResults.Count - 1];
    }

    private void RecalculateTotals()
    {
        var strokes = 0;
        var par = 0;

        for (var i = 0; i < HoleResults.Count; i += 1)
        {
            var result = HoleResults[i];
            strokes += Mathf.Max(0, result.Strokes);
            par += Mathf.Max(0, result.Par);
        }

        TotalStrokes = strokes;
        TotalPar = par;
    }

    private bool AreUpgradeRequirementsMet(UpgradeData upgrade)
    {
        for (var i = 0; i < upgrade.RequiredUpgradeIds.Count; i += 1)
        {
            if (!CurrentLoadout.PurchasedUpgradeIds.Contains(upgrade.RequiredUpgradeIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureLoadoutIsValid()
    {
        CurrentLoadout ??= PlayerLoadout.CreateDefault();
        CurrentLoadout.UnlockedBallIds ??= new List<string>();
        CurrentLoadout.PurchasedUpgradeIds ??= new List<string>();

        if (!CurrentLoadout.UnlockedBallIds.Contains(ProgressionCatalog.DefaultBallId))
        {
            CurrentLoadout.UnlockedBallIds.Add(ProgressionCatalog.DefaultBallId);
        }

        if (!CurrentLoadout.UnlockedBallIds.Contains(CurrentLoadout.ActiveBallId))
        {
            CurrentLoadout.ActiveBallId = ProgressionCatalog.DefaultBallId;
        }
    }

    private ClubData BuildEffectiveClubData(ClubType clubType, UpgradeData? additionalUpgrade, string? overrideBallId)
    {
        var club = ProgressionCatalog.GetBaseClub(clubType);

        for (var i = 0; i < CurrentLoadout.PurchasedUpgradeIds.Count; i += 1)
        {
            var upgrade = ProgressionCatalog.GetUpgrade(CurrentLoadout.PurchasedUpgradeIds[i]);
            if (upgrade == null || upgrade.Kind != UpgradeKind.ClubModifier)
            {
                continue;
            }

            club = upgrade.ApplyToClub(club);
        }

        if (additionalUpgrade != null && additionalUpgrade.Kind == UpgradeKind.ClubModifier)
        {
            club = additionalUpgrade.ApplyToClub(club);
        }

        var ballId = !string.IsNullOrEmpty(overrideBallId) ? overrideBallId : CurrentLoadout.ActiveBallId;
        var ball = ProgressionCatalog.GetBall(ballId);

        club.MinPower *= ball.DistanceMultiplier;
        club.MaxPower *= ball.DistanceMultiplier;
        club.FrictionMultiplier *= ball.ControlFrictionMultiplier;
        return club;
    }
}
