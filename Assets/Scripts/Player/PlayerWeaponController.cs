using FPSDemo.Target;
using FPSDemo.Weapons;
using UnityEngine;
using System;

namespace FPSDemo.Player
{
	public class PlayerWeaponController : MonoBehaviour
	{
		public Weapon equippedWeapon;
		public event Action OnUpdate;
		public event Action OnFire;
		public event Action OnReload;

		[SerializeField] LayerMask shotLayerMask;
		[SerializeField] int ragdollBodyLayerIndex;

		[SerializeField, Tooltip("Multiplier to apply to player speed when aiming.")]
		private float aimMultiplier = 0.4f;

		Player player;
		bool reloading = false;
		public int availableMagazines = 0;
		[SerializeField] Transform bulletSpawnPoint;
		public float lastFired = 0.0f;
		[HideInInspector] public bool weaponAtTheReady = false;
		[HideInInspector] public bool weaponReadyForReload = false;

		[HideInInspector] public float currentOverallAngleSpread;
		float currentSpreadFromMoving = 0;
		[SerializeField] float timeToReachFullMoveSpread = 0.5f;
		float angleSpreadFromShooting = 0;
		float maxAngleSpread = 15f;


		private void Awake()
		{
			player = GetComponent<Player>();
			InitStartingVariables();
		}

		void InitStartingVariables()
		{
			availableMagazines = equippedWeapon.startMagazineCount;
			lastFired = -equippedWeapon.fireRate;
		}

		

		bool ShouldFireTheGun()
		{
			// Tapping button for semi-auto, holding for full auto and gun pos needs to be either normal, or aiming
			return ((equippedWeapon.isAutomatic && player.inputManager.FireInputAction.IsPressed()) ||
				(!equippedWeapon.isAutomatic && player.inputManager.FireInputAction.WasPressedThisFrame())) &&
				(weaponAtTheReady);
		}

		private void Update()
		{
			if (player.ThisTarget.IsDead)
			{
				return;
			}

			OnUpdate.Invoke();

			if (ShouldFireTheGun())
			{
				FireInput();
			}

			if (weaponReadyForReload && player.inputManager.ReloadInputAction.WasPressedThisFrame())
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

			UpdateFiringSpread();
			FocusADS();
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
			OnFire.Invoke();
			float maxAngle = currentOverallAngleSpread;
			if (!player.IsAiming)
			{
				maxAngle += equippedWeapon.defaultHipFireAngleSpread;
			}
			float xAngle = UnityEngine.Random.Range(0, maxAngle);
			float yAngle = UnityEngine.Random.Range(0, maxAngle);
			if (UnityEngine.Random.Range(0, 2) == 1) { xAngle *= -1f; }
			if (UnityEngine.Random.Range(0, 2) == 1) { yAngle *= -1f; }
			bulletSpawnPoint.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);
			equippedWeapon.Fire(player.ThisTarget, bulletSpawnPoint, shotLayerMask, ragdollBodyLayerIndex);
			player.ThisTarget.LastTimeFired = Time.time;
			angleSpreadFromShooting += player.IsAiming ? equippedWeapon.angleSpreadPerShotADS : equippedWeapon.angleSpreadPerShot;
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

		void UpdateFiringSpread()
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
				player.IsAiming = true;
			}
			else if (player.IsAiming && !aimIn)
			{
				player.IsAiming = false;
			}
		}

		void EndReload()
		{
			reloading = false;
			equippedWeapon.currentAvailableAmmo = equippedWeapon.maxAmmoInMagazine;
			availableMagazines--;
			OnReload.Invoke();
		}
	}
}
