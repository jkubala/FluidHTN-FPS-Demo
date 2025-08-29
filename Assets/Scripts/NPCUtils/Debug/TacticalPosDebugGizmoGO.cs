using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPosDebugGizmoGO : MonoBehaviour
    {
        enum DebugMode { Corner, Non90DegreeCorner, Obstacle, NormalStandardisation }
        [SerializeField] DebugMode debugMode;
        [SerializeField] private TacticalDebugData _tacticalDebugData;
        public TacticalDebugData TacticalDebugData
        {
            get
            {
                return _tacticalDebugData;
            }
            set
            {
                _tacticalDebugData = value;
            }
        }
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
#if UNITY_EDITOR
            // To avoid gizmos showing when something upwards in the hierarchy is selected
            if (Selection.activeGameObject != null && Selection.activeGameObject == gameObject)
            {
                switch (debugMode)
                {
                    case DebugMode.Corner:
                        GetHighPosAdjustedToCornerDebug(_tacticalDebugData);
                        break;
                    case DebugMode.Obstacle:
                        ObstacleInFiringPositionDebug(_tacticalDebugData);
                        break;
                    case DebugMode.Non90DegreeCorner:
                        Non90DegreeCornerDebug(_tacticalDebugData);
                        break;
                    case DebugMode.NormalStandardisation:
                        StandardisationDebug(_tacticalDebugData);
                        break;
                }
            }
        }
#endif
    }

    [System.Serializable]
    public class TacticalDebugData
    {
        public TacticalPositionGenerator.CoverGenerationMode genMode;
        public TacticalPosition tacticalPosition;
        private bool finished;
        public Vector3 offsetPosition, leftDirection, finalCornerPos;
        public Vector3 sphereCastAnchor, sphereCastOrigin, sphereCastDirection, sphereCastNormal, cornerNormal, cornerFiringNormal;
        public float distanceToCornerLeft, distanceToCornerRight;
        public Vector3? leftCornerPos, rightCornerPos;
        public List<Vector3> hitPositions = new();
        public Vector3? initCornerNormal, initCornerFiringNormal;

        public Vector3 standardisationOrigin, standardisationDirection;
        public float standardisationDistance;

        public bool Finished { get { return finished; } }


        public void MarkAsFinished()
        {
            finished = true;
        }
    }
}