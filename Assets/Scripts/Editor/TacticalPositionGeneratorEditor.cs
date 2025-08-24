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
            TacticalPositionData tacticalPositionData = posGenerator.TacticalPositionData;

            if (GUILayout.Button("Generate tactical grid spawners"))
            {
                GenerateGridSpawners(posGenerator);
            }

            if (GUILayout.Button("Generate tactical positions for AI"))
            {
                GenerateTacticalPositions(posGenerator, tacticalPositionData);
            }

            if (GUILayout.Button("Clear generated tactical positions for AI"))
            {
                ClearTacticalPositions(posGenerator, tacticalPositionData);
            }

            if (GUILayout.Button("Verify cover of tactical positions"))
            {
                VertifyTacticalPositionsCover(posGenerator);
            }

            base.OnInspectorGUI();
        }

        private void VertifyTacticalPositionsCover(TacticalPositionGenerator posGenerator)
        {
            if (posGenerator.genMode == CoverGenMode.all)
            {
                VerifyCoverOfAllPositions(posGenerator);
            }
            else
            {
                VerifyCoverOfSpecificPositions(posGenerator);
            }
        }

        private void VerifyCoverOfAllPositions(TacticalPositionGenerator posGenerator)
        {
            int count = posGenerator.TacticalPositionData.HighCornerPositions.Count +
                posGenerator.TacticalPositionData.LowCornerPositions.Count +
                posGenerator.TacticalPositionData.LowCoverPositions.Count;
            if (count == 0)
            {
                Debug.Log("There are no tactical positions to verify!");
            }
            else
            {
                posGenerator.VerifyPositionsCover(posGenerator.TacticalPositionData.HighCornerPositions);
                posGenerator.VerifyPositionsCover(posGenerator.TacticalPositionData.LowCornerPositions);
                posGenerator.VerifyPositionsCover(posGenerator.TacticalPositionData.LowCoverPositions);
            }
        }

        private void VerifyCoverOfSpecificPositions(TacticalPositionGenerator posGenerator)
        {
            List<TacticalPosition> targetData = GetSpecificData(posGenerator);
            if (targetData != null && targetData.Count > 0)
            {
                Debug.Log("Verifying cover of tactical positions.");
                posGenerator.VerifyPositionsCover(targetData);
            }
            else
            {
                Debug.Log("There are no positions to verify!");
            }
        }

        private void ClearTacticalPositions(TacticalPositionGenerator posGenerator, TacticalPositionData tacticalPositionData)
        {
            if (posGenerator.genMode == CoverGenMode.all)
            {
                ClearAllPositions(posGenerator);
            }
            else
            {
                ClearSpecificPositions(posGenerator);
            }

            EditorUtility.SetDirty(tacticalPositionData);
            AssetDatabase.SaveAssetIfDirty(tacticalPositionData);
        }

        private void ClearAllPositions(TacticalPositionGenerator posGenerator)
        {
            int count = posGenerator.TacticalPositionData.HighCornerPositions.Count +
                posGenerator.TacticalPositionData.LowCornerPositions.Count +
                posGenerator.TacticalPositionData.LowCoverPositions.Count;
            if (count == 0)
            {
                Debug.Log("All TacticalPositionData has already been cleared!");
            }
            else
            {
                posGenerator.ClearTacticalData(posGenerator.TacticalPositionData.HighCornerPositions);
                posGenerator.ClearTacticalData(posGenerator.TacticalPositionData.LowCornerPositions);
                posGenerator.ClearTacticalData(posGenerator.TacticalPositionData.LowCoverPositions);
                Debug.Log("All TacticalPositionData cleared!");
            }
        }

        private void ClearSpecificPositions(TacticalPositionGenerator posGenerator)
        {
            List<TacticalPosition> targetData = GetSpecificData(posGenerator);
            if (targetData != null && targetData.Count > 0)
            {
                posGenerator.ClearTacticalData(targetData);
                Debug.Log("TacticalPositionData cleared!");
            }
            else
            {
                Debug.Log("TacticalPositionData already cleared.");
            }
        }

        private static void GenerateGridSpawners(TacticalPositionGenerator posGenerator)
        {
            if (posGenerator.SpawnerData == null)
            {
                Debug.LogError($"SpawnerData is missing!");
                return;
            }

            posGenerator.CreateSpawnersAlongTheGrid();

            EditorUtility.SetDirty(posGenerator.SpawnerData);
            AssetDatabase.SaveAssetIfDirty(posGenerator.SpawnerData);
        }

        private void GenerateTacticalPositions(TacticalPositionGenerator posGenerator, TacticalPositionData tacticalPositionData)
        {
            if (posGenerator.genMode == CoverGenMode.all)
            {
                GenerateAllPositions(posGenerator);
            }
            else
            {
                GenerateSpecificPositions(posGenerator);
            }

            EditorUtility.SetDirty(tacticalPositionData);
            AssetDatabase.SaveAssetIfDirty(tacticalPositionData);
        }

        private void GenerateSpecificPositions(TacticalPositionGenerator posGenerator)
        {
            List<TacticalPosition> targetData = GetSpecificData(posGenerator);
            TacticalCornerSettings targetCornerSettings = GetSpecificCornerSettings(posGenerator);
            if (targetData == null)
            {
                Debug.LogError($"TacticalPositionData for {posGenerator.genMode} is missing!");
                return;
            }

            LoggingUtils.ClearConsole();
            posGenerator.GenerateTacticalPositions(targetData, targetCornerSettings, posGenerator.UseHandplacedTacticalProbes == false);
        }

        private void GenerateAllPositions(TacticalPositionGenerator posGenerator)
        {
            posGenerator.GenerateTacticalPositions(posGenerator.TacticalPositionData.HighCornerPositions, posGenerator._highCornerSettings, posGenerator.UseHandplacedTacticalProbes == false);
            posGenerator.GenerateTacticalPositions(posGenerator.TacticalPositionData.LowCornerPositions, posGenerator._lowCornerSettings, posGenerator.UseHandplacedTacticalProbes == false);
            posGenerator.GenerateTacticalPositions(posGenerator.TacticalPositionData.LowCoverPositions, posGenerator._lowCoverSettings, posGenerator.UseHandplacedTacticalProbes == false);
        }

        private List<TacticalPosition> GetSpecificData(TacticalPositionGenerator posGenerator)
        {
            List<TacticalPosition> targetData = null;

            switch (posGenerator.genMode)
            {
                case CoverGenMode.highCorners:
                    targetData = posGenerator.TacticalPositionData.HighCornerPositions;
                    break;
                case CoverGenMode.lowCorners:
                    targetData = posGenerator.TacticalPositionData.LowCornerPositions;
                    break;
                case CoverGenMode.lowCover:
                    targetData = posGenerator.TacticalPositionData.LowCoverPositions;
                    break;
                default:
                    Debug.LogError("Invalid cover generation mode!");
                    break;
            }

            return targetData;
        }

        private TacticalCornerSettings GetSpecificCornerSettings(TacticalPositionGenerator posGenerator)
        {
            TacticalCornerSettings targetCornerSettings = null;

            switch (posGenerator.genMode)
            {
                case CoverGenMode.highCorners:
                    targetCornerSettings = posGenerator._highCornerSettings;
                    break;
                case CoverGenMode.lowCorners:
                    targetCornerSettings = posGenerator._lowCornerSettings;
                    break;
                case CoverGenMode.lowCover:
                    targetCornerSettings = posGenerator._lowCoverSettings;
                    break;
                default:
                    Debug.LogError("Invalid cover generation mode!");
                    break;
            }

            return targetCornerSettings;
        }
    }
}