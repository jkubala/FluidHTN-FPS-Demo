using UnityEngine;
using UnityEngine.AI;
using System;
using System.Linq;
using System.Collections;
using FPSDemo.Input;
using FPSDemo.Target;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace FPSDemo.Player
{
	public class Player : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private InputManager _inputManager;

        [Header("Collider")]
        [SerializeField] private float _characterHeight = 1.8f;
        [SerializeField] private float _skinWidth = 0.01f;
        [SerializeField] private float _headBumpSpeedReducer = 2f;

        [Header("Slopes")]
        [SerializeField] private float _maxSlopeAngle = 35f;
        [SerializeField] private AnimationCurve _slopeCurve;

        [Header("Stairs")]
        [SerializeField] private float _maxStairHeight = 0.25f;

        [Header("Floating")]
        [SerializeField][Range(0.0f, 1.0f)] private float _distanceToFloat = 0.1f;
        [SerializeField][Range(500.0f, 2000.0f)] private float _springStrength = 1000.0f;
        [SerializeField][Range(0.0f, 1000.0f)] private float _springDamp = 30.0f;
        [SerializeField][Range(0.1f, 0.9f)] private float _gravityForceToNeutralSpeed = 0.12f;

        [Header("Grounding")]
        [Tooltip("How much is distanceToCast moved up - to prevent accidental miss of a collider that is too close")]
        [SerializeField][Range(0.0f, 0.1f)] private float _castOriginOffsetDistance = 0.05f;

        [Tooltip("How frequently is lastGroundedPosition updated")]
        [SerializeField] private float _groundedPosUpdateInterval = 1f;

        [SerializeField] private int _stairCastMultiplier = 3;

        [Header("Speed")]
        [SerializeField] private float _accelerationGround = 8f;
        [Range(0, 1)]
        [SerializeField] private float _backwardSpeedPercentage = 0.4f;
        [Range(0, 1)]
        [SerializeField] private float _strafeSpeedPercentage = 0.8f;
        [SerializeField] private float _moveToSpeed = 2f;

        [SerializeField] private float _terminalVelocity = 30f;
        [SerializeField] private float _gravityMultiplierOnFall = 4f;
        [SerializeField] private float _defaultGravityMultiplier = 2f;

        [SerializeField] private int _maxVelocityChange = 5;

        [SerializeField] private float _desiredTargetSpeed;
        [SerializeField] private float _gravityForce;

        [Header("Misc")]
        [SerializeField] float _returnToGroundYPos = -100f;
        [SerializeField] float _moveToThreshold = 0.1f;

        [Header("Dependencies")]
        [SerializeField] private HumanTarget _thisTarget;
		[SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _capsuleCollider;


        // ========================================================= PRIVATE FIELDS

        private float _standingFloatingColliderHeight;
        private float _crouchFloatingColliderHeight = 0f;

		private Vector3 _averageSlopeNormal;

        private Vector3 _lastOnGroundPosition = Vector3.zero;

        private float _lastOnGroundPositionTime;
        private float _lastCastGridRotation;

        private float _gravityMultiplier = 1f;

        private float _targetSpeed;

        private float _crouchSpeedMultiplier = 1f;
        private float _aimingMultiplier = 1f;

        private float _speedModX = 1f;
        private float _speedModY = 1f;

        private Vector3 _moveDirection = Vector3.zero;

        private bool _freeze;

        private float _inputX = 0f;
        private float _inputY = 0f;

		private Coroutine _moveTowardsCoroutine;

        private float _lastTimeMoveTowardsInited;

        private Vector3 _levelStartPosition;
        private Quaternion _levelStartRotation;

        private readonly GroundCheckCastPoint[] _groundCasts = new GroundCheckCastPoint[12];


        // ========================================================= PROPERTIES

        public HumanTarget ThisTarget => _thisTarget;
		public float AverageYPos { get; private set; }
		public float CrouchPercentage => 1f - (CurrentFloatingColliderHeight - _crouchFloatingColliderHeight) / (_standingFloatingColliderHeight - _crouchFloatingColliderHeight);

        public CapsuleCollider Capsule => _capsuleCollider;

        public LayerMask GroundMask => 1;
        public float DistanceToFloat => _distanceToFloat;

        public float Radius => Capsule.radius;

        public float CurrentFloatingColliderHeight
		{
            get
            {
                return Capsule.height;
            }
            set
			{
				Capsule.height = value;
				Capsule.center = new Vector3(Capsule.center.x, (value / 2) + _distanceToFloat, Capsule.center.z);
				ThisTarget.IsCrouching = IsCrouching;
			}
		}

        public float WalkSpeed => 3f;

        // States
        public bool IsSprinting => _desiredTargetSpeed > WalkSpeed;
		public bool IsClimbing { get; set; }
		public bool IsJumping { get; set; }
		public bool IsAiming { get; set; }
		public bool IsGrounded { get; private set; }
		public bool IsSlidingDownSlope { get; private set; }
		public bool IsCrouching => _standingFloatingColliderHeight - CurrentFloatingColliderHeight > 0.1f;
		
		public RaycastHit? ClosestHitToAveragePos { get; private set; }

        public bool MoveTowardsFinished { get; private set; } = true;

        public InputManager InputManager => _inputManager;
        
        public float CharacterHeight => _characterHeight;
        public float StandingFloatingColliderHeight => _standingFloatingColliderHeight;
        public float LastOnGroundPositionTime => _lastOnGroundPositionTime;
        public float GravityForce
        {
            get => _gravityForce;
            set => _gravityForce = value;
        }

        public float CrouchFloatingColliderHeight
        {
			get => _crouchFloatingColliderHeight;
            set => _crouchFloatingColliderHeight = value;
        } 

        public float CrouchSpeedMultiplier
        {
            get => _crouchSpeedMultiplier;
            set => _crouchSpeedMultiplier = value;
        }

        public float AimingMultiplier
        {
            get => _aimingMultiplier;
			set => _aimingMultiplier = value;
        }

        public float DesiredTargetSpeed
        {
            get => _desiredTargetSpeed;
			set => _desiredTargetSpeed = value;
        }

        // Events
        public Action OnBeforeMove { get; set; }
		public Action OnAfterMove { get; set; }
        public Action OnPlayerUpdate { get; set; }
        public Action<float> OnLanding { get; set; }
        public Action<float> OnTeleport { get; set; }
        public Action<Quaternion> OnTeleportRotate { get; set; }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_thisTarget == null)
            {
                _thisTarget = GetComponent<HumanTarget>();
            }

            if (_capsuleCollider == null)
            {
                _capsuleCollider = GetComponentInChildren<CapsuleCollider>();
            }
        }

        void Awake()
		{
			_levelStartPosition = transform.position;
			_levelStartRotation = transform.rotation;
			
			ThisTarget.SetAsPlayer();
			
			_standingFloatingColliderHeight = _characterHeight - _distanceToFloat;
			CurrentFloatingColliderHeight = _standingFloatingColliderHeight;
			_targetSpeed = 0f;
			_lastCastGridRotation = transform.rotation.eulerAngles.y;
		}

		void Start()
		{
			InitGroundCastPositions(Capsule.radius - _skinWidth);

		}

		void Update()
		{
			// When player clips out of the map, return him to the last known grounded position
			if (transform.position.y < _returnToGroundYPos)
			{
				ReturnToLastGroundPosition();
			}

            if (_freeze)
            {
                return;
            }

			OnPlayerUpdate?.Invoke();
		}

		void LateUpdate()
		{
            if (_freeze)
            {
                return;
            }

			OnAfterMove?.Invoke();
		}

		void FixedUpdate()
		{
            if (_freeze)
            {
                return;
            }

			OnBeforeMove?.Invoke();
			UpdateDirectionFromInput();
			CheckGround(_moveDirection.normalized);

			if (IsJumping && _rigidbody.velocity.y < 0f)
			{
				_gravityMultiplier = _gravityMultiplierOnFall;
			}
			else
			{
				_gravityMultiplier = _defaultGravityMultiplier;
			}

			if (IsClimbing == false && MoveTowardsFinished)
			{
				// Update gravityForce and MoveDirection
				if (IsGrounded)
				{
					if (IsJumping == false)
					{
						_rigidbody.AddForce(PlayerFloatForce(), ForceMode.Acceleration);
					}

					if (Mathf.Abs(_gravityForce) < 0.01f)
					{
						_gravityForce = 0f;
					}
					else
					{
						_gravityForce *= 1 - _gravityForceToNeutralSpeed;
					}
				}
				else
				{
					var castOrigin = PlayerTopSphere();
                    var radius = Radius - _skinWidth;
                    var maxDistance = 1.0f;
                    var layerMask = ~LayerMask.GetMask("Player");

                    _gravityForce = Mathf.Clamp(_gravityForce + Physics.gravity.y * _gravityMultiplier * Time.deltaTime, -_terminalVelocity, _terminalVelocity);
                    
                    if (IsJumping && Physics.SphereCast(castOrigin, radius, Vector3.up, out var hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
					{
						var distanceToHit = Mathf.Clamp(hit.point.y - castOrigin.y - Radius, 0, Mathf.Infinity);
						if (distanceToHit < 0.05f)
						{
							_gravityForce = -Mathf.Abs(_gravityForce) / _headBumpSpeedReducer;
						}
					}
				}

				// Apply speed limits to _moveDirection vector
				float currSpeed = _targetSpeed * _crouchSpeedMultiplier * _speedModX * _speedModY * _aimingMultiplier;
				if (Vector3.Angle(transform.rotation * Vector3.forward, _averageSlopeNormal) > 90f)
				{
					currSpeed *= SpeedOnSlopeModifier();
				}
				_moveDirection *= currSpeed;

				Vector3 velocityChange = _moveDirection + Vector3.up * _gravityForce - _rigidbody.velocity;
				// Finally, add movement velocity to player rigidbody velocity
				_rigidbody.AddForce(Vector3.ClampMagnitude(velocityChange, _maxVelocityChange), ForceMode.VelocityChange);
			}
		}

        void OnEnable()
        {
            TargetRegister.onTargetDeath += DeathBehavior;
        }

        void OnDisable()
        {
            TargetRegister.onTargetDeath -= DeathBehavior;
        }


        // ========================================================= INIT

        void InitGroundCastPositions(float radius)
		{
			float stepSize = radius / 2;
			int i = 0;
			for (int col = 0; col < 4; col++)
			{
				for (int row = 0; row < 4; row++)
				{
					// Skip the corners of the grid
					if ((col != 0 && col != 3) || (row != 0 && row != 3))
					{
						float x = (col * stepSize) - (1.5f * stepSize);
						float z = (row * stepSize) - (1.5f * stepSize);
						GroundCheckCastPoint castPoint = new()
						{
							CastOriginPos = new Vector3(x, _castOriginOffsetDistance + _maxStairHeight + Capsule.radius / 4, z)
						};
						_groundCasts[i++] = castPoint;
					}
				}
			}
		}

		float SpeedOnSlopeModifier()
		{
			Vector3 forwardProjection = Vector3.ProjectOnPlane(transform.forward, _averageSlopeNormal);
			float angle = Vector3.Angle(forwardProjection, transform.forward);
			return Mathf.Clamp(1f - _slopeCurve.Evaluate(angle / _maxSlopeAngle), 0f, 1f);
		}

		void UpdateDirectionFromInput()
		{
			// Take side input only if not sprinting - can only sprint forwards
			Vector2 movementInput = _inputManager.GetMovementInput();
			_inputX = movementInput.x;
			_inputY = movementInput.y;
			if (movementInput != Vector2.zero)
			{
				_lastCastGridRotation = transform.rotation.eulerAngles.y;
			}

			// Create movementVector from input
			_moveDirection = (_inputX * transform.right + _inputY * transform.forward).normalized;

			if (IsGrounded)
			{
				// Limit speed when going backwards
				if (_inputY < 0 && _speedModY >= _backwardSpeedPercentage)
				{
					_speedModY = Mathf.Max(_speedModY - Time.deltaTime, _backwardSpeedPercentage);
				}
				// Increase the _speedModY back up to 1
				else
				{
					_speedModY = Mathf.Min(_speedModY + Time.deltaTime, 1f);
				}

				// Limit speed when going straight to the side
				if (Mathf.Abs(_inputX) > 0.01f && _speedModX >= _strafeSpeedPercentage)
				{
					_speedModX = Mathf.Max(_speedModX - Time.deltaTime, _strafeSpeedPercentage);
				}
				// Increase the _speedModX up to 1
				else
				{
					_speedModX = Mathf.Min(_speedModX + Time.deltaTime, 1f);
				}

			}
		}

		void DeathBehavior(HumanTarget target)
		{
			if (target == ThisTarget)
			{
				_distanceToFloat = 0f;
                _inputManager.SetActiveAllGameplayControls(false);
				Debug.Log("PLAYER HIT!");
			}
		}

		void OnDrawGizmos()
		{
			if (Application.isPlaying)
			{
				for (int i = 0; i < _groundCasts.Length; i++)
				{
					Vector3 rotatedCenter = Quaternion.Euler(0, _lastCastGridRotation, 0) * _groundCasts[i].CastOriginPos;
					Gizmos.DrawSphere(rotatedCenter + PlayerBottomPos(), Capsule.radius / 4);
				}
				Gizmos.DrawRay(transform.position, _moveDirection);
			}
		}

		public bool IsMoving()
		{
			if (_freeze)
			{
				return false;
			}
			else
			{
				return Mathf.Abs(_inputY) > 0.01f || Mathf.Abs(_inputX) > 0.01f;
			}
		}

		public Vector3 PlayerTopPos()
		{
			return transform.position + Vector3.up * (Capsule.height + _distanceToFloat);
		}

		public Vector3 PlayerBottomPosFloating()
		{
			return PlayerBottomPos() + Vector3.up * _distanceToFloat;
		}

		public Vector3 PlayerBottomPos()
		{
			return transform.position;
		}

		public Vector3 PlayerTopSphere()
		{
			return PlayerTopPos() - Vector3.up * Capsule.radius;
		}

		public void Stop()
		{
			_rigidbody.velocity = Vector3.zero;
		}

		public void ReturnToLastGroundPosition()
		{
			Vector3 lastPosOnNavmesh = _lastOnGroundPosition;
			if (NavMesh.SamplePosition(lastPosOnNavmesh, out NavMeshHit hit, 3f, NavMesh.AllAreas))
			{
				lastPosOnNavmesh = hit.position;
			}
			lastPosOnNavmesh += Vector3.up * _distanceToFloat;
			Teleport(lastPosOnNavmesh);
		}

		public void Freeze(bool value)
		{
			if (value)
			{
				_freeze = true;
				Stop();
				_rigidbody.Sleep();
			}
			else
			{
				_freeze = false;
			}
		}

		public void Teleport(Vector3 position, Quaternion? rotation = null)
		{
			Debug.LogWarning("TELEPORTING TO" + position);
			_rigidbody.position = position;
			transform.position = position;
			_gravityForce = 0f;
			if (rotation != null)
			{
				Rotate(((Quaternion)rotation).eulerAngles.y);
			}
			OnTeleport?.Invoke(transform.position.y);
		}

		public void Rotate(float yaw)
		{
			Vector3 tempEulerAngles2 = new(0f, yaw, 0f);
			transform.eulerAngles = tempEulerAngles2;
			OnTeleportRotate?.Invoke(Quaternion.Euler(tempEulerAngles2));
		}

		bool AnalyzeGroundFromCastGrid(float gridElementRadius)
		{
			bool atLeastOneValidHit = false;
			bool hitSomething = false;
			float distanceToCast = _stairCastMultiplier * _maxStairHeight + _castOriginOffsetDistance + Capsule.radius / 4;
			for (int i = 0; i < _groundCasts.Length; i++)
			{
				_groundCasts[i].ValidHit = null;
				Vector3 rotatedCenter = Quaternion.Euler(0, _lastCastGridRotation, 0) * _groundCasts[i].CastOriginPos + PlayerBottomPosFloating();
				if (Physics.SphereCast(rotatedCenter, gridElementRadius, Vector3.down, out RaycastHit hit, distanceToCast, GroundMask.value, QueryTriggerInteraction.Ignore))
				{
					hitSomething = true;
					if (Vector3.Angle(hit.normal, Vector3.up) < _maxSlopeAngle)
					{
						_groundCasts[i].ValidHit = hit;
						atLeastOneValidHit = true;
					}
				}
			}

			if (hitSomething && !atLeastOneValidHit)
			{
				IsSlidingDownSlope = true;
			}
			else
			{
				IsSlidingDownSlope = false;
			}
			return atLeastOneValidHit;
		}


		public void CheckGround(Vector3 direction)
		{
			if (AnalyzeGroundFromCastGrid(Capsule.radius / 4))
			{
				// Avg Y pos of all casts that hit something
				AverageYPos = _groundCasts.Where(element => element.ValidHit.HasValue).Average(element => element.ValidHit.Value.point.y);
				_averageSlopeNormal = _groundCasts.Where(element => element.ValidHit.HasValue)
					.Aggregate(Vector3.zero, (sum, element) => sum + element.ValidHit.Value.normal) / _groundCasts.Count(element => element.ValidHit.HasValue);

				ClosestHitToAveragePos = null;
				foreach (GroundCheckCastPoint point in _groundCasts)
				{
					float minDistance = Mathf.Infinity;
					if (point.ValidHit.HasValue)
					{
						float distance = Mathf.Abs(point.ValidHit.Value.point.y - AverageYPos);
						if (distance < minDistance)
						{
							minDistance = distance;
							ClosestHitToAveragePos = point.ValidHit.Value;
						}
					}
				}

				if (!IsGrounded)
				{
					IsGrounded = true;
					OnLanding?.Invoke(Mathf.Abs(_rigidbody.velocity.y));
				}
				if (_lastOnGroundPositionTime < Time.time)
				{
					_lastOnGroundPosition = _groundCasts.Where(element => element.ValidHit.HasValue)
			.Aggregate(Vector3.zero, (sum, element) => sum + element.ValidHit.Value.point) / _groundCasts.Count(element => element.ValidHit.HasValue);
					_lastOnGroundPositionTime = Time.time + _groundedPosUpdateInterval;
				}

				if (direction.magnitude > 0f)
				{
					_moveDirection = GetMoveDir(direction).normalized;
				}
				else
				{
					_moveDirection = Vector3.zero;
					_desiredTargetSpeed = 0f;
				}

				if (_targetSpeed <= _desiredTargetSpeed)
				{
					_targetSpeed = Mathf.Min(_targetSpeed + _accelerationGround * Time.deltaTime, _desiredTargetSpeed); // gradually accelerate
				}
				else
				{
					_targetSpeed = Mathf.Max(_targetSpeed - _accelerationGround * Time.deltaTime, _desiredTargetSpeed); // gradually decelerate
				}
			}
			else
			{
				IsGrounded = false;
			}
		}

		public Vector3 GetMoveDir(Vector3 direction)
		{
			// TODO add riding on rigidbody support
			return Vector3.ProjectOnPlane(direction, _averageSlopeNormal).normalized;
		}

		Vector3 PlayerFloatForce()
		{
			float dotDownVel = Vector3.Dot(-_averageSlopeNormal, _rigidbody.velocity);
			float distanceFromGround = PlayerBottomPosFloating().y - AverageYPos;
			float distanceToIdealFloatHeight = distanceFromGround - _distanceToFloat;
			float floatForce = distanceToIdealFloatHeight * _springStrength - dotDownVel * _springDamp;
			if (Mathf.Abs(distanceToIdealFloatHeight) < 0.001f)
			{
				floatForce = 0.0f;
			}
			return -floatForce * Vector3.up;
		}

		public void StartMovingTowards(Vector3 targetPos, float additionalTimeWithoutControl = 0f, float timeAfterWhichToGiveUp = -1f, bool teleportToTargetPosOnGiveUp = true)
		{
			Stop();
			if (_moveTowardsCoroutine != null)
			{
				StopCoroutine(_moveTowardsCoroutine);
			}
			MoveTowardsFinished = false;
			_lastTimeMoveTowardsInited = Time.time;
			_moveTowardsCoroutine = StartCoroutine(MoveTowards(targetPos, additionalTimeWithoutControl, timeAfterWhichToGiveUp, teleportToTargetPosOnGiveUp));
		}

		void FinishMoveTowards()
		{
			MoveTowardsFinished = true;
			_gravityForce = 0.0f;
		}

		IEnumerator MoveTowards(Vector3 targetPos, float additionalTimeWithoutControl, float timeAfterWhichToGiveUp, bool teleportToTargetPosOnGiveUp)
		{
			while (Vector3.Distance(transform.position, targetPos) > _moveToThreshold)
			{
				Vector3 newPosition = Vector3.MoveTowards(_rigidbody.position, targetPos, Time.deltaTime * _moveToSpeed);
				_rigidbody.MovePosition(newPosition);

				if(timeAfterWhichToGiveUp >= 0 && Time.time - _lastTimeMoveTowardsInited > timeAfterWhichToGiveUp)
				{
					if(teleportToTargetPosOnGiveUp)
					{
						_rigidbody.MovePosition(targetPos);
					}
					break;
				}
				yield return new WaitForFixedUpdate();
			}

			Invoke(nameof(FinishMoveTowards), additionalTimeWithoutControl);
		}

		public void StartClimbingUpwardsObstacle(Vector3 targetPos, float obstacleClimbSpeed, float obstacleMoveThreshold)
		{
			Stop();
			if (_moveTowardsCoroutine != null)
			{
				StopCoroutine(_moveTowardsCoroutine);
			}
			MoveTowardsFinished = false;
			_moveTowardsCoroutine = StartCoroutine(MoveUpwardsObstacle(targetPos, obstacleClimbSpeed, obstacleMoveThreshold));
		}

		IEnumerator MoveUpwardsObstacle(Vector3 targetPos, float obstacleClimbSpeed, float obstacleMoveThreshold)
		{
			Vector3 targetPosUp = targetPos;
			targetPosUp.x = transform.position.x;
			targetPosUp.z = transform.position.z;
			Vector3 newPosition;
			while (Vector3.Distance(transform.position, targetPos) > _moveToThreshold)
			{
				if (targetPos.y - transform.position.y > obstacleMoveThreshold)
				{
					newPosition = Vector3.MoveTowards(_rigidbody.position, targetPosUp, Time.deltaTime * obstacleClimbSpeed);
				}
				else
				{
					newPosition = Vector3.MoveTowards(_rigidbody.position, targetPos, Time.deltaTime * obstacleClimbSpeed);
				}
				_rigidbody.MovePosition(newPosition);
				yield return new WaitForFixedUpdate();
			}
			Invoke(nameof(FinishMoveTowards), 0);
		}


        // ========================================================= PRIVATE STRUCTS

        private struct GroundCheckCastPoint
        {
            public RaycastHit? ValidHit;
            public Vector3 CastOriginPos;
        }
    }
}
