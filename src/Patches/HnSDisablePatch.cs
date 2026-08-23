using UnityEngine;
using UnityEngine.SceneManagement;

namespace TONX.Patches;

public class HnSDisablePatch
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class MainMenuManagerStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((scene, _) =>
            {
                if (!scene.name.Equals("MatchMaking", StringComparison.Ordinal)) return;
                GameObject.Find("CreateHnSGameButton").SetActive(false);
            }));
        }
    }
    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Show))]
    static class CreateGameOptionsOpenShowPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            GameObject.Find("HideSeekOption").SetActive(false);
        }
    }
}