using System;
using System.Collections.Generic;
using FPSDemo.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPositionGenerator : MonoBehaviour
    {
        public enum CoverGenerationMode { all, lowCover, lowCorners, highCorners }
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private CoverGenerationMode _currentCoverGenMode = CoverGenerationMode.lowCover;
        [SerializeField] private TacticalGeneratorProfile _profile;
        [SerializeField] private bool _showPositions = false;

        [SerializeField] private bool _createDebugGameObjects = false;
        [SerializeField] private bool _createDebugGameObjectsPosChanges = false;
        [SerializeField] private GameObject _debugGameObjectPosChangePrefab;

        [SerializeField] List<BasePositionClassifier> _classifiers = new();
        private List<BasePositionClassifier> Classifiers
        {
            get
            {
                if (_classifiers == null || _classifiers.Count == 0)
                {
                    _classifiers = new List<BasePositionClassifier>
                    {
                        new AddedPositionClassifier(_debugParentAdded, maxDistanceToConsiderSamePosition, maxDegreesDifferenceToConsiderSamePosition),
                        new RemovedPositionClassifier(_debugParentRemoved, maxDistanceToConsiderSamePosition, maxDegreesDifferenceToConsiderSamePosition),
                        new ModifiedPositionClassifier(_debugParentModified, maxDistanceToConsiderSamePosition, maxDegreesDifferenceToConsiderSamePosition)
                    };
                }
                return _classifiers;
            }
        }
        [SerializeField] private GameObject _debugParentAdded;
        [SerializeField] private GameObject _debugParentRemoved;
        [SerializeField] private GameObject _debugParentModified;

        [SerializeField] private GameObject _debugGameObjectPrefab;
        [SerializeField] private GameObject _debugGameObjectParent;

        [SerializeField] private Transform _gizmo3DCursor;
        [Range(1f, 5f)][SerializeField] private float distanceToCreateGizmos = 3f;

        [Header("Limits for difference when comparing after regeneration")]
        [Range(0.01f, 0.25f)][SerializeField] private float maxDistanceToConsiderSamePosition = 0.05f;
        [Range(1f, 3f)][SerializeField] private float maxDegreesDifferenceToConsiderSamePosition = 1f;

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

            if (_profile.gridSpawnerData != null && _profile.gridSpawnerData.Positions.Count == 0)
            {
                CreateSpawnersAlongTheGrid();
            }

            foreach (CoverGenerationContext context in _profile.GetContextsFor(_currentCoverGenMode))
            {
                if (_currentCoverGenMode != CoverGenerationMode.all && _currentCoverGenMode != context.genMode)
                {
                    continue;
                }
                List<TacticalPosition> oldTacticalPositionsSnapshot = new(context.positionData.Positions);
                InitContextData(context);

                foreach (Vector3 position in _profile.gridSpawnerData.Positions)
                {
                    CreatePositionsAtHitsAround(position, context);
                }
                RemoveDuplicates(_profile.positionSettings.distanceToRemoveDuplicates, context.positionData.Positions);
                LogDifferencesInNewlyGeneratedPositions(oldTacticalPositionsSnapshot, context.positionData.Positions);
                Save(context.positionData);
                if (_createDebugGameObjects)
                {
                    EditorUtility.SetDirty(_debugGameObjectParent);
                }
            }
        }

        private void LogDifferencesInNewlyGeneratedPositions(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
        {
            ComputeDifferences(oldPositions, newPositions);
        }

        public static void Save<T>(T obj) where T : ScriptableObject
        {
#if UNITY_EDITOR
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssetIfDirty(obj);
            }
#endif
        }

        private void ComputeDifferences(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions)
        {
            GameObject debugGO = null;

            if (_createDebugGameObjectsPosChanges)
            {
                debugGO = _debugGameObjectPosChangePrefab;
            }

            foreach (var classifier in Classifiers)
            {
                classifier.Classify(oldPositions, newPositions, debugGO);
            }
        }

        public void VerifyPositionsCover()
        {
            foreach (var context in _profile.GetContextsFor(_currentCoverGenMode))
            {
                for (int i = context.positionData.Positions.Count - 1; i >= 0; i--)
                {
                    VerifyCoverOfAPosition(context.positionData.Positions[i], context.positionData.Positions);
                }
            }
        }

        private void VerifyCoverOfAPosition(TacticalPosition position, List<TacticalPosition> targetData)
        {
            if (!Physics.Raycast(position.Position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _profile.raycastMask))
            {
                Debug.LogError($"Position at {position.Position} did not have a solid ground underneath! Skipping validation!");
                return;
            }

            Vector3 direction = position.mainCover.rotationToAlignWithCover * Vector3.forward;
            Vector3 origin = hit.point;
            for (float currentHeight = hit.point.y + _profile.positionSettings.bottomRaycastBuffer; currentHeight < position.Position.y; currentHeight += _profile.positionSettings.verticalStepToCheckForCover)
            {
                origin.y = currentHeight;
                if (!Physics.Raycast(origin, direction, _profile.positionSettings.distanceToCheckForCover, _profile.raycastMask))
                {
                    Debug.LogWarning($"Position at {position.Position} did not have a continuous cover to the ground! Removing it!");

                    GameObject newDebugGO = Instantiate(_debugGameObjectPrefab, _debugGameObjectParent.transform);
                    newDebugGO.transform.position = position.Position;
                    targetData.Remove(position);
                    return;
                }
            }
        }

        public void ClearTacticalData()
        {
            foreach (CoverGenerationContext context in _profile.GetContextsFor(_currentCoverGenMode))
            {
                ClearDebugGOs(context);
                context.positionData.Positions.Clear();
                Save(context.positionData);
            }
        }
        private void ClearDebugGOs(CoverGenerationContext context)
        {
            for (int i = _debugGameObjectParent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _debugGameObjectParent.transform.GetChild(i).gameObject;
                if (child.TryGetComponent(out TacticalPosDebugGO debugGO))
                {
                    if (debugGO.TacticalDebugData.genMode == context.genMode)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(child);
#else
                    Destroy(child);
#endif
                        context.debugData.Remove(debugGO.TacticalDebugData);
                    }
                }
                else
                {
                    Debug.LogError($"{child.name} did not have TacticalPosDebugGO script attached!");
                }
            }
        }

        private void InitContextData(CoverGenerationContext context)
        {
            if (context.positionData.Positions.Count > 0)
            {
                context.positionData.Positions.Clear();
            }

            if (context.debugData.Count > 0)
            {
                ClearDebugGOs(context);
                context.debugData.Clear();
            }
        }

        public void CreateSpawnersAlongTheGrid()
        {
            _profile.gridSpawnerData.Positions.Clear();
            Debug.Log("Generating tactical position spawners");
            Vector3 currentPos = _profile.gridSettings.StartPos;
            bool otherRow = false;
            while (currentPos.x < _profile.gridSettings.EndPos.x)
            {
                currentPos.z = _profile.gridSettings.StartPos.z;

                if (_profile.gridSettings.OffsetEveryOtherRow)
                {
                    if (otherRow)
                    {
                        currentPos.z += _profile.gridSettings.DistanceBetweenPositions / 2;
                    }
                    otherRow = !otherRow;
                }

                while (currentPos.z < _profile.gridSettings.EndPos.z)
                {
                    RaycastHit[] hitsToConvertToPos = PhysicsUtils.RaycastTrulyAll(currentPos, Vector3.down, _profile.raycastMask, 0.1f, 100f);
                    foreach (var hit in hitsToConvertToPos)
                    {
                        if (PositionValid(hit.point))
                        {
                            _profile.gridSpawnerData.Positions.Add(hit.point);
                        }
                    }
                    currentPos.z += _profile.gridSettings.DistanceBetweenPositions;
                }
                currentPos.x += _profile.gridSettings.DistanceBetweenPositions;
            }
            Save(_profile.gridSpawnerData);
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
                        && angleDifference < _profile.positionSettings.maxAngleDifferenceToRemoveDuplicates)
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

            if (Physics.Raycast(start, direction.normalized, out _, distance, _profile.raycastMask))
            {
                return false;
            }

            return true;
        }

        private bool PositionValid(Vector3 position)
        {
            // Discard the position if it is too far from NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _profile.raycastMask))
            {
                Vector3 heightAdjustedHit = hit.position;
                heightAdjustedHit.y = position.y;

                if (Vector3.Distance(heightAdjustedHit, position) > _profile.positionSettings.RequiredProximityToNavMesh)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Discard the position if it spawned in the geometry
            Vector3 rayCastOrigin = position + Vector3.up * _profile.positionSettings.geometryCheckYOffset;

            if (Physics.Raycast(rayCastOrigin, Vector3.down, out RaycastHit geometryHit, _profile.positionSettings.geometryCheckYOffset + _profile.gridSettings.floatPrecisionBuffer, _profile.raycastMask))
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
            float angleBetweenRays = 360f / _profile.gridSettings.NumberOfRaysSpawner;
            Vector3 direction = Vector3.forward;
            Vector3 rayOrigin = position + Vector3.up * context.cornerSettings.heightToScanThisAt;

            for (int i = 0; i < _profile.gridSettings.NumberOfRaysSpawner; i++)
            {
                if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, _profile.gridSettings.DistanceOfRaycasts, _profile.raycastMask))
                {
                    TacticalDebugData debugData = null;
                    if (_createDebugGameObjects && Vector3.Distance(hit.point, _gizmo3DCursor.position) < distanceToCreateGizmos)
                    {
                        TacticalPosDebugGO debugGO = Instantiate(_debugGameObjectPrefab, _debugGameObjectParent.transform).GetComponent<TacticalPosDebugGO>();
                        debugGO.transform.position = hit.point;
                        debugData = debugGO.TacticalDebugData;
                        debugData.genMode = context.genMode;
                        context.debugData.Add(debugData);
                    }

                    if (context.cornerSettings.lowCover)
                    {
                        CoverPositioner.GetCoverPositioner.FindLowCoverPos(hit, context.cornerSettings, _profile.raycastMask, context.positionData.Positions, debugData);
                    }
                    else
                    {
                        CoverPositioner.GetCoverPositioner.FindCornerPos(hit, CoverHeight.HighCover, context.cornerSettings, _profile.raycastMask, context.positionData.Positions, debugData);
                    }
                }

                direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
            }
        }

        private bool ValidateParams()
        {
            if (_profile.gridSettings == null)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The grid settings are not assigned!");
                return false;
            }

            if (_profile.gridSettings.DistanceBetweenPositions < 0.5f)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The distance between positions is too small!");
                return false;
            }

            return true;
        }


        // ========================================================= DEBUG

        void OnDrawGizmosSelected()
        {
            if (!_showPositions)
            {
                return;
            }

            int totalCount = 0;
            foreach (var activeContext in _profile.GetContextsFor(_currentCoverGenMode))
            {
                totalCount += DisplayPositions(activeContext.positionData.Positions);
            }

            Debug.Log($"Currently displaying {totalCount} positions");
        }

        private int DisplayPositions(List<TacticalPosition> positions)
        {
            if (positions != null)
            {
                foreach (TacticalPosition position in positions)
                {
                    if (position.mainCover.type == CoverType.LeftCorner)
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (position.mainCover.type == CoverType.RightCorner)
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }

                    if (position.isOutside)
                    {
                        Gizmos.DrawSphere(position.Position, 0.1f);
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(position.Position, 0.1f);
                    }

                    Gizmos.DrawRay(position.Position, position.mainCover.rotationToAlignWithCover * Vector3.forward);
                }
                return positions.Count;
            }
            return 0;
        }

        public void ClearAllDebugGOs()
        {
            foreach (BasePositionClassifier classifier in Classifiers)
            {
                classifier.ClearDebugGOs();
            }
        }
    }
}