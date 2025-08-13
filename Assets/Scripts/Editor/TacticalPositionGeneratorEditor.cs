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

            if (GUILayout.Button("Generate tactical positions for AI"))
            {
                if (posGenerator.TacticalPositionData == null)
                {
                    Debug.LogError("TacticalPositionData is missing!");
                    return;
                }

                if (posGenerator.UseHandplacedTacticalProbes)
                {
                    var probes = FindObjectsByType<TacticalProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (probes != null && probes.Length > 0)
                    {
                        posGenerator.AddTacticalProbes(clearPositions: true, probes);
                        if (probes.Length == 1)
                        {
                            Debug.Log($"Added 1 tactical probe from scene!");
                        }
                        else
                        {
                            Debug.Log($"Added {probes.Length} tactical probes from scene!");
                        }
                    }
                    else
                    {
                        Debug.LogError("Found 0 tactical probes in scene!");
                        return;
                    }
                }

                if (posGenerator.GenerateAutoProbeGrid)
                {
                    LoggingUtils.ClearConsole();
                    posGenerator.GenerateTacticalPositionSpawners(clearPositions: posGenerator.UseHandplacedTacticalProbes == false);
                }

                EditorUtility.SetDirty(posGenerator.TacticalPositionData);
                AssetDatabase.SaveAssetIfDirty(posGenerator.TacticalPositionData);
            }

            if (GUILayout.Button("Clear generated tactical positions for AI"))
            {
                if (posGenerator.TacticalPositionData != null && posGenerator.TacticalPositionData.Positions.Count > 0)
                {
                    Debug.Log("TacticalPositionData cleared!");
                    posGenerator.TacticalPositionData.Positions.Clear();
                    EditorUtility.SetDirty(posGenerator.TacticalPositionData);
                    AssetDatabase.SaveAssetIfDirty(posGenerator.TacticalPositionData);
                }
                else
                {
                    Debug.Log("TacticalPositionData already cleared.");
                }
            }

            if (GUILayout.Button("Verify cover of tactical positions"))
            {
                if (posGenerator.TacticalPositionData != null && posGenerator.TacticalPositionData.Positions.Count > 0)
                {
                    Debug.Log("Verifying cover of tactical positions.");
                    posGenerator.VerifyPositionsCover();
                }
                else
                {
                    Debug.Log("There are no positions to verify!");
                }
            }

            base.OnInspectorGUI();
        }
    }
}