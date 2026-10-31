using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Impostor;
using TONX.Modules.Achievements.Crewmate;
using TONX.Modules.Achievements.Neutral;
using TONX.Roles.Crewmate;
using TONX.Roles.Impostor;
using TONX.Roles.Neutral;

internal static class CounterAchievementTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Counter achievement regression: " + message);
        Console.WriteLine("PASS " + message);
    }

    internal static void Run()
    {
        var player = new PlayerControl { PlayerId = 10, FriendCode = "counter#0010" };
        Main.AllPlayerControls = new() { player };
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerRoles.Clear();

        AssertCounter(new BountyStreak(), player, new BountyHunter { SuccessfulBounties = 2 }, 3,
            CustomWinner.Impostor, "three distinct bounty kills and impostor win");
        AssertCounter(new VampireFeast(), player, new Vampire { SuccessfulBites = 1 }, 2,
            CustomWinner.Impostor, "two distinct delayed vampire kills and impostor win");
        AssertCounter(new WarlockDoubleCurse(), player, new Warlock { SuccessfulCurseKills = 1 }, 2,
            CustomWinner.Impostor, "two distinct curse kills and impostor win");
        AssertCounter(new AssassinDoubleKill(), player, new Assassin { SuccessfulAssassinations = 1 }, 2,
            CustomWinner.Impostor, "two distinct assassination kills and impostor win");
        AssertCounter(new MinerThreeTeleports(), player,
            new Miner { SuccessfulTeleports = 2, KilledAfterTeleport = true }, 3,
            CustomWinner.Impostor, "three teleports, a post-teleport kill and impostor win");
        AssertCounter(new SniperDoubleKill(), player, new Sniper { SuccessfulSnipes = 1 }, 2,
            CustomWinner.Impostor, "two distinct ray kills and impostor win");
        AssertCounter(new MareDoubleKill(), player, new Mare { LightsOutKills = 1 }, 2,
            CustomWinner.Impostor, "two distinct blackout kills and impostor win");
        AssertCounter(new CleanerTwoBodies(), player, new Cleaner { CleanedBodies = 1 }, 2,
            CustomWinner.Impostor, "two distinct cleaned bodies and impostor win");
        AssertCounter(new KillingMachineTripleKill(), player, new KillingMachine { SuccessfulKills = 2 }, 3,
            CustomWinner.Impostor, "three distinct Killing Machine kills and impostor win");
        AssertCounter(new SpeedBoosterTripleTask(), player, new SpeedBooster { SuccessfulBoosts = 2 }, 3,
            CustomWinner.Crewmate, "three distinct task boosts, survival and crew win", requireAlive: true);
        AssertCounter(new TransporterTripleSwap(), player, new Transporter { SuccessfulTransports = 2 }, 3,
            CustomWinner.Crewmate, "three distinct successful swaps and crew win");
        AssertCounter(new PelicanThreeEats(), player, new Pelican { SuccessfulEats = 2 }, 3,
            CustomWinner.Pelican, "three distinct swallowed players and Pelican win");
        AssertCounter(new SoulCatcherDoubleStrike(), player, new SoulCatcher { KillsAfterSwap = 1 }, 2,
            CustomWinner.Impostor, "two kills after position swap and impostor win");
        AssertCounter(new EraserDoubleErase(), player, new Eraser { ErasedPlayers = 1 }, 2,
            CustomWinner.Impostor, "two erased crew roles and impostor win");
        AssertCounter(new MedicDoubleShield(), player, new Medic { ShieldsConsumed = 1 }, 2,
            CustomWinner.Crewmate, "two shields actually consumed and crew win");
        AssertCounter(new LuckyDoubleEscape(), player, new Luckey { BlockedAttacks = 1 }, 2,
            CustomWinner.Crewmate, "two probabilistic blocks and crew win", requireAlive: true);
        AssertCounter(new BardDoubleReductionKill(), player, new Bard(), 2,
            CustomWinner.Impostor, "two exile cooldown reductions, a following kill and impostor win");
        AssertCounter(new SuperStarDoubleBlock(), player, new SuperStar(), 2,
            CustomWinner.Crewmate, "two blocked attacks and crew win");
        AssertCounter(new RepairmanTripleFix(), player, new Repairman(), 3,
            CustomWinner.Crewmate, "three fast repairs and crew win");

        player.RoleClass = new Escapist { KilledAfterTeleport = true };
        player.Data.IsDead = false;
        CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        Check(new EscapistReturnKill().CheckClear(player), "Escapist kill within teleport window and impostor win");
        player.RoleClass = new Escapist { KilledAfterTeleport = false };
        Check(!new EscapistReturnKill().CheckClear(player), "Escapist teleport return without a window kill rejected");
        player.RoleClass = new Escapist { KilledAfterTeleport = true };
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!new EscapistReturnKill().CheckClear(player), "Escapist is not awarded without final winner qualification");

        player.RoleClass = new Gangster { HasSurvivingRecruit = true };
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        Check(new GangsterDoubleRecruit().CheckClear(player), "Gangster two recruits with a survivor and impostor win");
        player.RoleClass = new Gangster { HasSurvivingRecruit = false };
        Check(!new GangsterDoubleRecruit().CheckClear(player), "Gangster with a single recruit rejected");
        player.RoleClass = new Gangster { HasSurvivingRecruit = true };
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!new GangsterDoubleRecruit().CheckClear(player), "Gangster recruit count without winner qualification rejected");

        player.RoleClass = new Mario { VentWinTriggered = true };
        CustomWinnerHolder.WinnerTeam = CustomWinner.Mario;
        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        Check(new MarioVentWin().CheckClear(player), "Mario personal vent-win source and final winner qualify");
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!new MarioVentWin().CheckClear(player), "Mario role winner without personal winner ID does not qualify");
        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
        Check(!new MarioVentWin().CheckClear(player), "Mario vent count cannot substitute for Mario win source");
        player.Data.Disconnected = true;
        CustomWinnerHolder.WinnerTeam = CustomWinner.Mario;
        Check(!new MarioVentWin().CheckClear(player), "disconnected Mario cannot qualify");
        player.Data.Disconnected = false;
    }

    private static void AssertCounter(
        AchievementBase achievement,
        PlayerControl player,
        object role,
        int threshold,
        CustomWinner winner,
        string description,
        bool requireAlive = false)
    {
        player.RoleClass = role;
        player.Data.IsDead = false;
        player.Data.Disconnected = false;
        CustomWinnerHolder.WinnerTeam = winner;
        CustomWinnerHolder.WinnerIds.Clear();
        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        CustomWinnerHolder.WinnerRoles.Clear();
        Check(!achievement.CheckClear(player), description + ": below threshold");

        SetCounter(role, threshold);
        Check(achievement.CheckClear(player), description + ": threshold qualifies");

        CustomWinnerHolder.WinnerTeam = winner == CustomWinner.Impostor
            ? CustomWinner.Crewmate
            : winner == CustomWinner.Crewmate ? CustomWinner.Impostor : CustomWinner.Crewmate;
        Check(!achievement.CheckClear(player), description + ": wrong winner team rejected");

        CustomWinnerHolder.WinnerTeam = winner;
        CustomWinnerHolder.WinnerIds.Clear();
        Check(!achievement.CheckClear(player), description + ": non-winner rejected");

        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
        if (requireAlive)
        {
            player.Data.IsDead = true;
            Check(!achievement.CheckClear(player), description + ": dead actor rejected");
        }
    }

    private static void SetCounter(object role, int value)
    {
        switch (role)
        {
            case BountyHunter x: x.SuccessfulBounties = value; break;
            case Vampire x: x.SuccessfulBites = value; break;
            case Warlock x: x.SuccessfulCurseKills = value; break;
            case Assassin x: x.SuccessfulAssassinations = value; break;
            case Miner x: x.SuccessfulTeleports = value; break;
            case Sniper x: x.SuccessfulSnipes = value; break;
            case Mare x: x.LightsOutKills = value; break;
            case Cleaner x: x.CleanedBodies = value; break;
            case KillingMachine x: x.SuccessfulKills = value; break;
            case SpeedBooster x: x.SuccessfulBoosts = value; break;
            case Transporter x: x.SuccessfulTransports = value; break;
            case Pelican x: x.SuccessfulEats = value; break;
            case SoulCatcher x: x.KillsAfterSwap = value; break;
            case Eraser x: x.ErasedPlayers = value; break;
            case Medic x: x.ShieldsConsumed = value; break;
            case Luckey x: x.BlockedAttacks = value; break;
            case Bard x: x.CooldownReductions = value; x.KilledAfterSecondReduction = true; break;
            case SuperStar x: x.BlockedAttacks = value; break;
            case Repairman x: x.UsedSkillCount = value; break;
        }
    }
}
