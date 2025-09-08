using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class PositionValidator
    {
        public virtual CornerDetectionInfo ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, TacticalPositionSettings posSettings, LayerMask raycastMask)
        {
            Vector3? yStandardCornerPos = FindGroundLevelAtCorner(cornerInfo, cornerSettings, raycastMask);

            if (!yStandardCornerPos.HasValue)
            {
                return null;
            }

            float maxDistanceToRedo = 0.1f;
            int maxTries = 3;
            int currentTry = 0;

            Vector3? newPos = cornerInfo.position;
            // Try to juggle shifts due to cover continuity and also Y axis shifts
            do
            {
                //newPos = VerifyContinuousCoverOfCorner(newPos.Value, Quaternion.LookRotation(-cornerInfo.coverWallNormal, Vector3.up), -cornerInfo.outDirection, 2f, .75f, posSettings, raycastMask);
                newPos = VerifyContinuousCoverOfCorner(newPos.Value, cornerInfo, 2f, .75f, posSettings, raycastMask);
                // Cover verification failed, get out of there
                if (!newPos.HasValue)
                {
                    break;
                }

                cornerInfo.position = newPos.Value;
                newPos = FindGroundLevelAtCorner(cornerInfo, cornerSettings, raycastMask);
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

        protected virtual Vector3? VerifyContinuousCoverOfCorner(Vector3 position, CornerDetectionInfo cornerInfo, float maxDistanceToAnalyse, float minimumWidth, TacticalPositionSettings posSettings, LayerMask raycastMask)
        {
            float currentScanningDistance = 0;
            float currentContinuousCoverDistance = 0;
            Vector3? lastGoodPosition = null;

            while (currentScanningDistance < maxDistanceToAnalyse)
            {
                bool continuousCoverFound = false;
                Vector3 cornerPosition = position - cornerInfo.outDirection.normalized * currentScanningDistance;
                if (Physics.Raycast(cornerPosition, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, raycastMask))
                {
                    Vector3? holePos = FindHoleInCoverVertically(groundHit.point, cornerInfo, raycastMask, posSettings);
                    if (!holePos.HasValue)
                    {
                        continuousCoverFound = true;
                    }
                }

                if (continuousCoverFound)
                {
                    if (!lastGoodPosition.HasValue)
                    {
                        lastGoodPosition = position - cornerInfo.outDirection.normalized * currentScanningDistance;
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

        protected virtual Vector3? FindHoleInCoverVertically(Vector3 bottomStart, CornerDetectionInfo cornerInfo, LayerMask raycastMask, TacticalPositionSettings posSettings)
        {
            for (float currentHeight = bottomStart.y + posSettings.bottomRaycastBuffer; currentHeight < cornerInfo.position.y; currentHeight += posSettings.verticalStepToCheckForCover)
            {
                if (!Physics.Raycast(bottomStart, -cornerInfo.coverWallNormal, posSettings.distanceToCheckForCover, raycastMask))
                {
                    return bottomStart;
                }
                bottomStart.y = currentHeight;
            }
            return null;
        }

        protected virtual Vector3? FindGroundLevelAtCorner(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            // Not all walls are perfectly 90 degree ones. If the wall is leaning away from the cover position, the sphereCast needs to go along the wall normal, since it will hit the wall if it would go Vector3.down.
            Vector3 downAlongWall = GetVectorDownAlongWall(cornerInfo);

            if (Physics.SphereCast(cornerInfo.position, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, downAlongWall, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                return ValidateAndAdjustHeight(hit.point, cornerInfo, cornerSettings);
            }
            return null;
        }

        protected virtual Vector3? ValidateAndAdjustHeight(Vector3 pos, CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
        {
            float standardizedHeight = pos.y + cornerSettings.firingPositionHeight;

            if (Mathf.Abs(cornerInfo.position.y - standardizedHeight) > cornerSettings.maxYDifferenceWhenAdjusting)
            {
                return null;
            }
            else
            {
                cornerInfo.position.y = standardizedHeight;
                return cornerInfo.position;
            }
        }

        protected virtual Vector3 GetVectorDownAlongWall(CornerDetectionInfo cornerInfo)
        {
            Vector3 downAlongWall = Vector3.ProjectOnPlane(Vector3.down, cornerInfo.coverWallNormal).normalized;

            Vector3 outward = Vector3.Cross(Vector3.up, cornerInfo.coverWallNormal);
            if (cornerInfo.coverType == CoverType.LeftCorner)
            {
                outward = -outward;
            }
            // If the wall is leaning towards the cover position though, use straight Vector3.down, since it could be in the wall when standardizing it by a Y axis offset from the ground.
            if (Vector3.Dot(outward, downAlongWall) < 0)
            {
                downAlongWall = Vector3.down;
            }

            return downAlongWall;
        }

        protected virtual bool ObstacleInFiringPositionOfCorner(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sphereCastOrigin = CalculateSphereCastOrigin(cornerInfo, cornerSettings);

            Vector3 sphereCastDirection = cornerInfo.positionFiringDirection.normalized;

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

        protected virtual Vector3 CalculateSphereCastOrigin(CornerDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings)
        {
            return cornerInfo.position
                            + cornerInfo.coverWallNormal * (cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                            + cornerInfo.outDirection * (cornerSettings.sphereCastForFiringPositionCheckOffset.x + cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                            + Vector3.up * cornerSettings.sphereCastForFiringPositionCheckOffset.y;
        }
    }
}
