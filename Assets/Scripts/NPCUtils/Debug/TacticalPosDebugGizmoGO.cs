using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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
            DrawRay(tacticalDebugData.offsetPosition, tacticalDebugData.leftDirection * tacticalDebugData.leftCorner.distanceToCorner, Color.black);
            DrawRay(tacticalDebugData.offsetPosition, -tacticalDebugData.leftDirection * tacticalDebugData.rightCorner.distanceToCorner, Color.black);
            DrawSphere(tacticalDebugData.offsetPosition, 0.05f, Color.black);
            DisplayCornerDebugGizmos(tacticalDebugData.leftCorner);
            DisplayCornerDebugGizmos(tacticalDebugData.rightCorner);
        }

        private void DisplayCornerDebugGizmos(CornerDebugData cornerDebugData)
        {
            if (cornerDebugData.cornerPos.HasValue)
            {
                DrawSphere(cornerDebugData.cornerPos.Value, 0.05f, Color.cyan);
            }

            foreach (Vector3 pos in cornerDebugData.hitPositions)
            {
                DrawSphere(pos, 0.01f, Color.red);
            }

            if (cornerDebugData.finalCornerPos.HasValue)
            {
                DrawSphere(cornerDebugData.finalCornerPos.Value, 0.05f, Color.green);
                DrawRay(cornerDebugData.finalCornerPos.Value,  cornerDebugData.tacticalPosition.mainCover.rotationToAlignWithCover * Vector3.forward, Color.green);
            }
        }

        private void ObstacleInFiringPositionDebug(TacticalDebugData tacticalDebugData)
        {
            //DrawSphere(tacticalDebugData.sphereCastOrigin, 0.1f, Color.black);
            //DrawRay(tacticalDebugData.sphereCastOrigin, tacticalDebugData.sphereCastDirection, Color.blue);
            //DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.cornerNormal, Color.cyan);
            //DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.cornerFiringNormal, Color.black);
        }

        private void Non90DegreeCornerDebug(TacticalDebugData tacticalDebugData)
        {
            //if (tacticalDebugData.initCornerNormal.HasValue)
            //{
            //    DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.initCornerNormal.Value, Color.black);
            //}
            //if (tacticalDebugData.initCornerFiringNormal.HasValue)
            //{
            //    DrawRay(tacticalDebugData.finalCornerPos, tacticalDebugData.initCornerFiringNormal.Value, Color.yellow);
            //}
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
        private bool finished;
        public Vector3 offsetPosition, leftDirection;
        public CornerDebugData leftCorner;
        public CornerDebugData rightCorner;
        public Vector3 sphereCastAnchor, sphereCastOrigin, sphereCastDirection, sphereCastNormal, cornerNormal, cornerFiringNormal;
        public Vector3 standardisationOrigin, standardisationDirection;
        public float standardisationDistance;

        public bool Finished { get { return finished; } }


        public void MarkAsFinished()
        {
            finished = true;
        }
    }

    [System.Serializable]
    public struct CornerDebugData
    {

        public TacticalPosition tacticalPosition;
        public float distanceToCorner;
        public SN<Vector3> cornerPos;
        public SN<Vector3> finalCornerPos;
        public List<Vector3> hitPositions;
        public SN<Vector3> initCornerNormal, initCornerFiringNormal;
    }
}