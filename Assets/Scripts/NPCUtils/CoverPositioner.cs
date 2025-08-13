using System;
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

        struct PositionDetectionInfo
        {
            public CornerType cornerType;
            public CoverType coverType;
            public Vector3 position;
            public Vector3 coverWallNormal;
            public Vector3 positionFiringDirection;
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

        // Find corner vertically
        public void FindLowCoverPos(RaycastHit hit, CoverHeight coverHeight, TacticalCornerSettings cornerSettings, LayerMask raycastMask, List<TacticalPosition> listToAddTo)
        {
            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return;
            }

            // Vertical corner "over" the cover
            PositionDetectionInfo? cornerInfo = FindConvexCorner(offsetPosition, hit.normal, Vector3.up, cornerSettings, raycastMask);

            // No corner found
            if (cornerInfo == null)
            {
                return;
            }

            cornerInfo = AdjustLowPosition(cornerInfo.Value, cornerSettings, raycastMask);

            // Failed to adjust lowPosition
            if (cornerInfo == null)
            {
                return;
            }

            // Check if it is a valid firing position
            if (ObstacleInFiringPosition(cornerInfo.Value.position, cornerInfo.Value.coverWallNormal, cornerInfo.Value.positionFiringDirection.normalized, cornerSettings, raycastMask))
            {
                return;
            }

            // Find out the leftmost and rightmost "ends", like the sides of a window. Low cover height needs to be also checked - logarithmic approach cannot
            // be used, since every X centimeters need to be checked in case there is a hole in the cover there

            // If the ends are found, center the position in the middle, maybe add logic that will add Y positions and 
            // Else do nothing
            // TODO special logic when only one end is found? Move the position to some standardized distance from it, if it is closer than Z?

            // Add the tactical position
            AddCornerIfConvex(cornerInfo.Value, CoverType.LeftCorner, coverHeight, Vector3.up, cornerSettings.cornerCheckPositionOffset, raycastMask, listToAddTo);
        }

        private PositionDetectionInfo? AdjustLowPosition(PositionDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {

            Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo, cornerSettings.firingPositionHeight, cornerSettings, raycastMask);

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
            return cornerInfo;
        }

        public void FindCornerPos(RaycastHit hit, CoverHeight coverHeight, TacticalCornerSettings cornerSettings, LayerMask raycastMask, List<TacticalPosition> listToAddTo, List<TacticalDebugData> debugDataToAddTo = null)
        {
            TacticalDebugData debugData = debugDataToAddTo != null ? new TacticalDebugData() : null;

            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;
            Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (debugData != null)
            {
                debugDataToAddTo.Add(debugData);
                debugData.offsetPosition = offsetPosition;
                debugData.leftDirection = leftDirection;
            }

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return;
            }

            // Looking for left and right corners
            PositionDetectionInfo? leftCornerInfo = FindConvexCorner(offsetPosition, hit.normal, leftDirection, cornerSettings, raycastMask);
            PositionDetectionInfo? rightCornerInfo = FindConvexCorner(offsetPosition, hit.normal, -leftDirection, cornerSettings, raycastMask);

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

            if (debugData != null)
            {
                debugData.distanceToCornerLeft = distanceToLeftCorner;
                debugData.distanceToCornerRight = distanceToRightCorner;
            }

            // If there is not enough space (do not want to have a cover position behind thin objects)
            if (distanceToLeftCorner + distanceToRightCorner < cornerSettings.minWidthToConsiderAValidPosition)
            {
                return;
            }

            if (leftCornerInfo.HasValue)
            {
                AddCornerIfConvex(leftCornerInfo.Value, CoverType.LeftCorner, coverHeight, -leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, listToAddTo);
            }

            if (rightCornerInfo.HasValue)
            {
                AddCornerIfConvex(rightCornerInfo.Value, CoverType.RightCorner, coverHeight, leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, listToAddTo);
            }
        }

        private PositionDetectionInfo? FindConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 axis, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            float distanceToObstacleAlongTheAxis = cornerSettings.cornerCheckRaySequenceDistance;
            if (Physics.Raycast(offsetPosition, axis, out RaycastHit hit, cornerSettings.cornerCheckRaySequenceDistance, raycastMask))
            {
                distanceToObstacleAlongTheAxis = Vector3.Distance(offsetPosition, hit.point);
            }

            if (ScanForConvexCorner(offsetPosition, hitNormal, axis, cornerSettings, raycastMask, distanceToObstacleAlongTheAxis, out Vector3? detectedPosition, out Vector3? detectedFiringDirection))
            {
                CoverType coverType;
                if (axis == Vector3.up)
                {
                    coverType = CoverType.Normal;
                }
                else
                {
                    coverType = Vector3.Dot(Vector3.Cross(hitNormal, axis), Vector3.up) > 0 ? CoverType.LeftCorner : CoverType.RightCorner;
                }
                return new()
                {
                    position = detectedPosition.Value,
                    cornerType = CornerType.Convex,
                    coverWallNormal = hitNormal,
                    coverType = coverType,
                    positionFiringDirection = detectedFiringDirection.Value
                };
            }
            else
            {
                // Stopped before reaching the full distance due to obstacle
                if (!Mathf.Approximately(distanceToObstacleAlongTheAxis, cornerSettings.cornerCheckRaySequenceDistance))
                {
                    Vector3 flattenedFiringPosition = -hitNormal;
                    flattenedFiringPosition.y = 0;
                    flattenedFiringPosition.Normalize();
                    return new()
                    {
                        position = offsetPosition + axis * distanceToObstacleAlongTheAxis,
                        cornerType = CornerType.Concave,
                        coverWallNormal = hitNormal,
                        positionFiringDirection = flattenedFiringPosition
                    };
                }
            }

            return null;
        }

        private bool ScanForConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 axis, TacticalCornerSettings cornerSettings, LayerMask raycastMask, float maxDistance, out Vector3? detectedPosition, out Vector3? detectedFiringDirection)
        {
            detectedPosition = null;
            detectedFiringDirection = null;

            int currentHitsOfDifferentNormal = 0;
            RaycastHit? lastDifferentHit = null;
            Vector3? lastAdjustedPosition = null;
            Vector3 adjustedPosition = offsetPosition;

            // Determine if direction is to the left or right of hitNormal
            bool isLeftCorner = false;
            if (axis != Vector3.up)
            {
                isLeftCorner = Vector3.Dot(Vector3.Cross(hitNormal, axis), Vector3.up) > 0;
            }

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

                adjustedPosition = offsetPosition + axis * distance;
            }

            return false; // No convex corner found within the loop
        }

        private PositionDetectionInfo? AdjustCornerPosition(PositionDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {

            Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo, cornerSettings.firingPositionHeight, cornerSettings, raycastMask);

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

            Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.positionFiringDirection).normalized;
            if (cornerInfo.coverType == CoverType.LeftCorner)
            {
                cornerOutDirection = -cornerOutDirection;
            }
            if (ObstacleInFiringPosition(normalStandardCornerPos.Value, cornerInfo.coverWallNormal, cornerOutDirection, cornerSettings, raycastMask))
            {
                return null;
            }

            return cornerInfo;
        }

        private Vector3? StandardizePositionOnNormal(Vector3 position, PositionDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sideDirection = Vector3.Cross(Vector3.up, -cornerInfo.coverWallNormal);
            if (cornerInfo.coverType == CoverType.LeftCorner)
            {
                sideDirection = -sideDirection;
            }

            Vector3 origin = position - sideDirection * cornerSettings.cornerCheckRayStep;

            if (Physics.Raycast(origin, -cornerInfo.coverWallNormal, out _, cornerSettings.cornerCheckRayWallOffset + cornerSettings.floatPrecisionBuffer, raycastMask))
            {
                return position + cornerInfo.coverWallNormal * cornerSettings.cornerCheckRayWallOffset;
            }
            return null;
        }

        private Vector3? StandardizePositionOnYAxis(PositionDetectionInfo positionInfo, float distanceFromGround, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
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
                downAlongWall = Vector3.down;

            if (Physics.SphereCast(positionInfo.position, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, downAlongWall, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                float standardizedHeight = hit.point.y + distanceFromGround;
                float maxThresholdAboveGround = 2.5f;

                if (positionInfo.position.y - standardizedHeight < 0 || positionInfo.position.y - standardizedHeight > maxThresholdAboveGround)
                {
                    return null;
                }
                else
                {
                    positionInfo.position.y = standardizedHeight;
                    return positionInfo.position;
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

        private float CalculateCornerDistance(PositionDetectionInfo? cornerInfo, Vector3 offsetPosition)
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

        private void AddCornerIfConvex(PositionDetectionInfo cornerInfo, CoverType coverType, CoverHeight coverHeight, Vector3 direction, float cornerCheckPositionOffset, LayerMask raycastMask, List<TacticalPosition> listToAddTo, TacticalDebugData debugData = null)
        {
            if (cornerInfo.cornerType != CornerType.Convex)
            {
                return;
            }

            Vector3 flattenedWallNormal = cornerInfo.coverWallNormal;
            flattenedWallNormal.y = 0;
            flattenedWallNormal.Normalize();
            MainCover mainCover = new()
            {
                type = coverType,
                height = coverHeight,
                rotationToAlignWithCover = Quaternion.LookRotation(-flattenedWallNormal, Vector3.up)
            };

            TacticalPosition newTacticalPos = new()
            {
                Position = cornerInfo.position + direction * cornerCheckPositionOffset,
                mainCover = mainCover,
                isOutside = SimpleIsOutsideCheck(cornerInfo.position, raycastMask),
                CoverDirections = Array.Empty<CoverHeight>()
            };

            if (debugData != null)
            {
                debugData.tacticalPosition = newTacticalPos;
                debugData.finalCornerPos = newTacticalPos.Position;
            }

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
