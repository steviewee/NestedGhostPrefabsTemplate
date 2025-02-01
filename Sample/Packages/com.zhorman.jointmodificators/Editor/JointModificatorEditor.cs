using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zhorman.JointModificators.Runtime.Authoring;
    [CustomEditor(typeof(JointModificatorAuthoring))]
    public class JointModificatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            JointModificatorAuthoring script = (JointModificatorAuthoring)target;
            PrefabUtility.RecordPrefabInstancePropertyModifications(script.gameObject);
            if (GUILayout.Button("SetJoint"))
            {
                script.SetJoint();
                EditorUtility.SetDirty(script);
                EditorUtility.ClearDirty(script);
            }
        }
    }


