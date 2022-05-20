using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CreateNoise3D))]
public class CreateNoise3DEditor : Editor
{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();
        var noiseCreator = (CreateNoise3D)target;
        if(GUILayout.Button("生成噪声图")){
            noiseCreator.RenderNoiseTexture();
        }
        if (GUILayout.Button("保存贴图")) {
            noiseCreator.Saver();
        }
    }
}