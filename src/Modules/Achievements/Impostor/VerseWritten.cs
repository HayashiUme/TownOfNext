using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class VerseWritten : SingleUnlockAchievement<VerseWritten>
{
    public override int Id => 104401;
    public override Color TitleColor => new Color32(0xFF, 0x19, 0x19, 0xFF);
    public override string Category => "Impostor";
}
