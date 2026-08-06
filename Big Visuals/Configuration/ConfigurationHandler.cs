using System;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace Big_Visuals.Configuration;

public class ConfigurationHandler
{
    private ConfigFile _config;
    
    public KeyCode MenuKey { get; private set; }

    public ConfigEntry<float> ConfigRenderScale;
    public ConfigEntry<int> ConfigUpscalingFilter;
    public ConfigEntry<float> ConfigLODQuality;
    public ConfigEntry<string> ConfigMenuKey;
    // public ConfigEntry<int> ConfigShadowDistance;
    public ConfigEntry<int> ConfigShadowCascades;
    public ConfigEntry<int> ConfigShadowmapResolution;
    public ConfigEntry<bool> ConfigSoftShadows;
    public ConfigEntry<bool> ConfigAnisotropicFiltering;
    public ConfigEntry<int> ConfigCameraAA;
    // public ConfigEntry<int> ConfigMSAA;
    public ConfigEntry<int> ConfigFOV;

    public float RenderScale => ConfigRenderScale.Value;
    public int UpscalingFilter => ConfigUpscalingFilter.Value;
    public float LodQuality => ConfigLODQuality.Value;
    // public int ShadowDistance => ConfigShadowDistance.Value;
    public int ShadowCascades => ConfigShadowCascades.Value;
    public int ShadowmapResolution => ConfigShadowmapResolution.Value;
    public bool SoftShadows => ConfigSoftShadows.Value;
    public bool AnisotropicFiltering => ConfigAnisotropicFiltering.Value;
    public int CameraAA => ConfigCameraAA.Value;
    // public int MSAA => ConfigMSAA.Value;
    public int FOV => ConfigFOV.Value;

    public ConfigurationHandler(ConfigFile configFile)
    {
        _config = configFile;

        Plugin.Log.LogInfo("ConfigurationHandler initialising");

        ConfigRenderScale = Bind(
            "Scaling",
            "RenderScale",
            1.4f,
            "Controls the render scale of the game. Native is 1.0 (100%). Range 0.1-2.0",
            () => Plugin.Instance.Settings.SetResolutionScale(),
            v => Mathf.Clamp(v, 0.1f, 2f)
        );

        ConfigUpscalingFilter = Bind(
            "Scaling",
            "UpscalingFilter",
            1,
            "Controls what filter the game uses to scale to your monitor resolution. 0 = auto, 1 = linear, 2 = point, 3 = FSR 1.0",
            () => Plugin.Instance.Settings.SetUpscaler(),
            v => Mathf.Clamp(v, 0, 3)
        );

        ConfigLODQuality = Bind(
            "LOD",
            "LODQuality",
            4f,
            "Controls the LOD bias of the game. Higher values increase detail distance. Range 0.1-10",
            () => Plugin.Instance.Settings.SetLODQuality(),
            v => Mathf.Clamp(v, 0.1f, 10f)
        );

        // ConfigShadowDistance = Bind(
        //     "Shadows",
        //     "ShadowDistance",
        //     300,
        //     "Controls the maximum distance shadows are rendered. PEAK's High option equates to 200. Higher values improve distant shadows but reduce performance. Range 0-1000",
        //     () => Plugin.Instance.Settings.SetShadowDistance(),
        //     v => Mathf.Clamp(v, 0, 1000)
        // );

        ConfigShadowCascades = Bind(
            "Shadows",
            "ShadowCascades",
            4,
            "Controls the number of shadow cascades used by the directional light. Higher values improve shadow stability. Range 1-4",
            () => Plugin.Instance.Settings.SetShadowCascades(),
            ClampShadowCascades
        );

        ConfigShadowmapResolution = Bind(
            "Shadows",
            "ShadowmapResolution",
            8192,
            "Controls the quality of the shadows, can reduce performance so turn it down if you're having issues. Makes the trees less wobbly",
            () => Plugin.Instance.Settings.SetShadowmapResolution(),
            ClampShadowmapResolution
        );

        ConfigSoftShadows = Bind(
            "Shadows",
            "SoftShadows",
            true,
            "Allows shadows to be soft, if your PC is too low spec for a high shadowmap setting this to false can stop the wobblyness of the shadows (but will result in a pixelly effect if the shadowmap res is low)",
            () => Plugin.Instance.Settings.SetSoftShadows()
        );

        ConfigAnisotropicFiltering = Bind(
            "Quality",
            "AnisotropicFiltering",
            true,
            "Helps texture sharpness at angles (set to on)",
            () => Plugin.Instance.Settings.SetAnisotropicFiltering()
        );

        ConfigCameraAA = Bind(
            "AntiAliasing",
            "CameraAA",
            0,
            "Controls what type of AA the Camera uses. All of these options are essentially clever blur filters and sometimes people find them unpleasant. 0 = None, 1 = FXAA, 2 = SMAA, 3 = TAA.",
            () => Plugin.Instance.Settings.SetPostProcessAA(),
            v => Mathf.Clamp(v, 0, 3)
        );

        // ConfigMSAA = Bind(
        //     "AntiAliasing",
        //     "MSAA",
        //     8,
        //     "Controls whether Multi-Sample Anti-Aliasing (MSAA) is enabled. MSAA smooths jagged edges on geometry by sampling pixels multiple times. It is generally sharper than post-process AA methods but only affects object edges and can slightly reduce performance. PEAK does not use it by default. Higher values = better quality but worse performance. Valid values = 0, 2, 4, 8.",
        //     () => Plugin.Instance.Settings.SetMSAA(),
        //     ClampMsaa
        // );

        ConfigFOV = Bind(
            "Camera",
            "FOV",
            90,
            "Controls the camera field of view. Range 60-150. Will override the option in the game's menu.",
            () => Plugin.Instance.Settings.SetFOV(),
            v => Mathf.Clamp(v, 60, 150)
        );

        ConfigMenuKey = Bind(
            "General",
            "Config Menu Key",
            "F11",
            "Keyboard key for opening the mod configuration menu",
            SetupInputKey
        );

        SetupInputKey();

        Plugin.Log.LogInfo("ConfigurationHandler initialised");
    }

    private ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description, Action onChanged = null, Func<T, T> clamp = null)
    {
        var entry = _config.Bind(section, key, defaultValue, description);

        if (clamp != null) 
            entry.Value = clamp(entry.Value);

        Plugin.Log.LogInfo($"{key} set to: {entry.Value}");

        if (onChanged != null)
            AddSettingChangedHandler(entry, onChanged, clamp);

        return entry;
    }

    private static int ClampMsaa(int value)
    {
        if (value <= 0)
            return 0;
        if (value <= 2)
            return 2;
        if (value <= 4)
            return 4;
        return 8;
    }

    private static int ClampShadowCascades(int value)
    {
        if (value <= 1)
            return 1;
        if (value <= 2)
            return 2;
        return 4;
    }

    private static int ClampShadowmapResolution(int value)
    {
        if (value <= 1024)
            return 1024;
        if (value <= 2048)
            return 2048;
        if (value <= 4096)
            return 4096;
        return 8192;
    }

    private static void AddSettingChangedHandler<T>(ConfigEntry<T> entry, Action onChanged, Func<T, T> clamp)
    {
        var eventInfo = typeof(ConfigEntry<T>).GetEvent("SettingChanged");
        if (eventInfo == null)
        {
            Plugin.Log.LogWarning($"Could not subscribe to SettingChanged for {entry.Definition.Key}");
            return;
        }

        var listener = new ConfigChangeListener<T>(entry, onChanged, clamp);
        var method = typeof(ConfigChangeListener<T>).GetMethod(
            nameof(ConfigChangeListener<T>.OnSettingChanged),
            BindingFlags.Instance | BindingFlags.Public
        );
        var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, listener, method);
        eventInfo.AddEventHandler(entry, handler);
    }

    private sealed class ConfigChangeListener<T>
    {
        private readonly ConfigEntry<T> _entry;
        private readonly Action _onChanged;
        private readonly Func<T, T> _clamp;

        public ConfigChangeListener(ConfigEntry<T> entry, Action onChanged, Func<T, T> clamp)
        {
            _entry = entry;
            _onChanged = onChanged;
            _clamp = clamp;
        }

        public void OnSettingChanged(object sender, EventArgs args)
        {
            if (_clamp != null)
                _entry.Value = _clamp(_entry.Value);

            _onChanged();
        }
    }

    private void SetupInputKey()
    {
        if (Enum.TryParse(ConfigMenuKey.Value, out KeyCode key))
        {
            MenuKey = key;
            Plugin.Log.LogInfo($"Menu key set to {MenuKey}");
        }
        else
        {
            MenuKey = KeyCode.F11;
            Plugin.Log.LogWarning(
                $"Invalid menu key {ConfigMenuKey.Value}, using F11"
            );
        }
    }
}
