using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
	public static class CoverPositioner
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
			public Vector3? cornerNormal;
			public Vector3? cornerFiringPositionNormal;
		};
		// A buffer to see, if there are any colliders present
		private static readonly Collider[] nonAllocColBuffer = new Collider[1];

		public static void AddHighPosAdjustedToCorner(Vector3 position, Vector3 hitNormal, TacticalGridGenerationSettings gridSettings, List<TacticalPosition> listToAddTo, TacticalPosDebugGO debugData = null)
		{
			Vector3 offsetPosition = position + hitNormal * gridSettings.cornerCheckRayWallOffset;
			Vector3 leftDirection = Vector3.Cross(Vector3.up, hitNormal).normalized;

			if (debugData != null)
			{
				debugData.offsetPosition = offsetPosition;
				debugData.leftDirection = leftDirection;
			}

			// Return if inside geometry
			if (Physics.OverlapSphereNonAlloc(offsetPosition, gridSettings.cornerCheckRayWallOffset - 0.001f, nonAllocColBuffer) > 0)
			{
				return;
			}


			// Looking for left and right corners
			CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hitNormal, leftDirection, true, gridSettings, debugData);
			CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hitNormal, -leftDirection, false, gridSettings, debugData);


			float distanceToLeftCorner = CalculateCornerDistance(leftCornerInfo, offsetPosition);
			float distanceToRightCorner = CalculateCornerDistance(rightCornerInfo, offsetPosition);

			if (debugData != null)
			{
				debugData.distToLCorner = distanceToLeftCorner;
				debugData.distToRCorner = distanceToRightCorner;
			}

			// If there is not enough space (do not want to have a cover position behind thin objects)
			if (distanceToLeftCorner + distanceToRightCorner < gridSettings.minWidthToConsiderAValidPosition)
			{
				return;
			}

			SetCornerDebugData(debugData, leftCornerInfo, rightCornerInfo, offsetPosition, distanceToLeftCorner, distanceToRightCorner);

			TryAddCorner(leftCornerInfo, SpecialCoverType.LeftCorner,
				-leftDirection, gridSettings.cornerCheckPositionOffset,
				hitNormal, listToAddTo, offsetPosition, debugData);

			TryAddCorner(rightCornerInfo, SpecialCoverType.RightCorner,
						leftDirection, gridSettings.cornerCheckPositionOffset,
						hitNormal, listToAddTo, offsetPosition, debugData);
		}

		private static CornerDetectionInfo FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, bool checkingLeftCorner, TacticalGridGenerationSettings gridSettings, TacticalPosDebugGO debugData)
		{
			float distanceToHorizontalObstacle = GetDistanceToClosestHit(offsetPosition, direction, gridSettings.cornerCheckRaySequenceDistance, gridSettings.RaycastMask);

			Vector3 projectedHitNormal = Vector3.ProjectOnPlane(hitNormal, Vector3.up).normalized;
			CornerDetectionInfo cornerInfo = new()
			{
				cornerType = CornerType.NoCorner,
				cornerNormal = projectedHitNormal,
				cornerFiringPositionNormal = -projectedHitNormal
			};

			float wallOffset = 0.01f; // Sometimes raycasts fired from the point of hit with horizontalObstalce are inside the geometry 
			float distance;
			int currentHitsOfDifferentNormal = gridSettings.nOfHitsOfDifferentNormalToConsiderCorner;
			RaycastHit? lastDifferent = null;
			Vector3? lastAdjustedPosition = null;
			Vector3 adjustedPosition = offsetPosition;
			for (distance = 0; distance <= distanceToHorizontalObstacle - wallOffset; distance += gridSettings.cornerCheckRayStep)
			{
				if (Physics.Raycast(adjustedPosition, -hitNormal, out RaycastHit newHit, gridSettings.cornerCheckRayWallOffset + gridSettings.rayLengthBeyondWall, gridSettings.RaycastMask))
				{
					if (debugData != null)
					{
						debugData.hitPositions.Add(newHit.point);
					}
					Vector3 newProjectedHitNormal = Vector3.ProjectOnPlane(newHit.normal, Vector3.up).normalized;
					if (Mathf.Abs(Vector3.SignedAngle(projectedHitNormal, newProjectedHitNormal, Vector3.up)) > gridSettings.minAngleToConsiderCorner)
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

							if (currentHitsOfDifferentNormal >= gridSettings.nOfHitsOfDifferentNormalToConsiderCorner)
							{

								cornerInfo.position = lastAdjustedPosition;
								cornerInfo.cornerType = CornerType.Convex;

								Vector3 newHitFiringNormal = Vector3.Cross(lastDifferent.Value.normal, Vector3.up).normalized;

								if (checkingLeftCorner)
								{
									newHitFiringNormal = -newHitFiringNormal;
								}


								cornerInfo.cornerNormal = projectedHitNormal;
								cornerInfo.cornerFiringPositionNormal = newHitFiringNormal;
								if (debugData != null)
								{
									debugData.initCornerNormal = cornerInfo.cornerNormal;
									debugData.initCornerFiringNormal = cornerInfo.cornerFiringPositionNormal;
								}
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
			if (!Mathf.Approximately(distanceToHorizontalObstacle, gridSettings.cornerCheckRaySequenceDistance))
			{
				cornerInfo.position = offsetPosition + direction * distance;
				cornerInfo.cornerType = CornerType.Concave;
			}
			else
			{
				cornerInfo = ValidateCornerPosition(cornerInfo, gridSettings, checkingLeftCorner, debugData);
			}

			if (debugData != null)
			{
				if (checkingLeftCorner)
				{
					debugData.leftDirection = direction;
					debugData.maxDistLeft = distance;
					debugData.distanceToObstacleLeft = distanceToHorizontalObstacle;
				}
				else
				{
					debugData.maxDistRight = distance;
					debugData.distanceToObstacleRight = distanceToHorizontalObstacle;
				}
			}


			if (!cornerInfo.position.HasValue)
			{
				cornerInfo.cornerType = CornerType.NoCorner;
			}

			return cornerInfo;
		}

		private static float GetDistanceToClosestHit(Vector3 origin, Vector3 direction, float maxRayDistance, LayerMask layerMask)
		{
			if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, layerMask))
			{
				return Vector3.Distance(origin, hit.point);
			}
			else
			{
				return maxRayDistance;
			}
		}

		private static CornerDetectionInfo ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalGridGenerationSettings gridSettings, bool checkingLeftCorner, TacticalPosDebugGO debugData)
		{
			if (!cornerInfo.position.HasValue)
			{
				return cornerInfo;
			}

			Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo.position.Value, 1.6f, gridSettings);

			if (!yStandardCornerPos.HasValue)
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}

			Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.cornerFiringPositionNormal.Value).normalized;
			if (checkingLeftCorner)
			{
				cornerOutDirection = -cornerOutDirection;
			}

			Vector3? normalStandardCornerPos = StandardizePositionOnNormal(yStandardCornerPos.Value, cornerInfo, checkingLeftCorner, gridSettings, debugData);

			if (!normalStandardCornerPos.HasValue)
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}


			if (ObstacleInFiringPosition(normalStandardCornerPos.Value, cornerInfo.cornerNormal.Value, cornerOutDirection, gridSettings, debugData))
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}



			cornerInfo.position = normalStandardCornerPos;
			return cornerInfo;
		}

		private static Vector3? StandardizePositionOnNormal(Vector3 position, CornerDetectionInfo cornerInfo, bool checkingLeftCorner, TacticalGridGenerationSettings gridSettings, TacticalPosDebugGO debugData)
		{
			Vector3 sideDirection = Vector3.Cross(Vector3.up, -cornerInfo.cornerNormal.Value);
			if (checkingLeftCorner)
			{
				sideDirection = -sideDirection;
			}

			Vector3 origin = position - sideDirection * gridSettings.cornerCheckRayStep;



			if (debugData != null)
			{

				debugData.standardisationOrigin = origin;
				debugData.standardisationDirection = -cornerInfo.cornerNormal.Value;
				debugData.standardisationDistance = gridSettings.cornerCheckRayWallOffset + 0.1f;
			}

			if (Physics.Raycast(origin, -cornerInfo.cornerNormal.Value, out _, gridSettings.cornerCheckRayWallOffset + 0.1f, gridSettings.RaycastMask))
			{
				return position + cornerInfo.cornerNormal.Value * gridSettings.cornerCheckRayWallOffset;
			}
			return null;
		}

		private static Vector3? StandardizePositionOnYAxis(Vector3 position, float distanceFromGround, TacticalGridGenerationSettings gridSettings)
		{
			float radiusOffset = 0.05f;
			if (Physics.SphereCast(position, gridSettings.cornerCheckRayWallOffset - radiusOffset, Vector3.down, out RaycastHit hit, Mathf.Infinity, gridSettings.RaycastMask))
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
			//if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, raycastMask))
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

		private static bool ObstacleInFiringPosition(Vector3 cornerPosition, Vector3 cornerNormal, Vector3 cornerOutDirection, TacticalGridGenerationSettings gridSettings, TacticalPosDebugGO debugData)
		{
			float additionalOffset = 0.01f;

			Vector3 sphereCastOrigin = cornerPosition + cornerNormal * (gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset)
				+ cornerOutDirection * (gridSettings.sphereCastForFiringPositionCheckOffset.x + gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset)
				+ Vector3.up * gridSettings.sphereCastForFiringPositionCheckOffset.y;

			Vector3 sphereCastDirection = Vector3.Cross(Vector3.up, cornerOutDirection).normalized;

			if (Vector3.Dot(cornerOutDirection, Vector3.Cross(Vector3.up, cornerNormal)) < 0)
			{
				sphereCastDirection = -sphereCastDirection;
			}

			if (debugData != null)
			{
				debugData.sphereCastAnchor = cornerPosition;
				debugData.sphereCastOrigin = sphereCastOrigin;
				debugData.sphereCastNormal = cornerNormal;
				debugData.sphereCastDirection = sphereCastDirection * (gridSettings.sphereCastForFiringPositionCheckDistance + gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset);
				debugData.cornerNormal = cornerNormal;
				debugData.cornerFiringNormal = cornerOutDirection;
			}

			if (Physics.SphereCast(sphereCastOrigin,
				gridSettings.sphereCastForFiringPositionCheckRadius,
				sphereCastDirection, out _,
				gridSettings.sphereCastForFiringPositionCheckDistance + gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset,
				gridSettings.RaycastMask))
			{
				return true;
			}
			return false;
		}

		private static void SetCornerDebugData(TacticalPosDebugGO debugData, CornerDetectionInfo leftCorner, CornerDetectionInfo rightCorner,
									 Vector3 offsetPosition, float distLeft, float distRight)
		{
			if (debugData == null) return;

			if (leftCorner.position.HasValue)
			{
				Vector3 leftPos = leftCorner.position.Value;
				leftPos.y = offsetPosition.y;
				debugData.leftCornerPos = leftPos;
			}

			if (rightCorner.position.HasValue)
			{
				Vector3 rightPos = rightCorner.position.Value;
				rightPos.y = offsetPosition.y;
				debugData.rightCornerPos = rightPos;
			}

			debugData.distToLCorner = distLeft;
			debugData.distToRCorner = distRight;
		}

		private static float CalculateCornerDistance(CornerDetectionInfo cornerInfo, Vector3 offsetPosition)
		{
			// TODO MAYBE return -1 instead?
			if (!cornerInfo.position.HasValue)
			{
				return Mathf.Infinity;
			}

			Vector3 cornerPosYAdjusted = cornerInfo.position.Value;
			cornerPosYAdjusted.y = offsetPosition.y;
			return Vector3.Distance(offsetPosition, cornerPosYAdjusted);
		}

		private static void TryAddCorner(CornerDetectionInfo cornerInfo, SpecialCoverType coverType,
							   Vector3 direction, float cornerCheckPositionOffset, Vector3 hitNormal, List<TacticalPosition> listToAddTo,
							   Vector3 offsetPosition, TacticalPosDebugGO debugData)
		{
			if (cornerInfo.cornerType != CornerType.Convex)
			{
				return;
			}

			SpecialCover specialCover = new()
			{
				rotationToAlignWithCover = Quaternion.Euler(hitNormal),
				type = coverType
			};

			listToAddTo.Add(new TacticalPosition
			{
				Position = cornerInfo.position.Value + direction * cornerCheckPositionOffset,
				specialCover = specialCover
			});

			if (debugData != null)
			{
				debugData.transform.position = cornerInfo.position.Value;
				debugData.origCornerRayPos = offsetPosition;
				debugData.specialCover = specialCover;
				debugData.finalCornerPos = cornerInfo.position.Value;
			}
		}

		// Returns null if no cover found TODO: Fill the position info, probably another script for this
		//private static CoverType[] GetCoverAround(Vector3 position, TacticalGridGenerationSettings gridSettings)
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
