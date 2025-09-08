using System.Collections.Generic;
using FPSDemo.Utils;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class CornerFinder
    {
        // A buffer to see, if there are any colliders present
        private readonly Collider[] nonAllocColBuffer = new Collider[1];

        // Find corner vertically
        public virtual CornerDetectionInfo FindLowCoverPos(RaycastHit hit, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = CalculateOffsetPosition(hit, cornerSettings);

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return null;
            }

            // Vertical corner "over" the cover
            return FindCorner(offsetPosition, hit.normal, Vector3.up, cornerSettings, raycastMask);
        }

        public virtual List<CornerDetectionInfo> FindCorners(RaycastHit hit, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            List<CornerDetectionInfo> cornersFound = new();

            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = CalculateOffsetPosition(hit, cornerSettings);
            Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return cornersFound;
            }

            // Looking for left and right corners
            CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hit.normal, leftDirection, cornerSettings, raycastMask);
            CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hit.normal, -leftDirection, cornerSettings, raycastMask);

            // If there is not enough space (do not want to have a cover position behind thin objects)
            if (GetCornerDistance(leftCornerInfo) + GetCornerDistance(rightCornerInfo) < cornerSettings.minWidthToConsiderAValidPosition)
            {
                return cornersFound;
            }

            if (leftCornerInfo != null && leftCornerInfo.cornerType == CornerType.Convex)
            {
                cornersFound.Add(leftCornerInfo);
            }

            if (rightCornerInfo != null && rightCornerInfo.cornerType == CornerType.Convex)
            {
                cornersFound.Add(rightCornerInfo);
            }

            return cornersFound;
        }

        protected virtual Vector3 CalculateOffsetPosition(RaycastHit hit, TacticalPositionScanSettings cornerSettings)
        {
            return hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;
        }

        protected virtual CornerDetectionInfo FindCorner(Vector3 position, Vector3 hitNormal, Vector3 axis, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask)
        {
            float distanceToObstacleAlongTheAxis = cornerSettings.cornerCheckRaySequenceDistance;
            if (Physics.Raycast(position, axis, out RaycastHit hit, cornerSettings.cornerCheckRaySequenceDistance, raycastMask))
            {
                distanceToObstacleAlongTheAxis = Vector3.Distance(position, hit.point);
            }

            CornerDetectionInfo detectedCorner = ScanForConvexCorner(position, hitNormal, axis, cornerSettings, raycastMask, distanceToObstacleAlongTheAxis);

            // Convex corner not found
            if (detectedCorner == null)
            {
                // Stopped before reaching the full distance due to obstacle, return concave corner
                if (!Mathf.Approximately(distanceToObstacleAlongTheAxis, cornerSettings.cornerCheckRaySequenceDistance))
                {
                    detectedCorner = new()
                    {
                        position = position + axis * distanceToObstacleAlongTheAxis,
                        cornerType = CornerType.Concave,
                        distanceToCorner = distanceToObstacleAlongTheAxis
                    };
                }
            }

            return detectedCorner;
        }

        protected virtual CornerDetectionInfo ScanForConvexCorner(Vector3 position, Vector3 hitNormal, Vector3 axis, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, float maxDistance)
        {
            int currentHitsOfDifferentNormal = 0;
            Vector3? lastDifferentNormal = null;
            Vector3? lastAdjustedPosition = null;
            Vector3 adjustedPosition = position;

            // Determine if direction is to the left or right of hitNormal
            CoverType coverType;
            if (axis == Vector3.up)
            {
                coverType = CoverType.Normal;
            }
            else
            {
                coverType = Vector3.Dot(Vector3.Cross(hitNormal, axis), Vector3.up) > 0 ? CoverType.LeftCorner : CoverType.RightCorner;
            }

            // Subtracting floatPrecisionBuffer to avoid raycasts starting inside geometry
            for (float distance = 0; distance <= maxDistance - cornerSettings.floatPrecisionBuffer; distance += cornerSettings.cornerCheckRayStep)
            {
                if (Physics.Raycast(adjustedPosition, -hitNormal, out RaycastHit newHit, cornerSettings.cornerCheckRayWallOffset + cornerSettings.rayLengthBeyondWall, raycastMask))
                {
                    Vector3 newProjectedHitNormal = Vector3.ProjectOnPlane(newHit.normal, Vector3.up).normalized;
                    float angleDifference = Mathf.Abs(Vector3.SignedAngle(hitNormal, newProjectedHitNormal, Vector3.up));
                    if (IsCornerBend(newProjectedHitNormal, angleDifference, cornerSettings, ref currentHitsOfDifferentNormal, ref lastDifferentNormal, ref lastAdjustedPosition, adjustedPosition, newHit))
                    {
                        Vector3 newHitFiringNormal = Vector3.Cross(lastDifferentNormal.Value, Vector3.up).normalized;
                        if (coverType == CoverType.LeftCorner)
                        {
                            newHitFiringNormal = -newHitFiringNormal;
                        }

                        return new()
                        {
                            position = lastAdjustedPosition.Value,
                            cornerType = CornerType.Convex,
                            coverWallNormal = PhysicsUtils.FlattenVector(hitNormal),
                            coverType = coverType,
                            outDirection = axis.normalized,
                            positionFiringDirection = PhysicsUtils.FlattenVector(newHitFiringNormal),
                            distanceToCorner = Vector3.Distance(position, lastAdjustedPosition.Value)
                        };
                    }
                }
                else
                {
                    return new()
                    {
                        position = adjustedPosition,
                        cornerType = CornerType.Convex,
                        coverWallNormal = PhysicsUtils.FlattenVector(hitNormal),
                        coverType = coverType,
                        outDirection = axis.normalized,
                        positionFiringDirection = PhysicsUtils.FlattenVector(-hitNormal),
                        distanceToCorner = Vector3.Distance(position, adjustedPosition)
                    };
                }

                adjustedPosition = position + axis * distance;
            }

            return null; // No convex corner found within the loop
        }

        protected virtual bool IsCornerBend(Vector3 newProjectedNormal, float angleDifference, TacticalPositionScanSettings cornerSettings, ref int currentHitsOfDifferentNormal, ref Vector3? lastDifferentNormal, ref Vector3? lastAdjustedPosition, Vector3 adjustedPosition, RaycastHit newHit)
        {
            if (angleDifference > cornerSettings.minAngleToConsiderCorner)
            {
                if (!lastDifferentNormal.HasValue || lastDifferentNormal.Value != newHit.normal)
                {
                    lastDifferentNormal = newHit.normal;
                    lastAdjustedPosition = adjustedPosition;
                    currentHitsOfDifferentNormal = 1;
                }
                else
                {
                    currentHitsOfDifferentNormal++;

                    if (currentHitsOfDifferentNormal >= cornerSettings.hitsRequiredForCorner)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float GetCornerDistance(CornerDetectionInfo cornerInfo)
        {
            // Corner does not exist, so return infinity
            if (cornerInfo == null)
            {
                return Mathf.Infinity;
            }
            else
            {
                return cornerInfo.distanceToCorner;
            }
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
    }
}
