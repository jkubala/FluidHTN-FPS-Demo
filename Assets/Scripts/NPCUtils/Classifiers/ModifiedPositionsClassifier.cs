using System.Collections.Generic;
using UnityEngine;
namespace FPSDemo.NPC.Utilities
{
    public class ModifiedPositionClassifier : BasePositionClassifier
    {
        public ModifiedPositionClassifier(GameObject parentGO, float maxDistance, float maxDegrees)
            : base(parentGO, maxDistance, maxDegrees) { }

        protected override void CustomClassification(List<TacticalPosition> oldPositions, List<TacticalPosition> newPositions, GameObject debugGO = null)
        {
            List<TacticalPosition> oldCopy = new(oldPositions);
            List<TacticalPosition> newCopy = new(newPositions);
            int modifiedPositionCount = 0;
            for (int i = oldCopy.Count - 1; i >= 0; i--)
            {
                TacticalPosition oldPos = oldCopy[i];

                for (int j = 0; j < newCopy.Count; j++)
                {
                    TacticalPosition newPos = newCopy[j];

                    if (Vector3.Distance(oldPos.Position, newPos.Position) < _maxDistanceToConsiderSamePosition)
                    {
                        if (!ArePositionPropertiesEqual(oldPos, newPos, _maxDegreesDifferenceToConsiderSamePosition))
                        {
                            modifiedPositionCount++;
                            if (debugGO != null)
                            {
                                CreateDebugGameObject(debugGO, newPos.Position, oldPos, newPos);
                            }
                        }

                        oldCopy.RemoveAt(i);
                        newCopy.RemoveAt(j);
                        break;
                    }
                }
            }
            if (modifiedPositionCount > 0)
            {
                Debug.Log($"Modified {modifiedPositionCount} positions");
            }
        }
    }
}