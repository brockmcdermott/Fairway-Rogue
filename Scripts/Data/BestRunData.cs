using System;

[Serializable]
public class BestRunData
{
    public int ScoreRelativeToPar { get; set; }
    public int TotalStrokes { get; set; }
    public int TotalPar { get; set; }
    public int HolesCompleted { get; set; }
    public int TotalHoles { get; set; }
    public int CurrencyEarned { get; set; }
    public long CompletedAtUnixSeconds { get; set; }
}
