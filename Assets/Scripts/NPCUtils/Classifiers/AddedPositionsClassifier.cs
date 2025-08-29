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
            foreach (var newPos in newPositions)
            {
                if (!oldPositions.Exists(oldPos => Vector3.Distance(oldPos.Position, newPos.Position) < _maxDistanceToConsiderSamePosition))
                {
                    if (debugGO != null)
                    {
                        CreateDebugGO(debugGO, newPos.Position, newPos: newPos);
                    }
                }
            }
            if (newPositions.Count > 0)
            {
                Debug.Log($"Added {newPositions.Count} positions");
            }
        }
    }
}