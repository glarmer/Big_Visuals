using System;
using System.Collections.Generic;
using System.Reflection;
using Big_Visuals.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Big_Visuals;

public class Settings
{
    private static readonly BindingFlags PipelineMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private ConfigurationHandler _configurationHandler = Plugin.Instance.ConfigurationHandler;
    
    public void SetAllSettings()
    {
        SetResolutionScale();
        SetUpscaler();
        SetLODQuality();
        //SetShadowDistance();
        SetShadowCascades();
        SetAnisotropicFiltering();
        SetSoftShadows();
        SetShadowmapResolution();
    }

    public void SetAllCameraSettings()
    {
        SetPostProcessAA();
        // SetMSAA();
        SetFOV();
    }

    public void SetSoftShadows()
    {
        ApplyToRenderPipelines(
            "Soft Shadows",
            pipeline =>
            {
                SetPipelineMember(pipeline, "supportsSoftShadows", _configurationHandler.SoftShadows);
                SetPipelineMember(pipeline, "m_SoftShadowsSupported", _configurationHandler.SoftShadows);
            },
            pipeline => pipeline.supportsSoftShadows.ToString()
        );
    }

    public void SetShadowmapResolution()
    {
        ApplyToRenderPipelines(
            "Shadowmap Resolution",
            pipeline =>
            {
                pipeline.mainLightShadowmapResolution = _configurationHandler.ShadowmapResolution;
                SetPipelineMember(pipeline, "m_MainLightShadowmapResolution", (UnityEngine.Rendering.Universal.ShadowResolution)_configurationHandler.ShadowmapResolution);
            },
            pipeline => pipeline.mainLightShadowmapResolution.ToString()
        );
    }

    public void SetAnisotropicFiltering()
    {
        QualitySettings.anisotropicFiltering = _configurationHandler.AnisotropicFiltering ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Disable;
        Plugin.Log.LogInfo("Anisotropic Filtering applied: " + _configurationHandler.AnisotropicFiltering);
    }

    public void SetPostProcessAA()
    {
        if (Camera.main.TryGetComponent(out UniversalAdditionalCameraData data))
        {
            data.antialiasing = (AntialiasingMode)Plugin.Instance.ConfigurationHandler.CameraAA;
        }
        Plugin.Log.LogInfo("Camera AA applied: " + _configurationHandler.CameraAA);
    }

    // public void SetMSAA()
    // {
    //     int sampleCount = _configurationHandler.MSAA == 0 ? 1 : _configurationHandler.MSAA;
    //
    //     ApplyToRenderPipelines(
    //         "MSAA",
    //         pipeline =>
    //         {
    //             pipeline.msaaSampleCount = sampleCount;
    //             SetPipelineMember(pipeline, "m_MSAA", (MsaaQuality)sampleCount);
    //         },
    //         pipeline => pipeline.msaaSampleCount.ToString()
    //     );
    // }

    public void SetFOV()
    {
        PlayerCameraMinder.fovFromSettings = _configurationHandler.FOV;
        Plugin.Log.LogInfo("FOV applied: " + _configurationHandler.FOV);
    }

    public void SetResolutionScale()
    {
        ApplyToRenderPipelines(
            "Render Scale",
            pipeline =>
            {
                pipeline.renderScale = _configurationHandler.RenderScale;
                SetPipelineMember(pipeline, "m_RenderScale", _configurationHandler.RenderScale);
            },
            pipeline => pipeline.renderScale.ToString("F3")
        );
    }

    public void SetUpscaler()
    {
        ApplyToRenderPipelines(
            "Upscaling Filter",
            pipeline =>
            {
                pipeline.upscalingFilter = (UpscalingFilterSelection)_configurationHandler.UpscalingFilter;
                SetPipelineMember(pipeline, "m_UpscalingFilter", (UpscalingFilterSelection)_configurationHandler.UpscalingFilter);
            },
            pipeline => pipeline.upscalingFilter.ToString()
        );
    }

    public void SetLODQuality()
    {
        QualitySettings.lodBias = _configurationHandler.LodQuality;
        Plugin.Log.LogInfo("LOD Bias applied: " + _configurationHandler.LodQuality);
    }

    // public void SetShadowDistance()
    // {
    //     ApplyToRenderPipelines(
    //         "Shadow Distance",
    //         pipeline =>
    //         {
    //             pipeline.shadowDistance = _configurationHandler.ShadowDistance;
    //             SetPipelineMember(pipeline, "m_ShadowDistance", (float)_configurationHandler.ShadowDistance);
    //         },
    //         pipeline => pipeline.shadowDistance.ToString("F1")
    //     );
    // }

    public void SetShadowCascades()
    {
        ApplyToRenderPipelines(
            "Shadow Cascades",
            pipeline =>
            {
                pipeline.shadowCascadeCount = _configurationHandler.ShadowCascades;
                SetPipelineMember(pipeline, "m_ShadowCascadeCount", _configurationHandler.ShadowCascades);
                SetPipelineMember(pipeline, "m_ShadowCascades", GetShadowCascadeOption(_configurationHandler.ShadowCascades));
            },
            pipeline => pipeline.shadowCascadeCount.ToString()
        );
    }

    private void ApplyToRenderPipelines(string settingName, Action<UniversalRenderPipelineAsset> apply, Func<UniversalRenderPipelineAsset, string> readBack)
    {
        List<UniversalRenderPipelineAsset> pipelines = new List<UniversalRenderPipelineAsset>();
        
        foreach (UniversalRenderPipelineAsset pipeline in Resources.FindObjectsOfTypeAll<UniversalRenderPipelineAsset>())
            pipelines.Add(pipeline);
        
        if (pipelines.Count == 0)
        {
            Plugin.Log.LogWarning($"{settingName} not applied: no UniversalRenderPipelineAsset is active or loaded");
            return;
        }

        foreach (UniversalRenderPipelineAsset pipeline in pipelines)
            apply(pipeline);

        Plugin.Log.LogInfo($"{settingName} applied to {pipelines.Count} URP asset(s), active value: {readBack(pipelines[0])}");
    }

    private static void SetPipelineMember(UniversalRenderPipelineAsset pipeline, string memberName, object value)
    {
        var property = pipeline.GetType().GetProperty(memberName, PipelineMemberFlags);
        if (property != null && property.CanWrite)
        {
            property.SetValue(pipeline, value);
            return;
        }

        var field = pipeline.GetType().GetField(memberName, PipelineMemberFlags);
        if (field != null)
            field.SetValue(pipeline, value);
    }

    private static ShadowCascadesOption GetShadowCascadeOption(int cascadeCount)
    {
        if (cascadeCount <= 1)
            return ShadowCascadesOption.NoCascades;
        if (cascadeCount <= 2)
            return ShadowCascadesOption.TwoCascades;
        return ShadowCascadesOption.FourCascades;
    }
}
