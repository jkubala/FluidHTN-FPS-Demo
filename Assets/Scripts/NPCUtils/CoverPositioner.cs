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
			public Vector3 cornerWallNormal;
			public Vector3? cornerFiringPositionDirection;
		};
		// A buffer to see, if there are any colliders present
		private static readonly Collider[] nonAllocColBuffer = new Collider[1];

		public static void AddHighPosAdjustedToCornerAtHit(RaycastHit hit, TacticalGridGenerationSettings gridSettings, List<TacticalPosition> listToAddTo)
		{
			Vector3 offsetPosition = hit.point + hit.normal * gridSettings.cornerCheckRayWallOffset;
			Vector3 leftDirection = Vector3.Cross(Vector3.up, hit.normal).normalized;

			// Return if inside geometry
			if (Physics.OverlapSphereNonAlloc(offsetPosition, gridSettings.cornerCheckRayWallOffset - 0.001f, nonAllocColBuffer) > 0)
			{
				return;
			}


			// Looking for left and right corners
			CornerDetectionInfo leftCornerInfo = FindCorner(offsetPosition, hit.normal, leftDirection, true, gridSettings);
			CornerDetectionInfo rightCornerInfo = FindCorner(offsetPosition, hit.normal, -leftDirection, false, gridSettings);

			float distanceToLeftCorner = CalculateCornerDistance(leftCornerInfo, offsetPosition);
			float distanceToRightCorner = CalculateCornerDistance(rightCornerInfo, offsetPosition);

			// If there is not enough space (do not want to have a cover position behind thin objects)
			if (distanceToLeftCorner + distanceToRightCorner < gridSettings.minWidthToConsiderAValidPosition)
			{
				return;
			}

			AddCornerIfConvex(leftCornerInfo, SpecialCoverType.LeftCorner, -leftDirection, gridSettings.cornerCheckPositionOffset, listToAddTo);
			AddCornerIfConvex(rightCornerInfo, SpecialCoverType.RightCorner, leftDirection, gridSettings.cornerCheckPositionOffset, listToAddTo);
		}

		private static CornerDetectionInfo FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, bool checkingLeftCorner, TacticalGridGenerationSettings gridSettings)
		{
			float distanceToHorizontalObstacle = GetDistanceToClosestHit(offsetPosition, direction, gridSettings.cornerCheckRaySequenceDistance, gridSettings.RaycastMask);

			CornerDetectionInfo cornerInfo = new()
			{
				cornerType = CornerType.NoCorner,
				cornerWallNormal = hitNormal,
				cornerFiringPositionDirection = -hitNormal
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
					Vector3 newProjectedHitNormal = Vector3.ProjectOnPlane(newHit.normal, Vector3.up).normalized;
					if (Mathf.Abs(Vector3.SignedAngle(hitNormal, newProjectedHitNormal, Vector3.up)) > gridSettings.minAngleToConsiderCorner)
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
			if (!Mathf.Approximately(distanceToHorizontalObstacle, gridSettings.cornerCheckRaySequenceDistance))
			{
				cornerInfo.position = offsetPosition + direction * distance;
				cornerInfo.cornerType = CornerType.Concave;
			}
			else
			{
				cornerInfo = ValidateCornerPosition(cornerInfo, gridSettings, checkingLeftCorner);
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

		private static CornerDetectionInfo ValidateCornerPosition(CornerDetectionInfo cornerInfo, TacticalGridGenerationSettings gridSettings, bool checkingLeftCorner)
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

			Vector3 cornerOutDirection = Vector3.Cross(Vector3.up, cornerInfo.cornerFiringPositionDirection.Value).normalized;
			if (checkingLeftCorner)
			{
				cornerOutDirection = -cornerOutDirection;
			}

			Vector3? normalStandardCornerPos = StandardizePositionOnNormal(yStandardCornerPos.Value, cornerInfo, checkingLeftCorner, gridSettings);

			if (!normalStandardCornerPos.HasValue)
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}


			if (ObstacleInFiringPosition(normalStandardCornerPos.Value, cornerInfo.cornerWallNormal, cornerOutDirection, gridSettings))
			{
				cornerInfo.cornerType = CornerType.NoCorner;
				return cornerInfo;
			}



			cornerInfo.position = normalStandardCornerPos;
			return cornerInfo;
		}

		private static Vector3? StandardizePositionOnNormal(Vector3 position, CornerDetectionInfo cornerInfo, bool checkingLeftCorner, TacticalGridGenerationSettings gridSettings)
		{
			Vector3 sideDirection = Vector3.Cross(Vector3.up, -cornerInfo.cornerWallNormal);
			if (checkingLeftCorner)
			{
				sideDirection = -sideDirection;
			}

			Vector3 origin = position - sideDirection * gridSettings.cornerCheckRayStep;

			if (Physics.Raycast(origin, -cornerInfo.cornerWallNormal, out _, gridSettings.cornerCheckRayWallOffset + 0.1f, gridSettings.RaycastMask))
			{
				return position + cornerInfo.cornerWallNormal * gridSettings.cornerCheckRayWallOffset;
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

		private static bool ObstacleInFiringPosition(Vector3 cornerPosition, Vector3 cornerNormal, Vector3 cornerOutDirection, TacticalGridGenerationSettings gridSettings)
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

		private static void AddCornerIfConvex(CornerDetectionInfo cornerInfo, SpecialCoverType coverType, Vector3 direction, float cornerCheckPositionOffset, List<TacticalPosition> listToAddTo)
		{
			if (cornerInfo.cornerType != CornerType.Convex)
			{
				return;
			}

			SpecialCover specialCover = new()
			{
				rotationToAlignWithCover = Quaternion.Euler(cornerInfo.cornerWallNormal),
				type = coverType
			};

			TacticalPosition newTacticalPos = new()
			{
				Position = cornerInfo.position.Value + direction * cornerCheckPositionOffset,
				specialCover = specialCover
			};

			listToAddTo.Add(newTacticalPos);
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
