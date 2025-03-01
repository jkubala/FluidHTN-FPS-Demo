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

		public static TacticalPosition? GetHighPosAdjustedToCorner(Vector3 position, Vector3 hitNormal, TacticalGridGenerationSettings gridSettings, TacticalPosDebugGO debugData = null)
		{
			Vector3 offsetPosition = position + hitNormal * gridSettings.cornerCheckRayWallOffset;
			Vector3 leftDirection = Vector3.Cross(Vector3.up, hitNormal).normalized;

			if (debugData != null)
			{
				debugData.offsetPosition = offsetPosition;
				debugData.leftDirection = leftDirection;
			}

			// Return null if inside geometry
			if (Physics.OverlapSphereNonAlloc(offsetPosition, gridSettings.cornerCheckRayWallOffset - 0.001f, nonAllocColBuffer) > 0)
			{
				return null;
			}


			// Looking for left and right corners
			CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hitNormal, leftDirection, true, gridSettings, debugData);
			CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hitNormal, -leftDirection, false, gridSettings, debugData);


			float distanceToLeftCorner = Mathf.Infinity;
			float distanceToRightCorner = Mathf.Infinity;

			if (leftCornerInfo.position.HasValue)
			{
				Vector3 leftCornerPosYAdjusted = leftCornerInfo.position.Value;
				leftCornerPosYAdjusted.y = position.y;
				distanceToLeftCorner = Vector3.Distance(position, leftCornerPosYAdjusted);
				if (debugData != null)
				{
					debugData.leftCornerPos = leftCornerPosYAdjusted;
				}
			}

			if (rightCornerInfo.position.HasValue)
			{
				Vector3 rightCornerPosYAdjusted = rightCornerInfo.position.Value;
				rightCornerPosYAdjusted.y = position.y;
				distanceToRightCorner = Vector3.Distance(position, rightCornerPosYAdjusted);
				if (debugData != null)
				{
					debugData.rightCornerPos = rightCornerPosYAdjusted;
				}
			}


			// If there is not enough space (do not want to have a cover position behind thin objects)

			if (distanceToLeftCorner + distanceToRightCorner < gridSettings.minWidthToConsiderAValidPosition)
			{
				return null;
			}

			if (distanceToLeftCorner < distanceToRightCorner && leftCornerInfo.cornerType == CornerType.Convex)
			{
				SpecialCover specialCoverFound = new()
				{
					rotationToAlignWithCover = Quaternion.Euler(hitNormal),
					type = SpecialCoverType.LeftCorner
				};

				if (debugData != null)
				{
					debugData.transform.position = leftCornerInfo.position.Value;
					debugData.origCornerRayPos = position;
					debugData.specialCover = specialCoverFound;
					debugData.finalCornerPos = leftCornerInfo.position.Value;
					debugData.specialCover = specialCoverFound;
				}

				return new()
				{
					Position = leftCornerInfo.position.Value,
					specialCover = specialCoverFound
				};
			}

			if (distanceToLeftCorner > distanceToRightCorner && rightCornerInfo.cornerType == CornerType.Convex)
			{
				SpecialCover specialCoverFound = new()
				{
					rotationToAlignWithCover = Quaternion.Euler(hitNormal),
					type = SpecialCoverType.RightCorner
				};

				if (debugData != null)
				{
					debugData.transform.position = rightCornerInfo.position.Value;
					debugData.origCornerRayPos = position;
					debugData.specialCover = specialCoverFound;
					debugData.finalCornerPos = rightCornerInfo.position.Value;
					debugData.specialCover = specialCoverFound;
				}

				return new()
				{
					Position = rightCornerInfo.position.Value,
					specialCover = specialCoverFound
				};
			}

			// This high cover position is just against some wall, not useful
			return null;
		}

		private static CornerDetectionInfo FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, bool checkingLeftCorner, TacticalGridGenerationSettings gridSettings, TacticalPosDebugGO debugData)
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


			// Stopped before reaching the full distance
			if (!Mathf.Approximately(distanceToHorizontalObstacle, gridSettings.cornerCheckRaySequenceDistance))
			{
				cornerInfo.position = offsetPosition + direction * distance;
				cornerInfo.cornerType = CornerType.Concave;
			}
			else
			{
				cornerInfo = ValidateCornerPosition(cornerInfo, gridSettings);
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

		private static CornerDetectionInfo ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalGridGenerationSettings gridSettings)
		{
			if (!cornerInfo.position.HasValue)
			{
				return cornerInfo;
			}

			Vector3? yStandardCornerPos = StandardizePositionOnYAxis(cornerInfo.position.Value, 1.6f, gridSettings.RaycastMask);

			if (!yStandardCornerPos.HasValue)
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}

			if (ObstacleInFiringPosition(yStandardCornerPos.Value, cornerInfo.cornerNormal.Value, cornerInfo.cornerFiringPositionNormal.Value, gridSettings))
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}

			cornerInfo.position = yStandardCornerPos;
			return cornerInfo;
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
