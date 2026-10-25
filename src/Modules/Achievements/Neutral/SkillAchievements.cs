using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class BlankIntercepted : SingleUnlockAchievement<BlankIntercepted>
{
    public override int Id => 302901;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";
}
public sealed class PlayerSwallowed : SingleUnlockAchievement<PlayerSwallowed>
{
    public override int Id => 301101;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";
}
public sealed class MarioHalfway : SingleUnlockAchievement<MarioHalfway>
{
    public override int Id => 300901;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";
}
public sealed class CatConverted : SingleUnlockAchievement<CatConverted>
{
    public override int Id => 302201;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";
}
public sealed class IdentitySwapped : SingleUnlockAchievement<IdentitySwapped>
{
    public override int Id => 302401;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";
}
