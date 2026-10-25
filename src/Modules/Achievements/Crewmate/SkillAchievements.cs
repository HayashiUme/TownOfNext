using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class LuckyEscape : SingleUnlockAchievement<LuckyEscape>
{
    public override int Id => 200201;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override string Category => "Crewmate";
}
public sealed class StarProtected : SingleUnlockAchievement<StarProtected>
{
    public override int Id => 200401;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override string Category => "Crewmate";
}
public sealed class SpeedIncreased : SingleUnlockAchievement<SpeedIncreased>
{
    public override int Id => 201201;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override string Category => "Crewmate";
}
public sealed class PlayersTransported : SingleUnlockAchievement<PlayersTransported>
{
    public override int Id => 201701;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override string Category => "Crewmate";
}
