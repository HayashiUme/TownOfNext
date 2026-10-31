using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Neutral;
using TONX.Roles.Neutral;

internal static class WinnerEvidenceTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Winner evidence regression: " + message);
        Console.WriteLine("PASS " + message);
    }

    internal static async Task Run()
    {
        AmongUsClient.Instance = new() { AmHost = true };
        GameStates.IsInGame = true;
        GameStates.IsFreePlay = false;
        CustomAchievementDataManager.Clear();

        var jester = new PlayerControl { PlayerId = 1, FriendCode = "jest#0001", Role = CustomRoles.Jester };
        var jesterRole = new Jester { ExileWinTriggered = true };
        jester.RoleClass = jesterRole;
        Main.AllPlayerControls = new() { jester };
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerRoles.Clear();
        Check(!AchievementRegistry.GetById(300201).CheckClear(jester), "Jester exile source alone is not enough without final winner qualification");
        CustomWinnerHolder.WinnerIds.Add(jester.PlayerId);
        Check(AchievementRegistry.GetById(300201).CheckClear(jester), "Jester exile win uses role source and final winner list");
        Check(!AchievementRegistry.GetById(300202).CheckClear(jester), "first-meeting Jester win requires first-meeting evidence");
        jesterRole.WonInFirstMeeting = true;
        Check(AchievementRegistry.GetById(300202).CheckClear(jester), "first-meeting Jester win checks captured meeting evidence");
        jesterRole.ExileWinTriggered = false;
        Check(!AchievementRegistry.GetById(300201).CheckClear(jester)
              && !AchievementRegistry.GetById(300202).CheckClear(jester), "shared neutral winner status without own Jester source awards neither achievement");
        jesterRole.ExileWinTriggered = true;

        var lawyer = new PlayerControl { PlayerId = 2, FriendCode = "lawy#0002", Role = CustomRoles.Lawyer, Data = { IsDead = true } };
        var client = new PlayerControl { PlayerId = 3, FriendCode = "clie#0003", Role = CustomRoles.Crewmate };
        lawyer.RoleClass = new Lawyer
        {
            ClientWinGranted = true,
            AssignedClientCode = client.FriendCode,
            WinningClientCode = client.FriendCode,
            TargetId = client.PlayerId,
        };
        Main.AllPlayerControls = new() { lawyer, client };
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerRoles.Clear();
        CustomWinnerHolder.WinnerIds.UnionWith(new[] { lawyer.PlayerId, client.PlayerId });
        Check(AchievementRegistry.GetById(302801).CheckClear(lawyer), "dead Lawyer and assigned client both qualify as winners");
        var lawyerRole = (Lawyer)lawyer.RoleClass;
        lawyerRole.AssignedClientCode = null;
        Check(!AchievementRegistry.GetById(302801).CheckClear(lawyer), "Lawyer without original assigned client identity cannot inherit a shared win");
        lawyerRole.AssignedClientCode = client.FriendCode;
        client.Data.Disconnected = true;
        Check(!AchievementRegistry.GetById(302801).CheckClear(lawyer), "disconnected Lawyer client cannot satisfy shared win condition");
        client.Data.Disconnected = false;
        client.FriendCode = "reuse#0003";
        Check(!AchievementRegistry.GetById(302801).CheckClear(lawyer), "reused target player ID cannot inherit Lawyer evidence");
        client.FriendCode = "clie#0003";

        ((Lawyer)lawyer.RoleClass).ClientWinRemoved = true;
        lawyer.Data.IsDead = false;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(lawyer.PlayerId);
        Check(AchievementRegistry.GetById(302802).CheckClear(lawyer), "surviving Lawyer requires actual client removal and own final win");
        CustomWinnerHolder.WinnerIds.Add(client.PlayerId);
        Check(!AchievementRegistry.GetById(302802).CheckClear(lawyer), "client re-added as winner invalidates Lawyer takeover achievement");
        CustomWinnerHolder.WinnerIds.Remove(client.PlayerId);
        CustomWinnerHolder.WinnerRoles.Add(client.Role);
        Check(!AchievementRegistry.GetById(302802).CheckClear(lawyer), "role-wide client winner qualification also invalidates Lawyer takeover");
        CustomWinnerHolder.WinnerRoles.Clear();
        ((Lawyer)lawyer.RoleClass).ClientWinGranted = false;
        Check(!AchievementRegistry.GetById(302802).CheckClear(lawyer), "Lawyer shared winner status without own additional-win source awards nothing");

        var prosecutor = new PlayerControl { PlayerId = 4, FriendCode = "pros#0004", Role = CustomRoles.Prosecutors };
        prosecutor.RoleClass = new Prosecutors { SurvivalWinGranted = true, InterceptedKillers = 2 };
        Main.AllPlayerControls = new() { prosecutor };
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!AchievementRegistry.GetById(302902).CheckClear(prosecutor), "Prosecutor interception count alone is not a role win");
        CustomWinnerHolder.WinnerIds.Add(prosecutor.PlayerId);
        Check(AchievementRegistry.GetById(302902).CheckClear(prosecutor), "Prosecutor needs two actual interceptions, survival grant, and final win qualification");
        ((Prosecutors)prosecutor.RoleClass).InterceptedKillers = 1;
        Check(!AchievementRegistry.GetById(302902).CheckClear(prosecutor), "one intercepted killer does not satisfy two-distinct-player condition");
        ((Prosecutors)prosecutor.RoleClass).InterceptedKillers = 2;
        ((Prosecutors)prosecutor.RoleClass).SurvivalWinGranted = false;
        Check(!AchievementRegistry.GetById(302902).CheckClear(prosecutor), "shared neutral Prosecutor win without survival-rule grant awards nothing");
        ((Prosecutors)prosecutor.RoleClass).SurvivalWinGranted = true;
        prosecutor.Data.IsDead = true;
        Check(!AchievementRegistry.GetById(302902).CheckClear(prosecutor), "dead Prosecutor cannot satisfy survival win condition");

        var solo = new PlayerControl { PlayerId = 5, FriendCode = "solo#0005" };
        Main.AllPlayerControls = new() { solo };
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerRoles.Clear();

        solo.RoleClass = new Arsonist { IgnitionWinTriggered = true };
        CustomWinnerHolder.WinnerTeam = CustomWinner.Arsonist;
        Check(!AchievementRegistry.GetById(300102).CheckClear(solo), "Arsonist ignition source alone is not enough without personal winner place");
        CustomWinnerHolder.WinnerIds.Add(solo.PlayerId);
        Check(AchievementRegistry.GetById(300102).CheckClear(solo), "Arsonist ignition win requires ignition source and win qualification");
        ((Arsonist)solo.RoleClass).IgnitionWinTriggered = false;
        Check(!AchievementRegistry.GetById(300102).CheckClear(solo), "Arsonist shared winner status without ignition source awards nothing");

        solo.RoleClass = new Terrorist { ExileWinTriggered = true };
        CustomWinnerHolder.WinnerTeam = CustomWinner.Terrorist;
        Check(AchievementRegistry.GetById(300402).CheckClear(solo), "Terrorist exile win requires exile source and win qualification");
        solo.Data.IsDead = true;
        ((Terrorist)solo.RoleClass).ExileWinTriggered = false;
        Check(!AchievementRegistry.GetById(300402).CheckClear(solo), "Terrorist winner status without exile source awards nothing");
        solo.Data.IsDead = false;

        solo.RoleClass = new Workaholic { TaskWinTriggered = true };
        CustomWinnerHolder.WinnerTeam = CustomWinner.Workaholic;
        Check(AchievementRegistry.GetById(301602).CheckClear(solo), "Workaholic full task win requires task-win source and win qualification");
        ((Workaholic)solo.RoleClass).TaskWinTriggered = false;
        Check(!AchievementRegistry.GetById(301602).CheckClear(solo), "Workaholic shared winner status without task source awards nothing");
        ((Workaholic)solo.RoleClass).TaskWinTriggered = true;

        solo.RoleClass = new Follower { BetWinGranted = true };
        CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
        Check(!AchievementRegistry.GetById(302002).CheckClear(solo), "living Follower cannot satisfy the after-death bet condition");
        solo.Data.IsDead = true;
        Check(AchievementRegistry.GetById(302002).CheckClear(solo), "dead Follower with bet win and winner qualification qualifies");
        ((Follower)solo.RoleClass).BetWinGranted = false;
        Check(!AchievementRegistry.GetById(302002).CheckClear(solo), "dead Follower without bet win source awards nothing");
        solo.Data.IsDead = false;

        SchrodingerCat.ConversionWin = true;
        solo.RoleClass = new SchrodingerCat();
        CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Crewmate);
        Check(AchievementRegistry.GetById(302202).CheckClear(solo), "converted cat qualifying as a winner after conversion qualifies");
        SchrodingerCat.ConversionWin = false;
        Check(!AchievementRegistry.GetById(302202).CheckClear(solo), "cat without recorded conversion win source awards nothing");
        CustomWinnerHolder.WinnerRoles.Clear();

        Shifter.DistinctSwaps = true;
        solo.RoleClass = new Shifter();
        Check(AchievementRegistry.GetById(302402).CheckClear(solo), "Shifter two distinct swaps after returning qualifies");
        Shifter.DistinctSwaps = false;
        Check(!AchievementRegistry.GetById(302402).CheckClear(solo), "Shifter without completed distinct swaps awards nothing");

        solo.RoleClass = null;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(solo.PlayerId);
        CustomWinnerHolder.WinnerTeam = CustomWinner.Revolutionist;
        Check(AchievementRegistry.GetById(301202).CheckClear(solo), "Revolutionist win source with personal winner place qualifies");
        CustomWinnerHolder.WinnerTeam = CustomWinner.Collector;
        Check(!AchievementRegistry.GetById(301202).CheckClear(solo), "Revolutionist achievement requires the Revolutionist win source");
        Check(AchievementRegistry.GetById(301702).CheckClear(solo), "Collector win source with personal winner place qualifies");
        CustomWinnerHolder.WinnerTeam = CustomWinner.Yandere;
        Check(!AchievementRegistry.GetById(301702).CheckClear(solo), "Collector achievement requires the Collector win source");
        Check(AchievementRegistry.GetById(302602).CheckClear(solo), "Yandere win source with personal winner place qualifies");
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!AchievementRegistry.GetById(302602).CheckClear(solo), "Yandere achievement requires a personal winner place");

        solo.RoleClass = new Sunnyboy { DeathWinGranted = true };
        solo.Data.IsDead = true;
        CustomWinnerHolder.WinnerIds.Add(solo.PlayerId);
        Check(AchievementRegistry.GetById(302701).CheckClear(solo), "Sunnyboy death-win grant with winner qualification qualifies");
        ((Sunnyboy)solo.RoleClass).DeathWinGranted = false;
        Check(!AchievementRegistry.GetById(302701).CheckClear(solo), "Sunnyboy winner status without death-win grant awards nothing");
        solo.Data.IsDead = false;

        solo.RoleClass = null;
        solo.Role = CustomRoles.Jackal;
        solo.Data.IsDead = false;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(solo.PlayerId);
        CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
        Check(AchievementRegistry.GetById(300601).CheckClear(solo), "Jackal headcount win source with personal winner place qualifies");
        CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
        Check(!AchievementRegistry.GetById(300601).CheckClear(solo), "Jackal achievement requires the Jackal win source");
        CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!AchievementRegistry.GetById(300601).CheckClear(solo), "Jackal achievement requires a personal winner place");

        solo.Role = CustomRoles.Sidekick;
        solo.Data.IsDead = false;
        CustomWinnerHolder.WinnerIds.Add(solo.PlayerId);
        Check(AchievementRegistry.GetById(300701).CheckClear(solo), "living Sidekick on the Jackal winner list qualifies");
        solo.Data.IsDead = true;
        Check(!AchievementRegistry.GetById(300701).CheckClear(solo), "dead Sidekick cannot satisfy the survival condition");
        solo.Data.IsDead = false;
        CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
        Check(!AchievementRegistry.GetById(300701).CheckClear(solo), "Sidekick achievement requires the Jackal win source");
        solo.Role = CustomRoles.Crewmate;
        CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
        Check(!AchievementRegistry.GetById(300701).CheckClear(solo), "non-Sidekick cannot satisfy the Sidekick achievement");

        // Exercise the actual production settlement, not a stand-in for CheckClear.
        Main.AllPlayerControls = new() { jester };
        jester.Data.IsDead = true;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(jester.PlayerId);
        foreach (var winner in new[] { CustomWinner.Draw, CustomWinner.Error, CustomWinner.Default })
        {
            CustomWinnerHolder.WinnerTeam = winner;
            Check(!await AchievementSettlement.SettleAsync() && CustomAchievementDataManager.Count == 0,
                $"{winner} settlement does not evaluate otherwise satisfied winner achievements");
        }
        CustomWinnerHolder.WinnerTeam = CustomWinner.Jester;
        AmongUsClient.Instance.AmHost = false;
        Check(!await AchievementSettlement.SettleAsync(), "non-host cannot settle winner achievements");
        AmongUsClient.Instance.AmHost = true;
        GameStates.IsInGame = false;
        Check(!await AchievementSettlement.SettleAsync(), "lobby cannot settle winner achievements");
        GameStates.IsInGame = true;
        GameStates.IsFreePlay = true;
        Check(!await AchievementSettlement.SettleAsync(), "free play cannot settle winner achievements");
        GameStates.IsFreePlay = false;
        jester.Data.Disconnected = true;
        await AchievementSettlement.SettleAsync();
        Check(CustomAchievementDataManager.Count == 0, "disconnected actor is excluded from settlement");
        jester.Data.Disconnected = false;
        await AchievementSettlement.SettleAsync();
        await AchievementSettlement.SettleAsync();
        var result = CustomAchievementDataManager.Get(jester.FriendCode);
        Check(result?.Get(300201)?.Progress == 1 && result.Get(300202)?.Progress == 1,
            "normal repeated settlement awards both satisfied Jester conditions once, including a dead actor");

        var metadataOnly = AchievementRegistry.GetAll().OfType<ProposalAchievement>().First();
        Check(!metadataOnly.CheckClear(jester), "unimplemented proposal metadata cannot satisfy CheckClear");
        AchievementProgressService.RecordUnit(jester.FriendCode, metadataOnly.Id);
        Check(!result.Has(metadataOnly.Id), "unimplemented proposal metadata cannot award through progress service either");

        CustomAchievementDataManager.Clear();
        lawyer.Data.IsDead = true;
        lawyerRole.ClientWinGranted = true;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.UnionWith(new[] { lawyer.PlayerId, client.PlayerId });
        Main.AllPlayerControls = new() { lawyer, client };
        await AchievementSettlement.SettleAsync();
        Check(CustomAchievementDataManager.Get(lawyer.FriendCode)?.Has(302801) == true
              && CustomAchievementDataManager.Get(client.FriendCode)?.Has(302801) != true,
            "settlement awards Lawyer shared-client achievement only to the Lawyer");

        CustomAchievementDataManager.Clear();
        lawyer.Data.IsDead = false;
        CustomWinnerHolder.WinnerIds.Remove(client.PlayerId);
        await AchievementSettlement.SettleAsync();
        Check(CustomAchievementDataManager.Get(lawyer.FriendCode)?.Has(302802) == true,
            "settlement awards living Lawyer takeover only after actual removal evidence");

        CustomAchievementDataManager.Clear();
        prosecutor.Data.IsDead = false;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(prosecutor.PlayerId);
        Main.AllPlayerControls = new() { prosecutor };
        await AchievementSettlement.SettleAsync();
        Check(CustomAchievementDataManager.Get(prosecutor.FriendCode)?.Has(302902) == true,
            "settlement awards qualifying Prosecutor interception win");
    }
}
