using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudFeature : ScriptableRendererFeature {
    public static bool GlobalActive = false;
    /// <summary>
    /// 体积云设置
    /// </summary>
    [Serializable]
    public class Setting
    {
        public RenderPassEvent renderPassEvent;
        public Material cloudMaterial;
        public Texture3D noiseMap;
        public RenderTexture sceneCatch;
        [Range(0, 10f)] public float centerPower;
    }
    public Setting setting = new Setting();
    private CloudEffectPass _cloudEffectPass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _cloudEffectPass.SetUp(renderer.cameraColorTarget);
       renderer.EnqueuePass(_cloudEffectPass);
    }
    public override void Create() {
        _cloudEffectPass = new CloudEffectPass(setting);
    }
}

public class CloudEffectPass : ScriptableRenderPass {
    private CloudFeature.Setting setting;
    private RenderTargetIdentifier source;
    public CloudEffectPass(CloudFeature.Setting setting)
    {
        this.setting = setting;
        renderPassEvent = setting.renderPassEvent;

    }

    public void SetUp(RenderTargetIdentifier source)
    {
        this.source = source;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (PrepareData())
        {
            CommandBuffer cmd = CommandBufferPool.Get("CloudFeature");
            cmd.Blit(source,setting.sceneCatch);
            cmd.Blit(setting.sceneCatch,source,setting.cloudMaterial);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        else
        {
            Debug.Log("数据未能正常准备");
        }



    }

    public bool PrepareData()
    {
        var stack = VolumeManager.instance.stack;
        var cloudParameters = stack.GetComponent<CloudPostProgress>();
        if (cloudParameters == null||!cloudParameters.IsActive()) return false;
        var cloudMaterial = setting.cloudMaterial;
        Vector3 boundMin = cloudParameters.center.value - cloudParameters.size.value*1/2;
        Vector3 boundMax = cloudParameters.center.value + cloudParameters.size.value*1/2;
        cloudMaterial.SetVector("BoundMin",boundMin);
        cloudMaterial.SetVector("BoundMax",boundMax);
        cloudMaterial.SetTexture("_ShapeNoise",setting.noiseMap);
        cloudMaterial.SetVector("_NoiseMapOffset",cloudParameters.offset.value);
        cloudMaterial.SetFloat("_NoiseMapScale",cloudParameters.scale.value);
        cloudMaterial.SetFloat("_MarchStep",cloudParameters.marchStep.value);
        cloudMaterial.SetColor("_ColorDark",cloudParameters.darkColor.value);
        cloudMaterial.SetColor("_ColorBright",cloudParameters.brightColor.value);
        cloudMaterial.SetFloat("_LightAbsorption",cloudParameters.lightAbsorb.value);
        cloudMaterial.SetFloat("_ColorOffset1",cloudParameters.colorOffset1.value);
        cloudMaterial.SetFloat("_ColorOffset2",cloudParameters.colorOffset2.value);
        cloudMaterial.SetFloat("_DarknessThredhold",cloudParameters.brightness.value);
        cloudMaterial.SetFloat("_NoiseMapMul",cloudParameters.cloudDensity.value);
        cloudMaterial.SetVector("_PhaseParams",cloudParameters.scatter.value);
        return true;
    }


}