using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Crewmate;
using TONX.Modules.Achievements.General;
using TONX.Modules.Achievements.Impostor;
using TONX.Modules.Achievements.Neutral;

internal static class DirectTriggerTests
{
    internal static void Run()
    {
        AmongUsClient.Instance = new() { AmHost = true };
        GameStates.IsInGame = true;
        GameStates.IsFreePlay = false;
        CustomAchievementDataManager.Clear();
        var host = new PlayerControl { PlayerId = 1, FriendCode = "host#0001" };
        var actor = new PlayerControl { PlayerId = 2, FriendCode = "actor#0002" };
        PlayerControl.LocalPlayer = host;
        CustomAchievementDataManager.SetLocalFriendCode(host.FriendCode);
        Main.AllPlayerControls = new() { host, actor };

        foreach (var (id, trigger) in new (int, Action<PlayerControl>)[]
        {
            (203401, VentLocked.Trigger), (202301, ShieldGranted.Trigger),
            (103301, BulletStored.Trigger), (104401, VerseWritten.Trigger),
            (300101, DouseCompleted.Trigger), (302001, BetPlaced.Trigger),
            (102301, TrackerTargetLocked.Trigger), (301201, DrawCompleted.Trigger),
            (302101, CharmCompleted.Trigger),
            (100101, BountyCompleted.Trigger), (100301, BiteCompleted.Trigger),
            (100401, CurseKillCompleted.Trigger), (100501, AssassinationCompleted.Trigger),
            (101401, SnipeCompleted.Trigger), (102801, BodyCleaned.Trigger),
            (103501, RoleErased.Trigger), (102701, MadmateRecruited.Trigger),
            (100801, MinerTeleported.Trigger), (100901, EscapistReturned.Trigger),
            (103201, SoulsSwapped.Trigger), (101001, MachineKillCompleted.Trigger),
            (101601, LightsOutKillCompleted.Trigger), (200201, LuckyEscape.Trigger),
            (200401, StarProtected.Trigger), (201201, SpeedIncreased.Trigger),
            (201701, PlayersTransported.Trigger), (302901, BlankIntercepted.Trigger),
            (301101, PlayerSwallowed.Trigger), (300901, MarioHalfway.Trigger),
            (302201, CatConverted.Trigger), (302401, IdentitySwapped.Trigger),
            (101201, MafiaRevengeKill.Trigger),
            (200801, SheriffImpostorKill.Trigger), (201401, DictatorForcedExile.Trigger),
            (202601, JudgeCorrectTrial.Trigger), (201601, VigilanteEvilKill.Trigger),
            (202801, MediumAnswerReceived.Trigger), (202101, DeceiverSoldCounterfeit.Trigger),
            (203101, CollatorTwoSamples.Trigger), (203201, SwapperVoteSwap.Trigger),
            (203301, JusticeSpecialMeeting.Trigger),
        })
        {
            trigger(actor);
            trigger(actor);
            var record = CustomAchievementDataManager.Get(actor.FriendCode)?.Get(id);
            if (record?.Unlocked != true || record.Progress != 1
                || CustomAchievementDataManager.Get(host.FriendCode)?.Has(id) == true)
                throw new Exception($"Direct Trigger regression: {id}");
        }

        KeyCommandTester2.Trigger();
        if (CustomAchievementDataManager.Get(host.FriendCode)?.Has(9002) != true)
            throw new Exception("Legacy parameterless Trigger regression");

        CustomAchievementDataManager.Clear();
        AmongUsClient.Instance.AmHost = false;
        ShieldGranted.Trigger(actor);
        AmongUsClient.Instance.AmHost = true;
        GameStates.IsInGame = false;
        ShieldGranted.Trigger(actor);
        GameStates.IsInGame = true;
        if (CustomAchievementDataManager.Count != 0)
            throw new Exception("Direct Trigger must retain existing host and in-game guards");

        GameStates.IsFreePlay = true;
        ShieldGranted.Trigger(actor);
        KeyCommandTester2.Trigger(actor);
        AchievementProgressService.RecordUnit(actor.FriendCode, 202301);
        if (CustomAchievementDataManager.Count != 0)
            throw new Exception("Free play must not award through typed, legacy or numeric entry points");
        GameStates.IsFreePlay = false;

        actor.Data.Disconnected = true;
        ShieldGranted.Trigger(actor);
        actor.Data.Disconnected = false;
        var actorData = actor.Data;
        actor.Data = null;
        ShieldGranted.Trigger(actor);
        actor.Data = actorData;
        actor.FriendCode = null;
        ShieldGranted.Trigger(actor);
        actor.FriendCode = "";
        ShieldGranted.Trigger(actor);
        actor.FriendCode = "actor#0002";
        if (CustomAchievementDataManager.Count != 0)
            throw new Exception("Invalid explicit actors must not receive awards or redirect them to the host");

        ShieldGranted.Trigger(actor);
        if (CustomAchievementDataManager.Get(actor.FriendCode)?.Has(202301) != true)
            throw new Exception("Valid actor must still unlock after free-play and disconnect guards reset");

        CustomAchievementDataManager.Clear();
        var metadataOnly = AchievementRegistry.GetAll().OfType<ProposalAchievement>().First();
        AchievementProgressService.RecordUnit(actor.FriendCode, metadataOnly.Id);
        AchievementProgressService.RecordUnit(actor.FriendCode, int.MaxValue);
        AchievementProgressService.RecordUnit(null, 202301);
        AchievementProgressService.RecordUnit("", 202301);
        if (CustomAchievementDataManager.Count != 0)
            throw new Exception("Rejected definitions or missing identities must not create player data");

        actor.Data.IsDead = true;
        ShieldGranted.Trigger(actor);
        if (CustomAchievementDataManager.Get(actor.FriendCode)?.Has(202301) != true)
            throw new Exception("Dead but connected actors remain eligible for verified role evidence");
        actor.Data.IsDead = false;

        PlayerControl.LocalPlayer = null;
        CustomAchievementDataManager.SetLocalFriendCode("");
        KeyCommandTester2.Trigger(actor);
        Console.WriteLine("DIRECT TRIGGER CHECKS PASSED: 41 typed achievements and legacy 9002 use the existing Trigger path");
    }
}
