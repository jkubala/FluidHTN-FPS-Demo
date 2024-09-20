using FPSDemo.Target;
using FPSDemo.Weapons;
using TMPro;
using UnityEngine;

namespace FPSDemo.Player
{

	public class WeaponUI : MonoBehaviour
	{
		[SerializeField] GameObject crosshairGameObject;
		PlayerWeaponController weaponController;
		Player player;
		HealthSystem healthSystem;
		Weapon equippedWeapon;
		RectTransform reticleTransform;
		float maxAngleSpread = 15f;
		float maxReticleSize = 500;
		float minReticleSize = 100;

		[SerializeField] TextMeshProUGUI ammoText = null;

		void Awake()
		{
			player = GetComponent<Player>();
			weaponController = GetComponent<PlayerWeaponController>();
			healthSystem = GetComponent<HealthSystem>();
			equippedWeapon = weaponController.equippedWeapon;
			reticleTransform = crosshairGameObject.GetComponent<RectTransform>();
		}

		void Start()
		{
			UpdateAmmoText();
		}


		private void OnEnable()
		{
			healthSystem.OnDeath += OnDeath;
			weaponController.OnUpdate += UpdateCrosshair;
			weaponController.OnFire += UpdateAmmoText;
			weaponController.OnReload += UpdateAmmoText;
		}

		private void OnDisable()
		{
			healthSystem.OnDeath -= OnDeath;
			weaponController.OnUpdate -= UpdateCrosshair;
			weaponController.OnFire -= UpdateAmmoText;
			weaponController.OnReload -= UpdateAmmoText;
		}

		void OnDeath()
		{
			SetCrosshairVisibility(false);
		}

		void UpdateCrosshair()
		{
			UpdateReticleSize(weaponController.currentOverallAngleSpread);
			SetCrosshairVisibility(!player.IsAiming);
		}

		void UpdateReticleSize(float currentAngleSpread)
		{
			float currentSpreadToReticleSize = Mathf.Lerp(minReticleSize, maxReticleSize, currentAngleSpread / maxAngleSpread);
			reticleTransform.sizeDelta = new Vector2(currentSpreadToReticleSize, currentSpreadToReticleSize);
		}

		void UpdateAmmoText()
		{
			if (ammoText != null)
			{
				ammoText.text = equippedWeapon.currentAvailableAmmo.ToString() + " / " + (weaponController.availableMagazines * equippedWeapon.maxAmmoInMagazine).ToString();
			}
		}

		void SetCrosshairVisibility(bool visible)
		{
			crosshairGameObject.SetActive(visible);
		}
	}
}