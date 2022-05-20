using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateNoise3D : MonoBehaviour {
    [Header("编辑器设置")]
    int computeThreadGroupSize = 100;
    [SerializeField] private ComputeShader NoiseCompute;//GPU计算ComputeShader
    ComputeBuffer pointsBuffer;//储存随机的点的位置的buffer
    [HideInInspector][SerializeField] RenderTexture NoiseMap;//三维的噪点贴图
    [SerializeField] private Material OutPut;//查看输出的材质
    [SerializeField] private ComputeShader Slicer;
    [SerializeField] private string SaveName;
    [Range(0, 200)]
    [SerializeField] private int OutputLayer;//查看三维噪点图的一层
    private int formerLayer;
    [Header("噪声设置")]
    [Range(0.1f, 1)]
    public float tilling;//噪点的缩放
    const int maxCellPixelNum = 100;//最大每个格子的大小
    const int PixelNum = 200;//图像的像素宽度
    bool IsNoiseMapRendered = false;
    public void RenderNoiseTexture() {
        int cellPixelNum = (int)(tilling * maxCellPixelNum);
        //处理格子大小使得能够密铺
        while (PixelNum % cellPixelNum != 0) {
            cellPixelNum--;
        }
        var cellNum = PixelNum / cellPixelNum;
        //初始化噪声图
        NoiseMap?.Release();
        NoiseMap = new RenderTexture(PixelNum, PixelNum, 0) {
            volumeDepth = PixelNum,
            enableRandomWrite = true,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D
        };
        NoiseMap.Create();
        //预生成数据ComputeBuffer
        pointsBuffer = new ComputeBuffer(cellNum * cellNum * cellNum, sizeof(int) * 3);
        Vector3Int[] points = new Vector3Int[cellNum * cellNum * cellNum];
        for (int i = 0; i < cellNum; i++) {
            for (int j = 0; j < cellNum; j++) {
                for (int k = 0; k < cellNum; k++) {
                    int randomX = Random.Range(i * cellPixelNum, (i + 1) * cellPixelNum - 1);
                    int randomY = Random.Range(j * cellPixelNum, (j + 1) * cellPixelNum - 1);
                    int randomZ = Random.Range(k * cellPixelNum, (k + 1) * cellPixelNum - 1);
                    points[i + cellNum * (j + cellNum * k)] = new Vector3Int(randomX, randomY, randomZ);
                }
            }
        }
        pointsBuffer.SetData(points);
        //调用ComputerBuffer
        int kernel = NoiseCompute.FindKernel("CSMain");
        NoiseCompute.SetInt("CellNum", cellNum);
        NoiseCompute.SetInt("CellLength", cellPixelNum);
        NoiseCompute.SetBuffer(kernel, "PointsBuffer", pointsBuffer);
        NoiseCompute.SetTexture(kernel, "NoiseTexture", NoiseMap);
        computeThreadGroupSize = PixelNum / 8;
        NoiseCompute.Dispatch(kernel, computeThreadGroupSize, computeThreadGroupSize, computeThreadGroupSize);
        //OutPut.SetTexture("_OutPut", NoiseMap);
        IsNoiseMapRendered = true;//贴图已渲染完
        pointsBuffer.Release();
        Slicer.SetTexture(0, "voxels", NoiseMap);

    }
    public void OnDrawGizmos() {
        if (Slicer != null && formerLayer != OutputLayer && IsNoiseMapRendered) {
            RenderTexture outputTexture = new RenderTexture(PixelNum, PixelNum, 0);
            outputTexture.enableRandomWrite = true;
            outputTexture.Create();
            //OutPut.SetTexture("_OutPut", outputTexture);
            Slicer.SetInt("layer", OutputLayer);
            Slicer.SetTexture(0, "Result", outputTexture);

            Slicer.Dispatch(0, computeThreadGroupSize, computeThreadGroupSize, computeThreadGroupSize);
            Debug.Log("切片");
            OutPut.SetTexture("_OutPut", outputTexture);
        }
        formerLayer = OutputLayer;
    }
    public void Saver() {
        if (IsNoiseMapRendered) {
            Slicer.SetTexture(0, "voxels", NoiseMap);
            Texture3D SaveTex = GetTex3D(RT2TexArray());
            AssetDatabase.CreateAsset(SaveTex, "Assets/" + SaveName + ".asset");
        } else {
            Debug.Log("还没有渲染贴图");
        }
    }
    private Texture3D GetTex3D(Texture2D[] tex2DArray) {
        Texture3D export = new Texture3D(PixelNum,PixelNum,PixelNum,TextureFormat.ARGB32,false);
        export.filterMode = FilterMode.Trilinear;
        Color[] outputPixels = export.GetPixels();
        for (int k = 0; k < PixelNum; k++) {
            Color[] layerPixels = tex2DArray[k].GetPixels();
            for (int i = 0; i < PixelNum; i++) {
                for (int j = 0; j < PixelNum; j++) {
                    outputPixels[i + j * PixelNum + k * PixelNum * PixelNum] = layerPixels[i + j * PixelNum];
                }
            }
        }
        export.SetPixels(outputPixels);
        export.Apply();
        return export;
    }
    private Texture2D[] RT2TexArray() {
        Texture2D[] texArray = new Texture2D[PixelNum];
        for (int layer = 0; layer < PixelNum; layer++) {
            RenderTexture layerRT = new RenderTexture(PixelNum, PixelNum, 0);
            layerRT.enableRandomWrite = true;
            layerRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            Slicer.SetTexture(0, "Result", layerRT);
            Slicer.SetInt("layer", layer);
            Slicer.Dispatch(0, computeThreadGroupSize, computeThreadGroupSize, computeThreadGroupSize);
            texArray[layer] = RT2Tex2D(layerRT);
        }
        //NoiseMap?.Release();
        return texArray;
    }
    private Texture2D RT2Tex2D(RenderTexture rt) {
        Texture2D output = new Texture2D(PixelNum, PixelNum);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, PixelNum, PixelNum), 0, 0);
        output.Apply();
        //rt?.Release();
        return output;    
    }

}
