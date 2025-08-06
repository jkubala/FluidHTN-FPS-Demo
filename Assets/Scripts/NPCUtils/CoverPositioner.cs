using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class CoverPositioner
    {
        enum CornerType
        {
            Convex,
            Concave
        }

        struct CornerDetectionInfo
        {
            public CornerType cornerType;
            public bool leftCorner;
            public Vector3 position;
            public Vector3 cornerWallNormal;
            public Vector3 cornerFiringPositionDirection;
        };

        private static CoverPositioner _coverPositioner;

        public static CoverPositioner GetCoverPositioner
        {
            get
            {
                if (_coverPositioner == null)
                {
                    _coverPositioner = new CoverPositioner();
                    Debug.Log("CoverPositioner singleton created for TacticalPositionGenerator");
                }
                return _coverPositioner;
            }
        }

        // A buffer to see, if there are any colliders present
        private static readonly Collider[] nonAllocColBuffer = new Collider[1];
        private CoverPositioner()
        {
        }

        public void FindLowCoverPos()
        {
            // Find corner vertically
        }

        public void FindCornerPos(RaycastHit hit, CoverHeight coverHeight, TacticalCornerSettings cornerSettings, LayerMask raycastMask, List<TacticalPosition> listToAddTo)
        {
            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;
            Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return;
            }

            // Looking for left and right corners
            CornerDetectionInfo? leftCornerInfo = FindConvexCorner(offsetPosition, hit.normal, leftDirection, cornerSettings, raycastMask);
            CornerDetectionInfo? rightCornerInfo = FindConvexCorner(offsetPosition, hit.normal, -leftDirection, cornerSettings, raycastMask);

            if (leftCornerInfo.HasValue && leftCornerInfo.Value.cornerType == CornerType.Convex)
            {
                leftCornerInfo = AdjustCornerPosition(leftCornerInfo.Value, cornerSettings, raycastMask);
            }
            if (rightCornerInfo.HasValue && rightCornerInfo.Value.cornerType == CornerType.Convex)
            {
                rightCornerInfo = AdjustCornerPosition(rightCornerInfo.Value, cornerSettings, raycastMask);
            }

            // Infinity if there is no corner
            float distanceToLeftCorner = CalculateCornerDistance(leftCornerInfo, offsetPosition);
            float distanceToRightCorner = CalculateCornerDistance(rightCornerInfo, offsetPosition);

            // If there is not enough space (do not want to have a cover position behind thin objects)
            if (distanceToLeftCorner + distanceToRightCorner < cornerSettings.minWidthToConsiderAValidPosition)
            {
                return;
            }

            if (leftCornerInfo.HasValue)
            {
                AddCornerIfConvex(leftCornerInfo.Value, MainCoverType.LeftCorner, coverHeight, -leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, listToAddTo);
            }

            if (rightCornerInfo.HasValue)
            {
                AddCornerIfConvex(rightCornerInfo.Value, MainCoverType.RightCorner, coverHeight, leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, listToAddTo);
            }
        }

        private CornerDetectionInfo? FindConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            float distanceToHorizontalObstacle = cornerSettings.cornerCheckRaySequenceDistance;
            if (Physics.Raycast(offsetPosition, direction, out RaycastHit hit, cornerSettings.cornerCheckRaySequenceDistance, raycastMask))
            {
                distanceToHorizontalObstacle = Vector3.Distance(offsetPosition, hit.point);
            }

            if (ScanForConvexCorner(offsetPosition, hitNormal, direction, cornerSettings, raycastMask, distanceToHorizontalObstacle, out Vector3? detectedPosition, out Vector3? detectedFiringDirection))
            {
                return new()
                {
                    position = detectedPosition.Value,
                    cornerType = CornerType.Convex,
                    cornerWallNormal = hitNormal,
                    leftCorner = Vector3.Dot(Vector3.Cross(hitNormal, direction), Vector3.up) > 0,
                    cornerFiringPositionDirection = detectedFiringDirection.Value
                };
            }
            else
            {
                // Stopped before reaching the full distance due to obstacle
                if (!Mathf.Approximately(distanceToHorizontalObstacle, cornerSettings.cornerCheckRaySequenceDistance))
                {
                    return new()
                    {
                        position = offsetPosition + direction * distanceToHorizontalObstacle,
                        cornerType = CornerType.Concave,
                        cornerWallNormal = hitNormal,
                        cornerFiringPositionDirection = -hitNormal
                    };
                }
            }

            return null;
        }

        private bool ScanForConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, TacticalCornerSettings cornerSettings, LayerMask raycastMask, float maxDistance, out Vector3? detectedPosition, out Vector3? detectedFiringDirection)
        {
            detectedPosition = null;
            detectedFiringDirection = null;

            int currentHitsOfDifferentNormal = 0;
            RaycastHit? lastDifferentHit = null;
            Vector3? lastAdjustedPosition = null;
            Vector3 adjustedPosition = offsetPosition;

            // Determine if direction is to the left or right of hitNormal
            bool isLeftCorner = Vector3.Dot(Vector3.Cross(hitNormal, direction), Vector3.up) > 0;

            // Subtracting floatPrecisionBuffer to avoid raycasts starting inside geometry
            for (float distance = 0; distance <= maxDistance - cornerSettings.floatPrecisionBuffer; distance += cornerSettings.cornerCheckRayStep)
            {
                if (Physics.Raycast(adjustedPosition, -hitNormal, out RaycastHit newHit, cornerSettings.cornerCheckRayWallOffset + cornerSettings.rayLengthBeyondWall, raycastMask))
                {
                    Vector3 newProjectedHitNormal = Vector3.ProjectOnPlane(newHit.normal, Vector3.up).normalized;
                    float angleDifference = Mathf.Abs(Vector3.SignedAngle(hitNormal, newProjectedHitNormal, Vector3.up));

                    if (angleDifference > cornerSettings.minAngleToConsiderCorner)
                    {
                        if (!lastDifferentHit.HasValue || lastDifferentHit.Value.normal != newHit.normal)
                        {
                            lastDifferentHit = newHit;
                            lastAdjustedPosition = adjustedPosition;
                            currentHitsOfDifferentNormal = 1;
                        }
                        else
                        {
                            currentHitsOfDifferentNormal++;

                            if (currentHitsOfDifferentNormal >= cornerSettings.nOfHitsOfDifferentNormalToConsiderCorner)
                            {
                                Vector3 newHitFiringNormal = Vector3.Cross(lastDifferentHit.Value.normal, Vector3.up).normalized;
                                if (isLeftCorner)
                                {
                                    newHitFiringNormal = -newHitFiringNormal;
                                }

                                detectedPosition = lastAdjustedPosition;
                                detectedFiringDirection = newHitFiringNormal;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    detectedPosition = adjustedPosition;
                    detectedFiringDirection = -hitNormal;
                    return true;
                }

                adjustedPosition = offsetPosition + direction * distance;
            }

            return false; // No convex corner found within the loop
        }

        private CornerDetectionInfo? AdjustCornerPosition(CornerDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {

            Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo.position, cornerSettings.firingPositionHeight, cornerSettings, raycastMask);

            if (!yStandardCornerPos.HasValue)
            {
                return null;
            }

            Vector3? normalStandardCornerPos = StandardizePositionOnNormal(yStandardCornerPos.Value, cornerInfo, cornerSettings, raycastMask);

            if (!normalStandardCornerPos.HasValue)
            {
                return null;
            }

            cornerInfo.position = normalStandardCornerPos.Value;

            Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.cornerFiringPositionDirection).normalized;
            if (cornerInfo.leftCorner)
            {
                cornerOutDirection = -cornerOutDirection;
            }
            if (ObstacleInFiringPosition(normalStandardCornerPos.Value, cornerInfo.cornerWallNormal, cornerOutDirection, cornerSettings, raycastMask))
            {
                return null;
            }

            return cornerInfo;
        }

        private Vector3? StandardizePositionOnNormal(Vector3 position, CornerDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sideDirection = Vector3.Cross(Vector3.up, -cornerInfo.cornerWallNormal);
            if (cornerInfo.leftCorner)
            {
                sideDirection = -sideDirection;
            }

            Vector3 origin = position - sideDirection * cornerSettings.cornerCheckRayStep;

            if (Physics.Raycast(origin, -cornerInfo.cornerWallNormal, out _, cornerSettings.cornerCheckRayWallOffset + cornerSettings.floatPrecisionBuffer, raycastMask))
            {
                return position + cornerInfo.cornerWallNormal * cornerSettings.cornerCheckRayWallOffset;
            }
            return null;
        }

        private Vector3? StandardizePositionOnYAxis(Vector3 position, float distanceFromGround, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            float radiusOffset = 0.05f; // TODO if replaced by floatPrecisionBuffer, we lose 3 positions in the DemoMap1. Find out which ones and decide whether to replace or not
            if (Physics.SphereCast(position, cornerSettings.cornerCheckRayWallOffset - radiusOffset, Vector3.down, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                float standardizedHeight = hit.point.y + distanceFromGround;
                float maxThresholdAboveGround = 2.5f;

                if (position.y - standardizedHeight < 0 || position.y - standardizedHeight > maxThresholdAboveGround)
                {
                    return null;
                }
                else
                {
                    position.y = standardizedHeight;
                    return position;
                }
            }
            return null;
        }

        private bool ObstacleInFiringPosition(Vector3 cornerPosition, Vector3 cornerNormal, Vector3 cornerOutDirection, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sphereCastOrigin = cornerPosition + cornerNormal * (cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + cornerOutDirection * (cornerSettings.sphereCastForFiringPositionCheckOffset.x + cornerSettings.sphereCastForFiringPositionCheckRadius + cornerSettings.floatPrecisionBuffer)
                + Vector3.up * cornerSettings.sphereCastForFiringPositionCheckOffset.y;

            Vector3 sphereCastDirection = Vector3.Cross(Vector3.up, cornerOutDirection).normalized;

            if (Vector3.Dot(cornerOutDirection, Vector3.Cross(Vector3.up, cornerNormal)) < 0)
            {
                sphereCastDirection = -sphereCastDirection;
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

        private float CalculateCornerDistance(CornerDetectionInfo? cornerInfo, Vector3 offsetPosition)
        {
            // Corner does not exist, so return infinity
            if (!cornerInfo.HasValue)
            {
                return Mathf.Infinity;
            }

            Vector3 cornerPosYAdjusted = cornerInfo.Value.position;
            cornerPosYAdjusted.y = offsetPosition.y;
            return Vector3.Distance(offsetPosition, cornerPosYAdjusted);
        }

        private void AddCornerIfConvex(CornerDetectionInfo cornerInfo, MainCoverType coverType, CoverHeight coverHeight, Vector3 direction, float cornerCheckPositionOffset, LayerMask raycastMask, List<TacticalPosition> listToAddTo)
        {
            if (cornerInfo.cornerType != CornerType.Convex || cornerInfo.position == null)
            {
                return;
            }

            MainCover mainCover = new()
            {
                type = coverType,
                height = coverHeight,
                rotationToAlignWithCover = Quaternion.LookRotation(-cornerInfo.cornerWallNormal, Vector3.up)
            };

            TacticalPosition newTacticalPos = new()
            {
                Position = cornerInfo.position + direction * cornerCheckPositionOffset,
                mainCover = mainCover,
                isOutside = SimpleIsOutsideCheck(cornerInfo.position, raycastMask)
            };

            listToAddTo.Add(newTacticalPos);
        }

        // Returns null if no cover found TODO: Fill the position info, probably another script for this
        //private CoverType[] GetCoverAround(Vector3 position, TacticalGridGenerationSettings gridSettings)
        //{
        //	float angleBetweenRays = 360f / gridSettings.NumberOfRaysSpawner;
        //	List<CoverType> coverStates = new();
        //	Vector3 direction = Vector3.forward;
        //	bool atLeastOneCoverFound = false;

        //	Vector3 rayOriginForLowCover = position + Vector3.up * gridSettings.minHeightToConsiderLowCover;
        //	Vector3 rayOriginForHighCover = position + Vector3.up * gridSettings.minHeightToConsiderHighCover;


        //	for (int i = 0; i < gridSettings.NumberOfRaysSpawner; i++)
        //	{
        //		CoverType newCoverType;
        //		if (Physics.Raycast(rayOriginForHighCover, direction, out _, gridSettings.DistanceOfRaycasts, gridSettings.RaycastMask))
        //		{
        //			newCoverType = CoverType.HighCover;
        //			atLeastOneCoverFound = true;
        //		}
        //		else if (Physics.Raycast(rayOriginForLowCover, direction, out _, gridSettings.DistanceOfRaycasts, gridSettings.RaycastMask))
        //		{
        //			newCoverType = CoverType.LowCover;
        //			atLeastOneCoverFound = true;
        //		}
        //		else
        //		{
        //			newCoverType = CoverType.NoCover;
        //		}
        //		coverStates.Add(newCoverType);
        //		direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
        //	}

        //	if (atLeastOneCoverFound)
        //	{
        //		return coverStates.ToArray();
        //	}

        //	return null;
        //}

        bool SimpleIsOutsideCheck(Vector3 position, LayerMask raycastMask)
        {
            if (Physics.Raycast(position, Vector3.up, Mathf.Infinity, raycastMask))
            {
                return false;
            }

            return true;
        }
    }
}
