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
    public class TacticalPositionGenerator
    {
        public TacticalPositionGenerator(TacticalGeneratorSettings settings, CornerFinder cornerFinder, PositionValidator positionValidator)
        {
            _settings = settings;
            _cornerFinder = cornerFinder;
            _positionValidator = positionValidator;
        }
        public enum CoverGenerationMode { all, lowCover, lowCorners, highCorners, manual }
        [SerializeField] private TacticalGeneratorSettings _settings;
        [SerializeField] private CornerFinder _cornerFinder;
        [SerializeField] private PositionValidator _positionValidator;

        public void UpdateCornerFinder(CornerFinder cornerFinder)
        {
            _cornerFinder = cornerFinder;
        }

        public TacticalGridSpawnerData GetSpawnerData { get { return _settings.gridSpawnerData; } }

        public event Action<TacticalPositionData, CoverGenerationContext> OnContextUpdated;

        // ========================================================= GENERATION

        public void LoadManualPositions(GameObject manualPositionPrefab, GameObject positionsParent)
        {
            Undo.SetCurrentGroupName("Load Manual Positions");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(positionsParent.transform, "Clear manual position children");
            ClearManualPositionParentChildren(positionsParent);
            CoverGenerationContext manualContext = _settings.GetContextsFor(CoverGenerationMode.manual).First();
            foreach (TacticalPosition position in manualContext.positionData.Positions)
            {
                GameObject newManualPosition = GameObject.Instantiate(manualPositionPrefab, position.Position, position.mainCover.rotationToAlignWithCover, positionsParent.transform);
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

        private void ClearManualPositionParentChildren(GameObject positionsParent)
        {
            for (int i = positionsParent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = positionsParent.transform.GetChild(i).gameObject;

#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child);
#else
                Destroy(child);
#endif
            }
        }

        public void SaveManualPositions(GameObject positionsParent)
        {
            Undo.SetCurrentGroupName("Save Manual Positions");
            int undoGroup = Undo.GetCurrentGroup();

            CoverGenerationContext manualContext = _settings.GetContextsFor(CoverGenerationMode.manual).First();
            Undo.RecordObject(manualContext.positionData, "Save manual position data");
            manualContext.positionData.Positions.Clear();

            for (int i = 0; i < positionsParent.transform.childCount; i++)
            {
                Transform child = positionsParent.transform.GetChild(i);
                if (child.TryGetComponent(out ManualPosition pos))
                {
                    manualContext.positionData.Positions.Add(pos.tacticalPosition);
                }
                else
                {
                    Debug.LogError($"{positionsParent} has a child '{child.name}' which does not have ManualPosition script attached!");
                }
            }

            UpdateCoverPositionContext(null, manualContext);
            Undo.CollapseUndoOperations(undoGroup);
        }

        public void GenerateTacticalPositions(CoverGenerationMode genMode)
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
            foreach (CoverGenerationContext context in _settings.GetContextsFor(genMode))
            {
                if ((genMode != CoverGenerationMode.all && genMode != context.cornerSettings.genMode) || context.cornerSettings.genMode == CoverGenerationMode.manual)
                {
                    continue;
                }
                Undo.RecordObject(context.positionData, "Generate Tactical Positions");
                TacticalPositionData oldTacticalPositionsSnapshot = ScriptableObject.CreateInstance<TacticalPositionData>();
                oldTacticalPositionsSnapshot.Positions = new(context.positionData.Positions);
                ClearTacticalData(context);

                List<RaycastHit> hits = new();
                foreach (Vector3 position in _settings.gridSpawnerData.Positions)
                {
                    hits.AddRange(GetHitsAround(position, context));
                }

                RemoveDuplicateHits(hits);

                List<CornerDetectionInfo> cornersFound = new();

                foreach (RaycastHit hit in hits)
                {
                    cornersFound.AddRange(GetCornersAt(hit, context));
                }

                for (int j = cornersFound.Count - 1; j >= 0; j--)
                {
                    TacticalPosition newPosition = ProcessCornerToTacticalPosition(cornersFound[j], context);
                    if (newPosition != null)
                    {
                        context.positionData.Positions.Add(newPosition);
                        cornersFound[j].debugData?.parentData?.MarkAsFinished();
                    }
                }

                RemoveDuplicates(_settings.positionSettings.distanceToRemoveDuplicates, context.positionData.Positions);
                UpdateCoverPositionContext(oldTacticalPositionsSnapshot, context);
            }
            Undo.CollapseUndoOperations(undoGroup);
        }

        public List<CoverGenerationContext> GetActiveCoverGenContexts(CoverGenerationMode genMode)
        {
            return _settings.GetContextsFor(genMode);
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

        public void ClearAllTacticalData(CoverGenerationMode genMode)
        {
            Undo.SetCurrentGroupName("Clear All Tactical Data");
            int undoGroup = Undo.GetCurrentGroup();
            foreach (CoverGenerationContext context in _settings.GetContextsFor(genMode))
            {
                if (context.cornerSettings.genMode == CoverGenerationMode.manual)
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
            List<TacticalPosition> uniquePositions = new(_settings.GetManualPositionData().Positions);

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

        private List<RaycastHit> GetHitsAround(Vector3 position, CoverGenerationContext context)
        {
            float angleBetweenRays = 360f / _settings.gridSettings.NumberOfRaysSpawner;
            Vector3 direction = Vector3.forward;
            Vector3 rayOrigin = position + Vector3.up * context.cornerSettings.heightToScanThisAt;
            List<RaycastHit> hitsFound = new();

            for (int i = 0; i < _settings.gridSettings.NumberOfRaysSpawner; i++)
            {
                if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _settings.gridSettings.DistanceBetweenPositions * 2, _settings.raycastMask))
                {
                    hitsFound.Add(hit);
                }

                direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
            }

            return hitsFound;
        }

        private List<CornerDetectionInfo> GetCornersAt(RaycastHit hit, CoverGenerationContext context)
        {
            List<CornerDetectionInfo> cornersFound = new();

            if (context.cornerSettings.genMode == CoverGenerationMode.lowCover)
            {
                CornerDetectionInfo lowCoverInfo = _cornerFinder.FindLowCoverPos(hit, context.cornerSettings, _settings.raycastMask);
                if (lowCoverInfo != null)
                {
                    cornersFound = new()
                    {
                        lowCoverInfo
                    };
                }
            }
            else if (context.cornerSettings.genMode == CoverGenerationMode.lowCorners || context.cornerSettings.genMode == CoverGenerationMode.highCorners)
            {
                cornersFound = _cornerFinder.FindCorners(hit, context.cornerSettings, _settings.raycastMask);
            }

            return cornersFound;
        }

        private TacticalPosition ProcessCornerToTacticalPosition(CornerDetectionInfo corner, CoverGenerationContext context)
        {
            CornerDetectionInfo adjustedInfo = _positionValidator.ValidateCornerPosition(corner, context.cornerSettings, _settings.positionSettings, _settings.raycastMask);
            if (adjustedInfo != null)
            {
                return CreateTacticalPosition(adjustedInfo, context.cornerSettings.cornerCheckPositionOffset, _settings.raycastMask);
            }

            return null;
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

        private TacticalPosition CreateTacticalPosition(CornerDetectionInfo cornerInfo, float cornerCheckPositionOffset, LayerMask raycastMask)
        {
            MainCover mainCover = new()
            {
                type = cornerInfo.coverType,
                rotationToAlignWithCover = Quaternion.LookRotation(-cornerInfo.coverWallNormal, Vector3.up)
            };

            TacticalPosition newTacticalPos = new()
            {
                Position = cornerInfo.position - cornerInfo.outDirection * cornerCheckPositionOffset,
                mainCover = mainCover,
                isOutside = SimpleIsOutsideCheck(cornerInfo.position, raycastMask),
                CoverDirections = Array.Empty<CoverHeight>()
            };

            if (cornerInfo.debugData != null)
            {
                cornerInfo.debugData.finalCornerPos = newTacticalPos.Position;
                cornerInfo.debugData.tacticalPosition = newTacticalPos;
            }
            return newTacticalPos;
        }

        bool SimpleIsOutsideCheck(Vector3 position, LayerMask raycastMask)
        {
            if (Physics.Raycast(position, Vector3.up, Mathf.Infinity, raycastMask))
            {
                return false;
            }

            return true;
        }

        private void RemoveDuplicateHits(List<RaycastHit> hits, float positionThreshold = 0.1f, float normalThreshold = 0.9f)
        {
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    float posDistance = Vector3.Distance(hits[i].point, hits[j].point);
                    float normalDot = Vector3.Dot(hits[i].normal, hits[j].normal);

                    if (posDistance < positionThreshold && normalDot > normalThreshold)
                    {
                        if (hits[i].distance > hits[j].distance)
                        {
                            hits.RemoveAt(i);
                        }
                        else
                        {
                            hits.RemoveAt(j);
                            i--;
                        }
                        break;
                    }
                }
            }
        }
    }

    public class CornerDetectionInfo
    {
        public CornerType cornerType;
        public CoverType coverType;
        public Vector3 position;
        public Vector3 coverWallNormal;
        public Vector3 positionFiringDirection;
        public Vector3 outDirection;
        public float distanceToCorner;
        public CornerDebugData debugData;
    };

    public enum CornerType
    {
        Convex,
        Concave
    }
}