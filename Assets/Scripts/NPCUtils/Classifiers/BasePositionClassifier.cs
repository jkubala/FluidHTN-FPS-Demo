using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    public abstract class BasePositionClassifier
    {
        protected readonly GameObject _parentGO;
        protected readonly float _maxDistanceToConsiderSamePosition;
        protected readonly float _maxDegreesDifferenceToConsiderSamePosition;

        public BasePositionClassifier(GameObject parentGO, float maxDistance, float maxDegrees)
        {
            _parentGO = parentGO;
            _maxDistanceToConsiderSamePosition = maxDistance;
            _maxDegreesDifferenceToConsiderSamePosition = maxDegrees;
        }

        public void Classify(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions, GameObject debugGO = null)
        {
            ClearDebugGOs();
            CustomClassification(oldPositions, newPositions, debugGO);
        }

        protected abstract void CustomClassification(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions, GameObject debugGO = null);

        protected bool ArePositionsRoughlyEqual(TacticalPosition a, TacticalPosition b, float maxDegreesDifference)
        {
            if (a.mainCover.height != b.mainCover.height) return false;
            if (a.mainCover.type != b.mainCover.type) return false;
            if (Quaternion.Angle(a.mainCover.rotationToAlignWithCover, b.mainCover.rotationToAlignWithCover) > maxDegreesDifference) return false;
            if (!CoverDirectionsApproximatelyEqual(a, b)) return false;
            if (a.isOutside != b.isOutside) return false;
            return true;
        }

        protected void LogInvalidPositions(string errorMessage, TacticalPosition pos1, TacticalPosition pos2)
        {
            Debug.LogWarning($"{errorMessage}.\n{pos1}\n{pos2}");
        }

        protected bool CoverDirectionsApproximatelyEqual(TacticalPosition pos1, TacticalPosition pos2)
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

        protected void CreateDebugGO(GameObject debugPrefab, Vector3 position, TacticalPosition oldPos = null, TacticalPosition newPos = null)
        {
            if (debugPrefab == null)
            {
                return;
            }

            Undo.RecordObject(_parentGO, "Added a position change debug gameobject");
            GameObject debugGOInstance = GameObject.Instantiate(debugPrefab, _parentGO.transform);
            Undo.RegisterCreatedObjectUndo(debugGOInstance, "Added a position change debug gameobject");
            debugGOInstance.transform.position = position;
            if (debugGOInstance.TryGetComponent<PosChangeDebugGO>(out PosChangeDebugGO debugComp))
            {
                if (oldPos != null) debugComp.oldPosition = oldPos;
                if (newPos != null) debugComp.newPosition = newPos;
            }
        }

        public void ClearDebugGOs()
        {
            Undo.RecordObject(_parentGO, "Clear debug position change GOs");
            for (int i = _parentGO.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = _parentGO.transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child);
#else
                GameObject.Destroy(child);
#endif
            }
        }
    }
}