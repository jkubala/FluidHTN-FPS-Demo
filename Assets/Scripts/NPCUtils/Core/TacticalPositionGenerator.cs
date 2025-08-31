using System;
using System.Collections.Generic;
using System.Linq;
using FPSDemo.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC.Utilities
{
    [ExecuteInEditMode]
    public class TacticalPositionGenerator : MonoBehaviour
    {
        public enum CoverGenerationMode { all, lowCover, lowCorners, highCorners, manual }
        public enum GizmoViewMode { all, finished, unfinished }
        // ========================================================= INSPECTOR FIELDS
        [Header("General")]
        [SerializeField] private CoverGenerationMode _currentCoverGenMode = CoverGenerationMode.lowCover;
        [SerializeField] private TacticalGeneratorSettings _settings;
        [SerializeField] private GameObject _manualPositionsParent;
        [SerializeField] private GameObject manualPositionPrefab;
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

        public void LoadManualPositions()
        {
            Undo.SetCurrentGroupName("Load Manual Positions");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(_manualPositionsParent.transform, "Clear manual position children");
            ClearManualPositionParentChildren();
            CoverGenerationContext manualContext = _settings.GetContextsFor(CoverGenerationMode.manual).First();
            foreach (TacticalPosition position in manualContext.positionData.Positions)
            {
                GameObject newManualPosition = Instantiate(manualPositionPrefab, position.Position, position.mainCover.rotationToAlignWithCover, _manualPositionsParent.transform);
                Undo.RegisterCreatedObjectUndo(newManualPosition, "Create manual position GameObject");
                if (newManualPosition.TryGetComponent(out ManualPosition pos))
                {
                    Undo.RecordObject(pos, "Set tactical position data");
                    pos.tacticalPosition = position;
                }
                else
                {
                    Debug.LogError($"Manual position prefab not have ManualPosition script attached!");
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
        }

        private void ClearManualPositionParentChildren()
        {
            for (int i = _manualPositionsParent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _manualPositionsParent.transform.GetChild(i).gameObject;

#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child);
#else
                Destroy(child);
#endif
            }
        }

        public void SaveManualPositions()
        {
            Undo.SetCurrentGroupName("Save Manual Positions");
            int undoGroup = Undo.GetCurrentGroup();

            CoverGenerationContext manualContext = _settings.GetContextsFor(CoverGenerationMode.manual).First();
            Undo.RecordObject(manualContext.positionData, "Save manual position data");
            manualContext.positionData.Positions.Clear();

            for (int i = 0; i < _manualPositionsParent.transform.childCount; i++)
            {
                Transform child = _manualPositionsParent.transform.GetChild(i);
                if (child.TryGetComponent(out ManualPosition pos))
                {
                    manualContext.positionData.Positions.Add(pos.tacticalPosition);
                }
                else
                {
                    Debug.LogError($"{_manualPositionsParent} has a child '{child.name}' which does not have ManualPosition script attached!");
                }
            }

            Save(manualContext.positionData);
            Undo.CollapseUndoOperations(undoGroup);
        }

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

            Undo.SetCurrentGroupName("Generate Tactical Positions");
            int undoGroup = Undo.GetCurrentGroup();
            foreach (CoverGenerationContext context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                if ((_currentCoverGenMode != CoverGenerationMode.all && _currentCoverGenMode != context.genMode) || context.genMode == CoverGenerationMode.manual)
                {
                    continue;
                }
                Undo.RecordObject(context.positionData, "Generate Tactical Positions");
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
            Undo.CollapseUndoOperations(undoGroup);
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
            Undo.SetCurrentGroupName("Verify tactical positions");
            int undoGroup = Undo.GetCurrentGroup();
            foreach (var context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                if(context.genMode == CoverGenerationMode.manual)
                {
                    continue;
                }

                Undo.RecordObject(context.positionData, "Verify Tactical Data");
                TacticalPositionData oldTacticalPositionsSnapshot = ScriptableObject.CreateInstance<TacticalPositionData>();
                oldTacticalPositionsSnapshot.Positions = new(context.positionData.Positions);
                bool allPositionsValid = true;
                for (int i = context.positionData.Positions.Count - 1; i >= 0; i--)
                {
                    if (!VerifyCoverOfAPosition(context.positionData.Positions[i], context.positionData.Positions))
                    {
                        allPositionsValid = false;
                    }
                }
                if (!allPositionsValid)
                {
                    UpdateCoverPositionContext(oldTacticalPositionsSnapshot, context);
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
        }

        private bool VerifyCoverOfAPosition(TacticalPosition position, List<TacticalPosition> targetData)
        {
            if (!Physics.Raycast(position.Position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _settings.raycastMask))
            {
                Debug.LogError($"Position at {position.Position} did not have a solid ground underneath! Skipping validation!");
                return false;
            }

            Vector3 direction = position.mainCover.rotationToAlignWithCover * Vector3.forward;
            Vector3 origin = hit.point;
            for (float currentHeight = hit.point.y + _settings.positionSettings.bottomRaycastBuffer; currentHeight < position.Position.y; currentHeight += _settings.positionSettings.verticalStepToCheckForCover)
            {
                origin.y = currentHeight;
                if (!Physics.Raycast(origin, direction, _settings.positionSettings.distanceToCheckForCover, _settings.raycastMask))
                {
                    Debug.LogWarning($"Position at {position.Position} did not have a continuous cover to the ground! Removing it!");
                    targetData.Remove(position);
                    return false;
                }
            }
            return true;
        }


        public void ClearAllTacticalData()
        {
            Undo.SetCurrentGroupName("Clear All Tactical Data");
            int undoGroup = Undo.GetCurrentGroup();
            foreach (CoverGenerationContext context in _settings.GetContextsFor(_currentCoverGenMode))
            {
                if(context.genMode == CoverGenerationMode.manual)
                {
                    continue;
                }
                Undo.RecordObject(context.positionData, "Clear All Tactical Data");
                ClearTacticalData(context);
            }
            Undo.CollapseUndoOperations(undoGroup);
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
            Undo.RecordObject(_settings.gridSpawnerData, "Generate tactical position spawners");
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
            // List to store unique positions, initialized with manually placed positions
            List<TacticalPosition> uniquePositions = new(_settings.GetContextsFor(CoverGenerationMode.manual).First().positionData.Positions);

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