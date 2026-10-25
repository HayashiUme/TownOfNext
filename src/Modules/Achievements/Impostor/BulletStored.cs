using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class BulletStored : SingleUnlockAchievement<BulletStored>
{
    public override int Id => 103301;
    public override Color TitleColor => new Color32(0xFF, 0x19, 0x19, 0xFF);
    public override string Category => "Impostor";
}
