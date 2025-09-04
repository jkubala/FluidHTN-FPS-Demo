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
            int count = 0;
            foreach (var oldPos in oldPositions)
            {
                if (!newPositions.Exists(newPos => Vector3.Distance(oldPos.Position, newPos.Position) < _maxDistanceToConsiderSamePosition))
                {
                    count++;
                    if (debugGO != null)
                    {
                        CreateDebugGO(debugGO, oldPos.Position, oldPos: oldPos);
                    }
                }
            }
            if (count > 0)
            {
                Debug.LogWarning($"Removed {count} positions");
            }
        }
    }
}