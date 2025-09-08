using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPosDebugGizmoGO : MonoBehaviour
    {
        enum DebugMode { Corner, Non90DegreeCorner, Obstacle, YAxisStandardisation, Verification }
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
            DrawSphere(tacticalDebugData.offsetPosition, 0.05f, Color.black);
            foreach (CornerDebugData corner in tacticalDebugData.corners)
            {
                DisplayCornerDebugGizmos(corner);
            }
        }

        private void DisplayCornerDebugGizmos(CornerDebugData cornerDebugData)
        {
            if (cornerDebugData.cornerPos.HasValue)
            {
                DrawSphere(cornerDebugData.cornerPos.Value, 0.05f, Color.cyan);
            }

            foreach (Vector3 pos in cornerDebugData?.hitPositions)
            {
                DrawSphere(pos, 0.01f, Color.red);
            }

            if (cornerDebugData.finalCornerPos.HasValue)
            {
                DrawSphere(cornerDebugData.finalCornerPos.Value, 0.05f, Color.green);
                DrawRay(cornerDebugData.finalCornerPos.Value, cornerDebugData.tacticalPosition.mainCover.rotationToAlignWithCover * Vector3.forward, Color.green);
            }
        }

        private void ObstacleInFiringPositionDebug(TacticalDebugData tacticalDebugData)
        {
            foreach (CornerDebugData corner in tacticalDebugData.corners)
            {
                DisplayCornerObstacleCheck(corner);
            }
        }

        private void DisplayCornerObstacleCheck(CornerDebugData debugData)
        {
            if (debugData.sphereCastOrigin.HasValue && debugData.sphereCastDirection.HasValue && debugData.sphereCastRadius != 0)
            {
                DrawSphere(debugData.sphereCastOrigin.Value, debugData.sphereCastRadius, Color.black);
                DrawRay(debugData.sphereCastOrigin.Value, debugData.sphereCastDirection.Value, Color.blue);
            }
        }

        private void Non90DegreeCornerDebug(TacticalDebugData tacticalDebugData)
        {
            foreach (CornerDebugData corner in tacticalDebugData.corners)
            {
                DisplayCornerFiringNormals(corner);
            }
        }

        private void DisplayCornerFiringNormals(CornerDebugData debugData)
        {
            if (debugData.finalCornerPos.HasValue && debugData.cornerNormal.HasValue && debugData.positionFiringDirection.HasValue)
            {
                DrawRay(debugData.finalCornerPos.Value, debugData.cornerNormal.Value, Color.black);
                DrawRay(debugData.finalCornerPos.Value, debugData.positionFiringDirection.Value, Color.yellow);
            }
        }

        private void YAxisStandardisationDebug(TacticalDebugData tacticalDebugData)
        {
            foreach (CornerDebugData corner in tacticalDebugData.corners)
            {
                DrawYAxisStandardisationSphereCast(corner);
            }
        }

        private void DrawYAxisStandardisationSphereCast(CornerDebugData tacticalDebugData)
        {
            if (!tacticalDebugData.yAxisStandSphereCastOrigin.HasValue || !tacticalDebugData.yAxisStandSphereCastDirection.HasValue || tacticalDebugData.yAxisStandSphereCastRadius == 0)
            {
                return;
            }

            DrawSphere(tacticalDebugData.yAxisStandSphereCastOrigin.Value, tacticalDebugData.yAxisStandSphereCastRadius, Color.black);
            DrawRay(tacticalDebugData.yAxisStandSphereCastOrigin.Value, tacticalDebugData.yAxisStandSphereCastDirection.Value, Color.black);

            if (!tacticalDebugData.yAxisStandSphereCastHit.HasValue)
            {
                return;
            }

            DrawSphere(tacticalDebugData.yAxisStandSphereCastHit.Value, tacticalDebugData.yAxisStandSphereCastRadius, Color.red);
        }

        private void DisplayVerificationData(TacticalDebugData tacticalDebugData)
        {
            foreach (CornerDebugData corner in tacticalDebugData.corners)
            { 
                DisplayVerificationDataOfCorner(corner);
            }
        }

        private void DisplayVerificationDataOfCorner(CornerDebugData debugData)
        {
            if (debugData.verifyFailurePos.HasValue && debugData.verifyHorizStartPos.HasValue && debugData.verifyHorizEndPos.HasValue)
            {
                DrawSphere(debugData.verifyHorizStartPos.Value, 0.1f, Color.black);
                DrawRay(debugData.verifyHorizStartPos.Value, debugData.verifyHorizEndPos.Value - debugData.verifyHorizStartPos.Value, Color.black);
                DrawSphere(debugData.verifyHorizEndPos.Value, 0.1f, Color.cyan);
                DrawSphere(debugData.verifyFailurePos.Value, 0.1f, Color.green);
            }
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
                    case DebugMode.YAxisStandardisation:
                        YAxisStandardisationDebug(_tacticalDebugData);
                        break;
                    case DebugMode.Verification:
                        DisplayVerificationData(_tacticalDebugData);
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
        public Vector3 offsetPosition;
        public List<CornerDebugData> corners = new();
        public Vector3 standardisationOrigin, standardisationDirection;
        public float standardisationDistance;

        public bool Finished { get { return finished; } }


        public void MarkAsFinished()
        {
            finished = true;
        }
    }

    [System.Serializable]
    public class CornerDebugData
    {
        public CornerDebugData(TacticalDebugData parentData)
        {
            this.parentData = parentData;
        }
        [System.NonSerialized] public TacticalDebugData parentData;
        public TacticalPosition tacticalPosition;
        public float distanceToCorner;
        public SN<Vector3> cornerPos;
        public SN<Vector3> finalCornerPos;
        public List<Vector3> hitPositions;
        public SN<Vector3> sphereCastOrigin, sphereCastDirection, cornerNormal, positionFiringDirection;
        public float sphereCastRadius;
        public SN<Vector3> yAxisStandSphereCastOrigin, yAxisStandSphereCastDirection, yAxisStandSphereCastHit;
        public float yAxisStandSphereCastRadius;
        public float normStandDistance;

        public SN<Vector3> verifyHorizStartPos, verifyHorizEndPos, verifyFailurePos;
    }
}