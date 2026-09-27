using System.Text;
using TMPro;
using TONX.Roles.GameMode;
using UnityEngine;

namespace TONX.GameModes;

public sealed class HotPotato : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(HotPotato),
            () => new HotPotato(),
            CustomGameMode.HotPotato,
            40_000_000,
            SetupCustomOption,
            "#f55252",
            () => $"<color=#f55252><size=1.7>{GetString("ModeHotPotato")}</size></color>",
            (true, false)
        );

    public HotPotato() : base(ModeInfo)
    { }

    public const float ModeSpeed = 1.75f;

    public static OptionItem HotPotatoMaxNum;
    public static OptionItem ExplosionTotalTime;

    public static int RemainExplosionTime;
    public static int HotPotatoMax;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(ModeInfo, 100_001, "MenuTitle.GameMode");
        HotPotatoMaxNum = IntegerOptionItem.Create(ModeInfo, 1, "HotPotatoMaxNum", new(1, 64, 1), 2, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        ExplosionTotalTime = IntegerOptionItem.Create(ModeInfo, 2, "ExplosionTotalTime", new(10, 60, 5), 15, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        RemainExplosionTime = ExplosionTotalTime.GetInt() + 9;
        HotPotatoMax = HotPotatoMaxNum.GetInt();
    }

    public override AvailableRolesData AddAvailableRoles() => default;
    public override bool ShouldAssignAddons() => false;
    public override bool ShouldRandomSpawn() => true;
    public override void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult, ref AvailableRolesData data)
    {
        foreach (var pc in Main.AllAlivePlayerControls)
            RoleResult.Add(pc, CustomRoles.ColdPotato);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => false;

    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost || player != PlayerControl.LocalPlayer) return;

        if (RemainExplosionTime > 0)
        {
            RemainExplosionTime--;
            if (RemainExplosionTime > 0)
            {
                PotatoBase.SyncExplosionTime(RemainExplosionTime);
                Utils.NotifyRoles();
                return;
            }
        }
        Explode();
    }

    private static void Explode()
    {
        HotPotatoMax = GetMaxPotatoCount();

        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.HotPotato)).ToList())
            pc.RpcMurderPlayerV2(pc);

        var candidates = Main.AllAlivePlayerControls.Where(x => !x.Is(CustomRoles.HotPotato)).ToList();
        for (var i = 0; i < HotPotatoMax && candidates.Count > 0; i++)
        {
            var target = candidates[IRandom.Instance.Next(0, candidates.Count)];
            candidates.Remove(target);

            target.RpcChangeRole(CustomRoles.HotPotato, refreshSeen: false);
            target.Notify(GetString("GetHotPotato"), 1f);
        }

        _ = new LateTask(() =>
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.HotPotato)))
                pc.SetKillCooldownV2(0f);
        }, 0.1f, "HotPotato");

        RemainExplosionTime = ExplosionTotalTime.GetInt();
        PotatoBase.SyncExplosionTime(RemainExplosionTime);
        Utils.NotifyRoles();
    }

    /// <summary>
    /// 存活人数过少时削减同时存在的热土豆上限
    /// </summary>
    public static int GetMaxPotatoCount()
    {
        var max = HotPotatoMaxNum.GetInt();
        var alive = Main.AllAlivePlayerControls.Count();

        if (alive is >= 9 and <= 11 && max >= 3) max--;
        else if (alive is >= 5 and <= 7 && max >= 2) max--;

        if (alive <= max + 1) max = 1;
        return Math.Max(max, 1);
    }

    public override void EditIntroFormat(ref IntroCutscene intro)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        intro.TeamTitle.text = Utils.GetRoleName(role);
        intro.TeamTitle.color = Utils.GetRoleColor(role);
        intro.ImpostorText.gameObject.SetActive(true);
        intro.ImpostorText.text = GetString("ModeHotPotato");
        intro.BackgroundBar.material.color = ModeInfo.ModeColor;
        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx;
    }
    public override void EditOutroFormat(ref EndGameManager outro, ref TextMeshPro winnerText, ref string cwText, ref StringBuilder awText, ref string cwColor)
    {
        var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
        outro.WinText.text = Main.AllPlayerNames[winnerId] + GetString("Win");
        outro.WinText.fontSize -= 5f;
        outro.WinText.color = Main.PlayerColors[winnerId];
        outro.BackgroundBar.material.color = ModeInfo.ModeColor;
        winnerText.text = $"<color=#ff9900>{GetString("ModeHotPotato")}</color>";
        winnerText.color = ModeInfo.ModeColor;
    }

    public override void AfterCheckForGameEnd(GameOverReason reason, ref GameEndPredicate predicate)
    {
        if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            ShipStatus.Instance.enabled = false;
            GameEndChecker.StartEndGame(reason);
            predicate = null;
        }
    }
    public override GameEndPredicate Predicate() => new HotPotatoGameEndPredicate();

    private sealed class HotPotatoGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            var alive = Main.AllAlivePlayerControls.ToList();
            if (alive.Count > 1) return false;

            if (alive.Count == 1) CustomWinnerHolder.WinnerIds = new() { alive[0].PlayerId };
            else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            Main.DoBlockNameChange = true;
            return true;
        }
    }
}
