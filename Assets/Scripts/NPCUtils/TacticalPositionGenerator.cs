
using System.Collections.Generic;
using UnityEngine;

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
			set {_tacticalPositionData = value; }
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
                    CoverDirections = new CoverStatus[1]
                    {
                        new CoverStatus
                        {

                        }
                    }
                });
            }
        }

        public void GenerateTacticalPositions(bool clearPositions)
		{
			ValidateParams();

            if (_tacticalPositionData.Positions == null)
            {
                _tacticalPositionData.Positions = new List<TacticalPosition>();
            }
            else if (clearPositions && _tacticalPositionData.Positions.Count > 0)
            {
                _tacticalPositionData.Positions.Clear();
            }

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
							Position = hit.point
						};

						_tacticalPositionData.Positions.Add(newPositionToAdd);
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