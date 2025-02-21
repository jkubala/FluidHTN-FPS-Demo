
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC.Utilities
{
	public class TacticalPositionGenerator : MonoBehaviour
	{
		// ========================================================= INSPECTOR FIELDS

		[SerializeField] private TacticalPositionData _tacticalPositionData;
		[SerializeField] private TacticalGridGenerationSettings _gridSettings;
		[SerializeField] private bool _useHandplacedTacticalProbes = true;
		[SerializeField] private bool _generateAutoProbeGrid = true;
		[SerializeField] private bool _showThePositionsInEditor = false;
		[SerializeField] private bool generateDebugPositions = false;

		[SerializeField] private GameObject parentOfDebugGameobjects;
		[SerializeField] private GameObject debugGameobjectPrefab;


		// ========================================================= PROPERTIES

		public TacticalPositionData TacticalPositionData
		{
			get { return _tacticalPositionData; }
			set { _tacticalPositionData = value; }
		}
		public bool UseHandplacedTacticalProbes => _useHandplacedTacticalProbes;
		public bool GenerateAutoProbeGrid => _generateAutoProbeGrid;


		// ========================================================= GENERATION

		public void AddTacticalProbes(bool clearPositions, TacticalProbe[] probes)
		{
			if (probes == null || probes.Length == 0)
			{
				return;
			}

			ValidateParams();

			if (_tacticalPositionData.Positions == null)
			{
				_tacticalPositionData.Positions = new List<TacticalPosition>();
			}
			else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
			{
				_tacticalPositionData.Positions.Clear();
			}

			foreach (var probe in probes)
			{
				_tacticalPositionData.Positions.Add(new TacticalPosition
				{
					Position = probe.transform.position,
					CoverDirections = new CoverType[1]
					{
						new() {

						}
					}
				});
			}
		}

		public void GenerateTacticalPositionSpawners(bool clearPositions)
		{
			ValidateParams();

			if (_tacticalPositionData.Positions == null)
			{
				_tacticalPositionData.Positions = new();
			}
			else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
			{
				_tacticalPositionData.Positions.Clear();
			}
			ClearDebugGameObjects();

			Debug.Log("Generating tactical positions for AI");

			CreateSpawnersAlongTheGrid();
			StandardizePositionsOnYAxis();
			RemoveDuplicates(0.2f);
		}

		private void StandardizePositionsOnYAxis()
		{
			for (int i = _tacticalPositionData.Positions.Count - 1; i >= 0; i--) // Iterate from end to start
			{
				TacticalPosition currentPos = _tacticalPositionData.Positions[i];
				if (Physics.Raycast(currentPos.Position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _gridSettings.RaycastMask))
				{
					float standardizedHeight = currentPos.Position.y;
					standardizedHeight = hit.point.y + 1.8f;

					if (currentPos.Position.y - standardizedHeight < 0)
					{
						_tacticalPositionData.Positions.RemoveAt(i); // Remove by index
					}
					else
					{
						currentPos.Position.y = standardizedHeight;
						_tacticalPositionData.Positions[i] = currentPos; // Update the position
					}
				}
			}
		}

		private void RemoveDuplicates(float distanceThreshold)
		{
			// List to store unique positions
			List<TacticalPosition> uniquePositions = new List<TacticalPosition>();

			// Iterate over each position
			for (int i = 0; i < _tacticalPositionData.Positions.Count; i++)
			{
				TacticalPosition currentPos = _tacticalPositionData.Positions[i];
				bool isDuplicate = false;

				// Compare with every other position
				for (int j = 0; j < uniquePositions.Count; j++)
				{
					// If the distance between currentPos and any unique position is less than the threshold, it's a duplicate
					if (Vector3.Distance(currentPos.Position, uniquePositions[j].Position) < distanceThreshold
						&& HaveSameSpecialCover(currentPos, uniquePositions[j]))
					{
						isDuplicate = true;
						break; // No need to check further, it's already a duplicate
					}
				}

				if (!isDuplicate)
				{
					uniquePositions.Add(currentPos);
				}
			}

			_tacticalPositionData.Positions = uniquePositions;
		}

		private bool HaveSameSpecialCover(TacticalPosition position1, TacticalPosition position2)
		{
			return position1.specialCover.type == position2.specialCover.type;
		}

		private void CreateSpawnersAlongTheGrid()
		{
			Vector3 currentPos = _gridSettings.StartPos;
			bool otherRow = false;
			while (currentPos.x < _gridSettings.EndPos.x)
			{
				currentPos.z = _gridSettings.StartPos.z;

				if (_gridSettings.OffsetEveryOtherRow)
				{
					if (otherRow)
					{
						currentPos.z += _gridSettings.DistanceBetweenPositions / 2;
					}
					otherRow = !otherRow;
				}

				while (currentPos.z < _gridSettings.EndPos.z)
				{
					RaycastHit[] hitsToConvertToPos = RaycastTrulyAll(currentPos, Vector3.down, _gridSettings.RaycastMask, 0.1f, 100f);
					foreach (var hit in hitsToConvertToPos)
					{
						AddSpawnerIfValid(hit.point);
					}
					currentPos.z += _gridSettings.DistanceBetweenPositions;
				}
				currentPos.x += _gridSettings.DistanceBetweenPositions;
			}
		}

		private void AddSpawnerIfValid(Vector3 position)
		{
			// Discard the position if it is too far from NavMesh
			if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _gridSettings.RaycastMask))
			{
				Vector3 heightAdjustedHit = hit.position;
				heightAdjustedHit.y = position.y;

				if (Vector3.Distance(heightAdjustedHit, position) > _gridSettings.RequiredProximityToNavMesh)
				{
					return;
				}
			}
			else
			{
				return;
			}

			// Discard the position if it spawned in the geometry
			Vector3 rayCastOrigin = position + Vector3.up * _gridSettings.geometryCheckYOffset;

			if (Physics.Raycast(rayCastOrigin, Vector3.down, out RaycastHit geometryHit, _gridSettings.geometryCheckYOffset + _gridSettings.rayLengthBeyondWall, _gridSettings.RaycastMask))
			{
				if (!Mathf.Approximately(rayCastOrigin.y - geometryHit.point.y, rayCastOrigin.y - position.y))
				{
					return;
				}
			}

			CreatePositionsAtHitsAround(position);
		}

		private void CreatePositionsAtHitsAround(Vector3 position)
		{
			float angleBetweenRays = 360f / _gridSettings.NumberOfRaysSpawner;
			Vector3 direction = Vector3.forward;

			Vector3 rayOriginForLowCover = position + Vector3.up * _gridSettings.minHeightToConsiderLowCover;
			Vector3 rayOriginForHighCover = position + Vector3.up * _gridSettings.minHeightToConsiderHighCover;


			for (int i = 0; i < _gridSettings.NumberOfRaysSpawner; i++)
			{
				if (Physics.Raycast(rayOriginForHighCover, direction, out RaycastHit highHit, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				{
					TacticalPosition? newPosition = GetHighPosAdjustedToCorner(highHit.point, highHit.normal);

					if (newPosition.HasValue)
					{
						// Try to find special cover for high cover
						_tacticalPositionData.Positions.Add(newPosition.Value);
					}
				}
				//else if (Physics.Raycast(rayOriginForLowCover, direction, out RaycastHit lowHit, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				//{
				//	TacticalPosition newPosition = new()
				//	{
				//		Position = lowHit.point
				//	};
				//	// Try to find special cover for low cover
				//	_tacticalPositionData.Positions.Add(newPosition);
				//}

				direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
			}
		}



		private TacticalPosition? GetHighPosAdjustedToCorner(Vector3 position, Vector3 hitNormal)
		{
			//return new TacticalPosition() { Position = position }; // Debug to see where the positions are starting from


			Vector3 offsetPosition = position + hitNormal * _gridSettings.cornerCheckRayWallOffset;
			Vector3 leftDirection = Vector3.Cross(Vector3.up, hitNormal).normalized;

			// Return null if inside geometry
			if (Physics.OverlapSphere(offsetPosition, _gridSettings.cornerCheckRayWallOffset - 0.001f).Length > 0)
			{
				return null;
			}


			float distanceToLeftCorner = Mathf.Infinity;
			float distanceToRightCorner = Mathf.Infinity;

			// Looking for left and right corners
			Vector3 leftCornerPos = FindCorner(offsetPosition, hitNormal, leftDirection, ref distanceToLeftCorner);
			Vector3 rightCornerPos = FindCorner(offsetPosition, hitNormal, -leftDirection, ref distanceToRightCorner);

			// If there is not enough space (do not want to have a cover position behind thin objects)
			if (distanceToLeftCorner + distanceToRightCorner < _gridSettings.minWidthToConsiderAValidPosition)
			{
				return null;
			}

			if (distanceToLeftCorner != Mathf.Infinity || distanceToRightCorner != Mathf.Infinity)
			{
				if (distanceToLeftCorner < distanceToRightCorner)
				{
					SpecialCover specialCoverFound = new()
					{
						rotationToAlignWithCover = Quaternion.Euler(hitNormal),
						type = SpecialCoverType.LeftCorner
					};

					TacticalPosition newPositionToAdd = new()
					{
						Position = leftCornerPos,
						specialCover = specialCoverFound
					};

					if (generateDebugPositions)
					{
						if (parentOfDebugGameobjects == null)
						{
							Debug.LogError("Parent of the debug gameObjects is null!");
							return null;
						}

						if (debugGameobjectPrefab == null)
						{
							Debug.LogError("Debug gameObject prefab is null!");
							return null;
						}

						GameObject newDebugGO = Instantiate(debugGameobjectPrefab, parentOfDebugGameobjects.transform);
						newDebugGO.transform.position = leftCornerPos;
						TacticalPosDebugGO debugGO = newDebugGO.GetComponent<TacticalPosDebugGO>();
						debugGO.leftCornerDist = distanceToLeftCorner;
						debugGO.rightCornerDist = distanceToRightCorner;
						debugGO.specialCover = specialCoverFound;
						debugGO.origCornerRayPos = offsetPosition;
					}

					return newPositionToAdd;
				}
				else
				{
					SpecialCover specialCoverFound = new()
					{
						rotationToAlignWithCover = Quaternion.Euler(hitNormal),
						type = SpecialCoverType.RightCorner
					};

					TacticalPosition newPositionToAdd = new()
					{
						Position = rightCornerPos,
						specialCover = specialCoverFound
					};

					if (generateDebugPositions)
					{
						if (parentOfDebugGameobjects == null)
						{
							Debug.LogError("Parent of the debug gameObjects is null!");
							return null;
						}

						if (debugGameobjectPrefab == null)
						{
							Debug.LogError("Debug gameObject prefab is null!");
							return null;
						}

						GameObject newDebugGO = Instantiate(debugGameobjectPrefab, parentOfDebugGameobjects.transform);
						newDebugGO.transform.position = rightCornerPos;
						TacticalPosDebugGO debugGO = newDebugGO.GetComponent<TacticalPosDebugGO>();
						debugGO.leftCornerDist = distanceToLeftCorner;
						debugGO.rightCornerDist = distanceToRightCorner;
						debugGO.specialCover = specialCoverFound;
						debugGO.origCornerRayPos = offsetPosition;
					}

					return newPositionToAdd;
				}
			}

			// This high cover position is just against some wall, not useful
			return null;
		}

		private Vector3 FindCorner(Vector3 offsetPosition, Vector3 hitNormal, Vector3 direction, ref float distanceToCorner)
		{

			float wallOffset = 0.01f; // Sometimes raycasts fired from the point of hit are inside the geometry 
			float distanceClampedForObstacles = GetDistanceToClosestHit(offsetPosition, direction, _gridSettings.cornerCheckRaySequenceDistance, _gridSettings.RaycastMask) - wallOffset;

			for (float distance = 0; distance <= distanceClampedForObstacles; distance += _gridSettings.cornerCheckRayStep)
			{
				Vector3 adjustedPosition = offsetPosition + direction * distance;
				if (!Physics.Raycast(adjustedPosition, -hitNormal, _gridSettings.cornerCheckRayWallOffset + _gridSettings.rayLengthBeyondWall, _gridSettings.RaycastMask))
				{
					distanceToCorner = distance;
					return adjustedPosition - direction * _gridSettings.cornerCheckPositionOffset;
				}
			}

			if (_gridSettings.cornerCheckRaySequenceDistance > distanceClampedForObstacles && distanceToCorner == Mathf.Infinity)
			{
				distanceToCorner = distanceClampedForObstacles;
			}

			return Vector3.zero;
		}

		private float GetDistanceToClosestHit(Vector3 origin, Vector3 direction, float maxRayDistance, LayerMask layerMask)
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

		// Returns null if no cover found
		private CoverType[] GetCoverAround(Vector3 position)
		{
			float angleBetweenRays = 360f / _gridSettings.NumberOfRaysSpawner;
			List<CoverType> coverStates = new();
			Vector3 direction = Vector3.forward;
			bool atLeastOneCoverFound = false;

			Vector3 rayOriginForLowCover = position + Vector3.up * _gridSettings.minHeightToConsiderLowCover;
			Vector3 rayOriginForHighCover = position + Vector3.up * _gridSettings.minHeightToConsiderHighCover;


			for (int i = 0; i < _gridSettings.NumberOfRaysSpawner; i++)
			{
				CoverType newCoverType;
				if (Physics.Raycast(rayOriginForHighCover, direction, out _, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				{
					newCoverType = CoverType.HighCover;
					atLeastOneCoverFound = true;
				}
				else if (Physics.Raycast(rayOriginForLowCover, direction, out _, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				{
					newCoverType = CoverType.LowCover;
					atLeastOneCoverFound = true;
				}
				else
				{
					newCoverType = CoverType.NoCover;
				}
				coverStates.Add(newCoverType);
				direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
			}

			if (atLeastOneCoverFound)
			{
				return coverStates.ToArray();
			}

			return null;
		}

		private RaycastHit[] RaycastTrulyAll(Vector3 initialXZCoordToCheck, Vector3 direction, LayerMask layerMask, float offsetAfterHit, float maxLength)
		{
			List<RaycastHit> _raycasts = new();

			var thisRayOrigin = initialXZCoordToCheck;

			// TODO: Ensure this can't go infinite loop. I'd prefer a maxIteration failsafe mechanic here just to make sure it can't.
			while (Physics.Raycast(thisRayOrigin, direction, out var hit, maxLength, layerMask) && (initialXZCoordToCheck - hit.point).magnitude < maxLength)
			{
				_raycasts.Add(hit);
				thisRayOrigin = hit.point + direction * offsetAfterHit;
			}

			return _raycasts.ToArray();
		}

		private void ValidateParams()
		{
			if (_gridSettings == null)
			{
				Debug.LogError("Could not generate the grid of tactical positions. The grid settings are not assigned!");
				return;
			}

			if (_gridSettings.DistanceBetweenPositions < 0.5f)
			{
				Debug.LogError("Could not generate the grid of tactical positions. The distance between positions is too small!");
				return;
			}
		}


		// ========================================================= DEBUG

		void OnDrawGizmos()
		{
			if (_tacticalPositionData == null || !_showThePositionsInEditor)
			{
				return;
			}

			Debug.Log($"Currently displaying {_tacticalPositionData.Positions.Count} positions");
			foreach (TacticalPosition position in _tacticalPositionData.Positions)
			{
				if (position.specialCover.type == SpecialCoverType.LeftCorner)
				{
					Gizmos.color = Color.red;
				}
				else if (position.specialCover.type == SpecialCoverType.RightCorner)
				{
					Gizmos.color = Color.blue;
				}
				else
				{
					Gizmos.color = Color.white;
				}
				Gizmos.DrawSphere(position.Position, 0.1f);
			}
		}

		public void ClearDebugGameObjects()
		{
			if (parentOfDebugGameobjects == null)
			{
				Debug.LogError("Parent of the debug gameObjects is null!");
				return;
			}

			if (Application.isPlaying)
			{
				Debug.Log("Cannot destroy them in play mode!");
				return;
			}

			for (int i = parentOfDebugGameobjects.transform.childCount - 1; i >= 0; i--)
			{
				DestroyImmediate(parentOfDebugGameobjects.transform.GetChild(i).gameObject);
			}
		}
	}
}