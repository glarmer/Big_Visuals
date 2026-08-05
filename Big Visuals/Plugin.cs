using System;
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
        _ui.Init();
        
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        
        GameObject bigVisualsObject = new GameObject("BigVisuals");
        bigVisualsObject.AddComponent<CameraWatcher>();
        UnityEngine.Object.DontDestroyOnLoad(bigVisualsObject);
    }
}
