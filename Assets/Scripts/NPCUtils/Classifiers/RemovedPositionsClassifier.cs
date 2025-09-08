using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    public class RemovedPositionClassifier : BasePositionClassifier
    {
        public RemovedPositionClassifier(GameObject parentGO, float maxDistance, float maxDegrees)
            : base(parentGO, maxDistance, maxDegrees) { }
        protected override void CustomClassification(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions, GameObject debugGO = null)
        {
            int removedPositionCount = 0;
            foreach (var oldPos in oldPositions)
            {
                if (!newPositions.Exists(newPos => Vector3.Distance(oldPos.Position, newPos.Position) < _maxDistanceToConsiderSamePosition))
                {
                    removedPositionCount++;
                    if (debugGO != null)
                    {
                        CreateDebugGameObject(debugGO, oldPos.Position, oldPos: oldPos);
                    }
                }
            }
            if (removedPositionCount > 0)
            {
                Debug.LogWarning($"Removed {removedPositionCount} positions");
            }
        }
    }
}