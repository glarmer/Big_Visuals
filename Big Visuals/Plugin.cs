using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Big_Visuals.Configuration;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Big_Visuals;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log { get; private set; } = null!;
    public static Plugin Instance {get; private set;} = null!;
    public ConfigurationHandler ConfigurationHandler {get; private set;} = null!;
    public Settings Settings { get; private set; } = null!;
    private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);
    
    private ModConfigurationUI _ui;
    
    
    public override void Load()
    {
        Log = base.Log;
        if (Instance == null)
        {
            Instance = this;
        }
        
        ClassInjector.RegisterTypeInIl2Cpp<ModConfigurationUI>();
        ClassInjector.RegisterTypeInIl2Cpp<CameraWatcher>();
        

        ConfigurationHandler = new ConfigurationHandler(Config);
        Settings = new Settings();
        
        _harmony.PatchAll();
        
        var go = new GameObject("BigVisualsUI");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _ui = go.AddComponent<ModConfigurationUI>();
        _ui.Init(new List<Option>
        {
            Option.Float("Render Scale", ConfigurationHandler.ConfigRenderScale, 0.1f, 2f, 0.1f),
            Option.Int(
                "Upscaling Filter",
                ConfigurationHandler.ConfigUpscalingFilter, 0, 4,
                displayValue: () => ConfigurationHandler.ConfigUpscalingFilter.Value switch
                {
                    0 => "Auto",
                    1 => "Linear",
                    2 => "Nearest Neighbor",
                    3 => "FSR 1.0",
                    4 => "STP",
                    _ => "Invalid Value"
                }
            ),
            Option.Bool("Anisotropic Filtering", ConfigurationHandler.ConfigAnisotropicFiltering),
            Option.Float("LOD Quality", ConfigurationHandler.ConfigLODQuality, 0.1f, 10f, 0.1f),
            Option.Int("Shadowmap Resolution", ConfigurationHandler.ConfigShadowmapResolution, 1024, 8192, 1024),
            //Option.Int("Shadow Distance", ConfigurationHandler.ConfigShadowDistance, 0, 1000, 25),
            Option.Int("Shadow Cascades", ConfigurationHandler.ConfigShadowCascades, 1, 10),
            Option.Bool("Soft Shadows", ConfigurationHandler.ConfigSoftShadows),
            Option.Int(
                "Camera Antialiasing",
                ConfigurationHandler.ConfigCameraAA, 0, 3,
                displayValue: () => ConfigurationHandler.ConfigCameraAA.Value switch
                {
                    0 => "None",
                    1 => "FXAA",
                    2 => "SMAA",
                    3 => "TAA",
                    _ => "???"
                }
            ),
            Option.Int(
                "MSAA",
                ConfigurationHandler.ConfigMSAA, 0, 8, 2,
                displayValue: () => ConfigurationHandler.ConfigMSAA.Value switch
                {
                    0 => "Off",
                    2 => "2x",
                    4 => "4x",
                    8 => "8x",
                    _ => ConfigurationHandler.ConfigMSAA.Value + "x"
                }
            ),
            Option.Int("FOV", ConfigurationHandler.ConfigFOV, 60, 150),
            Option.InputAction("Menu Key", ConfigurationHandler.ConfigMenuKey)
        });
        
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        
        GameObject bigVisualsObject = new GameObject("BigVisuals");
        bigVisualsObject.AddComponent<CameraWatcher>();
        UnityEngine.Object.DontDestroyOnLoad(bigVisualsObject);
    }
}
