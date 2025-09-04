using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class TacticalPosDebugGizmoGO : MonoBehaviour
    {
        enum DebugMode { Corner, Non90DegreeCorner, Obstacle, YAxisStandardisation, NormalStandardisation, Verification }
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
                DrawRay(cornerDebugData.finalCornerPos.Value, cornerDebugData.tacticalPosition.mainCover.rotationToAlignWithCover * Vector3.forward, Color.green);
            }
        }

        private void ObstacleInFiringPositionDebug(TacticalDebugData tacticalDebugData)
        {
            DisplayCornerObstacleCheck(tacticalDebugData.leftCorner);
            DisplayCornerObstacleCheck(tacticalDebugData.rightCorner);
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
            DisplayCornerFiringNormals(tacticalDebugData.leftCorner);
            DisplayCornerFiringNormals(tacticalDebugData.rightCorner);
        }

        private void DisplayCornerFiringNormals(CornerDebugData debugData)
        {
            if (debugData.finalCornerPos.HasValue && debugData.cornerNormal.HasValue && debugData.cornerFiringNormal.HasValue)
            {
                DrawRay(debugData.finalCornerPos.Value, debugData.cornerNormal.Value, Color.black);
                DrawRay(debugData.finalCornerPos.Value, debugData.cornerFiringNormal.Value, Color.yellow);
            }
        }

        private void YAxisStandardisationDebug(TacticalDebugData tacticalDebugData)
        {
            DrawYAxisStandardisationSphereCast(tacticalDebugData.leftCorner);
            DrawYAxisStandardisationSphereCast(tacticalDebugData.rightCorner);
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

            if (!tacticalDebugData.yAxisStandPos.HasValue)
            {
                return;
            }

            DrawSphere(tacticalDebugData.yAxisStandPos.Value, tacticalDebugData.yAxisStandSphereCastRadius, Color.yellow);
        }

        private void NormalStandardisationDebug(TacticalDebugData tacticalDebugData)
        {
            DisplayCornerNormalStandardisation(tacticalDebugData.leftCorner);
            DisplayCornerNormalStandardisation(tacticalDebugData.rightCorner);
        }

        private void DisplayCornerNormalStandardisation(CornerDebugData debugData)
        {
            if (!debugData.normStandOrigin.HasValue || !debugData.normStandNormal.HasValue || debugData.normStandDistance == 0)
            {
                return;
            }

            DrawSphere(debugData.normStandOrigin.Value, 0.05f, Color.black);
            DrawRay(debugData.normStandOrigin.Value, debugData.normStandDistance * debugData.normStandNormal.Value, Color.black);
        }

        private void DisplayVerificationData(TacticalDebugData debugData)
        {
            DisplayVerificationDataOfCorner(debugData.leftCorner);
            DisplayVerificationDataOfCorner(debugData.rightCorner);
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
                    case DebugMode.NormalStandardisation:
                        NormalStandardisationDebug(_tacticalDebugData);
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
        public Vector3 offsetPosition, leftDirection;
        public CornerDebugData leftCorner;
        public CornerDebugData rightCorner;
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
        public SN<Vector3> sphereCastOrigin, sphereCastDirection, cornerNormal, cornerFiringNormal;
        public float sphereCastRadius;
        public SN<Vector3> yAxisStandSphereCastOrigin, yAxisStandSphereCastDirection, yAxisStandSphereCastHit, yAxisStandPos;
        public float yAxisStandSphereCastRadius;
        public SN<Vector3> normStandOrigin, normStandNormal;
        public float normStandDistance;

        public SN<Vector3> verifyHorizStartPos, verifyHorizEndPos, verifyFailurePos;
    }
}