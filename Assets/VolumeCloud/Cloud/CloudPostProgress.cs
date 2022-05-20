using System;
using UnityEditor.Experimental.TerrainAPI;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Addition-Post-processing/Cloud")]
public class CloudPostProgress : VolumeComponent, IPostProcessComponent
{

    public Vector3Parameter size = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter center = new Vector3Parameter(new Vector3(1,1,1));
    public FloatParameter scale = new FloatParameter(1);
    public Vector3Parameter offset = new Vector3Parameter(new Vector3(1, 1, 1));
    public BoolParameter cantainerUIOn = new BoolParameter(false);
    public ColorParameter brightColor = new ColorParameter(Color.white, true, false, true, false);
    public ColorParameter darkColor = new ColorParameter(Color.white, true, false, true, false);
    [Range(0,0.1f)]
    public FloatParameter marchStep = new FloatParameter(0.01f);

    public FloatParameter lightAbsorb = new FloatParameter(0.3f);
    public FloatParameter colorOffset1 = new FloatParameter(0.72f);
    public FloatParameter colorOffset2 = new FloatParameter(0.36f);
    public FloatParameter brightness = new FloatParameter(0.14f);
    public FloatParameter cloudDensity = new FloatParameter(1f);
    public Vector4Parameter scatter = new Vector4Parameter(Vector4.one,false);

    public bool IsActive()
    {
        return active;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}