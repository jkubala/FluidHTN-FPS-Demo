using System;
using System.Collections.Generic;
using FPSDemo.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPositionGenerator : MonoBehaviour
    {
        enum CoverGenMode { low, corners }
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] CoverGenMode genMode = CoverGenMode.low;
        [SerializeField] private TacticalPositionData _tacticalPositionData;
        [SerializeField] private TacticalGridGenerationSettings _gridSettings;
        [SerializeField] private TacticalCornerSettings _highCornerSettings;
        [SerializeField] private TacticalCornerSettings _lowCornerSettings;
        [SerializeField] private TacticalCornerSettings _lowCoverSettings;
        [SerializeField] private TacticalPositionSettings _positionSettings;
        [SerializeField] private LayerMask _raycastMask = 1 << 0;
        [SerializeField] private bool _useHandplacedTacticalProbes = true;
        [SerializeField] private bool _generateAutoProbeGrid = true;
        [SerializeField] private bool _showThePositionsInEditor = false;

        [SerializeField] private bool _createDebugGameObjects = false;
        [SerializeField] private Vector3 _debugGameObjectsSpawn;
        [SerializeField] private GameObject _debugGameObject;
        [SerializeField] private GameObject _debugGameObjectParent;

        [SerializeField] private TacticalPosDebugGO _gizmoShowGameObject;
        [Range(1f, 5f)][SerializeField] private float distanceToCreateGizmos = 10f;
        [Range(0.01f, 0.25f)][SerializeField] private float maxDistanceToConsiderSamePosition = 0.05f;
        [Range(1f, 3f)][SerializeField] private float maxDegreesDifferenceToConsiderSamePosition = 1f;

        // ========================================================= PROPERTIES

        public TacticalPositionData TacticalPositionData
        {
            get { return _tacticalPositionData; }
            set { _tacticalPositionData = value; }
        }
        public bool UseHandplacedTacticalProbes => _useHandplacedTacticalProbes;
        public bool GenerateAutoProbeGrid => _generateAutoProbeGrid;


        // ========================================================= GENERATION

        // TODO this approach not done, just the GenerateTacticalPositionSpawners below
        public void AddTacticalProbes(bool clearPositions, TacticalProbe[] probes)
        {
            if (probes == null || probes.Length == 0)
            {
                return;
            }

            if (!ValidateParams())
            {
                return;
            }

            if (_tacticalPositionData.Positions == null)
            {
                _tacticalPositionData.Positions = new List<TacticalPosition>();
            }
            else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
            {
                _tacticalPositionData.Positions.Clear();
            }

            foreach (var probe in probes)
            {
                _tacticalPositionData.Positions.Add(new TacticalPosition
                {
                    Position = probe.transform.position,
                    CoverDirections = new CoverHeight[1]
                    {
                        new() {

                        }
                    }
                });
            }
        }

        public void GenerateTacticalPositionSpawners(bool clearPositions)
        {
            if (!ValidateParams())
            {
                return;
            }

            List<TacticalPosition> oldTacticalPositions = new(_tacticalPositionData.Positions);
            if (_createDebugGameObjects)
            {
                _gizmoShowGameObject.tacticalDebugDataOfAllPositions.Clear();
            }
            InitTacticalPositionData(clearPositions);
            CreateSpawnersAlongTheGrid();
            RemoveDuplicates(_positionSettings.distanceToRemoveDuplicates);
            CompareTheNewPositions(oldTacticalPositions);
        }

        private void CompareTheNewPositions(List<TacticalPosition> copyOfOldPositions)
        {
            List<TacticalPosition> copyOfNew = new(_tacticalPositionData.Positions);
            List<TacticalPosition> modifiedPositions = new();
            FilterListsForChanges(copyOfOldPositions, copyOfNew, modifiedPositions);

            if (copyOfNew.Count == 0 && copyOfOldPositions.Count == 0 && modifiedPositions.Count == 0)
            {
                Debug.Log("The positions generated without any changes");
            }
            else
            {
                Debug.Log($"Added: {copyOfNew.Count}, Removed: {copyOfOldPositions.Count}, Modified: {modifiedPositions.Count}");
            }

            foreach (var pos in copyOfNew)
            {
                Debug.Log($"[ADDED] {pos}");
            }

            foreach (var pos in copyOfOldPositions)
            {
                Debug.Log($"[REMOVED] {pos}");
            }

            foreach (var pos in modifiedPositions)
            {
                Debug.Log($"[MODIFIED] {pos}");
            }
        }

        private void FilterListsForChanges(List<TacticalPosition> oldList, List<TacticalPosition> newList, List<TacticalPosition> modifiedList)
        {
            for (int i = oldList.Count - 1; i >= 0; i--)
            {
                TacticalPosition pos1 = oldList[i];

                // Look for exact match in list2
                for (int j = 0; j < newList.Count; j++)
                {
                    // If it is in the same spot
                    if (Vector3.Distance(pos1.Position, newList[j].Position) < maxDistanceToConsiderSamePosition)
                    {
                        // But does not have the same values
                        if (!ArePositionsRoughlyEqual(pos1, newList[j], maxDegreesDifferenceToConsiderSamePosition))
                        {
                            modifiedList.Add(newList[j]);
                        }

                        // They are equal, remove them
                        oldList.RemoveAt(i);
                        newList.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        private void LogInvalidPositions(string errorMessage, TacticalPosition pos1, TacticalPosition pos2)
        {
            Debug.LogWarning($"{errorMessage}.\n{pos1}\n{pos2}");
        }

        private bool ArePositionsRoughlyEqual(TacticalPosition pos1, TacticalPosition pos2, float maxDifferenceInRotation)
        {
            if (pos1.mainCover.height != pos2.mainCover.height)
            {
                return false;
            }

            if (pos1.mainCover.type != pos2.mainCover.type)
            {
                return false;
            }

            if (Quaternion.Angle(pos1.mainCover.rotationToAlignWithCover, pos2.mainCover.rotationToAlignWithCover) > (maxDifferenceInRotation))
            {
                LogInvalidPositions("Positions rotationToAlignWithCover are not the same", pos1, pos2);
                return false;
            }

            if (!CoverDirectionsApproximatelyEqual(pos1, pos2))
            {
                return false;
            }

            if (pos1.isOutside != pos2.isOutside)
            {
                LogInvalidPositions("Positions outside parameters are not the same", pos1, pos2);
                return false;
            }

            return true;
        }

        private bool CoverDirectionsApproximatelyEqual(TacticalPosition pos1, TacticalPosition pos2)
        {
            if (pos1.CoverDirections == null && pos2.CoverDirections == null)
            {
                return true;
            }
            if (pos1.CoverDirections == null || pos2.CoverDirections == null)
            {
                LogInvalidPositions("One CoverDirections array is null, while the other is not", pos1, pos2);
                return false;
            }
            if (pos1.CoverDirections.Length != pos2.CoverDirections.Length)
            {
                LogInvalidPositions("CoverDirections length mismatch: {pos1.CoverDirections.Length} vs {pos2.CoverDirections.Length}", pos1, pos2);
                return false;
            }

            for (int i = 0; i < pos1.CoverDirections.Length; i++)
            {
                if (pos1.CoverDirections[i] != pos2.CoverDirections[i])
                {
                    LogInvalidPositions("CoverDirections[{i}] mismatch: {pos1.CoverDirections[i]} vs {pos2.CoverDirections[i]}", pos1, pos2);
                    return false;
                }
            }
            return true;
        }


        public void VerifyPositionsCover()
        {
            for (int i = _tacticalPositionData.Positions.Count - 1; i >= 0; i--)
            {
                VerifyCoverOfAPosition(TacticalPositionData.Positions[i]);
            }
        }

        private void VerifyCoverOfAPosition(TacticalPosition position)
        {
            if (!Physics.Raycast(position.Position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _raycastMask))
            {
                Debug.LogError($"Position at {position.Position} did not have a solid ground underneath! Skipping validation!");
                return;
            }

            Vector3 direction = position.mainCover.rotationToAlignWithCover * Vector3.forward;
            Vector3 origin = hit.point;
            for (float currentHeight = hit.point.y + _positionSettings.bottomRaycastBuffer; currentHeight < position.Position.y; currentHeight += _positionSettings.verticalStepToCheckForCover)
            {
                origin.y = currentHeight;
                if (!Physics.Raycast(origin, direction, _positionSettings.distanceToCheckForCover, _raycastMask))
                {
                    Debug.LogWarning($"Position at {position.Position} did not have a continuous cover to the ground! Removing it!");

                    GameObject newDebugGO = Instantiate(_debugGameObject, _debugGameObjectParent.transform);
                    newDebugGO.transform.position = position.Position;
                    _tacticalPositionData.Positions.Remove(position);
                    return;
                }
            }
        }

        private void InitTacticalPositionData(bool clearPositions)
        {
            if (_tacticalPositionData.Positions == null)
            {
                _tacticalPositionData.Positions = new();
            }
            else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
            {
                _tacticalPositionData.Positions.Clear();
                _gizmoShowGameObject.tacticalDebugDataOfAllPositions.Clear();
            }

            if (_debugGameObjectParent.transform.childCount > 0)
            {
                for (int i = _debugGameObjectParent.transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(_debugGameObjectParent.transform.GetChild(i).gameObject);
                }
            }
        }

        private void CreateSpawnersAlongTheGrid()
        {
            Debug.Log("Generating tactical positions for AI");
            Vector3 currentPos = _gridSettings.StartPos;
            bool otherRow = false;
            while (currentPos.x < _gridSettings.EndPos.x)
            {
                currentPos.z = _gridSettings.StartPos.z;

                if (_gridSettings.OffsetEveryOtherRow)
                {
                    if (otherRow)
                    {
                        currentPos.z += _gridSettings.DistanceBetweenPositions / 2;
                    }
                    otherRow = !otherRow;
                }

                while (currentPos.z < _gridSettings.EndPos.z)
                {
                    RaycastHit[] hitsToConvertToPos = PhysicsUtils.RaycastTrulyAll(currentPos, Vector3.down, _raycastMask, 0.1f, 100f);
                    foreach (var hit in hitsToConvertToPos)
                    {
                        if (PositionValid(hit.point))
                        {
                            CreatePositionsAtHitsAround(hit.point);
                        }
                    }
                    currentPos.z += _gridSettings.DistanceBetweenPositions;
                }
                currentPos.x += _gridSettings.DistanceBetweenPositions;
            }
        }

        private void RemoveDuplicates(float distanceThreshold)
        {
            // List to store unique positions
            List<TacticalPosition> uniquePositions = new();

            // Iterate over each position
            for (int i = 0; i < _tacticalPositionData.Positions.Count; i++)
            {
                TacticalPosition currentPos = _tacticalPositionData.Positions[i];
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
                        && angleDifference < _positionSettings.maxAngleDifferenceToRemoveDuplicates)
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

            _tacticalPositionData.Positions = uniquePositions;
        }

        public bool NoObstacleBetween(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (Physics.Raycast(start, direction.normalized, out _, distance, _raycastMask))
            {
                return false;
            }

            return true;
        }

        private bool PositionValid(Vector3 position)
        {
            // Discard the position if it is too far from NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _raycastMask))
            {
                Vector3 heightAdjustedHit = hit.position;
                heightAdjustedHit.y = position.y;

                if (Vector3.Distance(heightAdjustedHit, position) > _positionSettings.RequiredProximityToNavMesh)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Discard the position if it spawned in the geometry
            Vector3 rayCastOrigin = position + Vector3.up * _positionSettings.geometryCheckYOffset;

            if (Physics.Raycast(rayCastOrigin, Vector3.down, out RaycastHit geometryHit, _positionSettings.geometryCheckYOffset + _highCornerSettings.rayLengthBeyondWall, _raycastMask))
            {
                if (!Mathf.Approximately(rayCastOrigin.y - geometryHit.point.y, rayCastOrigin.y - position.y))
                {
                    return false;
                }
            }

            return true;
        }

        private void CreatePositionsAtHitsAround(Vector3 position)
        {
            float angleBetweenRays = 360f / _gridSettings.NumberOfRaysSpawner;
            Vector3 direction = Vector3.forward;

            Vector3 rayOriginForLowCover = position + Vector3.up * _positionSettings.minHeightToConsiderLowCover;
            Vector3 rayOriginForHighCover = position + Vector3.up * _positionSettings.minHeightToConsiderHighCover;

            for (int i = 0; i < _gridSettings.NumberOfRaysSpawner; i++)
            {
                if (Physics.Raycast(rayOriginForHighCover, direction, out RaycastHit highHit, _gridSettings.DistanceOfRaycasts, _raycastMask))
                {
                    if (genMode == CoverGenMode.corners)
                    {
                        if (_createDebugGameObjects && Vector3.Distance(highHit.point, _gizmoShowGameObject.transform.position) < distanceToCreateGizmos)
                        {
                            CoverPositioner.GetCoverPositioner.FindCornerPos(highHit, CoverHeight.HighCover, _highCornerSettings, _raycastMask, _tacticalPositionData.Positions, _gizmoShowGameObject.tacticalDebugDataOfAllPositions);
                        }
                        else
                        {
                            CoverPositioner.GetCoverPositioner.FindCornerPos(highHit, CoverHeight.HighCover, _highCornerSettings, _raycastMask, _tacticalPositionData.Positions);
                        }
                    }
                }
                if (Physics.Raycast(rayOriginForLowCover, direction, out RaycastHit lowHit, _gridSettings.DistanceOfRaycasts, _raycastMask))
                {
                    if (genMode == CoverGenMode.corners)
                    {
                        if (_createDebugGameObjects && Vector3.Distance(lowHit.point, _gizmoShowGameObject.transform.position) < distanceToCreateGizmos)
                        {
                            CoverPositioner.GetCoverPositioner.FindCornerPos(lowHit, CoverHeight.LowCover, _lowCornerSettings, _raycastMask, _tacticalPositionData.Positions, _gizmoShowGameObject.tacticalDebugDataOfAllPositions);
                        }
                        else
                        {
                            CoverPositioner.GetCoverPositioner.FindCornerPos(lowHit, CoverHeight.LowCover, _lowCornerSettings, _raycastMask, _tacticalPositionData.Positions);
                        }
                    }
                    else if (genMode == CoverGenMode.low)
                    {
                        if (_createDebugGameObjects && Vector3.Distance(position, _gizmoShowGameObject.transform.position) < distanceToCreateGizmos)
                        {
                            CoverPositioner.GetCoverPositioner.FindLowCoverPos(lowHit, CoverHeight.LowCover, _lowCoverSettings, _raycastMask, _tacticalPositionData.Positions, _gizmoShowGameObject.tacticalDebugDataOfAllPositions);
                        }
                        else
                        {
                            Debug.Log("SUSIK 3");
                            CoverPositioner.GetCoverPositioner.FindLowCoverPos(lowHit, CoverHeight.LowCover, _lowCoverSettings, _raycastMask, _tacticalPositionData.Positions);
                        }
                    }
                }

                direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
            }
        }

        private bool ValidateParams()
        {
            if (_gridSettings == null)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The grid settings are not assigned!");
                return false;
            }

            if (_gridSettings.DistanceBetweenPositions < 0.5f)
            {
                Debug.LogError("Could not generate the grid of tactical positions. The distance between positions is too small!");
                return false;
            }

            return true;
        }


        // ========================================================= DEBUG

        void OnDrawGizmosSelected()
        {
            if (_tacticalPositionData == null || !_showThePositionsInEditor)
            {
                return;
            }

            Debug.Log($"Currently displaying {_tacticalPositionData.Positions.Count} positions");
            foreach (TacticalPosition position in _tacticalPositionData.Positions)
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
        }
    }
}