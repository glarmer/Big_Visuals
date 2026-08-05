using HarmonyLib;

namespace Big_Visuals;

public class PlayerCameraMinderPatches
{
    [HarmonyPatch(typeof (PlayerCameraMinder), nameof(PlayerCameraMinder.baseFieldOfView))]
    [HarmonyPostfix]
    public static void PostFix(ref float __result)
    {
        __result = Plugin.Instance.ConfigurationHandler.FOV;
    }
}
