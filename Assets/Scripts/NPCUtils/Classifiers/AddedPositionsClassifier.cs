using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    public class AddedPositionClassifier : BasePositionClassifier
    {
        public AddedPositionClassifier(GameObject parentGO, float maxDistance, float maxDegrees)
            : base(parentGO, maxDistance, maxDegrees) { }
        protected override void CustomClassification(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions, GameObject debugGO = null)
        {
            int addedPositionCount = 0;
            foreach (var newPos in newPositions)
            {
                if (!oldPositions.Exists(oldPos => Vector3.Distance(oldPos.Position, newPos.Position) < _maxDistanceToConsiderSamePosition))
                {
                    addedPositionCount++;
                    if (debugGO != null)
                    {
                        CreateDebugGameObject(debugGO, newPos.Position, newPos: newPos);
                    }
                }
            }
            if (addedPositionCount > 0)
            {
                Debug.LogWarning($"Added {addedPositionCount} positions");
            }
        }
    }
}