using UnityEngine;
using TMPro;

namespace FPSDemo.FPSController
{
	public class PlayerWeaponController : MonoBehaviour
	{
		enum GunPosition { low, run, raised }
		GunPosition gunPosition = GunPosition.raised;
		[SerializeField] Weapon equippedWeapon;

		[SerializeField, Tooltip("Multiplier to apply to player speed when aiming.")]
		private float aimMultiplier = 0.4f;

		[Header("Reload & Ammo")]
		[SerializeField]
		private int startMagazineCount = 3;

		[SerializeField]
		private float reloadTime = 3.0f;

		[SerializeField]
		private TextMeshProUGUI ammoText = null;

		[SerializeField] WeaponCollisionDetector weaponCollisionDetector;

		Player player;
		[SerializeField] GameObject crosshairGameObject;
		bool reloading = false;
		int availableMagazines = 0;
		[SerializeField] Transform bulletSpawnPoint;
		private float lastFired = 0.0f;
		bool weaponObstructed = false;

		// Reticle
		RectTransform reticleTransform;
		float currentOverallReticleAngleSpread;
		float currentReticleAngleSpreadFromMoving = 0;
		[SerializeField] float timeToReachFullMoveSpread = 0.5f;
		float angleSpreadFromShooting = 0;
		float maxAngleSpread = 15f;
		float maxReticleSize = 500;
		float minReticleSize = 100;

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
			weaponCollisionDetector.CollisionEntered += LowerWeapon;
			weaponCollisionDetector.CollisionExited += RaiseWeapon;
			healthSystem.OnDeath += OnDeath;
		}

		private void OnDisable()
		{
			weaponCollisionDetector.CollisionEntered -= LowerWeapon;
			weaponCollisionDetector.CollisionExited -= RaiseWeapon;
			healthSystem.OnDeath -= OnDeath;
		}

		void OnDeath()
		{
			// TODO Put gun away
			crosshairGameObject.SetActive(false);
		}

		void InitStartingVariables()
		{
			availableMagazines = startMagazineCount;
			lastFired = -equippedWeapon.fireRate;
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

		private void OnPlayerStateChanged()
		{
			if (weaponObstructed || player.IsClimbing || !player.moveTowardsFinished)
			{
				gunPosition = GunPosition.low;
				// TODO Put gun away
			}
			else if ((player.IsSprinting || !player.IsGrounded) && !player.IsAiming)
			{
				gunPosition = GunPosition.run;
				// TODO Put gun into running position
			}
			else if (gunPosition != GunPosition.raised)
			{
				gunPosition = GunPosition.raised;
				if (!player.IsAiming)
				{
					// TODO Put gun to default position
				}
				else
				{
					// TODO Aim down sights
				}
			}
		}

		private void Update()
		{
			if (player.ThisTarget.IsDead)
			{
				return;
			}

			if (gunPosition == GunPosition.raised)
			{
				if (player.inputManager.FireInputAction.WasPressedThisFrame()) // && not moving the gun between various positions
				{
					FireInput();
				}

				if (player.inputManager.ReloadInputAction.WasPressedThisFrame())
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
				FocusADS();
			}
			UpdateReticleSize();
			OnPlayerStateChanged();
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

		private void Fire()
		{
			if (player.IsAiming)
			{
				bulletSpawnPoint.localRotation = Quaternion.identity;
			}
			else
			{
				// Apply hip inaccuracy
				float xAngle = Random.Range(equippedWeapon.defaultHipFireAngleSpread, currentOverallReticleAngleSpread);
				float yAngle = Random.Range(equippedWeapon.defaultHipFireAngleSpread, currentOverallReticleAngleSpread);
				if (Random.Range(0, 2) == 1) { xAngle *= -1f; }
				if (Random.Range(0, 2) == 1) { yAngle *= -1f; }
				bulletSpawnPoint.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);
			}
			equippedWeapon.Fire(player.ThisTarget, bulletSpawnPoint);
			player.ThisTarget.LastTimeFired = Time.time;
			// Update ammo
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
						angleSpreadFromShooting += equippedWeapon.angleSpreadPerShot;
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
				Invoke(nameof(EndReload), reloadTime);
			}
		}

		public void AimInput(bool aimIn)
		{
			if (aimIn)
			{
				// TODO Aim down sights
				player.IsAiming = true;
			}
			else if (player.IsAiming && !aimIn)
			{
				// TODO Set the gun to default position
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
				currentReticleAngleSpreadFromMoving = Mathf.Clamp(currentReticleAngleSpreadFromMoving + equippedWeapon.maxAngleSpreadWhenMoving / timeToReachFullMoveSpread * Time.deltaTime, 0, equippedWeapon.maxAngleSpreadWhenMoving);
			}
			else
			{
				currentReticleAngleSpreadFromMoving = Mathf.Clamp(currentReticleAngleSpreadFromMoving - equippedWeapon.maxAngleSpreadWhenMoving / timeToReachFullMoveSpread * Time.deltaTime, 0, equippedWeapon.maxAngleSpreadWhenMoving);
			}

			if (angleSpreadFromShooting > 0)
			{
				angleSpreadFromShooting = Mathf.Clamp(angleSpreadFromShooting - equippedWeapon.spreadStabilityGain.Evaluate(Time.time - lastFired) * Time.deltaTime, 0, equippedWeapon.maxAngleSpreadWhenShooting);
			}

			currentOverallReticleAngleSpread = Mathf.Clamp(currentReticleAngleSpreadFromMoving + angleSpreadFromShooting, equippedWeapon.defaultHipFireAngleSpread, maxAngleSpread);
			float currentSpreadToReticleSize = Mathf.Lerp(minReticleSize, maxReticleSize, currentOverallReticleAngleSpread / maxAngleSpread);
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
