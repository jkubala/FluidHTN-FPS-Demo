using UnityEngine;
using static FPSDemo.NPC.Utilities.CornerFinder;

namespace FPSDemo.NPC.Utilities
{
    public static class PositionValidator
    {
        public static CornerDetectionInfo? ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, TacticalPositionSettings posSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
        {

            Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo, cornerSettings, raycastMask, debugData);

            if (!yStandardCornerPos.HasValue)
            {
                return null;
            }

            if (debugData != null)
            {
                if (cornerInfo.coverType == CoverType.LeftCorner)
                {
                    debugData.leftCorner.yAxisStandPos = yStandardCornerPos.Value;
                }
                else
                {
                    debugData.rightCorner.yAxisStandPos = yStandardCornerPos.Value;
                }
            }

            float maxDistanceToRedo = 0.1f;
            int maxTries = 3;
            int currentTry = 0;

            Vector3? newPos = cornerInfo.position;
            // Try to juggle shifts due to cover continuity and also Y axis shifts
            do
            {
                newPos = VerifyContinuousCoverOfPosition(newPos.Value, Quaternion.LookRotation(-cornerInfo.coverWallNormal, Vector3.up), -cornerInfo.outDirection, 2f, .75f, posSettings, raycastMask, cornerInfo.coverType == CoverType.LeftCorner, debugData);
                // Cover verification failed, get out of there
                if (!newPos.HasValue)
                {
                    break;
                }

                cornerInfo.position = newPos.Value;
                newPos = StandardizePositionOnYAxis(cornerInfo, cornerSettings, raycastMask, debugData);
                currentTry++;
            }
            while (newPos.HasValue && Vector3.Distance(cornerInfo.position, newPos.Value) > maxDistanceToRedo && currentTry < maxTries);

            if (!newPos.HasValue)
            {
                return null;
            }

            cornerInfo.position = newPos.Value;
            return cornerInfo;
        }

        private static Vector3? VerifyContinuousCoverOfPosition(Vector3 position, Quaternion rotation, Vector3 directionToScanIn, float maxDistanceToAnalyse, float minimumWidth, TacticalPositionSettings posSettings, LayerMask raycastMask, bool isLeftCorner, TacticalDebugData debugData = null)
        {
            float currentScanningDistance = 0;
            float currentContinuousCoverDistance = 0;
            Vector3? lastGoodPosition = null;
            if (debugData != null)
            {
                if (isLeftCorner)
                {
                    debugData.leftCorner.verifyHorizStartPos = position;
                    debugData.leftCorner.verifyHorizEndPos = position + directionToScanIn.normalized * maxDistanceToAnalyse;
                }
                else
                {
                    debugData.rightCorner.verifyHorizStartPos = position;
                    debugData.rightCorner.verifyHorizEndPos = position + directionToScanIn.normalized * maxDistanceToAnalyse;
                }
            }
            while (currentScanningDistance < maxDistanceToAnalyse)
            {
                if (VerifyContinuousVerticalCoverSliceOfAPosition(position + directionToScanIn.normalized * currentScanningDistance, rotation, raycastMask, posSettings, isLeftCorner, debugData))
                {
                    if (!lastGoodPosition.HasValue)
                    {
                        lastGoodPosition = position + directionToScanIn.normalized * currentScanningDistance;
                    }
                    if (currentContinuousCoverDistance >= minimumWidth)
                    {
                        break;
                    }
                    currentContinuousCoverDistance += posSettings.horizontalStepToCheckForCover;
                }
                else
                {
                    lastGoodPosition = null;
                    currentContinuousCoverDistance = 0;
                }

                // When corner is found and is just probing the wall for holes
                if (lastGoodPosition.HasValue)
                {
                    // Increment the bigger step
                    currentScanningDistance += posSettings.horizontalStepToCheckForCover;
                }
                // When the corner is not found
                else
                {
                    // Increment just a tiny bit to find the corner more precisely
                    currentScanningDistance += posSettings.horizontalStepToCheckForCoverPreciseMode;
                }
            }

            if (currentContinuousCoverDistance < minimumWidth)
            {
                lastGoodPosition = null;
            }

            return lastGoodPosition;
        }

        private static bool VerifyContinuousVerticalCoverSliceOfAPosition(Vector3 position, Quaternion rotation, LayerMask raycastMask, TacticalPositionSettings posSettings, bool isLeftCorner, TacticalDebugData debugData = null)
        {
            if (!Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                return false;
            }

            Vector3 direction = rotation * Vector3.forward;
            Vector3 origin = hit.point;


            for (float currentHeight = hit.point.y + posSettings.bottomRaycastBuffer; currentHeight < position.y; currentHeight += posSettings.verticalStepToCheckForCover)
            {
                origin.y = currentHeight;
                if (!Physics.Raycast(origin, direction, out RaycastHit hit2, posSettings.distanceToCheckForCover, raycastMask))
                {
                    if (debugData != null)
                    {
                        if (isLeftCorner)
                        {
                            debugData.leftCorner.verifyFailurePos = origin;
                        }
                        else
                        {
                            debugData.rightCorner.verifyFailurePos = origin;
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        private static Vector3? StandardizePositionOnYAxis(CornerDetectionInfo positionInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
        {
            Vector3 outward = Vector3.Cross(Vector3.up, positionInfo.coverWallNormal);
            if (positionInfo.coverType == CoverType.LeftCorner)
            {
                outward = -outward;
            }

            // Not all walls are perfectly 90 degree ones. If the wall is leaning away from the cover position, the sphereCast needs to go along the wall normal, since it will hit the wall if it would go Vector3.down.
            Vector3 downAlongWall = Vector3.ProjectOnPlane(Vector3.down, positionInfo.coverWallNormal).normalized;

            // If the wall is leaning towards the cover position though, use straight Vector3.down, since it could be in the wall when standardizing it by a Y axis offset from the ground.
            if (Vector3.Dot(outward, downAlongWall) < 0)
            {
                downAlongWall = Vector3.down;
            }

            if (debugData != null)
            {
                if (positionInfo.coverType == CoverType.LeftCorner)
                {
                    debugData.leftCorner.yAxisStandSphereCastOrigin = positionInfo.position;
                    debugData.leftCorner.yAxisStandSphereCastDirection = downAlongWall;
                    debugData.leftCorner.yAxisStandSphereCastRadius = cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer;
                }
                else
                {
                    debugData.rightCorner.yAxisStandSphereCastOrigin = positionInfo.position;
                    debugData.rightCorner.yAxisStandSphereCastDirection = downAlongWall;
                    debugData.rightCorner.yAxisStandSphereCastRadius = cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer;
                }
            }

            if (Physics.SphereCast(positionInfo.position, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, downAlongWall, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                float standardizedHeight = hit.point.y + cornerSettings.firingPositionHeight;

                if (Mathf.Abs(positionInfo.position.y - standardizedHeight) > cornerSettings.maxYDifferenceWhenAdjusting)
                {
                    return null;
                }
                else
                {
                    if (debugData != null)
                    {
                        if (positionInfo.coverType == CoverType.LeftCorner)
                        {
                            debugData.leftCorner.yAxisStandSphereCastHit = hit.point;
                        }
                        else
                        {
                            debugData.rightCorner.yAxisStandSphereCastHit = hit.point;
                        }
                    }
                    positionInfo.position.y = standardizedHeight;
                    return positionInfo.position;
                }
            }
            return null;
        }

        private static bool ObstacleInFiringPosition(CornerDetectionInfo positionInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
        {
            Vector3 sphereCastOrigin = positionInfo.position + positionInfo.coverWallNormal * (cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + positionInfo.positionFiringDirection * (cornerSettings.sphereCastForFiringPositionCheckOffset.x + cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + Vector3.up * cornerSettings.sphereCastForFiringPositionCheckOffset.y;

            Vector3 sphereCastDirection = Vector3.Cross(Vector3.up, positionInfo.positionFiringDirection).normalized;

            if (Vector3.Dot(positionInfo.positionFiringDirection, Vector3.Cross(Vector3.up, positionInfo.coverWallNormal)) < 0)
            {
                sphereCastDirection = -sphereCastDirection;
            }

            if (debugData != null)
            {
                if (positionInfo.coverType == CoverType.LeftCorner)
                {
                    debugData.leftCorner.sphereCastOrigin = sphereCastOrigin;
                    debugData.leftCorner.sphereCastDirection = sphereCastDirection;
                    debugData.leftCorner.sphereCastRadius = cornerSettings.sphereCastForFiringPositionCheckRadius;
                }
                else
                {
                    debugData.rightCorner.sphereCastOrigin = sphereCastOrigin;
                    debugData.rightCorner.sphereCastDirection = sphereCastDirection;
                    debugData.rightCorner.sphereCastRadius = cornerSettings.sphereCastForFiringPositionCheckRadius;
                }
            }

            // Make sure there is no obstacle between the position and firing pos
            if (Physics.Linecast(positionInfo.position, sphereCastOrigin))
            {
                return true;
            }


            if (Physics.SphereCast(sphereCastOrigin,
                cornerSettings.sphereCastForFiringPositionCheckRadius,
                sphereCastDirection, out _,
                cornerSettings.sphereCastForFiringPositionCheckDistance + cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer,
                raycastMask))
            {
                return true;
            }
            return false;
        }
    }
}
