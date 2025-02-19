
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
                        if (ValidatePosition(hit.point))
                        {
                            TacticalPosition newPositionToAdd = new()
                            {
                                Position = hit.point
                            };

                            _tacticalPositionData.Positions.Add(newPositionToAdd);
                        }
                    }
					currentPos.z += _gridSettings.DistanceBetweenPositions;
				}
				currentPos.x += _gridSettings.DistanceBetweenPositions;
			}
		}

        private bool ValidatePosition(Vector3 position)
        {
            // Discard the position if it is too far from NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, _gridSettings.RaycastMask))
            {
                Vector3 heightAdjustedHit = hit.position;
                heightAdjustedHit.y = position.y;
                if (Vector3.Distance(heightAdjustedHit, position) > _gridSettings.RequiredProximityToNavMesh)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            // Discard the position if it provides no cover
            RaycastHit[] raycastHits = RaycastFanAround(position);
            if (raycastHits.Length == 0)
            {
                return false;
            }
            return true;
        }
        private RaycastHit[] RaycastFanAround(Vector3 position)
        {
            float angleBetweenRays = 360f / _gridSettings.NumberOfRays;
            List<RaycastHit> raycastHits = new();
            Vector3 rcOrigin = position + Vector3.up * 0.5f;
            Vector3 direction = Vector3.forward;
            for (int i = 0; i < _gridSettings.NumberOfRays; i++)
            {
                if (Physics.Raycast(rcOrigin, direction, out RaycastHit hit, _gridSettings.DistanceOfRaycasts, _gridSettings.RaycastMask))
                {
                    raycastHits.Add(hit);
                }
                direction = Quaternion.Euler(0, angleBetweenRays, 0) * direction;
            }
            return raycastHits.ToArray();
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