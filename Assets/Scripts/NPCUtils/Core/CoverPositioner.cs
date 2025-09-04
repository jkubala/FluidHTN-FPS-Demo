using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        public List<TacticalPosition> FindLowCoverPos(RaycastHit hit, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalPositionSettings posSettings, TacticalDebugData debugData = null)
        {
            List<TacticalPosition> tacticalPositions = new();
            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;

            Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (debugData != null)
            {
                debugData.offsetPosition = offsetPosition;
                debugData.leftDirection = leftDirection;
                debugData.leftCorner.hitPositions = new();
                debugData.rightCorner.hitPositions = new();
            }

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return null;
            }

            // Vertical corner "over" the cover
            PositionDetectionInfo? cornerInfo = FindConvexCorner(offsetPosition, hit.normal, Vector3.up, cornerSettings, raycastMask, debugData);
            // No corner found
            if (cornerInfo == null)
            {
                return null;
            }

            if (debugData != null)
            {
                debugData.leftCorner.cornerNormal = cornerInfo.Value.coverWallNormal;
                debugData.leftCorner.cornerFiringNormal = cornerInfo.Value.positionFiringDirection;
            }

            cornerInfo = AdjustLowPosition(cornerInfo.Value, cornerSettings, raycastMask, debugData);

            // Failed to adjust lowPosition
            if (cornerInfo == null)
            {
                return null;
            }

            // Check if it is a valid firing position
            if (ObstacleInFiringPosition(cornerInfo.Value, cornerSettings, raycastMask, debugData))
            {
                return null;
            }

            // Find out the leftmost and rightmost "ends", like the sides of a window. Low cover height needs to be also checked - logarithmic approach cannot
            // be used, since every X centimeters need to be checked in case there is a hole in the cover there

            // If the ends are found, center the position in the middle, maybe add logic that will add Y positions and 
            // Else do nothing
            // TODO special logic when only one end is found? Move the position to some standardized distance from it, if it is closer than Z?

            TacticalPosition tacticalPosition = AddCornerIfConvex(cornerInfo.Value, CoverHeight.LowCover, Vector3.up, cornerSettings.cornerCheckPositionOffset, raycastMask, cornerSettings.minWidthToConsiderAValidPosition, posSettings, debugData);

            if (tacticalPosition != null)
            {
                tacticalPositions.Add(tacticalPosition);
            }

            return tacticalPositions;
        }

        private PositionDetectionInfo? AdjustLowPosition(PositionDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
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

            return cornerInfo;
        }

        public List<TacticalPosition> FindCornerPos(RaycastHit hit, CoverHeight coverHeight, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalPositionSettings posSettings, TacticalDebugData debugData = null)
        {
            // Offset self from the wall in the direction of its normal
            Vector3 offsetPosition = hit.point + hit.normal * cornerSettings.cornerCheckRayWallOffset;
            Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (debugData != null)
            {
                debugData.offsetPosition = offsetPosition;
                debugData.leftDirection = leftDirection;
                debugData.leftCorner.hitPositions = new();
                debugData.rightCorner.hitPositions = new();
            }

            // Return if inside geometry
            if (Physics.OverlapSphereNonAlloc(offsetPosition, cornerSettings.cornerCheckRayWallOffset - cornerSettings.floatPrecisionBuffer, nonAllocColBuffer) > 0)
            {
                return null;
            }

            float distanceToLeftCorner = Mathf.Infinity;
            float distanceToRightCorner = Mathf.Infinity;

            // Looking for left and right corners
            PositionDetectionInfo? leftCornerInfo;
            leftCornerInfo = FindConvexCorner(offsetPosition, hit.normal, leftDirection, cornerSettings, raycastMask, debugData);
            if (leftCornerInfo.HasValue && leftCornerInfo.Value.cornerType == CornerType.Convex)
            {
                if (debugData != null)
                {
                    debugData.leftCorner.cornerNormal = leftCornerInfo.Value.coverWallNormal;
                    debugData.leftCorner.cornerFiringNormal = leftCornerInfo.Value.positionFiringDirection;
                }
                distanceToLeftCorner = CalculateCornerDistance(leftCornerInfo, offsetPosition);
                leftCornerInfo = AdjustCornerPosition(leftCornerInfo.Value, cornerSettings, raycastMask, debugData);
            }
            // Infinity if there is no corner

            PositionDetectionInfo? rightCornerInfo = FindConvexCorner(offsetPosition, hit.normal, -leftDirection, cornerSettings, raycastMask, debugData);
            if (rightCornerInfo.HasValue && rightCornerInfo.Value.cornerType == CornerType.Convex)
            {
                if (debugData != null)
                {
                    debugData.rightCorner.cornerNormal = rightCornerInfo.Value.coverWallNormal;
                    debugData.rightCorner.cornerFiringNormal = rightCornerInfo.Value.positionFiringDirection;
                }
                distanceToRightCorner = CalculateCornerDistance(rightCornerInfo, offsetPosition);
                rightCornerInfo = AdjustCornerPosition(rightCornerInfo.Value, cornerSettings, raycastMask, debugData);
            }


            if (debugData != null)
            {
                debugData.leftCorner.distanceToCorner = distanceToLeftCorner;
                debugData.rightCorner.distanceToCorner = distanceToRightCorner;
            }

            // If there is not enough space (do not want to have a cover position behind thin objects)
            if (distanceToLeftCorner + distanceToRightCorner < cornerSettings.minWidthToConsiderAValidPosition)
            {
                return null;
            }

            List<TacticalPosition> tacticalPositions = new();


            if (leftCornerInfo.HasValue)
            {
                if (debugData != null)
                {
                    debugData.leftCorner.cornerPos = leftCornerInfo.Value.position;
                }
                TacticalPosition leftCornerPosition = AddCornerIfConvex(leftCornerInfo.Value, coverHeight, -leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, cornerSettings.minWidthToConsiderAValidPosition, posSettings, debugData);
                if (leftCornerPosition != null)
                {
                    tacticalPositions.Add(leftCornerPosition);
                }
            }

            if (rightCornerInfo.HasValue)
            {
                if (debugData != null)
                {
                    debugData.rightCorner.cornerPos = rightCornerInfo.Value.position;
                }
                TacticalPosition rightCornerPosition = AddCornerIfConvex(rightCornerInfo.Value, coverHeight, leftDirection, cornerSettings.cornerCheckPositionOffset, raycastMask, cornerSettings.minWidthToConsiderAValidPosition, posSettings, debugData);
                if (rightCornerPosition != null)
                {
                    tacticalPositions.Add(rightCornerPosition);
                }
            }

            return tacticalPositions;
        }

        private PositionDetectionInfo? FindConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 axis, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugDataToAddTo = null)
        {
            float distanceToObstacleAlongTheAxis = cornerSettings.cornerCheckRaySequenceDistance;
            if (Physics.Raycast(offsetPosition, axis, out RaycastHit hit, cornerSettings.cornerCheckRaySequenceDistance, raycastMask))
            {
                distanceToObstacleAlongTheAxis = Vector3.Distance(offsetPosition, hit.point);
            }

            CoverType coverType;
            if (axis == Vector3.up)
            {
                coverType = CoverType.Normal;
            }
            else
            {
                coverType = Vector3.Dot(Vector3.Cross(hitNormal, axis), Vector3.up) > 0 ? CoverType.LeftCorner : CoverType.RightCorner;
            }

            if (ScanForConvexCorner(offsetPosition, hitNormal, axis, cornerSettings, raycastMask, distanceToObstacleAlongTheAxis, out Vector3? detectedPosition, out Vector3? detectedFiringDirection, debugDataToAddTo))
            {
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
                        coverType = coverType,
                        positionFiringDirection = flattenedFiringPosition
                    };
                }
            }

            return null;
        }

        private bool ScanForConvexCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 axis, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, float maxDistance, out Vector3? detectedPosition, out Vector3? detectedFiringDirection, TacticalDebugData debugDataToAddTo = null)
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
                    if (isLeftCorner)
                    {
                        debugDataToAddTo?.leftCorner.hitPositions.Add(newHit.point);
                    }
                    else
                    {
                        debugDataToAddTo?.rightCorner.hitPositions.Add(newHit.point);
                    }
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

        private PositionDetectionInfo? AdjustCornerPosition(PositionDetectionInfo cornerInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
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

            //Vector3? normalStandardCornerPos = StandardizePositionOnNormal(yStandardCornerPos.Value, cornerInfo, cornerSettings, raycastMask, debugData);

            //if (!normalStandardCornerPos.HasValue)
            //{
            //    return null;
            //}

            //cornerInfo.position = normalStandardCornerPos.Value;

            //Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.positionFiringDirection).normalized;
            //if (cornerInfo.coverType == CoverType.LeftCorner)
            //{
            //    cornerOutDirection = -cornerOutDirection;
            //}

            //cornerInfo.positionFiringDirection = cornerOutDirection;
            //if (ObstacleInFiringPosition(cornerInfo, cornerSettings, raycastMask, debugData))
            //{
            //    return null;
            //}

            return cornerInfo;
        }

        bool VerifyContinuousCoverOfPosition(Vector3 position, Quaternion rotation, Vector3 directionToScanIn, float maxDistanceToAnalyse, float minimumWidth, TacticalPositionSettings posSettings, LayerMask raycastMask, bool isLeftCorner, TacticalDebugData debugData = null)
        {
            float currentScanningDistance = 0;
            float currentContinuousCoverDistance = 0;
            if (debugData != null)
            {
                if (isLeftCorner)
                {
                    debugData.leftCorner.verifyHorizStartPos = position;
                    debugData.leftCorner.verifyHorizEndPos = position + directionToScanIn * maxDistanceToAnalyse;
                }
                else
                {
                    debugData.rightCorner.verifyHorizStartPos = position;
                    debugData.rightCorner.verifyHorizEndPos = position + directionToScanIn * maxDistanceToAnalyse;
                }
            }
            while (currentScanningDistance < maxDistanceToAnalyse)
            {
                if (VerifyContinuousVerticalCoverSliceOfAPosition(position + directionToScanIn.normalized * currentScanningDistance, rotation, raycastMask, posSettings, isLeftCorner, debugData))
                {

                    if (currentContinuousCoverDistance >= minimumWidth)
                    {
                        return true;
                    }
                    currentContinuousCoverDistance += posSettings.horizontalStepToCheckForCover;
                }
                else
                {
                    currentContinuousCoverDistance = 0;
                }
                currentScanningDistance += posSettings.horizontalStepToCheckForCover;
            }
            return currentContinuousCoverDistance >= minimumWidth;
        }

        private bool VerifyContinuousVerticalCoverSliceOfAPosition(Vector3 position, Quaternion rotation, LayerMask raycastMask, TacticalPositionSettings posSettings, bool isLeftCorner, TacticalDebugData debugData = null)
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

        private Vector3? StandardizePositionOnYAxis(PositionDetectionInfo positionInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
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

        private bool ObstacleInFiringPosition(PositionDetectionInfo positionInfo, TacticalPositionScanSettings cornerSettings, LayerMask raycastMask, TacticalDebugData debugData = null)
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

        private TacticalPosition AddCornerIfConvex(PositionDetectionInfo cornerInfo, CoverHeight coverHeight, Vector3 outDirection, float cornerCheckPositionOffset, LayerMask raycastMask, float minWidth, TacticalPositionSettings posSettings, TacticalDebugData debugData = null)
        {
            if (cornerInfo.cornerType != CornerType.Convex)
            {
                return null;
            }

            Vector3 flattenedWallNormal = cornerInfo.coverWallNormal;
            flattenedWallNormal.y = 0;
            flattenedWallNormal.Normalize();
            MainCover mainCover = new()
            {
                type = cornerInfo.coverType,
                height = coverHeight,
                rotationToAlignWithCover = Quaternion.LookRotation(-flattenedWallNormal, Vector3.up)
            };

            TacticalPosition newTacticalPos = new()
            {
                Position = cornerInfo.position + outDirection * cornerCheckPositionOffset,
                mainCover = mainCover,
                isOutside = SimpleIsOutsideCheck(cornerInfo.position, raycastMask),
                CoverDirections = Array.Empty<CoverHeight>()
            };

            if (debugData != null)
            {
                if (cornerInfo.coverType == CoverType.LeftCorner)
                {
                    debugData.leftCorner.finalCornerPos = newTacticalPos.Position;
                    debugData.leftCorner.tacticalPosition = newTacticalPos;
                }
                else
                {
                    debugData.rightCorner.finalCornerPos = newTacticalPos.Position;
                    debugData.rightCorner.tacticalPosition = newTacticalPos;
                }
            }

            // TODO refactor
            float maxDistanceToAnalyse = 2f;
            if (!VerifyContinuousCoverOfPosition(cornerInfo.position, newTacticalPos.mainCover.rotationToAlignWithCover, outDirection, maxDistanceToAnalyse, minWidth, posSettings, raycastMask, cornerInfo.coverType == CoverType.LeftCorner, debugData))
            {
                return null;
            }

            if (debugData != null)
            {
                debugData.MarkAsFinished();
            }
            return newTacticalPos;
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
