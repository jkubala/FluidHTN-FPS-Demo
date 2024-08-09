using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Player
{
	public class PlayerClimbing : MonoBehaviour
	{
		[Header("Climbing")]
		[Tooltip("Speed that player moves vertically when climbing.")]
		public float climbingSpeed = 80.0f;
		public float angleToClimb = 60f;
		public float grabbingTime = 0.4f;
		public float climbingTime = 2.0f;
		public float clampClimbingCameraAngle = 60f;
		[SerializeField] float shouldClimbPressedDuration = 0.5f;
		[SerializeField] float obstacleClimbSpeed = 3f;
		[SerializeField] float obstacleMoveThreshold = 0.5f;
		Player player;
		CapsuleCollider capsuleCollider;
		PlayerCrouching playerCrouching;
		CameraMovement cameraMovement;
		EnvironmentScanner environmentScanner;
		bool shouldClimb;
		float lastTimeShouldClimbPressed;


		enum ClimbingState
		{
			None,
			Grabbing,
			ClimbingLedge,
			Grabbed,
			Releasing
		}

		ClimbingState climbingState;

		void Awake()
		{
			player = GetComponent<Player>();
			playerCrouching = GetComponent<PlayerCrouching>();
			cameraMovement = GetComponentInChildren<CameraMovement>();
			environmentScanner = GetComponent<EnvironmentScanner>();
			capsuleCollider = GetComponent<CapsuleCollider>();
			InitVariables();
		}

		void InitVariables()
		{
			shouldClimb = false;
			lastTimeShouldClimbPressed = Mathf.NegativeInfinity;
			climbingState = ClimbingState.None;
			player.IsClimbing = false;
		}

		void OnEnable()
		{
			player.OnBeforeMove += OnBeforeMove;
			player.OnPlayerUpdate += OnPlayerUpdate;
		}
		void OnDisable()
		{
			player.OnBeforeMove -= OnBeforeMove;
			player.OnPlayerUpdate -= OnPlayerUpdate;
		}

		void OnPlayerUpdate()
		{
			if (player.inputManager.InteractInputAction.WasPressedThisFrame() && !player.IsClimbing)
			{
				shouldClimb = true;
				lastTimeShouldClimbPressed = Time.time;
			}

			if (Time.time - lastTimeShouldClimbPressed > shouldClimbPressedDuration)
			{
				shouldClimb = false;
			}
			if (shouldClimb && !player.IsClimbing)
			{
				// Pick the best valid hit
				ValidHit? chosenHit = GetBestHit(cameraMovement.CameraBase.transform.rotation);
				if (chosenHit != null)
				{
					playerCrouching.SetCrouchLevelToMatchHeight(chosenHit.Value.heightSpace);
					shouldClimb = false;
					ClimbObstacle(chosenHit.Value.destination, chosenHit.Value.rotation);
				}
			}
		}

		ValidHit? GetBestHit(Quaternion cameraLookDirection)
		{
			List<ValidHit> validHits = environmentScanner.ObstacleCheck();
			if (validHits.Count == 0)
			{
				return null;
			}
			ValidHit bestHit = validHits[0]; // Assume the first hit is the best initially
			float minAngle = Vector3.Angle((bestHit.destination - cameraMovement.transform.position).normalized, cameraLookDirection * Vector3.forward);
			foreach (ValidHit validHit in validHits)
			{
				// Compare the angle between the direction to each valid hit's destination and the camera look direction
				float angle = Vector3.Angle((validHit.destination - cameraMovement.transform.position).normalized, cameraLookDirection * Vector3.forward);

				// Update the best hit if the current hit has a smaller angle
				if (angle < minAngle)
				{
					minAngle = angle;
					bestHit = validHit;
				}
			}

			return bestHit;
		}

		void OnBeforeMove()
		{
			if (climbingState != ClimbingState.None)
			{
				Climbing();
			}
		}

		public void ClimbObstacle(Vector3 destination, Quaternion rotation)
		{
			if (player.IsClimbing)
			{
				return;
			}
			climbingState = ClimbingState.ClimbingLedge;
			capsuleCollider.enabled = false;

			player.IsClimbing = true;
			player.IsJumping = false;
			player.StartClimbingUpwardsObstacle(destination, obstacleClimbSpeed, obstacleMoveThreshold);
			cameraMovement.StartRotatingCameraTowards(rotation, 0f);
		}


		void Unclimb()
		{
			climbingState = ClimbingState.None;
			player.IsClimbing = false;
			capsuleCollider.enabled = true;
			cameraMovement.ResetXAngle();
			cameraMovement.CanRotateCamera = true;
		}

		void Climbing()
		{
			switch (climbingState)
			{
				case ClimbingState.Releasing:
					if (player.moveTowardsFinished)
					{
						Unclimb();
					}
					break;
				case ClimbingState.Grabbing:
					if (player.moveTowardsFinished && cameraMovement.RotateTowardsFinished)
					{
						climbingState = ClimbingState.Grabbed;
						cameraMovement.ClampXAngle(clampClimbingCameraAngle);
						player.IsClimbing = true;
						cameraMovement.CanRotateCamera = true;
					}
					break;
				case ClimbingState.ClimbingLedge:
					if (player.moveTowardsFinished && cameraMovement.RotateTowardsFinished)
					{
						climbingState = ClimbingState.None;
						capsuleCollider.enabled = true;
						player.IsClimbing = false;
						cameraMovement.CanRotateCamera = true;
					}
					break;
			}
		}
	}
}
