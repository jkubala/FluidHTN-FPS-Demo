using System;
using System.Collections.Generic;
using FPSDemo.Utils;
using UnityEditor;
using UnityEngine;
using static FPSDemo.NPC.Utilities.TacticalPositionGenerator;

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
                posGenerator.ClearTacticalData();
            }

            if (GUILayout.Button("Verify cover of tactical positions"))
            {
                posGenerator.VerifyPositionsCover();
            }

            base.OnInspectorGUI();
        }
    }
}