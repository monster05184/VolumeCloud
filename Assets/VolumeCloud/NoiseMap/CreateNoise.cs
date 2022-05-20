using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateNoise : MonoBehaviour
{
    [Header("编辑器设置")]
    int computeThreadGroupSize = 100;
    [SerializeField] private ComputeShader NoiseCompute;
    ComputeBuffer pointsBuffer;
    Texture2D noiseTexture;
    [SerializeField] private Material OutPut;
    [Header("噪声设置")]
    [Range(0.1f,1)]
    public float tilling;
    const int maxCellPixelNum = 100;
    const int PixelNum = 800;
    public void RenderNoiseTexture() {
        int cellPixelNum = (int)(tilling * maxCellPixelNum);
        //处理格子大小使得能够密铺
        while (PixelNum % cellPixelNum != 0) {
            cellPixelNum--;
        }
        var cellNum = PixelNum / cellPixelNum;
        //初始化噪声图
        RenderTexture NoiseMap = new RenderTexture(PixelNum,PixelNum,1);
        NoiseMap.enableRandomWrite = true;
        NoiseMap.Create();
        //预生成数据ComputeBuffer
        pointsBuffer = new ComputeBuffer(cellNum * cellNum, sizeof(float) * 2);
        Vector2Int[] points = new Vector2Int[cellNum*cellNum];
        for (int i = 0; i < cellNum;i++) {
            for (int j = 0; j < cellNum; j++) {
                int randomX = 0;//
                int randomY = 0;//
                randomX = Random.Range(i* cellPixelNum, (i+1)* cellPixelNum - 1);
                randomY = Random.Range(j* cellPixelNum, (j+1)* cellPixelNum - 1);
                points[i + j*cellNum] = new Vector2Int(randomX,randomY);
              
            }
        }  
        pointsBuffer.SetData(points);
        //调用ComputerBuffer
        int kernel = NoiseCompute.FindKernel("CSMain");
        NoiseCompute.SetInt("CellNum", cellNum);
        NoiseCompute.SetInt("CellLength",cellPixelNum);
        NoiseCompute.SetBuffer(kernel, "PointsBuffer", pointsBuffer);
        NoiseCompute.SetTexture(kernel,"NoiseTexture",NoiseMap);
        computeThreadGroupSize = PixelNum / 8;
        NoiseCompute.Dispatch(kernel,computeThreadGroupSize,computeThreadGroupSize,1);
        OutPut.SetTexture("_OutPut", NoiseMap);
    }
}
