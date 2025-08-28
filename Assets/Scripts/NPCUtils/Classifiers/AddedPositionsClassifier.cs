using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
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
                    Positions.Add(newPos);
                    if (debugGO != null)
                    {
                        CreateDebugGO(debugGO, newPos.Position, newPos: newPos);
                    }
                }
            }
        }
    }
}