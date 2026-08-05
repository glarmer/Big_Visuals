using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Big_Visuals;

public class SettingsRetry : MonoBehaviour
{
    private const int MaxAttempts = 300;

    private static SettingsRetry _instance;
    private static int _remainingAttempts;

    public SettingsRetry(IntPtr ptr) : base(ptr)
    {
    }

    internal static void SetInstance(SettingsRetry instance)
    {
        _instance = instance;
    }

    internal static void Request()
    {
        _remainingAttempts = MaxAttempts;

        if (_instance == null)
        {
            Plugin.Log.LogWarning("Camera settings retry requested before retry component was ready");
            return;
        }

        _instance.enabled = true;
    }

    private void Update()
    {
        if (_remainingAttempts <= 0)
        {
            enabled = false;
            Plugin.Log.LogWarning("Settings retry timed out before a camera was available");
            return;
        }

        _remainingAttempts--;

        if (!CameraSettingsTargetIsReady())
            return;

        Plugin.Instance.Settings.SetAllCameraSettings();
        Plugin.Instance.Settings.SetAllSettings();
        enabled = false;
        Plugin.Log.LogInfo("Settings applied...");
    }

    private static bool CameraSettingsTargetIsReady()
    {
        Camera camera = Camera.main;
        return camera != null && camera.GetComponent<UniversalAdditionalCameraData>() != null;
    }
}
