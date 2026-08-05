using Big_Visuals;
using HarmonyLib;

namespace PEAK_Visuals.Patches;

public static class SettingsHandlerPatches
{
    [HarmonyPatch(typeof(SettingsMenu))]
    [HarmonyPatch(MethodType.Constructor)]
    class ConstructorPatch
    {
        [HarmonyPostfix]
        static void Postfix(SettingsMenu __instance)
        {
            Plugin.Instance.Settings.SetAllSettings();
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.ActionResetGraphics))]
    class SaveSettingPatch
    {
        [HarmonyPostfix]
        static void Postfix(SettingsMenu __instance)
        {
            Plugin.Instance.Settings.SetAllSettings();
        }
    }
}