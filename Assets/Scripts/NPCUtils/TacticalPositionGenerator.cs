
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

			Debug.Log("Generating tactical positions for AI");

			CreateSpawnersAlongTheGrid();
			RemoveDuplicates(_gridSettings.distanceToRemoveDuplicates);
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
					TacticalPosition? newPosition = CoverPositioner.GetHighPosAdjustedToCorner(highHit.point, highHit.normal, _gridSettings);

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
	}
}