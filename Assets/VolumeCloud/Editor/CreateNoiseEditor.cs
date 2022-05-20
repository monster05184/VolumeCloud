using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CreateNoise))]
public class CreateNoiseEditor : Editor
{
    public override void OnInspectorGUI(){
        base.OnInspectorGUI();
        var noiseCreator = (CreateNoise)target;
        if(GUILayout.Button("生成噪声图")){
            noiseCreator.RenderNoiseTexture();

        }
    }
}
