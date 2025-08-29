using System;
using System.Collections.Generic;
using Codice.Client.Common.GameUI;
using FPSDemo.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC.Utilities
{
    [ExecuteInEditMode]
    public class TacticalPositionGenerator : MonoBehaviour
    {
        public enum CoverGenerationMode { all, lowCover, lowCorners, highCorners }
        public enum GizmoViewMode { all, finished, unfinished }
        // ========================================================= INSPECTOR FIELDS
        [Header("General")]
        [SerializeField] private CoverGenerationMode _currentCoverGenMode = CoverGenerationMode.lowCover;
        [SerializeField] private TacticalGeneratorSettings _settings;
        public bool _showPositions = false;
        [Header("Gizmo debug")]
        [SerializeField] private GizmoViewMode _currentGizmoViewMode;
        private GizmoViewMode _lastGizmoViewMode;
        [SerializeField] private bool _createGizmoDebugObjects = false;
        [Range(1f, 5f)] public float _distanceToCreateGizmos = 3f;
        [Header("Position modification debug")]
        public bool _createPosChangesDebugObjects = false;

        public bool ShowPositions { get { return _showPositions; } }
        public float DistanceToCreateGizmos { get { return _distanceToCreateGizmos; } }
        public bool CreatePosChangesDebugObjects
        {
            get { return _createPosChangesDebugObjects; }
        }

        public event Action<TacticalPositionData, CoverGenerationContext> OnContextUpdated;
        public event Action<Vector3, TacticalDebugData, GizmoViewMode> OnNewPotentialPositionCreated;
        public event Action<GizmoViewMode> OnGizmoViewModeChange;

        private void OnEnable()
        {
            _lastGizmoViewMode = _currentGizmoViewMode;
            OnGizmoViewModeChange?.Invoke(_currentGizmoViewMode);
        }

        private void OnValidate()
        {
            if (_lastGizmoViewMode != _currentGizmoViewMode)
            {
                _lastGizmoViewMode = _currentGizmoViewMode;
                OnGizmoViewModeChange?.Invoke(_lastGizmoViewMode);
            }
        }

        // ========================================================= GENERATION

        //// TODO this approach not done, just the GenerateTacticalPositionSpawners below
        //public void AddTacticalProbes(bool clearPositions, TacticalProbe[] probes)
        //{
        //    if (probes == null || probes.Length == 0)
        //    {
        //        return;
        //    }

        //    if (!ValidateParams())
        //    {
        //        return;
        //    }

        //    if (_tacticalPositionData.Positions == null)
        //    {
        //        _tacticalPositionData.Positions = new List<TacticalPosition>();
        //    }
        //    else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
        //    {
        //        _tacticalPositionData.Positions.Clear();
        //    }

        //    foreach (var probe in probes)
        //    {
        //        _tacticalPositionData.Positions.Add(new TacticalPosition
        //        {
        //            Position = probe.transform.position,
        //            CoverDirections = new CoverHeight[1]
        //            {
        //                new() {

        //                }
        //            }
        //        });
        //    }
        //}

        public void GenerateTacticalPositions()
        {
            Debug.Log("Generating tactical positions for AI");
            if (!ValidateParams())
            {
                return;
            }

            if (_settings.gridSpawnerData != null && _settings.gridSpawnerData.Positions.Count == 0)
            {
                CreateSpawnersAlongTheGrid();
            }

            foreach (CoverGenerationContext context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                if (_currentCoverGenMode != CoverGenerationMode.all && _currentCoverGenMode != context.genMode)
                {
                    continue;
                }
                TacticalPositionData oldTacticalPositionsSnapshot = ScriptableObject.CreateInstance<TacticalPositionData>();
                oldTacticalPositionsSnapshot.Positions = new(context.positionData.Positions);
                ClearTacticalData(context);

                foreach (Vector3 position in _settings.gridSpawnerData.Positions)
                {
                    CreatePositionsAtHitsAround(position, context);
                }
                RemoveDuplicates(_settings.positionSettings.distanceToRemoveDuplicates, context.positionData.Positions);
                UpdateCoverPositionContext(oldTacticalPositionsSnapshot, context);
            }
        }

        public List<CoverGenerationContext> GetActiveCoverGenContexts()
        {
            return _settings.GetContextsFor(_currentCoverGenMode);
        }

        private void UpdateCoverPositionContext(TacticalPositionData oldPositions, CoverGenerationContext context)
        {
            OnContextUpdated?.Invoke(oldPositions, context);
            Save(context.positionData);
        }

        private void Save<T>(T obj) where T : ScriptableObject
        {
#if UNITY_EDITOR
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssetIfDirty(obj);
            }
#endif
        }

        public void VerifyPositionsCover()
        {
            foreach (var context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                for (int i = context.positionData.Positions.Count - 1; i >= 0; i--)
                {
                    VerifyCoverOfAPosition(context.positionData.Positions[i], context.positionData.Positions);
                }
            }
        }

        private void VerifyCoverOfAPosition(TacticalPosition position, List<TacticalPosition> targetData)
        {
            if (!Physics.Raycast(position.Position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _settings.raycastMask))
            {
                Debug.LogError($"Position at {position.Position} did not have a solid ground underneath! Skipping validation!");
                return;
            }

            Vector3 direction = position.mainCover.rotationToAlignWithCover * Vector3.forward;
            Vector3 origin = hit.point;
            for (float currentHeight = hit.point.y + _settings.positionSettings.bottomRaycastBuffer; currentHeight < position.Position.y; currentHeight += _settings.positionSettings.verticalStepToCheckForCover)
            {
                origin.y = currentHeight;
                if (!Physics.Raycast(origin, direction, _settings.positionSettings.distanceToCheckForCover, _settings.raycastMask))
                {
                    Debug.LogWarning($"Position at {position.Position} did not have a continuous cover to the ground! Removing it!");

                    //GameObject newDebugGO = Instantiate(_debugGameObjectPrefab, _debugGameObjectParent.transform);
                    //newDebugGO.transform.position = position.Position;
                    targetData.Remove(position);
                    return;
                }
            }
        }

        public void ClearAllTacticalData()
        {
            foreach (CoverGenerationContext context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                ClearTacticalData(context);
            }
        }


        private void ClearTacticalData(CoverGenerationContext context)
        {
            if (context.positionData.Positions.Count > 0)
            {
                context.positionData.Positions.Clear();
            }
            UpdateCoverPositionContext(null, context);
        }

        public void CreateSpawnersAlongTheGrid()
        {
            _settings.gridSpawnerData.Positions.Clear();
            Debug.Log("Generating tactical position spawners");
            Vector3 currentPos = _settings.gridSettings.StartPos;
            bool otherRow = false;
            while (currentPos.x < _settings.gridSettings.EndPos.x)
            {
                currentPos.z = _settings.gridSettings.StartPos.z;

                if (_settings.gridSettings.OffsetEveryOtherRow)
                {
                    if (otherRow)
                    {
                        currentPos.z += _settings.gridSettings.DistanceBetweenPositions / 2;
                    }
                    otherRow = !otherRow;
                }

                while (currentPos.z < _settings.gridSettings.EndPos.z)
                {
                    RaycastHit[] hitsToConvertToPos = PhysicsUtils.RaycastTrulyAll(currentPos, Vector3.down, _settings.raycastMask, 0.1f, 100f);
                    foreach (var hit in hitsToConvertToPos)
                    {
                        if (PositionValid(hit.point))
                        {
                            _settings.gridSpawnerData.Positions.Add(hit.point);
                        }
                    }
                    currentPos.z += _settings.gridSettings.DistanceBetweenPositions;
                }
                currentPos.x += _settings.gridSettings.DistanceBetweenPositions;
            }
            Save(_settings.gridSpawnerData);
        }

        private void RemoveDuplicates(float distanceThreshold, List<TacticalPosition> targetData)
        {
            // List to store unique positions
            List<TacticalPosition> uniquePositions = new();

            // Iterate over each position
            for (int i = 0; i < targetData.Count; i++)
            {
                TacticalPosition currentPos = targetData[i];
                bool isDuplicate = false;

                // Compare with every other position
                for (int j = 0; j < uniquePositions.Count; j++)
                {
                    float angleDifference = Vector3.Angle(currentPos.mainCover.rotationToAlignWithCover * Vector3.forward, uniquePositions[j].mainCover.rotationToAlignWithCover * Vector3.forward);
                    // If the distance between currentPos and any unique position is less than the threshold, it's a duplicate
                    if (Vector3.Distance(currentPos.Position, uniquePositions[j].Position) < distanceThreshold
                        && currentPos.mainCover.type == uniquePositions[j].mainCover.type
                        && currentPos.mainCover.height == uniquePositions[j].mainCover.height
                        && NoObstacleBetween(currentPos.Position, uniquePositions[j].Position)
                        && angleDifference < _settings.positionSettings.maxAngleDifferenceToRemoveDuplicates)
                    {
                        isDuplicate = true;
                        break; // No need to check further, it's already a duplicate
                    }
                }

                if (!isDuplicate)
                {
                    uniquePositions.Add(currentPos);
                }

            }
            targetData.Clear();
            targetData.AddRange(uniquePositions);
        }

        public bool NoObstacleBetween(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (Physics.Raycast(start, direction.normalized, out _, distance, _settings.raycastMask))
            {
                return false;
            }

            return true;
        }

        private bool PositionValid(Vector3 position)
        {
            // Discard the position if it is too far from NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _settings.raycastMask))
            {
                Vector3 heightAdjustedHit = hit.position;
                heightAdjustedHit.y = position.y;

                if (Vector3.Distance(heightAdjustedHit, position) > _settings.positionSettings.RequiredProximityToNavMesh)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Discard the position if it spawned in the geometry
            Vector3 rayCastOrigin = position + Vector3.up * _settings.positionSettings.geometryCheckYOffset;

            if (Physics.Raycast(rayCastOrigin, Vector3.down, out RaycastHit geometryHit, _settings.positionSettings.geometryCheckYOffset + _settings.gridSettings.floatPrecisionBuffer, _settings.raycastMask))
            {
                if (!Mathf.Approximately(rayCastOrigin.y - geometryHit.point.y, rayCastOrigin.y - position.y))
                {
                    return false;
                }
            }

            return true;
        }

        private void CreatePositionsAtHitsAround(Vector3 position, CoverGenerationContext context)
        {
            float angleBetweenRays = 360f / _settings.gridSettings.NumberOfRaysSpawner;
            Vector3 direction = Vector3.forward;
            Vector3 rayOrigin = position + Vector3.up * context.cornerSettings.heightToScanThisAt;

            for (int i = 0; i < _settings.gridSettings.NumberOfRaysSpawner; i++)
            {
                if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _settings.gridSettings.DistanceOfRaycasts, _settings.raycastMask))
                {
                    TacticalDebugData debugData = null;
                    if (_createGizmoDebugObjects)
                    {
                        debugData = new()
                        {
                            genMode = context.genMode
                        };
                    }

                    if (context.cornerSettings.lowCover)
                    {
                        CoverPositioner.GetCoverPositioner.FindLowCoverPos(hit, context.cornerSettings, _settings.raycastMask, context.positionData.Positions, debugData);
                    }
                    else
                    {
                        CoverPositioner.GetCoverPositioner.FindCornerPos(hit, CoverHeight.HighCover, context.cornerSettings, _settings.raycastMask, context.positionData.Positions, debugData);
                    }

                    if (debugData != null)
                    {
                        OnNewPotentialPositionCreated?.Invoke(hit.point, debugData, _currentGizmoViewMode);
                    }
                }

                direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
            }
        }

        private bool ValidateParams()
        {
            if (_settings.gridSettings == null)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The grid settings are not assigned!");
                return false;
            }

            if (_settings.gridSettings.DistanceBetweenPositions < 0.5f)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The distance between positions is too small!");
                return false;
            }

            return true;
        }
    }
}