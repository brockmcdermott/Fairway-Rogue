using System;

[Serializable]
public class HoleResultData
{
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int Strokes { get; set; }

    public int ScoreRelativeToPar => Strokes - Par;
}
