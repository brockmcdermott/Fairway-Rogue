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
            HoleResults = new List<HoleResultData>(HoleResults)
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
}
