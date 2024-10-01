using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FPSDemo.Player
{
	public class EnvironmentScanner : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private float _forwardRaysYPosStart = 1f;
		[SerializeField] private float _forwardRaysForwardOffset = 0.3f;
		[SerializeField] private float _forwardRayLength = 0.8f;
		[SerializeField] private float _heightRayLength = 3.25f;
		[SerializeField] private float _heightRayLengthUngrounded = 2f;
		[SerializeField] private float _minDistanceForUniqueXZPoint = 0.01f;

		[SerializeField] private LayerMask _obstacleLayer;

        [SerializeField] private Player _player;
        [SerializeField] private PlayerCrouching _playerCrouching;


        // ========================================================= PRIVATE FIELDS

        private float _forwardRayEveryXDistance = 0.2f;
        private float _actualRayLengthFromGround;
        private float _actualRayLengthFromGroundUngrounded;
        private float _actualForwardRayLength;

        private readonly List<Vector3> _forwardRayOrigins = new();
		private List<RaycastHit> _forwardHitPositions = new();

		private List<RaycastHit> _raycasts = null;
		private List<ValidHit> _validHits = new();


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponent<Player>();
            }

            if (_playerCrouching == null)
            {
                _playerCrouching = GetComponent<PlayerCrouching>();
            }
        }

        void Awake()
		{
			_actualRayLengthFromGround = _heightRayLength - _forwardRaysYPosStart;
			_actualRayLengthFromGroundUngrounded = _heightRayLengthUngrounded - _forwardRaysYPosStart;
			_actualForwardRayLength = _forwardRayLength - _forwardRaysForwardOffset;
			
			InitRaycastColumn();
		}


        // ========================================================= INIT

        private void InitRaycastColumn()
		{
			var nOfRaycasts = Mathf.CeilToInt(_actualRayLengthFromGround / _forwardRayEveryXDistance);
			var newRayCastOriginPoint = Vector3.up * _forwardRaysYPosStart;

			for (var i = 0; i < nOfRaycasts; i++)
			{
				_forwardRayOrigins.Add(newRayCastOriginPoint);
				newRayCastOriginPoint = newRayCastOriginPoint + Vector3.up * _forwardRayEveryXDistance;
			}
		}


        // ========================================================= PUBLIC METHODS

        public List<ValidHit> ObstacleCheck()
        {
            _validHits.Clear();

			var heightRayLengthToUse = _player.IsGrounded ? _heightRayLength : _heightRayLengthUngrounded;
			FindOutUniquePointsOnXZPlane();

			var offsetIntoWall = transform.forward * 0.01f;
			foreach (var forwardHit in _forwardHitPositions)
			{
				var coordXZToCheck = forwardHit.point + offsetIntoWall;
				coordXZToCheck.y = transform.position.y + heightRayLengthToUse;

				_raycasts = RaycastTrulyAll(coordXZToCheck, Vector3.down, _obstacleLayer, 0.01f);
				if (_raycasts.Count > 0)
				{
					foreach (var hit in _raycasts)
					{
						var heightSpace = ValidateHit(hit.point);
						if (heightSpace > 0f)
						{
							_validHits.Add(new ValidHit(hit.point, Quaternion.LookRotation(-forwardHit.normal), heightSpace));
						}
					}
				}
			}
			return _validHits;
		}

		private void FindOutUniquePointsOnXZPlane()
		{
			_forwardHitPositions.Clear();

			var maxHeight = _player.IsGrounded ? _heightRayLength : _heightRayLengthUngrounded;
			foreach (var origin in _forwardRayOrigins)
			{
				var offsetOrigin = origin + transform.forward * _forwardRaysForwardOffset;

				if (origin.y <= maxHeight && 
                    Physics.Raycast(transform.position + offsetOrigin, transform.forward, out var forwardHit, _actualForwardRayLength, _obstacleLayer))
				{
					// TODO: Ensure the refactor in AnyValidForwardPositions is correct. This was a bit complex, and Any LINQ in hot path is a big no no.
					//if (_forwardHitPositions.Count == 0 || 
                    //    !_forwardHitPositions.Any(v => Mathf.Abs(v.point.x - forwardHit.point.x) < _minDistanceForUniqueXZPoint && 
                    //                                            Mathf.Abs(v.point.z - forwardHit.point.z) < _minDistanceForUniqueXZPoint))

					if (AnyValidForwardPositions(in forwardHit))
					{
						_forwardHitPositions.Add(forwardHit);
					}
				}
			}
		}

        private bool AnyValidForwardPositions(in RaycastHit forwardHit)
        {
            if (_forwardHitPositions.Count == 0)
            {
                return true;
            }

            foreach (var v in _forwardHitPositions)
            {
                if (Mathf.Abs(v.point.x - forwardHit.point.x) < _minDistanceForUniqueXZPoint ||
                    Mathf.Abs(v.point.z - forwardHit.point.z) < _minDistanceForUniqueXZPoint)
                {
                    return false;
                }
            }

            return true;
        }


        // ========================================================= VALIDATORS

        private float ValidateHit(Vector3 posToValidate)
		{
			var origin = posToValidate + new Vector3(0, _player.Radius + 0.01f, 0);
			var crouchingDistance = _player.CrouchFloatingColliderHeight + _player.DistanceToFloat - _player.Radius * 2;

			if (Physics.CheckSphere(origin, _player.Radius, _obstacleLayer) || 
                RaycastAccessibilityCheck(posToValidate) == false)
			{
				return -1;
			}

			if (Physics.SphereCast(origin, _player.Radius, Vector3.up, out RaycastHit hit, crouchingDistance, _obstacleLayer) == false)
			{
				var standingDistance = _player.CharacterHeight - _player.Radius * 2;
				if (Physics.SphereCast(origin, _player.Radius, Vector3.up, out RaycastHit standingHit, standingDistance, _obstacleLayer))
				{
					if (standingHit.point.y - posToValidate.y >= _playerCrouching.CrouchColliderHeight)
					{
						return standingHit.point.y - posToValidate.y;
					}
				}
				else
				{
					return _player.CharacterHeight;
				}
			}
			return -1;
		}

		bool RaycastAccessibilityCheck(Vector3 posToCheck)
		{
			var origin = transform.position + Vector3.up * (_player.CurrentFloatingColliderHeight + _player.DistanceToFloat);
			var target = posToCheck + Vector3.up * _playerCrouching.CrouchColliderHeight;

			return Physics.Raycast(origin, target - origin, Vector3.Distance(target, origin), _obstacleLayer) == false;
		}


        // ========================================================= RAYCASTS

        private List<RaycastHit> RaycastTrulyAll(Vector3 initialXZCoordToCheck, Vector3 direction, LayerMask layerMask, float offsetAfterHit)
        {
            if (_raycasts == null)
            {
                _raycasts = new List<RaycastHit>();
            }
            else
            {
                _raycasts.Clear();
            }

            var thisRayOrigin = initialXZCoordToCheck;

			// If something is hit within the max length
			var maxLength = _player.IsGrounded ? _actualRayLengthFromGround : _actualRayLengthFromGroundUngrounded;

			// TODO: Ensure this can't go infinite loop. I'd prefer a maxIteration failsafe mechanic here just to make sure it can't.
			while (Physics.Raycast(thisRayOrigin, direction, out var hit, maxLength, layerMask) && (initialXZCoordToCheck - hit.point).magnitude < maxLength)
			{
                _raycasts.Add(hit);
				thisRayOrigin = hit.point + direction * offsetAfterHit;
			}

			return _raycasts;
		}


        // ========================================================= DEBUG

        void OnDrawGizmos()
        {
            if (_raycasts != null)
            {
                foreach (var hit in _raycasts)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(hit.point, 0.25f);
                }
            }

            if (_forwardHitPositions != null)
            {
                foreach (var hit in _forwardHitPositions)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(hit.point, 0.5f);
                }
            }
        }
    }

	public struct ValidHit
	{
		public Vector3 Destination;
		public Quaternion Rotation;
		public float HeightSpace;

		public ValidHit(Vector3 dest, Quaternion rot, float space)
		{
			Destination = dest;
			Rotation = rot;
			HeightSpace = space;
		}
	}
}