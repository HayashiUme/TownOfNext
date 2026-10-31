using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Titles;

internal static class TitleDisplayTests
{
    public static void Run()
    {
        var originalLanguage = Translator.Language;
        var player = new PlayerControl { FriendCode = "title-name-regression" };
        var data = CustomAchievementDataManager.GetOrCreate(player.FriendCode);
        var provider = new AchievementTitleHandler.AchievementTitleProvider();
        try
        {
            foreach (var language in Translator.translateMaps.Keys)
            {
                Translator.Language = language;
                foreach (var achievement in AchievementRegistry.GetAll())
                {
                    data.SetWearedTitle(achievement.Id);
                    var expected = $"<size=75%><color={achievement.TitleColorHex}>[{GetString(achievement.Name)}]</color></size>";
                    if (provider.GetPrefix(player) != expected)
                        throw new Exception($"{achievement.Id}: equipped title must display localized Name in language {language}");
                }
            }

            data.SetWearedTitle(0);
            if (provider.GetPrefix(player) != null)
                throw new Exception("Unequipped achievement must not add a prefix");
            data.SetWearedTitle(int.MaxValue);
            if (provider.GetPrefix(player) != null)
                throw new Exception("Unknown achievement must not add a prefix");

            Console.WriteLine("TITLE NAME CHECKS PASSED: production prefix uses localized Name for all achievements and languages");
        }
        finally
        {
            Translator.Language = originalLanguage;
            CustomAchievementDataManager.Remove(player.FriendCode);
        }
    }
}
