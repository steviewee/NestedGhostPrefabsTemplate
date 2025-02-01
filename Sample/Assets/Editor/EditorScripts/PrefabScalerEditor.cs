using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NGPTemplate.Authoring;
[CustomEditor(typeof(PrefabScalerAuthoring))]
public class PrefabScalerRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabScalerAuthoring script = (PrefabScalerAuthoring)target;
        PrefabUtility.RecordPrefabInstancePropertyModifications(script.gameObject);
        if (GUILayout.Button("CopyScale"))
        {
            script.CopyScale();
            EditorUtility.SetDirty(script);
            EditorUtility.ClearDirty(script);
        }
        if (GUILayout.Button("PasteScale"))
        {
            script.PasteScale();
            EditorUtility.SetDirty(script);
            EditorUtility.ClearDirty(script);
        }
        if (GUILayout.Button("ResetTransformScale"))
        {
            script.ResetTransformScale();
            EditorUtility.SetDirty(script);
            EditorUtility.ClearDirty(script);
        }
        if (GUILayout.Button("ResetCopiedScale"))
        {
            script.ResetCopiedScale();
            EditorUtility.SetDirty(script);
            EditorUtility.ClearDirty(script);
        }
    }
}
