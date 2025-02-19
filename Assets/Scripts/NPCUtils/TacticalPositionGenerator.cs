
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
		[SerializeField] private bool _showThePositionsInEditor = false;


		// ========================================================= PROPERTIES

		public TacticalPositionData TacticalPositionData
		{
			get { return _tacticalPositionData; }
			set { _tacticalPositionData = value; }
		}


		// ========================================================= GENERATION

		public void GenerateTacticalPositions()
		{
			ValidateParams();

			if (_tacticalPositionData.Positions == null)
			{
				_tacticalPositionData.Positions = new List<TacticalPosition>();
			}
			else if (_tacticalPositionData.Positions.Count > 0)
			{
				_tacticalPositionData.Positions.Clear();
			}

			Debug.Log("Generating tactical positions for AI");

			CreatePositionsAlongTheGrid();
			AdjustPositions();
		}

		private void AdjustPositions()
		{
			for (int i = 0; i < _tacticalPositionData.Positions.Count; i++)
			{
				bool containsHighCover = false;
				for (int j = 0; j < _gridSettings.NumberOfRays; j++)
				{
					if (_tacticalPositionData.Positions[i].CoverDirections[j].coverType == CoverType.HighCover)
						containsHighCover = true;
				}

				if (containsHighCover)
				{
					if(CheckForCorner(_tacticalPositionData.Positions[i]))
					{
						AdjustForCorner(_tacticalPositionData.Positions[i]);
					}

					// Lift them up for debug purposes
					TacticalPosition modifiedPosition = _tacticalPositionData.Positions[i];
					modifiedPosition.Position += Vector3.up * 1f;
					_tacticalPositionData.Positions[i] = modifiedPosition;
				}
			}
		}

		private bool CheckForCorner(TacticalPosition position)
		{
			return false;
		}

		private void AdjustForCorner(TacticalPosition position)
		{

		}

		private void CreatePositionsAlongTheGrid()
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
						AddPositionIfValid(hit.point);
					}
					currentPos.z += _gridSettings.DistanceBetweenPositions;
				}
				currentPos.x += _gridSettings.DistanceBetweenPositions;
			}
		}

		private void AddPositionIfValid(Vector3 position)
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

			// Discard the position if it provides no cover
			CoverStatus[] coverStates = GetCoverAround(position);
			if (coverStates == null)
			{
				return;
			}

			// Discard the position if it spawned in the geometry
			Vector3 rayCastOrigin = position + Vector3.up * _gridSettings.geometryCheckYOffset;
			float rayLengthExtension = 0.001f; // To avoid situations of no hit, because the ray is 0.000001f short due to float imprecision

			if (Physics.Raycast(rayCastOrigin, Vector3.down, out RaycastHit geometryHit, _gridSettings.geometryCheckYOffset + rayLengthExtension, _gridSettings.RaycastMask))
			{
				if (!Mathf.Approximately(rayCastOrigin.y - geometryHit.point.y, rayCastOrigin.y - position.y))
				{
					return;
				}
			}

			TacticalPosition newPositionToAdd = new()
			{
				Position = position,
				CoverDirections = coverStates
			};

			_tacticalPositionData.Positions.Add(newPositionToAdd);
		}

		// Returns null if no cover found
		private CoverStatus[] GetCoverAround(Vector3 position)
		{
			float angleBetweenRays = 360f / _gridSettings.NumberOfRays;
			List<CoverStatus> coverStates = new();
			Vector3 direction = Vector3.forward;
			bool atLeastOneCoverFound = false;

			Vector3 rayOriginForLowCover = position + Vector3.up * _gridSettings.minHeightToConsiderLowCover;
			Vector3 rayOriginForHighCover = position + Vector3.up * _gridSettings.minHeightToConsiderHighCover;


			for (int i = 0; i < _gridSettings.NumberOfRays; i++)
			{
				CoverStatus newCoverStatus = new();
				if (Physics.Raycast(rayOriginForHighCover, direction, out _, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				{
					newCoverStatus.coverType = CoverType.HighCover;
					atLeastOneCoverFound = true;
				}
				else if (Physics.Raycast(rayOriginForLowCover, direction, out _, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
				{
					newCoverStatus.coverType = CoverType.LowCover;
					atLeastOneCoverFound = true;
				}
				else
				{
					newCoverStatus.coverType = CoverType.NoCover;
				}
				coverStates.Add(newCoverStatus);
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
				Gizmos.DrawSphere(position.Position, 0.1f);
			}
		}
	}
}