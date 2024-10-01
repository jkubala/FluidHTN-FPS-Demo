using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Player
{
	public class PlayerClimbing : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [Header("Climbing")]
		[Tooltip("Speed that player moves vertically when climbing.")]
		[SerializeField] private float _climbingSpeed = 80.0f;
		[SerializeField] private float _angleToClimb = 60f;
		[SerializeField] private float _grabbingTime = 0.4f;
		[SerializeField] private float _climbingTime = 2.0f;
        [SerializeField] private float _clampClimbingCameraAngle = 60f;
		[SerializeField] private float _shouldClimbPressedDuration = 0.5f;
		[SerializeField] private float _obstacleClimbSpeed = 3f;
		[SerializeField] private float _obstacleMoveThreshold = 0.5f;

		[SerializeField] private Player _player;
		[SerializeField] private CapsuleCollider _capsuleCollider;
		[SerializeField] private PlayerCrouching _playerCrouching;
		[SerializeField] private CameraMovement _cameraMovement;
		[SerializeField] private EnvironmentScanner _environmentScanner;


        // ========================================================= PRIVATE FIELDS

        private bool _shouldClimb;
        private float _lastTimeShouldClimbPressed;
        private ClimbingState _climbingState;


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

            if (_cameraMovement == null)
            {
                _cameraMovement = GetComponentInChildren<CameraMovement>();
            }

            if (_environmentScanner == null)
            {
                _environmentScanner = GetComponent<EnvironmentScanner>();
            }

            if (_capsuleCollider == null)
            {
                _capsuleCollider = GetComponent<CapsuleCollider>();
            }
        }

        private void Awake()
		{
			InitVariables();
		}

        private void OnEnable()
        {
            _player.OnBeforeMove += OnBeforeMove;
            _player.OnPlayerUpdate += OnPlayerUpdate;
        }
        private void OnDisable()
        {
            _player.OnBeforeMove -= OnBeforeMove;
            _player.OnPlayerUpdate -= OnPlayerUpdate;
        }


        // ========================================================= INIT

        private void InitVariables()
		{
			_shouldClimb = false;
			_lastTimeShouldClimbPressed = Mathf.NegativeInfinity;
			_climbingState = ClimbingState.None;
			_player.IsClimbing = false;
		}


        // ========================================================= PUBLIC METHODS

        public void ClimbObstacle(Vector3 destination, Quaternion rotation)
        {
            if (_player.IsClimbing)
            {
                return;
            }

            _climbingState = ClimbingState.ClimbingLedge;
            _capsuleCollider.enabled = false;

            _player.IsClimbing = true;
            _player.IsJumping = false;
            _player.StartClimbingUpwardsObstacle(destination, _obstacleClimbSpeed, _obstacleMoveThreshold);
            _cameraMovement.StartRotatingCameraTowards(rotation, 0f);
        }


        // ========================================================= CALLBACKS

        private void OnPlayerUpdate()
		{
			if (_player.InputManager.InteractInputAction.WasPressedThisFrame() && !_player.IsClimbing)
			{
				_shouldClimb = true;
				_lastTimeShouldClimbPressed = Time.time;
			}

			if (Time.time - _lastTimeShouldClimbPressed > _shouldClimbPressedDuration)
			{
				_shouldClimb = false;
			}

			if (_shouldClimb && !_player.IsClimbing)
			{
				// Pick the best valid hit
				var chosenHit = GetBestHit(_cameraMovement.CameraBase.transform.rotation);
				if (chosenHit != null)
				{
					_playerCrouching.SetCrouchLevelToMatchHeight(chosenHit.Value.HeightSpace);
					_shouldClimb = false;
					ClimbObstacle(chosenHit.Value.Destination, chosenHit.Value.Rotation);
				}
			}
		}

        private void OnBeforeMove()
        {
            if (_climbingState != ClimbingState.None)
            {
                Climbing();
            }
        }


        // ========================================================= PRIVATE METHODS

        private ValidHit? GetBestHit(Quaternion cameraLookDirection)
        {
            var validHits = _environmentScanner.ObstacleCheck();
            if (validHits.Count == 0)
            {
                return null;
            }

            var bestHit = validHits[0]; // Assume the first hit is the best initially
            var minAngle = Vector3.Angle((bestHit.Destination - _cameraMovement.transform.position).normalized, cameraLookDirection * Vector3.forward);
            
            foreach (var validHit in validHits)
            {
                // Compare the angle between the direction to each valid hit's destination and the camera look direction
                var angle = Vector3.Angle((validHit.Destination - _cameraMovement.transform.position).normalized, cameraLookDirection * Vector3.forward);

                // Update the best hit if the current hit has a smaller angle
                if (angle < minAngle)
                {
                    minAngle = angle;
                    bestHit = validHit;
                }
            }

            return bestHit;
        }

        private void Unclimb()
		{
			_climbingState = ClimbingState.None;
			_player.IsClimbing = false;
			_capsuleCollider.enabled = true;
			_cameraMovement.ResetXAngle();
			_cameraMovement.CanRotateCamera = true;
		}

		private void Climbing()
		{
			switch (_climbingState)
			{
                case ClimbingState.Releasing:
                {
                    if (_player.MoveTowardsFinished)
                    {
                        Unclimb();
                    }

                    break;
                }
                case ClimbingState.Grabbing:
                {
                    if (_player.MoveTowardsFinished && _cameraMovement.RotateTowardsFinished)
                    {
                        _climbingState = ClimbingState.Grabbed;
                        _cameraMovement.ClampXAngle(_clampClimbingCameraAngle);
                        _player.IsClimbing = true;
                        _cameraMovement.CanRotateCamera = true;
                    }

                    break;
                }
                case ClimbingState.ClimbingLedge:
                {
                    if (_player.MoveTowardsFinished && _cameraMovement.RotateTowardsFinished)
                    {
                        _climbingState = ClimbingState.None;
                        _capsuleCollider.enabled = true;
                        _player.IsClimbing = false;
                        _cameraMovement.CanRotateCamera = true;
                    }

                    break;
                }   
            }
		}

        // ========================================================= PRIVATE ENUMS

        private enum ClimbingState
        {
            None,
            Grabbing,
            ClimbingLedge,
            Grabbed,
            Releasing
        }
    }
}
