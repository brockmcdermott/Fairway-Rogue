using System.Collections.Generic;
using Godot;

public partial class RunManager : Node
{
    public int CurrentHoleIndex { get; private set; } = 1;
    public int TotalHoles { get; private set; } = 9;
    public int Currency { get; private set; }
    public List<HoleResultData> HoleResults { get; private set; } = new();
    public int TotalStrokes { get; private set; }
    public int TotalPar { get; private set; }
    public int Seed { get; private set; }

    public void StartRun(int seed)
    {
        Seed = seed;
        CurrentHoleIndex = 1;
        TotalHoles = 9;
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
        HoleResults.Add(result);
        TotalPar += result.Par;
    }

    public bool IsFinalHole()
    {
        return CurrentHoleIndex >= TotalHoles;
    }

    public void AdvanceToNextHole()
    {
        if (!IsFinalHole())
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
        TotalHoles = data.TotalHoles > 0 ? data.TotalHoles : 9;
        Currency = Mathf.Max(data.Currency, 0);
        TotalStrokes = Mathf.Max(data.TotalStrokes, 0);
        TotalPar = Mathf.Max(data.TotalPar, 0);
        HoleResults = data.HoleResults ?? new List<HoleResultData>();
    }
}
