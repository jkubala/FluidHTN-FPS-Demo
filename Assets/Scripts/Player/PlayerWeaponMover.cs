using FPSDemo.Target;
using FPSDemo.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPSDemo.Player
{
	public class PlayerWeaponMover : MonoBehaviour
	{
		[SerializeField] CameraMovement cameraMovement;
		Player player;
		PlayerWeaponController weaponController;

		enum GunPosition { away, aiming, run, normal }
		GunPosition gunPosition = GunPosition.normal;
		[SerializeField] float weaponPosChangeSpeed = 3f;
		[SerializeField] float weaponRotChangeSpeed = 3f;
		Vector3 currentWeaponPos;
		Vector3 currentWeaponRot;
		Vector3 targetWeaponPos;
		Vector3 targetWeaponRot;
		bool weaponObstructed;

		[Header("Recoil")]
		Vector3 currentRecoilRotation = Vector3.zero;
		Vector3 targetRecoilRotation = Vector3.zero;

		[Header("Weapon Sway")]
		[SerializeField] Vector3 lookSway = Vector3.zero;
		[SerializeField] Vector3 movementSway = Vector3.zero;
		[SerializeField] private float maxSway = 4f;
		[SerializeField] private float swaySmoothness = 0.02f;
		Vector3 currentSway;
		Vector3 weaponSway = Vector3.zero;
		Vector3 smoothedWeaponSway = Vector3.zero;
		HealthSystem healthSystem;

		[Header("Bob Motion")]
		[SerializeField] Vector3 crouchAimBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] Vector3 crouchAimBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] Vector3 aimBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] Vector3 aimBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] Vector3 crouchBobAmount = new Vector3(1f, 1f, 0f);
		[SerializeField] Vector3 crouchBobSpeed = new Vector3(8f, 4f, 0f);
		[Space(10)]
		[SerializeField] Vector3 walkBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] Vector3 walkBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] Vector3 runBobAmount = new Vector3(5f, 5f, 1f);
		[SerializeField] Vector3 runBobSpeed = new Vector3(18f, 7f, 7f);
		[Space(10)]
		Vector3 targetBobAmount;
		Vector3 targetBobSpeed;
		[SerializeField] float bobDamp = 0.1f;
		[SerializeField] float resetSpeed = 6.0f;
		Vector3 finalBob = Vector3.zero;
		Vector3 bobVelocity = Vector3.zero;
		Vector3 bobSmoothed = Vector3.zero;

		[Header("Breath Sway")]
		[SerializeField] Vector3 breathSwayAmount = Vector3.zero;
		[SerializeField] Vector3 breathSwaySpeed = Vector3.zero;
		Vector3 targetBreathSway = Vector3.zero;
		Vector3 targetBreathSwaySmoothed = Vector3.zero;

		void Awake()
		{
			weaponController = GetComponent<PlayerWeaponController>();
			player = GetComponent<Player>();
			healthSystem = GetComponent<HealthSystem>();
		}

		void Start()
		{
			targetWeaponPos = weaponController.EquippedWeapon.normalPos;
			targetWeaponRot = weaponController.EquippedWeapon.normalRot;
			currentWeaponPos = targetWeaponPos;
			currentWeaponRot = targetWeaponRot;
		}

		private void OnEnable()
		{
			weaponController.EquippedWeapon.weaponCollisionDetector.CollisionEntered += LowerWeapon;
			weaponController.EquippedWeapon.weaponCollisionDetector.CollisionExited += RaiseWeapon;
			weaponController.OnUpdate += MoveWeapon;
			weaponController.OnFire += AddRecoilToTheTargetRotation;
			healthSystem.OnDeath += OnDeath;
		}

		private void OnDisable()
		{
			weaponController.EquippedWeapon.weaponCollisionDetector.CollisionEntered -= LowerWeapon;
			weaponController.EquippedWeapon.weaponCollisionDetector.CollisionExited -= RaiseWeapon;
			weaponController.OnUpdate -= MoveWeapon;
			weaponController.OnFire -= AddRecoilToTheTargetRotation;
			healthSystem.OnDeath -= OnDeath;
		}

		private void LowerWeapon()
		{
			weaponObstructed = true;
		}

		private void RaiseWeapon()
		{
			weaponObstructed = false;
		}

		void MoveWeapon()
		{
			UpdateWeaponPosition();
			UpdateThePositionOfCollisionDetector();
			UpdateWeaponSway();
			WeaponBreathingSway();
		}

		void OnDeath()
		{
			ChangeGunPos(GunPosition.away);
		}

		void ChangeGunPos(GunPosition newGunPos)
		{
			gunPosition = newGunPos;
			weaponController.WeaponReadyForReload = false;
			weaponController.WeaponAtTheReady = false;
			switch (newGunPos)
			{
				case GunPosition.normal:
					targetWeaponPos = weaponController.EquippedWeapon.normalPos;
					targetWeaponRot = weaponController.EquippedWeapon.normalRot;
					weaponController.WeaponReadyForReload = true;
					weaponController.WeaponAtTheReady = true;
					break;
				case GunPosition.aiming:
					targetWeaponPos = weaponController.EquippedWeapon.ADSPos;
					targetWeaponRot = weaponController.EquippedWeapon.ADSRot;
					weaponController.WeaponAtTheReady = true;
					break;
				case GunPosition.run:
					targetWeaponPos = weaponController.EquippedWeapon.runPos;
					targetWeaponRot = weaponController.EquippedWeapon.runRot;
					break;
				case GunPosition.away:
					targetWeaponPos = weaponController.EquippedWeapon.awayPos;
					targetWeaponRot = weaponController.EquippedWeapon.awayRot;
					break;
			}
		}

		void UpdateThePositionOfCollisionDetector()
		{
			if (player.IsAiming)
			{
				weaponController.EquippedWeapon.weaponCollisionDetector.transform.localPosition = weaponController.EquippedWeapon.ADSPos;
			}
			else
			{
				weaponController.EquippedWeapon.weaponCollisionDetector.transform.localPosition = weaponController.EquippedWeapon.normalPos;
			}
		}

		void UpdateWeaponPosition()
		{
			if (weaponObstructed || player.IsClimbing || !player.moveTowardsFinished)
			{
				ChangeGunPos(GunPosition.away);
			}
			else if ((player.IsSprinting || !player.IsGrounded) && !player.IsAiming)
			{
				ChangeGunPos(GunPosition.run);
			}
			else if (gunPosition != GunPosition.normal || gunPosition != GunPosition.aiming)
			{
				if (!player.IsAiming)
				{
					ChangeGunPos(GunPosition.normal);
				}
				else
				{
					ChangeGunPos(GunPosition.aiming);
				}
			}

			targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, weaponController.EquippedWeapon.recoilRecoverSpeed * Time.deltaTime);
			currentRecoilRotation = Vector3.Slerp(currentRecoilRotation, targetRecoilRotation, weaponController.EquippedWeapon.blowbackForce * Time.deltaTime);

			currentWeaponPos = Vector3.Lerp(currentWeaponPos, targetWeaponPos, weaponPosChangeSpeed * Time.deltaTime);
			currentWeaponRot = Vector3.Slerp(currentWeaponRot, targetWeaponRot, weaponRotChangeSpeed * Time.deltaTime);

			weaponController.EquippedWeapon.transform.SetLocalPositionAndRotation(currentWeaponPos, Quaternion.Euler(currentWeaponRot + currentRecoilRotation + smoothedWeaponSway + BobbingCalculation() + targetBreathSwaySmoothed));
		}

		void UpdateWeaponSway()
		{
			Vector2 moveInput = player.inputManager.GetMovementInput();
			Vector2 lookInput = cameraMovement.CameraMovementThisFrame;

			weaponSway.x = lookInput.y * lookSway.x + moveInput.y * movementSway.x;
			weaponSway.y = lookInput.x * lookSway.y + moveInput.x * movementSway.y;
			weaponSway.z = lookInput.x * lookSway.z - moveInput.x * movementSway.z;
			weaponSway = Vector3.ClampMagnitude(weaponSway, maxSway);

			smoothedWeaponSway = Vector3.SmoothDamp(smoothedWeaponSway, weaponSway, ref currentSway, swaySmoothness);
		}

		public Vector3 BobbingCalculation()
		{
			if (!player.IsMoving() || !player.IsGrounded)
			{
				bobSmoothed = Vector3.Lerp(bobSmoothed, Vector3.zero, Time.deltaTime * resetSpeed);
			}
			else
			{
				SetTargetBobAmountAndSpeed();
				finalBob = new Vector3(
	Mathf.Sin(Time.time * targetBobSpeed.x) * targetBobAmount.x,
	Mathf.Sin(Time.time * targetBobSpeed.y) * targetBobAmount.y,
	Mathf.Sin(Time.time * targetBobSpeed.z) * targetBobAmount.z);
				bobSmoothed = Vector3.SmoothDamp(bobSmoothed, finalBob, ref bobVelocity, bobDamp);
			}
			return bobSmoothed;
		}

		void SetTargetBobAmountAndSpeed()
		{
			if (player.IsSprinting)
			{
				targetBobAmount = runBobAmount;
				targetBobSpeed = runBobSpeed;
			}
			else if (player.IsMoving())
			{
				if (player.IsAiming)
				{
					if (player.IsCrouching)
					{
						targetBobAmount = crouchAimBobAmount;
						targetBobSpeed = crouchAimBobSpeed;
					}
					else
					{
						targetBobAmount = aimBobAmount;
						targetBobSpeed = aimBobSpeed;
					}
				}
				else if (player.IsCrouching)
				{
					targetBobAmount = crouchBobAmount;
					targetBobSpeed = crouchBobSpeed;
				}
				else
				{
					targetBobAmount = walkBobAmount;
					targetBobSpeed = walkBobSpeed;
				}
			}
		}

		void WeaponBreathingSway()
		{
			if (player.IsAiming)
			{
				if (targetBreathSway != Vector3.zero)
				{
					targetBreathSway = Vector3.MoveTowards(targetBreathSway, Vector3.zero, Time.deltaTime * resetSpeed);
				}
			}
			else
			{
				targetBreathSway.x = Mathf.Sin(Time.time * breathSwaySpeed.x) * breathSwayAmount.x;
				targetBreathSway.y = Mathf.Sin(Time.time * breathSwaySpeed.y) * breathSwayAmount.y;
				targetBreathSway.z = Mathf.Sin(Time.time * breathSwaySpeed.z) * breathSwayAmount.z;
			}
			targetBreathSwaySmoothed = Vector3.Lerp(targetBreathSwaySmoothed, targetBreathSway, Time.deltaTime);
		}

		public void AddRecoilToTheTargetRotation()
		{
			Vector3 recoilToAdd = player.IsAiming ? weaponController.EquippedWeapon.adsRecoil : weaponController.EquippedWeapon.recoil;
			targetRecoilRotation += new Vector3(recoilToAdd.x, UnityEngine.Random.Range(recoilToAdd.y, -recoilToAdd.y), UnityEngine.Random.Range(recoilToAdd.z, -recoilToAdd.z));
		}
	}
}
