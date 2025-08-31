using FPSDemo.Utils;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    [CustomEditor(typeof(TacticalPositionGenerator))]
    public class TacticalPositionGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TacticalPositionGenerator posGenerator = (TacticalPositionGenerator)target;

            if (GUILayout.Button("Generate tactical grid spawners"))
            {
                posGenerator.CreateSpawnersAlongTheGrid();
            }

            if (GUILayout.Button("Generate tactical positions for AI"))
            {
                LoggingUtils.ClearConsole();
                posGenerator.GenerateTacticalPositions();
            }

            if (GUILayout.Button("Clear generated tactical positions for AI"))
            {
                posGenerator.ClearAllTacticalData();
            }

            if (GUILayout.Button("Verify cover of tactical positions"))
            {
                posGenerator.VerifyPositionsCover();
            }

            if (GUILayout.Button("Load manual positions"))
            {
                posGenerator.LoadManualPositions();
            }

            if (GUILayout.Button("Save manual positions"))
            {
                posGenerator.SaveManualPositions();
            }

            base.OnInspectorGUI();
        }
    }
}