using HarmonyLib;

namespace Big_Visuals.Patches;

public static class PlayerLoadPatches
{
    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.OnLocalPlayerCharcterStart))]
    private static class LocalPlayerStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Plugin.Log.LogInfo("Local player loaded, applying Big Visuals settings");
            SettingsRetry.Request();
        }
    }
}
