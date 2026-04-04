using System;
using System.Collections.Generic;

[Serializable]
public class RunSaveData
{
    public int Seed { get; set; }
    public int CurrentHoleIndex { get; set; } = 1;
    public int TotalHoles { get; set; } = 9;
    public int Currency { get; set; }
    public int TotalStrokes { get; set; }
    public int TotalPar { get; set; }
    public List<HoleResultData> HoleResults { get; set; } = new();
}
