using FPSDemo.Target;
using FPSDemo.Weapons;
using TMPro;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace FPSDemo.Player
{

	public class WeaponUI : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private RectTransform _reticleTransform;
        [SerializeField] private TextMeshProUGUI _ammoText = null;

		[Header("Dependencies")]
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private Player _player;
        [SerializeField] private HealthSystem _healthSystem;


        // ========================================================= PRIVATE FIELDS

        private Weapon _equippedWeapon;
		private float _maxAngleSpread = 15f;
		private float _maxReticleSize = 500;
		private float _minReticleSize = 100;


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_weaponController == null)
            {
                _weaponController = GetComponent<PlayerWeaponController>();
            }

            if (_player == null)
            {
                _player = GetComponent<Player>();
            }

            if (_healthSystem == null)
            {
                _healthSystem = GetComponent<HealthSystem>();
            }
        }

        void Awake()
		{
			_equippedWeapon = _weaponController.EquippedWeapon;
		}

		void Start()
		{
			OnUpdateAmmoText();
		}

		private void OnEnable()
		{
			_healthSystem.OnDeath += OnDeath;
			_weaponController.OnUpdate += OnUpdateCrosshair;
			_weaponController.OnFire += OnUpdateAmmoText;
			_weaponController.OnReload += OnUpdateAmmoText;
		}

		private void OnDisable()
		{
			_healthSystem.OnDeath -= OnDeath;
			_weaponController.OnUpdate -= OnUpdateCrosshair;
			_weaponController.OnFire -= OnUpdateAmmoText;
			_weaponController.OnReload -= OnUpdateAmmoText;
		}


        // ========================================================= CALLBACKS

        private void OnDeath()
		{
			SetCrosshairVisibility(false);
		}

        private void OnUpdateCrosshair()
		{
			UpdateReticleSize(_weaponController.CurrentOverallAngleSpread);
			SetCrosshairVisibility(_player.IsAiming == false);
		}

        private void OnUpdateAmmoText()
        {
            if (_ammoText != null)
            {
				_ammoText.text = $"{_equippedWeapon.currentAvailableAmmo} / {(_weaponController.AvailableMagazines * _equippedWeapon.maxAmmoInMagazine)}";
            }
        }


        // ========================================================= TICK

        private void UpdateReticleSize(float currentAngleSpread)
		{
			var currentSpreadToReticleSize = Mathf.Lerp(_minReticleSize, _maxReticleSize, currentAngleSpread / _maxAngleSpread);

			_reticleTransform.sizeDelta = new Vector2(currentSpreadToReticleSize, currentSpreadToReticleSize);
		}


        // ========================================================= SETTERS

        private void SetCrosshairVisibility(bool visible)
		{
			_reticleTransform.gameObject.SetActive(visible);
		}
	}
}