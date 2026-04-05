using System;
using System.Collections.Generic;

[Serializable]
public class PlayerLoadout
{
    public string ActiveBallId { get; set; } = ProgressionCatalog.DefaultBallId;
    public List<string> UnlockedBallIds { get; set; } = new List<string> { ProgressionCatalog.DefaultBallId };
    public List<string> PurchasedUpgradeIds { get; set; } = new List<string>();

    public static PlayerLoadout CreateDefault()
    {
        return new PlayerLoadout
        {
            ActiveBallId = ProgressionCatalog.DefaultBallId,
            UnlockedBallIds = new List<string> { ProgressionCatalog.DefaultBallId },
            PurchasedUpgradeIds = new List<string>()
        };
    }

    public PlayerLoadout Clone()
    {
        return new PlayerLoadout
        {
            ActiveBallId = ActiveBallId,
            UnlockedBallIds = new List<string>(UnlockedBallIds),
            PurchasedUpgradeIds = new List<string>(PurchasedUpgradeIds)
        };
    }
}
