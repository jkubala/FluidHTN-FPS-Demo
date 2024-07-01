using UnityEngine;
using TMPro;

namespace FPSDemo.FPSController
{
	public class PlayerWeaponController : MonoBehaviour
	{
		enum GunPosition { away, aiming, run, normal }
		GunPosition gunPosition = GunPosition.normal;
		[SerializeField] CameraMovement cameraMovement;
		[SerializeField] Weapon equippedWeapon;

		[SerializeField] LayerMask shotLayerMask;
		[SerializeField] int ragdollBodyLayerIndex;

		[SerializeField, Tooltip("Multiplier to apply to player speed when aiming.")]
		private float aimMultiplier = 0.4f;

		[SerializeField]
		private TextMeshProUGUI ammoText = null;

		Player player;
		[SerializeField] GameObject crosshairGameObject;
		bool reloading = false;
		int availableMagazines = 0;
		[SerializeField] Transform bulletSpawnPoint;
		private float lastFired = 0.0f;
		bool weaponObstructed = false;

		// Reticle
		RectTransform reticleTransform;
		float currentOverallAngleSpread;
		float currentSpreadFromMoving = 0;
		[SerializeField] float timeToReachFullMoveSpread = 0.5f;
		float angleSpreadFromShooting = 0;
		float maxAngleSpread = 15f;
		float maxReticleSize = 500;
		float minReticleSize = 100;

		[SerializeField] float weaponPosChangeSpeed = 3f;
		[SerializeField] float weaponRotChangeSpeed = 3f;
		Vector3 currentWeaponPos;
		Vector3 currentWeaponRot;
		Vector3 targetWeaponPos;
		Vector3 targetWeaponRot;

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

		private void Awake()
		{
			player = GetComponent<Player>();
			healthSystem = GetComponent<HealthSystem>();
			reticleTransform = crosshairGameObject.GetComponent<RectTransform>();
			InitStartingVariables();
		}

		private void OnEnable()
		{
			equippedWeapon.weaponCollisionDetector.CollisionEntered += LowerWeapon;
			equippedWeapon.weaponCollisionDetector.CollisionExited += RaiseWeapon;
			player.OnBeforeMove += OnBeforeMove;
			healthSystem.OnDeath += OnDeath;
		}

		private void OnDisable()
		{
			equippedWeapon.weaponCollisionDetector.CollisionEntered -= LowerWeapon;
			equippedWeapon.weaponCollisionDetector.CollisionExited -= RaiseWeapon;
			player.OnBeforeMove -= OnBeforeMove;
			healthSystem.OnDeath -= OnDeath;
		}

		void OnDeath()
		{
			ChangeGunPos(GunPosition.away);
			crosshairGameObject.SetActive(false);
		}

		void InitStartingVariables()
		{
			availableMagazines = equippedWeapon.startMagazineCount;
			lastFired = -equippedWeapon.fireRate;
			targetWeaponPos = equippedWeapon.normalPos;
			targetWeaponRot = equippedWeapon.normalRot;
			currentWeaponPos = targetWeaponPos;
			currentWeaponRot = targetWeaponRot;
		}

		void Start()
		{
			UpdateAmmoText();
		}

		private void LowerWeapon()
		{
			weaponObstructed = true;
		}

		private void RaiseWeapon()
		{
			weaponObstructed = false;
		}

		void ChangeGunPos(GunPosition newGunPos)
		{
			gunPosition = newGunPos;
			switch (newGunPos)
			{
				case GunPosition.normal:
					targetWeaponPos = equippedWeapon.normalPos;
					targetWeaponRot = equippedWeapon.normalRot;
					break;
				case GunPosition.aiming:
					targetWeaponPos = equippedWeapon.ADSPos;
					targetWeaponRot = equippedWeapon.ADSRot;
					break;
				case GunPosition.run:
					targetWeaponPos = equippedWeapon.runPos;
					targetWeaponRot = equippedWeapon.runRot;
					break;
				case GunPosition.away:
					targetWeaponPos = equippedWeapon.awayPos;
					targetWeaponRot = equippedWeapon.awayRot;
					break;
			}
		}

		bool ShouldFireTheGun()
		{
			// Tapping button for semi-auto, holding for full auto and gun pos needs to be either normal, or aiming
			return ((equippedWeapon.isAutomatic && player.inputManager.FireInputAction.IsPressed()) ||
				(!equippedWeapon.isAutomatic && player.inputManager.FireInputAction.WasPressedThisFrame())) &&
				(gunPosition == GunPosition.normal || gunPosition == GunPosition.aiming);
		}

		private void Update()
		{
			if (player.ThisTarget.IsDead)
			{
				return;
			}

			UpdateReticleSize();
			UpdateThePositionOfCollisionDetector();
			if (ShouldFireTheGun())
			{
				FireInput();
			}

			if (gunPosition == GunPosition.normal && player.inputManager.ReloadInputAction.WasPressedThisFrame())
			{
				ReloadInput();
			}

			if (!player.IsAiming && player.inputManager.AimInputAction.IsPressed())
			{
				AimInput(true);
			}
			else if (player.IsAiming && !player.inputManager.AimInputAction.IsPressed())
			{
				AimInput(false);
			}
			UpdateWeaponSway();
			FocusADS();
		}

		void UpdateThePositionOfCollisionDetector()
		{
			if (player.IsAiming)
			{
				equippedWeapon.weaponCollisionDetector.transform.localPosition = equippedWeapon.ADSPos;
			}
			else
			{
				equippedWeapon.weaponCollisionDetector.transform.localPosition = equippedWeapon.normalPos;
			}
		}

		void OnBeforeMove()
		{
			UpdateWeaponPosition();
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

			targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, equippedWeapon.recoilRecoverSpeed * Time.deltaTime);
			currentRecoilRotation = Vector3.Slerp(currentRecoilRotation, targetRecoilRotation, equippedWeapon.blowbackForce * Time.deltaTime);

			currentWeaponPos = Vector3.Lerp(currentWeaponPos, targetWeaponPos, weaponPosChangeSpeed * Time.deltaTime);
			currentWeaponRot = Vector3.Slerp(currentWeaponRot, targetWeaponRot, weaponRotChangeSpeed * Time.deltaTime);

			equippedWeapon.gameObject.transform.localPosition = currentWeaponPos;
			equippedWeapon.gameObject.transform.localRotation = Quaternion.Euler(currentWeaponRot + currentRecoilRotation + smoothedWeaponSway);
		}

		void UpdateWeaponSway()
		{
			Vector2 moveInput = player.inputManager.GetMovementInput();
			Vector2 lookInput = cameraMovement.cameraMovementThisFrame;

			weaponSway.x = lookInput.y * lookSway.x + moveInput.y * movementSway.x;
			weaponSway.y = lookInput.x * lookSway.y + moveInput.x * movementSway.y;
			weaponSway.z = lookInput.x * lookSway.z - moveInput.x * movementSway.z;
			weaponSway = Vector3.ClampMagnitude(weaponSway, maxSway);

			smoothedWeaponSway = Vector3.SmoothDamp(smoothedWeaponSway, weaponSway, ref currentSway, swaySmoothness);
		}

		void FocusADS()
		{
			if (player.IsAiming)
			{
				player.aimingMultiplier = aimMultiplier;
			}
			else
			{
				player.aimingMultiplier = 1.0f;
			}
		}

		void AddRecoilToTheTargetPosition()
		{
			Vector3 recoilToAdd = player.IsAiming ? equippedWeapon.adsRecoil : equippedWeapon.recoil;
			targetRecoilRotation += new Vector3(recoilToAdd.x, Random.Range(recoilToAdd.y, -recoilToAdd.y), Random.Range(recoilToAdd.z, -recoilToAdd.z));
		}

		private void Fire()
		{
			float maxAngle = currentOverallAngleSpread;
			if (!player.IsAiming)
			{
				maxAngle += equippedWeapon.defaultHipFireAngleSpread;
			}
			float xAngle = Random.Range(0, maxAngle);
			float yAngle = Random.Range(0, maxAngle);
			if (Random.Range(0, 2) == 1) { xAngle *= -1f; }
			if (Random.Range(0, 2) == 1) { yAngle *= -1f; }
			bulletSpawnPoint.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);
			equippedWeapon.Fire(player.ThisTarget, bulletSpawnPoint, shotLayerMask, ragdollBodyLayerIndex);

			AddRecoilToTheTargetPosition();
			player.ThisTarget.LastTimeFired = Time.time;
			angleSpreadFromShooting += player.IsAiming ? equippedWeapon.angleSpreadPerShotADS : equippedWeapon.angleSpreadPerShot;
			UpdateAmmoText();
		}


		public void FireInput()
		{
			if (!reloading)
			{
				if (equippedWeapon.currentAvailableAmmo > 0)
				{
					if (Time.time > equippedWeapon.fireRate + lastFired)
					{
						lastFired = Time.time;
						Fire();
					}
				}
			}
		}

		public void ReloadInput()
		{
			if (!reloading && availableMagazines > 0)
			{
				reloading = true;
				Invoke(nameof(EndReload), equippedWeapon.reloadTime);
			}
		}

		public void AimInput(bool aimIn)
		{
			if (aimIn)
			{
				ChangeGunPos(GunPosition.aiming);
				player.IsAiming = true;
			}
			else if (player.IsAiming && !aimIn)
			{
				ChangeGunPos(GunPosition.normal);
				player.IsAiming = false;
			}

			crosshairGameObject.SetActive(!player.IsAiming);
		}

		void EndReload()
		{
			reloading = false;
			equippedWeapon.currentAvailableAmmo = equippedWeapon.maxAmmoInMagazine;
			availableMagazines--;
			UpdateAmmoText();
		}

		void UpdateReticleSize()
		{
			if (player.IsMoving())
			{
				currentSpreadFromMoving = Mathf.Clamp(currentSpreadFromMoving + equippedWeapon.maxAngleSpreadWhenMoving / timeToReachFullMoveSpread * Time.deltaTime, 0, equippedWeapon.maxAngleSpreadWhenMoving);
			}
			else
			{
				currentSpreadFromMoving = Mathf.Clamp(currentSpreadFromMoving - equippedWeapon.maxAngleSpreadWhenMoving / timeToReachFullMoveSpread * Time.deltaTime, 0, equippedWeapon.maxAngleSpreadWhenMoving);
			}

			if (angleSpreadFromShooting > 0)
			{
				float targetMaxSpreadFromShooting = player.IsAiming ? equippedWeapon.maxAngleSpreadWhenShootingADS : equippedWeapon.maxAngleSpreadWhenShooting;
				angleSpreadFromShooting = Mathf.Clamp(angleSpreadFromShooting - equippedWeapon.spreadStabilityGain.Evaluate(Time.time - lastFired) * Time.deltaTime, 0, targetMaxSpreadFromShooting);
			}
			currentOverallAngleSpread = Mathf.Clamp(currentSpreadFromMoving + currentSpreadFromMoving + angleSpreadFromShooting, 0, maxAngleSpread);
			float currentSpreadToReticleSize = Mathf.Lerp(minReticleSize, maxReticleSize, currentOverallAngleSpread / maxAngleSpread);
			reticleTransform.sizeDelta = new Vector2(currentSpreadToReticleSize, currentSpreadToReticleSize);
		}

		void UpdateAmmoText()
		{
			if (ammoText != null)
			{
				ammoText.text = equippedWeapon.currentAvailableAmmo.ToString() + " / " + (availableMagazines * equippedWeapon.maxAmmoInMagazine).ToString();
			}
		}
	}
}
