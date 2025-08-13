using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPosDebugGO : MonoBehaviour
    {
        enum DebugMode { Corner, Non90DegreeCorner, Obstacle, NormalStandardisation }
        enum FinishedMode { All, OnlyFinished, OnlyUnfinished }
        [SerializeField] DebugMode debugMode;
        [SerializeField] FinishedMode finishedMode;
        public List<TacticalDebugData> tacticalDebugDataOfAllPositions = new();

        private void DrawSphere(Vector3 position, float radius, Color color)
        {
            Color curColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, radius);
            Gizmos.color = curColor;
        }

        private void DrawRay(Vector3 position, Vector3 direction, Color color)
        {
            Color curColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawRay(position, direction);
            Gizmos.color = curColor;
        }

        private void GetHighPosAdjustedToCornerDebug(TacticalDebugData tacticalDebugData)
        {
            // Looking for left and right corners
            DrawRay(tacticalDebugData.offsetPosition, tacticalDebugData.leftDirection * tacticalDebugData.distanceToCornerLeft, Color.black);
            DrawRay(tacticalDebugData.offsetPosition, -tacticalDebugData.leftDirection * tacticalDebugData.distanceToCornerRight, Color.black);
            DrawSphere(tacticalDebugData.offsetPosition, 0.1f, Color.black);

            if (tacticalDebugData.leftCornerPos.HasValue)
            {
                DrawSphere(tacticalDebugData.leftCornerPos.Value, 0.1f, Color.yellow);
            }

            if (tacticalDebugData.rightCornerPos.HasValue)
            {
                DrawSphere(tacticalDebugData.rightCornerPos.Value, 0.1f, Color.cyan);
            }

            foreach (Vector3 pos in tacticalDebugData.hitPositions)
            {
                DrawSphere(pos, 0.01f, Color.red);
            }

            //DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.tacticalPosition.mainCover.rotationToAlignWithCover.eulerAngles, Color.green);
        }

        private void ObstacleInFiringPositionDebug(TacticalDebugData tacticalDebugData)
        {
            DrawSphere(tacticalDebugData.sphereCastOrigin, 0.1f, Color.black);
            DrawRay(tacticalDebugData.sphereCastOrigin, tacticalDebugData.sphereCastDirection, Color.blue);
            DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.cornerNormal, Color.cyan);
            DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.cornerFiringNormal, Color.black);
        }

        private void Non90DegreeCornerDebug(TacticalDebugData tacticalDebugData)
        {
            if (tacticalDebugData.initCornerNormal.HasValue)
            {
                DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.initCornerNormal.Value, Color.black);
            }
            if (tacticalDebugData.initCornerFiringNormal.HasValue)
            {
                DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.initCornerFiringNormal.Value, Color.yellow);
            }
        }

        private void StandardisationDebug(TacticalDebugData tacticalDebugData)
        {
            DrawRay(tacticalDebugData.standardisationOrigin, tacticalDebugData.standardisationDirection * tacticalDebugData.standardisationDistance, Color.black);
        }


        void OnDrawGizmosSelected()
        {
            Debug.Log($"Overall tactical debug positions: {tacticalDebugDataOfAllPositions.Count}");
            foreach (TacticalDebugData tacticalDebugData in tacticalDebugDataOfAllPositions)
            {
                if((finishedMode == FinishedMode.OnlyUnfinished && tacticalDebugData.finishedPosition) || (finishedMode == FinishedMode.OnlyFinished && !tacticalDebugData.finishedPosition))
                {
                    continue;
                }
                switch (debugMode)
                {
                    case DebugMode.Corner:
                        GetHighPosAdjustedToCornerDebug(tacticalDebugData);
                        break;
                    case DebugMode.Obstacle:
                        ObstacleInFiringPositionDebug(tacticalDebugData);
                        break;
                    case DebugMode.Non90DegreeCorner:
                        Non90DegreeCornerDebug(tacticalDebugData);
                        break;
                    case DebugMode.NormalStandardisation:
                        StandardisationDebug(tacticalDebugData);
                        break;
                }
            }
        }
    }

    [System.Serializable]
    public class TacticalDebugData
    {
        public TacticalPosition tacticalPosition;
        public bool finishedPosition;
        public Vector3 offsetPosition, leftDirection, finalCornerPos;
        public Vector3 sphereCastAnchor, sphereCastOrigin, sphereCastDirection, sphereCastNormal, cornerNormal, cornerFiringNormal;
        public float distanceToCornerLeft, distanceToCornerRight;
        public Vector3? leftCornerPos, rightCornerPos;
        public List<Vector3> hitPositions;
        public Vector3? initCornerNormal, initCornerFiringNormal;

        public Vector3 standardisationOrigin, standardisationDirection;
        public float standardisationDistance;
    }
}