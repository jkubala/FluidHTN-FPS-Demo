using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FPSDemo.NPC
{
	public class ThirdPersonController : MonoBehaviour
	{
		[SerializeField] Transform playerTransform;
		[SerializeField] Transform pointToRotateTo;
		[SerializeField] Transform rootModel;

		bool isCrouched;
		[Tooltip("How fast the character un/crouches in s")]
		[SerializeField] float speedToCrouch = 1f;
		[SerializeField] float speedToStop = 0.2f;
		IEnumerator crouchCoroutine;
		float crouchAmount;
		[Header("Speed")]
		[Tooltip("Crouched speed of the character in m/s")]
		public float CrouchedSpeed = 1.5f;
		[Tooltip("Walk speed of the character in m/s")]
		public float WalkSpeed = 1f;

		[Tooltip("Run speed of the character in m/s")]
		public float RunSpeed = 5;

		float targetSpeed;
		float targetSpeedCached;

		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;

		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		public AudioClip[] FootstepAudioClips;
		[Range(0, 1)] public float FootstepAudioVolume = 0.5f;

		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;

		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;

		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.28f;

		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

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

        [SerializeField] Animator _animator;
		NavMeshAgent navAgent;

		private const float _threshold = 0.01f;


		private void Awake()
		{
			navAgent = GetComponent<NavMeshAgent>();
		}

		private void Start()
		{
			AssignAnimationIDs();
			SetDestination(null);
			// reset our timeouts on start
			_fallTimeoutDelta = FallTimeout;
			targetSpeed = WalkSpeed;
			targetSpeedCached = WalkSpeed;
		}

		public void SetDestination(Vector3? destination)
		{
			if (destination.HasValue)
			{
				navAgent.isStopped = false;
				navAgent.destination = destination.Value;
			}
			else
			{
				navAgent.isStopped = true;
			}
		}

		public void NavigateToPlayer()
		{
			SetDestination(playerTransform.position);
		}

		public void SetPlayerAsRotatePoint()
		{
			pointToRotateTo = playerTransform;
		}

		public void ClearRotateToPoint()
		{
			pointToRotateTo = null;
		}

		public void StartShooting()
        {
            _isShooting = true;
			_animator.SetBool(_animIsShooting, _isShooting);
		}

		public void StopShooting()
		{
			_isShooting = false;
			_animator.SetBool(_animIsShooting, _isShooting);
		}

		public void SetWalkSpeed()
		{
			Uncrouch();
			targetSpeed = WalkSpeed;
			targetSpeedCached = WalkSpeed;
		}

		public void SetRunSpeed()
		{
			Uncrouch();
			targetSpeed = RunSpeed;
			targetSpeedCached = RunSpeed;
		}

		public void Crouch()
		{
			isCrouched = true;
			if (crouchCoroutine != null)
			{
				StopCoroutine(crouchCoroutine);
			}
			crouchCoroutine = StartCrouching();
			StartCoroutine(crouchCoroutine);
		}

		public void Uncrouch()
		{
			isCrouched = false;
			if (crouchCoroutine != null)
			{
				StopCoroutine(crouchCoroutine);
			}
			crouchCoroutine = StartUncrouching();
			StartCoroutine(crouchCoroutine);
		}

        public void Reload()
        {
            _isReloading = true;

            if (_isShooting)
            {
				StopShooting();
            }

			_animator.SetTrigger(_animReload);
        }

		void Update()
		{
			HandleGravity();
			GroundedCheck();
			Move();
			RotateAgent();
			UpdateAnimator();
		}

		void AssignAnimationIDs()
		{
			_animX = Animator.StringToHash("x");
			_animY = Animator.StringToHash("y");
			_animCrouched = Animator.StringToHash("Crouched");
            _animIsShooting = Animator.StringToHash("IsShooting");
            _animReload = Animator.StringToHash("Reload");
        }

		void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
				transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
				QueryTriggerInteraction.Ignore);
		}

		IEnumerator StartCrouching()
		{
			while (crouchAmount < 1f)
			{
				crouchAmount = Mathf.Clamp01(crouchAmount + (Time.deltaTime / speedToCrouch));
				yield return null;
			}
		}

		IEnumerator StartUncrouching()
		{
			while (crouchAmount > 0f)
			{
				crouchAmount = Mathf.Clamp01(crouchAmount - (Time.deltaTime / speedToCrouch));
				yield return null;
			}
		}

		private void Move()
		{
			if (!navAgent.hasPath || navAgent.remainingDistance < navAgent.stoppingDistance)
			{
				return;
			}

			targetSpeed = targetSpeedCached;
			Vector3 navAgentVelocity = navAgent.velocity;
			navAgentVelocity.y = 0f;
			Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
			navAgent.speed = targetSpeed;
		}

		void UpdateAnimator()
		{
			Vector2 convertedAnimParams = CalculateAnimatorMoveParams(rootModel.InverseTransformDirection(navAgent.velocity));
			_animator.SetFloat(_animX, convertedAnimParams.x);
			_animator.SetFloat(_animY, convertedAnimParams.y);
			_animator.SetFloat(_animCrouched, crouchAmount);

            if (_isReloading)
            {
                var state = _animator.GetCurrentAnimatorStateInfo(1);
                if (state.fullPathHash != _animReload)
                {
                    _isReloading = false;
                }
            }
		}

		void RotateAgent()
		{
			Vector3 inputDirection;
			if (pointToRotateTo != null)
			{
				inputDirection = (pointToRotateTo.position - transform.position).normalized;
			}
			else if (navAgent.hasPath)
			{
				inputDirection = (navAgent.steeringTarget - transform.position).normalized;
			}
			else
			{
				return;
			}

			_targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
			float rotation = Mathf.SmoothDampAngle(rootModel.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
			rootModel.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
		}

		Vector2 CalculateAnimatorMoveParams(Vector3 directionOfModelRootMovement)
		{
			Vector3 flattenedNavAgentVelocity = directionOfModelRootMovement;
			flattenedNavAgentVelocity.y = 0;
			float currentSpeed = flattenedNavAgentVelocity.magnitude;
			float scale;

			if (isCrouched)
			{
				scale = Mathf.Lerp(0f, 0.5f, currentSpeed / CrouchedSpeed);
			}
			else
			{
				if (currentSpeed <= WalkSpeed)

				{
					scale = Mathf.Lerp(0f, 0.5f, currentSpeed / WalkSpeed);
				}
				else
				{
					scale = Mathf.Lerp(0.5f, 1f, (currentSpeed - WalkSpeed) / (RunSpeed - WalkSpeed));
				}
			}

			// Assuming velocity is the movement direction, normalize it to get direction (x, y)
			Vector2 direction = new Vector2(flattenedNavAgentVelocity.x, flattenedNavAgentVelocity.z).normalized;
			return direction * scale;
		}

		private void HandleGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

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
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private void OnFootstep(AnimationEvent animationEvent)
		{
			if (animationEvent.animatorClipInfo.weight > 0.5f)
			{
				if (FootstepAudioClips.Length > 0)
				{
					var index = Random.Range(0, FootstepAudioClips.Length);
					AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
				}
			}
		}
	}
}