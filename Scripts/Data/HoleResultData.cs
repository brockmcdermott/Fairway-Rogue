using System;

[Serializable]
public class HoleResultData
{
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int Strokes { get; set; }

    public int ScoreRelativeToPar => Strokes - Par;
    public string Label => GetScoreLabel(ScoreRelativeToPar);
    public string RelativeScoreText => FormatRelativeScore(ScoreRelativeToPar);

    public static string GetScoreLabel(int scoreRelativeToPar)
    {
        switch (scoreRelativeToPar)
        {
            case -2:
                return "Eagle";
            case -1:
                return "Birdie";
            case 0:
                return "Par";
            case 1:
                return "Bogey";
            case 2:
                return "Double Bogey";
            default:
                if (scoreRelativeToPar < -2)
                {
                    return $"{Math.Abs(scoreRelativeToPar)} Under Par";
                }

                return $"{scoreRelativeToPar} Over Par";
        }
    }

    public static string FormatRelativeScore(int scoreRelativeToPar)
    {
        if (scoreRelativeToPar == 0)
        {
            return "E";
        }

        return scoreRelativeToPar > 0 ? $"+{scoreRelativeToPar}" : scoreRelativeToPar.ToString();
    }
}
