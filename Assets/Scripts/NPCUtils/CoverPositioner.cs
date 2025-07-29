using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
    public class CoverPositioner
    {
        enum CornerType
        {
            NoCorner,
            Convex,
            Concave
        }

        struct CornerDetectionInfo
        {
            public CornerType cornerType;
            public Vector3? position;
            public Vector3 cornerWallNormal;
            public Vector3? cornerFiringPositionDirection;
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
            CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hit.normal, leftDirection, true, cornerSettings, raycastMask);
            CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hit.normal, -leftDirection, false, cornerSettings, raycastMask);

            // Infinity if there is no corner
            float distanceToLeftCorner = CalculateCornerDistance(leftCornerInfo, offsetPosition);
            float distanceToRightCorner = CalculateCornerDistance(rightCornerInfo, offsetPosition);

            // If there is not enough space (do not want to have a cover position behind thin objects)
            if (distanceToLeftCorner + distanceToRightCorner < cornerSettings.minWidthToConsiderAValidPosition)
            {
                return;
            }

            AddCornerIfConvex(leftCornerInfo, MainCoverType.LeftCorner, coverHeight, -leftDirection, cornerSettings.cornerCheckPositionOffset, listToAddTo);
            AddCornerIfConvex(rightCornerInfo, MainCoverType.RightCorner, coverHeight, leftDirection, cornerSettings.cornerCheckPositionOffset, listToAddTo);
        }

        private CornerDetectionInfo FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, bool checkingLeftCorner, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            float distanceToHorizontalObstacle = GetDistanceToClosestHit(offsetPosition, direction, cornerSettings.cornerCheckRaySequenceDistance, raycastMask);

            CornerDetectionInfo cornerInfo = new()
            {
                cornerType = CornerType.NoCorner,
                cornerWallNormal = hitNormal,
                cornerFiringPositionDirection = -hitNormal
            };

            float distance;
            int currentHitsOfDifferentNormal = cornerSettings.nOfHitsOfDifferentNormalToConsiderCorner;
            RaycastHit? lastDifferent = null;
            Vector3? lastAdjustedPosition = null;
            Vector3 adjustedPosition = offsetPosition;
            // Subtracting floatPrecisionBuffer, because sometimes raycasts fired from the point of hit with horizontalObstalce are inside the geometry 
            for (distance = 0; distance <= distanceToHorizontalObstacle - cornerSettings.floatPrecisionBuffer; distance += cornerSettings.cornerCheckRayStep)
            {
                if (Physics.Raycast(adjustedPosition, -hitNormal, out RaycastHit newHit, cornerSettings.cornerCheckRayWallOffset + cornerSettings.rayLengthBeyondWall, raycastMask))
                {
                    Vector3 newProjectedHitNormal = Vector3.ProjectOnPlane(newHit.normal, Vector3.up).normalized;
                    if (Mathf.Abs(Vector3.SignedAngle(hitNormal, newProjectedHitNormal, Vector3.up)) > cornerSettings.minAngleToConsiderCorner)
                    {
                        if (lastDifferent == null || lastDifferent.Value.normal != newHit.normal)
                        {
                            lastDifferent = newHit;
                            lastAdjustedPosition = adjustedPosition;
                            currentHitsOfDifferentNormal = 1;
                        }
                        else
                        {
                            currentHitsOfDifferentNormal++;

                            if (currentHitsOfDifferentNormal >= cornerSettings.nOfHitsOfDifferentNormalToConsiderCorner)
                            {

                                cornerInfo.position = lastAdjustedPosition;
                                cornerInfo.cornerType = CornerType.Convex;

                                Vector3 newHitFiringNormal = Vector3.Cross(lastDifferent.Value.normal, Vector3.up).normalized;

                                if (checkingLeftCorner)
                                {
                                    newHitFiringNormal = -newHitFiringNormal;
                                }


                                cornerInfo.cornerFiringPositionDirection = newHitFiringNormal;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    cornerInfo.position = adjustedPosition;
                    cornerInfo.cornerType = CornerType.Convex;
                    break;
                }
                adjustedPosition = offsetPosition + direction * distance;
            }


            // Stopped before reaching the full distance
            if (!Mathf.Approximately(distanceToHorizontalObstacle, cornerSettings.cornerCheckRaySequenceDistance))
            {
                cornerInfo.position = offsetPosition + direction * distance;
                cornerInfo.cornerType = CornerType.Concave;
            }
            else
            {
                cornerInfo = ValidateCornerPosition(cornerInfo, cornerSettings, checkingLeftCorner, raycastMask);
            }

            if (!cornerInfo.position.HasValue)
            {
                cornerInfo.cornerType = CornerType.NoCorner;
            }

            return cornerInfo;
        }

        private float GetDistanceToClosestHit(Vector3 origin, Vector3 direction, float maxRayDistance, LayerMask raycastMask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, raycastMask))
            {
                return Vector3.Distance(origin, hit.point);
            }
            else
            {
                return maxRayDistance;
            }
        }

        private CornerDetectionInfo ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalCornerSettings cornerSettings, bool checkingLeftCorner, LayerMask raycastMask)
        {
            if (!cornerInfo.position.HasValue)
            {
                return cornerInfo;
            }

            Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo.position.Value, cornerSettings.firingPositionHeight, cornerSettings, raycastMask);

            if (!yStandardCornerPos.HasValue)
            {
                cornerInfo.cornerType = CornerType.NoCorner;
                return cornerInfo;
            }

            Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.cornerFiringPositionDirection.Value).normalized;
            if (checkingLeftCorner)
            {
                cornerOutDirection = -cornerOutDirection;
            }

            Vector3? normalStandardCornerPos = StandardizePositionOnNormal(yStandardCornerPos.Value, cornerInfo, checkingLeftCorner, cornerSettings, raycastMask);

            if (!normalStandardCornerPos.HasValue)
            {
                cornerInfo.cornerType = CornerType.NoCorner;
                return cornerInfo;
            }


            if (ObstacleInFiringPosition(normalStandardCornerPos.Value, cornerInfo.cornerWallNormal, cornerOutDirection, cornerSettings, raycastMask))
            {
                cornerInfo.cornerType = CornerType.NoCorner;
                return cornerInfo;
            }



            cornerInfo.position = normalStandardCornerPos;
            return cornerInfo;
        }

        private Vector3? StandardizePositionOnNormal(Vector3 position, CornerDetectionInfo cornerInfo, bool checkingLeftCorner, TacticalCornerSettings cornerSettings, LayerMask raycastMask)
        {
            Vector3 sideDirection = Vector3.Cross(Vector3.up, -cornerInfo.cornerWallNormal);
            if (checkingLeftCorner)
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
            //if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _raycastMask))
            //{
            //	float standardizedHeight = hit.point.y + distanceFromGround;
            //	float maxThresholdAboveGround = 2.5f;

            //	if (position.y - standardizedHeight < 0 || position.y - standardizedHeight > maxThresholdAboveGround)
            //	{
            //		return null;
            //	}
            //	else
            //	{
            //		position.y = standardizedHeight;
            //		return position;
            //	}
            //}
            //return null;
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

        private float CalculateCornerDistance(CornerDetectionInfo cornerInfo, Vector3 offsetPosition)
        {
            // Corner does not exist, so return infinity
            if (!cornerInfo.position.HasValue)
            {
                return Mathf.Infinity;
            }

            Vector3 cornerPosYAdjusted = cornerInfo.position.Value;
            cornerPosYAdjusted.y = offsetPosition.y;
            return Vector3.Distance(offsetPosition, cornerPosYAdjusted);
        }

        private void AddCornerIfConvex(CornerDetectionInfo cornerInfo, MainCoverType coverType, CoverHeight coverHeight, Vector3 direction, float cornerCheckPositionOffset, List<TacticalPosition> listToAddTo)
        {
            if (cornerInfo.cornerType != CornerType.Convex)
            {
                return;
            }

            MainCover specialCover = new()
            {
                type = coverType,
                height = coverHeight,
                rotationToAlignWithCover = Quaternion.Euler(cornerInfo.cornerWallNormal)
            };

            TacticalPosition newTacticalPos = new()
            {
                Position = cornerInfo.position.Value + direction * cornerCheckPositionOffset,
                specialCover = specialCover
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
    }
}
