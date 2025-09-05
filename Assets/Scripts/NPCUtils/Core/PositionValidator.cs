using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public static class PositionValidator
    {
        public static CornerDetectionInfo? ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, TacticalPositionSettings posSettings, LayerMask raycastMask)
        {
            Vector3? yStandardCornerPos = StandardizeCornerOnYAxis(cornerInfo, cornerSettings, raycastMask);

            if (!yStandardCornerPos.HasValue)
            {
                return null;
            }

            if (cornerInfo.debugData != null)
            {
                cornerInfo.debugData.yAxisStandPos = yStandardCornerPos.Value;
            }

            float maxDistanceToRedo = 0.1f;
            int maxTries = 3;
            int currentTry = 0;

            Vector3? newPos = cornerInfo.position;
            // Try to juggle shifts due to cover continuity and also Y axis shifts
            do
            {
                newPos = VerifyContinuousCoverOfCorner(newPos.Value, Quaternion.LookRotation(-cornerInfo.coverWallNormal, Vector3.up), -cornerInfo.outDirection, 2f, .75f, posSettings, raycastMask, cornerInfo.debugData);
                // Cover verification failed, get out of there
                if (!newPos.HasValue)
                {
                    break;
                }

                cornerInfo.position = newPos.Value;
                newPos = StandardizeCornerOnYAxis(cornerInfo, cornerSettings, raycastMask);
                currentTry++;
            }
            while (newPos.HasValue && Vector3.Distance(cornerInfo.position, newPos.Value) > maxDistanceToRedo && currentTry < maxTries);

            if (!newPos.HasValue)
            {
                return null;
            }

            cornerInfo.position = newPos.Value;

            if (ObstacleInFiringPositionOfCorner(cornerInfo, cornerSettings, raycastMask))
            {
                return null;
            }
            return cornerInfo;
        }

        private static Vector3? VerifyContinuousCoverOfCorner(Vector3 position, Quaternion rotation, Vector3 directionToScanIn, float maxDistanceToAnalyse, float minimumWidth, TacticalPositionSettings posSettings, LayerMask raycastMask, CornerDebugData debugData = null)
        {
            float currentScanningDistance = 0;
            float currentContinuousCoverDistance = 0;
            Vector3? lastGoodPosition = null;
            if (debugData != null)
            {
                debugData.verifyHorizStartPos = position;
                debugData.verifyHorizEndPos = position + directionToScanIn.normalized * maxDistanceToAnalyse;
            }
            while (currentScanningDistance < maxDistanceToAnalyse)
            {
                if (VerifyContinuousVerticalCoverSliceOfACorner(position + directionToScanIn.normalized * currentScanningDistance, rotation, raycastMask, posSettings, debugData))
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

        private static bool VerifyContinuousVerticalCoverSliceOfACorner(Vector3 position, Quaternion rotation, LayerMask raycastMask, TacticalPositionSettings posSettings, CornerDebugData debugData = null)
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
                if (!Physics.Raycast(origin, direction, posSettings.distanceToCheckForCover, raycastMask))
                {
                    if (debugData != null)
                    {
                        debugData.verifyFailurePos = origin;
                    }
                    return false;
                }
            }
            return true;
        }

        private static Vector3? StandardizeCornerOnYAxis(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 outward = Vector3.Cross(Vector3.up, cornerInfo.coverWallNormal);
            if (cornerInfo.coverType == CoverType.LeftCorner)
            {
                outward = -outward;
            }

            // Not all walls are perfectly 90 degree ones. If the wall is leaning away from the cover position, the sphereCast needs to go along the wall normal, since it will hit the wall if it would go Vector3.down.
            Vector3 downAlongWall = Vector3.ProjectOnPlane(Vector3.down, cornerInfo.coverWallNormal).normalized;

            // If the wall is leaning towards the cover position though, use straight Vector3.down, since it could be in the wall when standardizing it by a Y axis offset from the ground.
            if (Vector3.Dot(outward, downAlongWall) < 0)
            {
                downAlongWall = Vector3.down;
            }

            if (cornerInfo.debugData != null)
            {
                cornerInfo.debugData.yAxisStandSphereCastOrigin = cornerInfo.position;
                cornerInfo.debugData.yAxisStandSphereCastDirection = downAlongWall;
                cornerInfo.debugData.yAxisStandSphereCastRadius = cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer;
            }

            if (Physics.SphereCast(cornerInfo.position, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, downAlongWall, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                float standardizedHeight = hit.point.y + cornerSettings.firingPositionHeight;

                if (Mathf.Abs(cornerInfo.position.y - standardizedHeight) > cornerSettings.maxYDifferenceWhenAdjusting)
                {
                    return null;
                }
                else
                {
                    if (cornerInfo.debugData != null)
                    {
                        cornerInfo.debugData.yAxisStandSphereCastHit = hit.point;
                    }
                    cornerInfo.position.y = standardizedHeight;
                    return cornerInfo.position;
                }
            }
            return null;
        }

        private static bool ObstacleInFiringPositionOfCorner(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sphereCastOrigin = cornerInfo.position
                + cornerInfo.coverWallNormal * (cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + cornerInfo.outDirection * (cornerSettings.sphereCastForFiringPositionCheckOffset.x + cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + Vector3.up * cornerSettings.sphereCastForFiringPositionCheckOffset.y;

            Vector3 sphereCastDirection = cornerInfo.positionFiringDirection.normalized;

            if (cornerInfo.debugData != null)
            {
                cornerInfo.debugData.sphereCastOrigin = sphereCastOrigin;
                cornerInfo.debugData.sphereCastDirection = sphereCastDirection;
                cornerInfo.debugData.sphereCastRadius = cornerSettings.sphereCastForFiringPositionCheckRadius;
            }

            // Make sure there is no obstacle between the position and firing pos
            if (Physics.Linecast(cornerInfo.position, sphereCastOrigin))
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
