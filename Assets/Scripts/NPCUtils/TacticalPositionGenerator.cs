
using UnityEngine;

namespace FPSDemo.NPC.Utilities
{
	public class TacticalPositionGenerator : MonoBehaviour
	{
		public TacticalPositionData _tacticalPositionData;
		[SerializeField] TacticalGridGenerationSettings _gridSettings;
		[SerializeField] bool _showThePositionsInEditor = false;

		public void GenerateTacticalPositions()
		{
			ValidateParams();

			_tacticalPositionData._positions.Clear();
			Debug.Log("Generating tactical positions for AI");

			PerformRaycastsAlongTheGrid();
		}

		private void PerformRaycastsAlongTheGrid()
		{
			Vector3 currentPos = _gridSettings.StartPos;
			while (currentPos.x < _gridSettings.EndPos.x)
			{
				currentPos.z = _gridSettings.StartPos.z;
				while (currentPos.z < _gridSettings.EndPos.z)
				{
					RaycastHit[] hitsToConvertToPos = Physics.RaycastAll(currentPos, Vector3.down, Mathf.Infinity, _gridSettings.RaycastMask);
					foreach (var hit in hitsToConvertToPos)
					{
						TacticalPosition newPositionToAdd = new()
						{
							position = hit.point
						};

						_tacticalPositionData._positions.Add(newPositionToAdd);
					}
					currentPos.z += _gridSettings.DistanceBetweenPositions;
				}
				currentPos.x += _gridSettings.DistanceBetweenPositions;
			}
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

		void OnDrawGizmos()
		{
			if (_tacticalPositionData == null || !_showThePositionsInEditor)
			{
				return;
			}

			Debug.Log($"Currently displaying {_tacticalPositionData._positions.Count} positions");
			foreach (TacticalPosition position in _tacticalPositionData._positions)
			{
				Gizmos.DrawSphere(position.position, 0.1f);
			}
		}
	}
}