using UnityEngine;
using UnityEngine.AI;
using System;
using System.Linq;
using System.Collections;
using FPSDemo.Input;
using FPSDemo.Target;

namespace FPSDemo.Player
{
	public class Player : MonoBehaviour
	{
		struct GroundCheckCastPoint
		{
			public RaycastHit? validHit;
			public Vector3 castOriginPos;
		}

		#region Properties
		public HumanTarget ThisTarget { get; private set; }
		public float AverageYPos { get; private set; }
		public float CrouchPercentage => 1f - (CurrentFloatingColliderHeight - crouchFloatingColliderHeight) / (standingFloatingColliderHeight - crouchFloatingColliderHeight);

		// Collider properties
		public CapsuleCollider Capsule { get; private set; }
		public LayerMask GroundMask { get { return 1; } private set { } }
		public float DistanceToFloat { get { return distanceToFloat; } private set { } }
		public float Radius
		{
			get => Capsule.radius;
		}

		public float CurrentFloatingColliderHeight
		{
			get => Capsule.height;
			set
			{
				Capsule.height = value;
				Capsule.center = new Vector3(Capsule.center.x, (value / 2) + distanceToFloat, Capsule.center.z);
				ThisTarget.IsCrouching = IsCrouching;
			}
		}

		// States
		public bool IsSprinting => desiredTargetSpeed > WalkSpeed;
		public bool IsClimbing { get; set; }
		public bool IsJumping { get; set; }
		public bool IsAiming { get; set; }
		public bool IsGrounded { get; private set; }
		public bool IsSlidingDownSlope { get; private set; }

		public bool IsCrouching
		{
			get
			{
				return standingFloatingColliderHeight - CurrentFloatingColliderHeight > 0.1f;
			}
		}
		#endregion

		#region Variables
		public InputManager inputManager;
		public new Rigidbody rigidbody;

		[Header("Collider")]
		public float characterHeight = 1.8f;
		[SerializeField] float skinWidth = 0.01f;
		[SerializeField] float headBumpSpeedReducer = 2f;
		[HideInInspector] public float standingFloatingColliderHeight;
		[HideInInspector] public float crouchFloatingColliderHeight = 0f;

		[Header("Slopes")]
		[SerializeField] float maxSlopeAngle = 35f;
		[SerializeField] AnimationCurve slopeCurve;
		Vector3 averageSlopeNormal;

		[Header("Stairs")]
		[SerializeField] float maxStairHeight = 0.25f;

		[Header("Floating")]
		[SerializeField][Range(0.0f, 1.0f)] float distanceToFloat = 0.1f;
		[SerializeField][Range(500.0f, 2000.0f)] float springStrength = 1000.0f;
		[SerializeField][Range(0.0f, 1000.0f)] float springDamp = 30.0f;
		[SerializeField][Range(0.1f, 0.9f)] float gravityForceToNeutralSpeed = 0.12f;

		[Header("Grounding")]
		[Tooltip("How much is distanceToCast moved up - to prevent accidental miss of a collider that is too close")]
		[SerializeField][Range(0.0f, 0.1f)] float castOriginOffsetDistance = 0.05f;
		Vector3 lastOnGroundPosition = Vector3.zero;
		[Tooltip("How frequently is lastGroundedPosition updated")]
		[SerializeField] float groundedPosUpdateInterval = 1f;

		[SerializeField] int stairCastMultiplier = 3;
		readonly GroundCheckCastPoint[] groundCasts = new GroundCheckCastPoint[12];
		public RaycastHit? ClosestHitToAveragePos { get; private set; }
		[HideInInspector] public float lastOnGroundPositionTime;
		float lastCastGridRotation;

		[Header("Speed")]
		[SerializeField] float accelerationGround = 8f;
		[Range(0, 1)]
		[SerializeField] float backwardSpeedPercentage = 0.4f;
		[Range(0, 1)]
		[SerializeField] float strafeSpeedPercentage = 0.8f;
		[SerializeField] float moveToSpeed = 2f;
		public float WalkSpeed { get; private set; } = 3f;
		[SerializeField] float terminalVelocity = 30f;
		[SerializeField] float gravityMultiplierOnFall = 4f;
		[SerializeField] float defaultGravityMultiplier = 2f;
		float gravityMultiplier = 1f;
		[SerializeField] int maxVelocityChange = 5;
		float targetSpeed;
		public float desiredTargetSpeed;
		public float gravityForce;
		[HideInInspector] public float crouchSpeedMultiplier = 1f;
		[HideInInspector] public float aimingMultiplier = 1f;
		float speedModX = 1f;
		float speedModY = 1f;
		Vector3 moveDirection = Vector3.zero;

		[Header("Misc")]
		bool freeze;
		[SerializeField] float returnToGroundYPos = -100f;
		[SerializeField] float moveToThreshold = 0.1f;
		
		float inputX = 0f;
		float inputY = 0f;

		// Events
		public event Action OnBeforeMove;
		public event Action OnAfterMove;
		public event Action OnPlayerUpdate;
		public event Action<float> OnLanding;
		public event Action<float> OnTeleport;
		public event Action<Quaternion> OnTeleportRotate;

		Coroutine moveTowardsCoroutine;
		public bool moveTowardsFinished { get; private set; } = true;
		float lastTimeMoveTowardsInited;

		Vector3 levelStartPosition;
		Quaternion levelStartRotation;
		#endregion

		void Awake()
		{
			levelStartPosition = transform.position;
			levelStartRotation = transform.rotation;
			rigidbody = GetComponent<Rigidbody>();
			ThisTarget = GetComponent<HumanTarget>();
			ThisTarget.SetAsPlayer();
			Capsule = GetComponentInChildren<CapsuleCollider>();
			standingFloatingColliderHeight = characterHeight - distanceToFloat;
			CurrentFloatingColliderHeight = standingFloatingColliderHeight;
			targetSpeed = 0f;
			lastCastGridRotation = transform.rotation.eulerAngles.y;
		}

		void Start()
		{
			InitGroundCastPositions(Capsule.radius - skinWidth);

		}

		void Update()
		{
			// When player clips out of the map, return him to the last known grounded position
			if (transform.position.y < returnToGroundYPos)
			{
				ReturnToLastGroundPosition();
			}

			if (freeze) { return; }

			OnPlayerUpdate?.Invoke();
		}

		void LateUpdate()
		{
			if (freeze) return;

			OnAfterMove?.Invoke();
		}

		void FixedUpdate()
		{
			if (freeze) return;


			OnBeforeMove?.Invoke();
			UpdateDirectionFromInput();
			CheckGround(moveDirection.normalized);

			if (IsJumping && rigidbody.velocity.y < 0f)
			{
				gravityMultiplier = gravityMultiplierOnFall;
			}
			else
			{
				gravityMultiplier = defaultGravityMultiplier;
			}
			if (!IsClimbing && moveTowardsFinished)
			{
				// Update gravityForce and MoveDirection
				if (IsGrounded)
				{
					if (!IsJumping)
					{
						rigidbody.AddForce(PlayerFloatForce(), ForceMode.Acceleration);
					}

					if (Mathf.Abs(gravityForce) < 0.01f)
					{
						gravityForce = 0f;
					}
					else
					{
						gravityForce *= 1 - gravityForceToNeutralSpeed;
					}
				}
				else
				{
					Vector3 castOrigin = PlayerTopSphere();
					gravityForce = Mathf.Clamp(gravityForce + Physics.gravity.y * gravityMultiplier * Time.deltaTime, -terminalVelocity, terminalVelocity);
					if (IsJumping && Physics.SphereCast(castOrigin, Radius - skinWidth, Vector3.up, out RaycastHit hit, 1f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
					{
						float distanceToHit = Mathf.Clamp(hit.point.y - castOrigin.y - Radius, 0, Mathf.Infinity);
						if (distanceToHit < 0.05f)
						{
							gravityForce = -Mathf.Abs(gravityForce) / headBumpSpeedReducer;
						}
					}
				}

				// Apply speed limits to moveDirection vector
				float currSpeed = targetSpeed * crouchSpeedMultiplier * speedModX * speedModY * aimingMultiplier;
				if (Vector3.Angle(transform.rotation * Vector3.forward, averageSlopeNormal) > 90f)
				{
					currSpeed *= SpeedOnSlopeModifier();
				}
				moveDirection *= currSpeed;

				Vector3 velocityChange = moveDirection + Vector3.up * gravityForce - rigidbody.velocity;
				// Finally, add movement velocity to player rigidbody velocity
				rigidbody.AddForce(Vector3.ClampMagnitude(velocityChange, maxVelocityChange), ForceMode.VelocityChange);
			}
		}

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
							castOriginPos = new Vector3(x, castOriginOffsetDistance + maxStairHeight + Capsule.radius / 4, z)
						};
						groundCasts[i++] = castPoint;
					}
				}
			}
		}

		float SpeedOnSlopeModifier()
		{
			Vector3 forwardProjection = Vector3.ProjectOnPlane(transform.forward, averageSlopeNormal);
			float angle = Vector3.Angle(forwardProjection, transform.forward);
			return Mathf.Clamp(1f - slopeCurve.Evaluate(angle / maxSlopeAngle), 0f, 1f);
		}

		void UpdateDirectionFromInput()
		{
			// Take side input only if not sprinting - can only sprint forwards
			Vector2 movementInput = inputManager.GetMovementInput();
			inputX = movementInput.x;
			inputY = movementInput.y;
			if (movementInput != Vector2.zero)
			{
				lastCastGridRotation = transform.rotation.eulerAngles.y;
			}

			// Create movementVector from input
			moveDirection = (inputX * transform.right + inputY * transform.forward).normalized;

			if (IsGrounded)
			{
				// Limit speed when going backwards
				if (inputY < 0 && speedModY >= backwardSpeedPercentage)
				{
					speedModY = Mathf.Max(speedModY - Time.deltaTime, backwardSpeedPercentage);
				}
				// Increase the speedModY back up to 1
				else
				{
					speedModY = Mathf.Min(speedModY + Time.deltaTime, 1f);
				}

				// Limit speed when going straight to the side
				if (Mathf.Abs(inputX) > 0.01f && speedModX >= strafeSpeedPercentage)
				{
					speedModX = Mathf.Max(speedModX - Time.deltaTime, strafeSpeedPercentage);
				}
				// Increase the speedModX up to 1
				else
				{
					speedModX = Mathf.Min(speedModX + Time.deltaTime, 1f);
				}

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

		void DeathBehavior(HumanTarget target)
		{
			if (target == ThisTarget)
			{
				distanceToFloat = 0f;
				inputManager.SetActiveAllGameplayControls(false);
				Debug.Log("PLAYER HIT!");
			}
		}

		void OnDrawGizmos()
		{
			if (Application.isPlaying)
			{
				for (int i = 0; i < groundCasts.Length; i++)
				{
					Vector3 rotatedCenter = Quaternion.Euler(0, lastCastGridRotation, 0) * groundCasts[i].castOriginPos;
					Gizmos.DrawSphere(rotatedCenter + PlayerBottomPos(), Capsule.radius / 4);
				}
				Gizmos.DrawRay(transform.position, moveDirection);
			}
		}

		public bool IsMoving()
		{
			if (freeze)
			{
				return false;
			}
			else
			{
				return Mathf.Abs(inputY) > 0.01f || Mathf.Abs(inputX) > 0.01f;
			}
		}

		public Vector3 PlayerTopPos()
		{
			return transform.position + Vector3.up * (Capsule.height + distanceToFloat);
		}

		public Vector3 PlayerBottomPosFloating()
		{
			return PlayerBottomPos() + Vector3.up * distanceToFloat;
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
			rigidbody.velocity = Vector3.zero;
		}

		public void ReturnToLastGroundPosition()
		{
			Vector3 lastPosOnNavmesh = lastOnGroundPosition;
			if (NavMesh.SamplePosition(lastPosOnNavmesh, out NavMeshHit hit, 3f, NavMesh.AllAreas))
			{
				lastPosOnNavmesh = hit.position;
			}
			lastPosOnNavmesh += Vector3.up * distanceToFloat;
			Teleport(lastPosOnNavmesh);
		}

		public void Freeze(bool value)
		{
			if (value)
			{
				freeze = true;
				Stop();
				rigidbody.Sleep();
			}
			else
			{
				freeze = false;
			}
		}

		public void Teleport(Vector3 position, Quaternion? rotation = null)
		{
			Debug.LogWarning("TELEPORTING TO" + position);
			rigidbody.position = position;
			transform.position = position;
			gravityForce = 0f;
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
			float distanceToCast = stairCastMultiplier * maxStairHeight + castOriginOffsetDistance + Capsule.radius / 4;
			for (int i = 0; i < groundCasts.Length; i++)
			{
				groundCasts[i].validHit = null;
				Vector3 rotatedCenter = Quaternion.Euler(0, lastCastGridRotation, 0) * groundCasts[i].castOriginPos + PlayerBottomPosFloating();
				if (Physics.SphereCast(rotatedCenter, gridElementRadius, Vector3.down, out RaycastHit hit, distanceToCast, GroundMask.value, QueryTriggerInteraction.Ignore))
				{
					hitSomething = true;
					if (Vector3.Angle(hit.normal, Vector3.up) < maxSlopeAngle)
					{
						groundCasts[i].validHit = hit;
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
				AverageYPos = groundCasts.Where(element => element.validHit.HasValue).Average(element => element.validHit.Value.point.y);
				averageSlopeNormal = groundCasts.Where(element => element.validHit.HasValue)
					.Aggregate(Vector3.zero, (sum, element) => sum + element.validHit.Value.normal) / groundCasts.Count(element => element.validHit.HasValue);

				ClosestHitToAveragePos = null;
				foreach (GroundCheckCastPoint point in groundCasts)
				{
					float minDistance = Mathf.Infinity;
					if (point.validHit.HasValue)
					{
						float distance = Mathf.Abs(point.validHit.Value.point.y - AverageYPos);
						if (distance < minDistance)
						{
							minDistance = distance;
							ClosestHitToAveragePos = point.validHit.Value;
						}
					}
				}

				if (!IsGrounded)
				{
					IsGrounded = true;
					OnLanding?.Invoke(Mathf.Abs(rigidbody.velocity.y));
				}
				if (lastOnGroundPositionTime < Time.time)
				{
					lastOnGroundPosition = groundCasts.Where(element => element.validHit.HasValue)
			.Aggregate(Vector3.zero, (sum, element) => sum + element.validHit.Value.point) / groundCasts.Count(element => element.validHit.HasValue);
					lastOnGroundPositionTime = Time.time + groundedPosUpdateInterval;
				}

				if (direction.magnitude > 0f)
				{
					moveDirection = GetMoveDir(direction).normalized;
				}
				else
				{
					moveDirection = Vector3.zero;
					desiredTargetSpeed = 0f;
				}

				if (targetSpeed <= desiredTargetSpeed)
				{
					targetSpeed = Mathf.Min(targetSpeed + accelerationGround * Time.deltaTime, desiredTargetSpeed); // gradually accelerate
				}
				else
				{
					targetSpeed = Mathf.Max(targetSpeed - accelerationGround * Time.deltaTime, desiredTargetSpeed); // gradually decelerate
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
			return Vector3.ProjectOnPlane(direction, averageSlopeNormal).normalized;
		}

		Vector3 PlayerFloatForce()
		{
			float dotDownVel = Vector3.Dot(-averageSlopeNormal, rigidbody.velocity);
			float distanceFromGround = PlayerBottomPosFloating().y - AverageYPos;
			float distanceToIdealFloatHeight = distanceFromGround - distanceToFloat;
			float floatForce = distanceToIdealFloatHeight * springStrength - dotDownVel * springDamp;
			if (Mathf.Abs(distanceToIdealFloatHeight) < 0.001f)
			{
				floatForce = 0.0f;
			}
			return -floatForce * Vector3.up;
		}

		public void StartMovingTowards(Vector3 targetPos, float additionalTimeWithoutControl = 0f, float timeAfterWhichToGiveUp = -1f, bool teleportToTargetPosOnGiveUp = true)
		{
			Stop();
			if (moveTowardsCoroutine != null)
			{
				StopCoroutine(moveTowardsCoroutine);
			}
			moveTowardsFinished = false;
			lastTimeMoveTowardsInited = Time.time;
			moveTowardsCoroutine = StartCoroutine(MoveTowards(targetPos, additionalTimeWithoutControl, timeAfterWhichToGiveUp, teleportToTargetPosOnGiveUp));
		}

		void FinishMoveTowards()
		{
			moveTowardsFinished = true;
			gravityForce = 0.0f;
		}

		IEnumerator MoveTowards(Vector3 targetPos, float additionalTimeWithoutControl, float timeAfterWhichToGiveUp, bool teleportToTargetPosOnGiveUp)
		{
			while (Vector3.Distance(transform.position, targetPos) > moveToThreshold)
			{
				Vector3 newPosition = Vector3.MoveTowards(rigidbody.position, targetPos, Time.deltaTime * moveToSpeed);
				rigidbody.MovePosition(newPosition);

				if(timeAfterWhichToGiveUp >= 0 && Time.time - lastTimeMoveTowardsInited > timeAfterWhichToGiveUp)
				{
					if(teleportToTargetPosOnGiveUp)
					{
						rigidbody.MovePosition(targetPos);
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
			if (moveTowardsCoroutine != null)
			{
				StopCoroutine(moveTowardsCoroutine);
			}
			moveTowardsFinished = false;
			moveTowardsCoroutine = StartCoroutine(MoveUpwardsObstacle(targetPos, obstacleClimbSpeed, obstacleMoveThreshold));
		}

		IEnumerator MoveUpwardsObstacle(Vector3 targetPos, float obstacleClimbSpeed, float obstacleMoveThreshold)
		{
			Vector3 targetPosUp = targetPos;
			targetPosUp.x = transform.position.x;
			targetPosUp.z = transform.position.z;
			Vector3 newPosition;
			while (Vector3.Distance(transform.position, targetPos) > moveToThreshold)
			{
				if (targetPos.y - transform.position.y > obstacleMoveThreshold)
				{
					newPosition = Vector3.MoveTowards(rigidbody.position, targetPosUp, Time.deltaTime * obstacleClimbSpeed);
				}
				else
				{
					newPosition = Vector3.MoveTowards(rigidbody.position, targetPos, Time.deltaTime * obstacleClimbSpeed);
				}
				rigidbody.MovePosition(newPosition);
				yield return new WaitForFixedUpdate();
			}
			Invoke(nameof(FinishMoveTowards), 0);
		}
	}
}
