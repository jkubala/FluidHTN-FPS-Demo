using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

		public static TacticalPosition? GetHighPosAdjustedToCorner(Vector3 position, Vector3 hitNormal, TacticalGridGenerationSettings gridSettings)
		{
			Vector3 offsetPosition = position + hitNormal * gridSettings.cornerCheckRayWallOffset;
			Vector3 leftDirection = Vector3.Cross(Vector3.up, hitNormal).normalized;

			// Return null if inside geometry
			if (Physics.OverlapSphereNonAlloc(offsetPosition, gridSettings.cornerCheckRayWallOffset - 0.001f, nonAllocColBuffer) > 0)
			{
				return null;
			}


			// Looking for left and right corners
			CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hitNormal, leftDirection, gridSettings);
			CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hitNormal, -leftDirection, gridSettings);

			float distanceToLeftCorner = Mathf.Infinity;
			float distanceToRightCorner = Mathf.Infinity;

			if (leftCornerInfo.position.HasValue)
			{
				Vector3 leftCornerPosYAdjusted = leftCornerInfo.position.Value;
				leftCornerPosYAdjusted.y = position.y;
				distanceToLeftCorner = Vector3.Distance(position, leftCornerPosYAdjusted);
			}

			if (rightCornerInfo.position.HasValue)
			{
				Vector3 rightCornerPosYAdjusted = rightCornerInfo.position.Value;
				rightCornerPosYAdjusted.y = position.y;
				distanceToRightCorner = Vector3.Distance(position, rightCornerPosYAdjusted);
			}


			// If there is not enough space (do not want to have a cover position behind thin objects)

			if (distanceToLeftCorner + distanceToRightCorner < gridSettings.minWidthToConsiderAValidPosition)
			{
				return null;
			}
			if (leftCornerInfo.cornerType == CornerType.Convex || rightCornerInfo.cornerType == CornerType.Convex)
			{
				SpecialCover specialCoverFound = new()
				{
					rotationToAlignWithCover = Quaternion.Euler(hitNormal),
				};

				if (distanceToLeftCorner < distanceToRightCorner)
				{
					specialCoverFound.type = SpecialCoverType.LeftCorner;

					return new()
					{
						Position = leftCornerInfo.position.Value,
						specialCover = specialCoverFound
					};
				}
				else
				{
					specialCoverFound.type = SpecialCoverType.RightCorner;

					return new()
					{
						Position = rightCornerInfo.position.Value,
						specialCover = specialCoverFound
					};
				}
			}

			// This high cover position is just against some wall, not useful
			return null;
		}

		private static bool ObstacleInFiringPosition(Vector3 cornerPosition, Vector3 cornerNormal, Vector3 cornerOutDirection, TacticalGridGenerationSettings gridSettings)
		{
			float additionalOffset = 0.01f;
			Vector3 sphereCastOrigin = cornerPosition +
				-cornerNormal * (gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset) +
				cornerOutDirection * (gridSettings.sphereCastForFiringPositionCheckOffset.x + gridSettings.cornerCheckPositionOffset + gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset)
				+ Vector3.up * gridSettings.sphereCastForFiringPositionCheckOffset.y;


			if (Physics.SphereCast(sphereCastOrigin,
				gridSettings.sphereCastForFiringPositionCheckRadius,
				cornerNormal, out _,
				gridSettings.sphereCastForFiringPositionCheckDistance + gridSettings.sphereCastForFiringPositionCheckRadius + additionalOffset,
				gridSettings.RaycastMask))
			{
				return true;
			}
			return false;
		}

		private static CornerDetectionInfo FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, TacticalGridGenerationSettings gridSettings)
		{
			float distanceToHorizontalObstacle = GetDistanceToClosestHit(offsetPosition, direction, gridSettings.cornerCheckRaySequenceDistance, gridSettings.RaycastMask);
			Vector2 projectedHitNormal = new Vector2(hitNormal.x, hitNormal.z).normalized;
			CornerDetectionInfo cornerInfo = new()
			{
				cornerType = CornerType.NoCorner,
				cornerNormal = projectedHitNormal,
				cornerFiringPositionNormal = -hitNormal
			};
			float wallOffset = 0.01f; // Sometimes raycasts fired from the point of hit with horizontalObstalce are inside the geometry 
			float distance;
			for (distance = 0; distance <= distanceToHorizontalObstacle - wallOffset; distance += gridSettings.cornerCheckRayStep)
			{
				Vector3 adjustedPosition = offsetPosition + direction * distance;
				if (Physics.Raycast(adjustedPosition, -hitNormal, out RaycastHit hit, gridSettings.cornerCheckRayWallOffset + gridSettings.rayLengthBeyondWall, gridSettings.RaycastMask))
				{
					Vector2 newProjectedHitNormal = new Vector2(hit.normal.x, hit.normal.z).normalized;

					if (Vector2.Angle(projectedHitNormal, newProjectedHitNormal) > gridSettings.minAngleToConsiderCorner)
					{
						cornerInfo.position = adjustedPosition - direction * gridSettings.cornerCheckPositionOffset;
						cornerInfo.cornerType = CornerType.Convex;
						cornerInfo.cornerNormal = newProjectedHitNormal;


						Vector3 newHitNormalDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;

						if (Vector3.Dot(direction, newHitNormalDirection) < 0)
						{
							newHitNormalDirection = -newHitNormalDirection;
						}

						cornerInfo.cornerFiringPositionNormal = newHitNormalDirection;
						break;
					}
				}
				else
				{
					cornerInfo.position = adjustedPosition - direction * gridSettings.cornerCheckPositionOffset;
					cornerInfo.cornerType = CornerType.Convex;
					break;
				}
			}


			if(!ValidateCorner(cornerInfo, gridSettings))
			{
				cornerInfo.cornerType = CornerType.NoCorner;
			}
			// Stopped before reaching the full distance
			else if (!Mathf.Approximately(distanceToHorizontalObstacle, gridSettings.cornerCheckRaySequenceDistance))
			{
				cornerInfo.cornerType = CornerType.Concave;
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

		private static bool ValidateCorner(CornerDetectionInfo cornerInfo, TacticalGridGenerationSettings gridSettings)
		{
			if(!cornerInfo.position.HasValue)
			{
				return false;
			}

			Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo.position.Value, 1.6f, gridSettings.RaycastMask);

			if (!yStandardCornerPos.HasValue)
			{
				return false;
			}

			cornerInfo.position = yStandardCornerPos.Value;


			//if (ObstacleInFiringPosition(cornerInfo.position.Value, cornerInfo.cornerFiringPositionNormal.Value, cornerInfo.cornerNormal.Value, gridSettings))
			//{
			//	return false;
			//}

			return true;
		}

		private static Vector3? StandardizePositionOnYAxis(Vector3 position, float distanceFromGround, LayerMask raycastMask)
		{
			if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, raycastMask))
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
