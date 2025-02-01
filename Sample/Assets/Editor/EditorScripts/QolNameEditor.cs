using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NGPTemplate.Authoring;
[CustomEditor(typeof(QolNameAuthoring))]
public class QolNameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        QolNameAuthoring script = (QolNameAuthoring)target;
        PrefabUtility.RecordPrefabInstancePropertyModifications(script.gameObject);
        if (GUILayout.Button("SetName"))
        {
            script.SetName();
            EditorUtility.SetDirty(script);
            EditorUtility.ClearDirty(script);
        }
    }
}
