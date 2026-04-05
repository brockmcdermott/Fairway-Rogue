using System;
using Godot;

public partial class ClubController : Node
{
    private static readonly ClubType[] ClubOrder =
    {
        ClubType.Driver,
        ClubType.Iron,
        ClubType.Wedge,
        ClubType.Putter
    };

    public int SelectedClubIndex { get; private set; }
    public ClubType CurrentClubType => ClubOrder[Mathf.Clamp(SelectedClubIndex, 0, ClubOrder.Length - 1)];
    public ClubData CurrentClub => ResolveCurrentClubData();

    public event Action<ClubData>? ClubChanged;

    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");

    public override void _Ready()
    {
        EmitClubChanged();
    }

    public ClubData SelectNextClub()
    {
        SelectedClubIndex = (SelectedClubIndex + 1) % ClubOrder.Length;
        EmitClubChanged();
        return CurrentClub;
    }

    public ClubData SelectPreviousClub()
    {
        SelectedClubIndex = (SelectedClubIndex - 1 + ClubOrder.Length) % ClubOrder.Length;
        EmitClubChanged();
        return CurrentClub;
    }

    public void RefreshCurrentClubData()
    {
        EmitClubChanged();
    }

    private ClubData ResolveCurrentClubData()
    {
        var runManager = RunManagerSingleton;
        if (runManager != null)
        {
            return runManager.GetEffectiveClubData(CurrentClubType);
        }

        return ProgressionCatalog.GetBaseClub(CurrentClubType);
    }

    private void EmitClubChanged()
    {
        ClubChanged?.Invoke(CurrentClub);
    }
}
