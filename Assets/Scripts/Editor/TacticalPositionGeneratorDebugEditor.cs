using UnityEngine;
using UnityEditor;
namespace FPSDemo.NPC.Utilities
{

    [CustomEditor(typeof(TacticalPositionGeneratorDebug))]
    public class TacticalPositionGeneratorDebugEditor : Editor
    {
        private TacticalPositionGeneratorDebug _generatorDebug;

        private void OnEnable()
        {
            _generatorDebug = (TacticalPositionGeneratorDebug)target;
        }


        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.LabelField("3D Cursor", EditorStyles.boldLabel);

            if (_generatorDebug.Gizmo3DCursor == null)
            {
                EditorGUILayout.HelpBox("No 3D cursor assigned.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Cursor Position", _generatorDebug.Gizmo3DCursor.position.ToString("F2"));
                EditorGUILayout.HelpBox("Hold Shift and click in Scene view to move the 3D cursor.", MessageType.Info);
            }
        }

        private void OnSceneGUI()
        {
            if (_generatorDebug.Gizmo3DCursor == null)
            {
                return;
            }

            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                Vector3 newPosition;
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    newPosition = hit.point;
                }
                else
                {
                    newPosition = ray.origin + ray.direction * 10f;
                }

                Undo.RecordObject(_generatorDebug.Gizmo3DCursor, "Move 3D Cursor");
                _generatorDebug.Gizmo3DCursor.position = newPosition;
                EditorUtility.SetDirty(_generatorDebug.Gizmo3DCursor);

                e.Use();
            }
        }
    }
}