using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    [CustomEditor(typeof(TacticalPosDebugGizmoGO))]
    public class TacticalPosDebugGizmoGOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gizmoScript = (TacticalPosDebugGizmoGO)target;

            if (!gizmoScript.ShowAllCorners)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Showing corner: " + (gizmoScript.CurrentCornerIndex + 1));
                //EditorGUILayout.LabelField("Corner Cycling Controls", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Previous"))
                {
                    gizmoScript.DecrementCornerIndex();
                }
                if (GUILayout.Button("Next"))
                {
                    gizmoScript.IncrementCornerIndex();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
