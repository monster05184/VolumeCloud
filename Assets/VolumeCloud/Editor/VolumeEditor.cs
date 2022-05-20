using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
[ExecuteInEditMode]
public class VolumeEditor : Editor
{
    
    public void OnSceneGUI()
    {
        var stack = VolumeManager.instance.stack;
        var cloudParameters = stack.GetComponent<CloudPostProgress>();
        if (cloudParameters.cantainerUIOn.value)
        {
            Gizmos.DrawWireCube(cloudParameters.center.value,cloudParameters.size.value);
        }
    }
}
