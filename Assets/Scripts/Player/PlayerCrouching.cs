using UnityEngine;

namespace FPSDemo.Player
{
	public class PlayerCrouching : MonoBehaviour
	{
		#region Variables
		[Header("Crouching")]
		Player player;
		public float CrouchColliderHeight { get; private set; } = 1.25f;
		[SerializeField] float crouchSpeed = 10f;
		[SerializeField] float crouchSpeedMultiplier = 0.4f;
		[SerializeField] int numberOfStepsToCrouch = 3;
		[SerializeField] float spaceFromCeilingWhenUncrouching = 0.1f;
		[SerializeField] float deadHeight = 0.8f;

		int currentCrouchLevel = 0;
		bool isAdjustingCrouchLevel = false;
		float stepSize;
		#endregion

		#region Builtin methods
		void Awake()
		{
			player = GetComponent<Player>();
			player.crouchFloatingColliderHeight = CrouchColliderHeight - player.DistanceToFloat;
		}

		void Start()
		{
			stepSize = (player.standingFloatingColliderHeight - player.crouchFloatingColliderHeight) / numberOfStepsToCrouch;
		}

		void OnEnable()
		{
			player.OnPlayerUpdate += OnPlayerUpdate;
			player.OnBeforeMove += OnBeforeMove;
		}
		void OnDisable()
		{
			player.OnPlayerUpdate -= OnPlayerUpdate;
			player.OnBeforeMove -= OnBeforeMove;
		}
		#endregion
		Vector3 castOrigin = Vector3.zero;
		#region Custom methods
		void OnBeforeMove()
		{
			float heightTarget;
			if (player.ThisTarget.IsDead)
			{
				heightTarget = deadHeight;
			}
			else
			{
				heightTarget = player.standingFloatingColliderHeight - (currentCrouchLevel * stepSize);
				if (player.IsCrouching && !Mathf.Approximately(heightTarget, player.CurrentFloatingColliderHeight))
				{
					castOrigin = player.PlayerTopSphere();
					if (Physics.SphereCast(castOrigin, player.Radius, Vector3.up, out RaycastHit hit, player.standingFloatingColliderHeight - player.CurrentFloatingColliderHeight, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
					{
						float distanceToHit = Mathf.Clamp(hit.point.y - castOrigin.y - player.Radius, 0, Mathf.Infinity);
						heightTarget = Mathf.Clamp(Mathf.Min(player.CurrentFloatingColliderHeight + distanceToHit - spaceFromCeilingWhenUncrouching, heightTarget), player.crouchFloatingColliderHeight, player.standingFloatingColliderHeight);
					}
				}
			}

			// Gradually adjust current height to target height
			if (Mathf.Abs(heightTarget - player.CurrentFloatingColliderHeight) > 0.001f)
			{
				player.CurrentFloatingColliderHeight = Mathf.Lerp(player.CurrentFloatingColliderHeight, heightTarget, crouchSpeed * Time.deltaTime);
			}
			else
			{
				player.CurrentFloatingColliderHeight = heightTarget;
			}

			// Calculate crouch speed
			if (player.IsCrouching)
			{
				player.crouchSpeedMultiplier = Mathf.Lerp(1f, crouchSpeedMultiplier, player.CrouchPercentage);
			}
			else
			{
				player.crouchSpeedMultiplier = 1f;
			}
		}
		void OnPlayerUpdate()
		{
			UpdateCrouchLevel();
		}

		void UpdateCrouchLevel()
		{
			if (!player.IsSprinting)
			{
				if (player.inputManager.IncreaseCrouchLevel)
				{
					currentCrouchLevel = Mathf.Clamp(++currentCrouchLevel, 0, numberOfStepsToCrouch);
					isAdjustingCrouchLevel = true;
				}
				else if (player.inputManager.DecreaseCrouchLevel)
				{
					currentCrouchLevel = Mathf.Clamp(--currentCrouchLevel, 0, numberOfStepsToCrouch);
					isAdjustingCrouchLevel = true;
				}
				else if (player.inputManager.CrouchToggled)
				{
					if (isAdjustingCrouchLevel)
					{
						isAdjustingCrouchLevel = false;
					}
					else
					{
						if (currentCrouchLevel > 0)
						{
							currentCrouchLevel = 0;
						}
						else
						{
							currentCrouchLevel = numberOfStepsToCrouch;
						}
					}
				}
			}
			else
			{
				currentCrouchLevel = 0;
			}
		}

		public void SetCrouchLevelToMatchHeight(float heightToMatch)
		{
			int crouchLevelToSet;
			if (Mathf.Approximately(heightToMatch, player.characterHeight))
			{
				crouchLevelToSet = 0;
			}
			else
			{
				crouchLevelToSet = numberOfStepsToCrouch - Mathf.FloorToInt((heightToMatch - CrouchColliderHeight - spaceFromCeilingWhenUncrouching) / stepSize);
			}

			if (crouchLevelToSet != currentCrouchLevel)
			{
				currentCrouchLevel = crouchLevelToSet;
			}

		}

		void OnRestart()
		{
			SetCrouchLevelToMatchHeight(player.characterHeight);
		}
		#endregion
	}
}
