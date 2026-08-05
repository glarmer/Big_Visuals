using System;
using UnityEngine;

namespace Big_Visuals;

public class CameraWatcher : MonoBehaviour
{
    private bool applied;

    public CameraWatcher(IntPtr ptr) : base(ptr)
    {
    }
    
    void Update()
    {
        if (!applied && Camera.main != null)
        {
            applied = true;
            Plugin.Instance.Settings.SetAllCameraSettings();
        }
    }
}