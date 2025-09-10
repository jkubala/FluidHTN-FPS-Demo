using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

namespace FPSDemo.NPC
{
	public class ThirdPersonController : MonoBehaviour
	{
		// ========================================================= INSPECTOR FIELDS

		[SerializeField] Transform _playerTransform;
		[SerializeField] Transform _pointToAimAt;
		[SerializeField] Transform _rootModel;
		[SerializeField] Animator _animator;
		[SerializeField] NavMeshAgent _navAgent;
		[SerializeField] Rig _ikRig;
		[SerializeField] Transform _ikRigAim;
		[SerializeField] float _ikRigWeightChangeSpeed = 10f;
		[SerializeField] float angleToTargetToEngageIK = 50f;
		float _targetIKRigWeight = 1;

		[Tooltip("How fast the character un/crouches in s")]
		[SerializeField] private float _speedToCrouch = 1f;
		[SerializeField] private float _speedToStop = 0.2f;

		[Header("Speed")]
		[Tooltip("Crouched speed of the character in m/s")]
		[SerializeField] private float _crouchedSpeed = 1.5f;

		[Tooltip("Walk speed of the character in m/s")]
		[SerializeField] private float _walkSpeed = 1f;

		[Tooltip("Run speed of the character in m/s")]
		[SerializeField] private float _runSpeed = 5;

		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		[SerializeField] private float _rotationSmoothTime = 0.12f;

		[Tooltip("Acceleration and deceleration")]
		[SerializeField] private float _speedChangeRate = 10.0f;

		[SerializeField] private AudioClip[] _footstepAudioClips;

		[Range(0, 1)]
		[SerializeField] private float _footstepAudioVolume = 0.5f;

		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		[SerializeField] private float _gravity = -15.0f;

		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		[SerializeField] private float _fallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		[SerializeField] private bool _grounded = true;

		[Tooltip("Useful for rough ground")]
		[SerializeField] private float _groundedOffset = -0.14f;

		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		[SerializeField] private float _groundedRadius = 0.28f;

		[Tooltip("What layers the character uses as ground")]
		[SerializeField] private LayerMask _groundLayers;


		// ========================================================= PRIVATE FIELDS

		// state
		private bool _isCrouched;
		private bool _isShooting;
		private bool _isReloading;

		// player
		private float _targetRotation = 0.0f;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _fallTimeoutDelta;

		// animation IDs
		private int _animX;
		private int _animCrouched;
		private int _animY;
		private int _animIsShooting;
		private int _animReload;
        private int _animDeath;

        // crouching
        private IEnumerator _crouchCoroutine;
		private float _crouchAmount;

		// target
		private float _targetSpeed;
		private float _targetSpeedCached;


        // ========================================================= PUBLIC PROPERTIES

        public float DistanceToDestination => _navAgent?.remainingDistance ?? 0.0f;
        public float StoppingDistance => _navAgent?.stoppingDistance ?? 0.0f;
		public bool IsStopped => _navAgent?.isStopped ?? true;
        public bool IsReloading => _isReloading;
        public bool IsShooting => _isShooting;
        public Vector3 Destination => _navAgent?.destination ?? Vector3.zero;
        public float Speed 
        { 
            get => _navAgent?.speed ?? 0.0f;
            set 
            { 
                if (_navAgent != null) 
                    _navAgent.speed = value; 
            } 
        }


        // ========================================================= UNITY METHODS

        private void OnValidate()
		{
			_navAgent = GetComponent<NavMeshAgent>();
		}

		private void Start()
		{
			AssignAnimationIDs();
			SetDestination(null);

			// reset our timeouts on start
			_fallTimeoutDelta = _fallTimeout;
			_targetSpeed = _walkSpeed;
			_targetSpeedCached = _walkSpeed;
		}

		private void Update()
		{
			TickGravity();
			GroundedCheck();
			TickMove();
			TickRotateAgent();
			HandleIK();
			TickAnimator();
			_ikRig.weight = Mathf.Lerp(_ikRig.weight, _targetIKRigWeight, _ikRigWeightChangeSpeed * Time.deltaTime);
		}


		// ========================================================= INIT

		private void AssignAnimationIDs()
		{
			_animX = Animator.StringToHash("x");
			_animY = Animator.StringToHash("y");
			_animCrouched = Animator.StringToHash("Crouched");
			_animIsShooting = Animator.StringToHash("IsShooting");
			_animReload = Animator.StringToHash("Reload");
            _animDeath = Animator.StringToHash("Death");

        }


		// ========================================================= RESET / CLEAR

		public void ClearAimAtPoint()
		{
			_pointToAimAt = null;
		}


		// ========================================================= APPLY SETTINGS

		public void ApplyPlayerAsAimAtPoint()
		{
			_pointToAimAt = _playerTransform;
		}

		public void ApplyWalkSpeed()
		{
			Uncrouch();
			_targetSpeed = _walkSpeed;
			_targetSpeedCached = _walkSpeed;
		}

		public void ApplyRunSpeed()
		{
			Uncrouch();
			_targetSpeed = _runSpeed;
			_targetSpeedCached = _runSpeed;
		}

		// ========================================================= TICK

		private void TickGravity()
		{
			if (_grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = _fallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}
			}
			else
			{
				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += _gravity * Time.deltaTime;
			}
		}

		private void TickMove()
		{
			if (!_navAgent.hasPath || _navAgent.remainingDistance < _navAgent.stoppingDistance)
			{
				return;
			}

			_targetSpeed = _targetSpeedCached;
			Vector3 navAgentVelocity = _navAgent.velocity;
			navAgentVelocity.y = 0f;
			Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
			_navAgent.speed = _targetSpeed;
		}

		private void TickRotateAgent()
		{
			Vector3 inputDirection;
			if (_pointToAimAt != null)
			{
				inputDirection = (_pointToAimAt.position - transform.position).normalized;
			}
			else if (_navAgent.hasPath)
			{
				inputDirection = (_navAgent.steeringTarget - transform.position).normalized;

			}
			else
			{
				return;
			}

			_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
			float rotation = Mathf.SmoothDampAngle(_rootModel.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
			_rootModel.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		private void HandleIK()
		{
			if (_pointToAimAt == null)
			{
				_targetIKRigWeight = 0f;
			}
			else if(Mathf.Abs(Vector3.SignedAngle(transform.position, _pointToAimAt.position, Vector3.up)) < angleToTargetToEngageIK)
			{
				_ikRigAim.transform.position = _pointToAimAt.position;
				_targetIKRigWeight = 1f;
			}

		}

		private void TickAnimator()
		{
			Vector2 convertedAnimParams = CalculateAnimatorMoveParams(_rootModel.InverseTransformDirection(_navAgent.velocity));
			_animator.SetFloat(_animX, convertedAnimParams.x);
			_animator.SetFloat(_animY, convertedAnimParams.y);
			_animator.SetFloat(_animCrouched, _crouchAmount);

			if (_isReloading)
			{
				var state = _animator.GetCurrentAnimatorStateInfo(1);
				if (state.fullPathHash != _animReload)
				{
					_isReloading = false;
				}
			}
		}


		// ========================================================= SETTERS

		public void SetDestination(Vector3? destination)
		{
			if (destination.HasValue)
			{
				_navAgent.isStopped = false;
				_navAgent.destination = destination.Value;
			}
			else
			{
				_navAgent.isStopped = true;
			}
		}


		// ========================================================= START BEHAVIORS

		public void StartShooting()
		{
			_isShooting = true;
			_animator.SetBool(_animIsShooting, _isShooting);
		}

		public void Crouch()
		{
			_isCrouched = true;
			if (_crouchCoroutine != null)
			{
				StopCoroutine(_crouchCoroutine);
			}
			_crouchCoroutine = SimulateCrouching();
			StartCoroutine(_crouchCoroutine);
		}

		public void StartIK()
		{
			_targetIKRigWeight = 1f;
		}

		public void StopIK()
		{
			_targetIKRigWeight = 0f;
		}


		// ========================================================= STOP BEHAVIORS

		public void StopShooting()
		{
			_isShooting = false;
			_animator.SetBool(_animIsShooting, _isShooting);
		}

		public void Uncrouch()
		{
			_isCrouched = false;
			if (_crouchCoroutine != null)
			{
				StopCoroutine(_crouchCoroutine);
			}
			_crouchCoroutine = SimulateUncrouching();
			StartCoroutine(_crouchCoroutine);
		}


		// ========================================================= TRIGGER BEHAVIORS

		public void Reload()
		{
			_isReloading = true;

			if (_isShooting)
			{
				StopShooting();
			}

			_animator.SetTrigger(_animReload);
		}

		public void NavigateToPlayer()
		{
			SetDestination(_playerTransform.position);
		}

        // ========================================================= DEATH BEHAVIORS

        public void Death()
        {
            _isReloading = true;

            if (_isShooting)
            {
                StopShooting();
            }

            _animator.SetTrigger(_animDeath);
        }


        // ========================================================= COROUTINES

        // TODO: We want to move these into tick and stop using coroutines (they're super expensive performance wise)
        // TODO: E.g. TickCrouchTransition()
        private IEnumerator SimulateCrouching()
		{
			while (_crouchAmount < 1f)
			{
				_crouchAmount = Mathf.Clamp01(_crouchAmount + (Time.deltaTime / _speedToCrouch));
				yield return null;
			}
		}

		private IEnumerator SimulateUncrouching()
		{
			while (_crouchAmount > 0f)
			{
				_crouchAmount = Mathf.Clamp01(_crouchAmount - (Time.deltaTime / _speedToCrouch));
				yield return null;
			}
		}


		// ========================================================= CHECKS / VALIDATORS

		void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset,
				transform.position.z);
			_grounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers,
				QueryTriggerInteraction.Ignore);
		}


		// ========================================================= CALCULATIONS

		Vector2 CalculateAnimatorMoveParams(Vector3 directionOfModelRootMovement)
		{
			Vector3 flattenedNavAgentVelocity = directionOfModelRootMovement;
			flattenedNavAgentVelocity.y = 0;
			float currentSpeed = flattenedNavAgentVelocity.magnitude;
			float scale;

			if (_isCrouched)
			{
				scale = Mathf.Lerp(0f, 0.5f, currentSpeed / _crouchedSpeed);
			}
			else
			{
				if (currentSpeed <= _walkSpeed)
				{
					scale = Mathf.Lerp(0f, 0.5f, currentSpeed / _walkSpeed);
				}
				else
				{
					scale = Mathf.Lerp(0.5f, 1f, (currentSpeed - _walkSpeed) / (_runSpeed - _walkSpeed));
				}
			}

			// Assuming velocity is the movement direction, normalize it to get direction (x, y)
			Vector2 direction = new Vector2(flattenedNavAgentVelocity.x, flattenedNavAgentVelocity.z).normalized;
			return direction * scale;
		}

		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
			{
				if (_footstepAudioClips.Length > 0)
				{
					var index = Random.Range(0, _footstepAudioClips.Length);
					AudioSource.PlayClipAtPoint(_footstepAudioClips[index], transform.position, _footstepAudioVolume);
				}
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.black;
			Gizmos.DrawWireSphere(_ikRigAim.position, 0.1f);
			Gizmos.color = Color.white;
		}
	}
}