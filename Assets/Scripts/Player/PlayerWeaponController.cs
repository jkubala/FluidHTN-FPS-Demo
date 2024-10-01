using FPSDemo.Target;
using FPSDemo.Weapons;
using UnityEngine;
using System;

namespace FPSDemo.Player
{
	public class PlayerWeaponController : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private Weapon _equippedWeapon;
		[SerializeField] private LayerMask _shotLayerMask;
		[SerializeField] private int _ragdollBodyLayerIndex;

		[Tooltip("Multiplier to apply to player speed when aiming.")]
        [SerializeField] private float _aimMultiplier = 0.4f;
        [SerializeField] private float _timeToReachFullMoveSpread = 0.5f;
        [SerializeField] private Transform _bulletSpawnPoint;
		
        [SerializeField] private Player _player;


        // ========================================================= PRIVATE FIELDS
        
        private bool _reloading = false;
		private bool _weaponAtTheReady = false;
		private bool _weaponReadyForReload = false;

		private float _currentOverallAngleSpread;
        private float _currentSpreadFromMoving = 0;
        private float _angleSpreadFromShooting = 0;
		private float _maxAngleSpread = 15f;
        private float _lastFired = 0.0f;

        private int _availableMagazines = 0;


        // ========================================================= PROPERTIES

        public Weapon EquippedWeapon => _equippedWeapon;

        public bool WeaponAtTheReady
        {
			set => _weaponAtTheReady = value;
            get => _weaponAtTheReady;
        }

        public bool WeaponReadyForReload
        {
            set => _weaponReadyForReload = value;
			get => _weaponReadyForReload;
        }

        public float CurrentOverallAngleSpread => _currentOverallAngleSpread;

        public int AvailableMagazines => _availableMagazines;

        public Action OnUpdate { get; set; }
        public Action OnFire { get; set; }
        public Action OnReload { get; set; }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponent<Player>();
            }
        }

        private void Awake()
		{
			InitStartingVariables();
		}

        private void Update()
        {
            if (_player.ThisTarget.IsDead)
            {
                return;
            }

            OnUpdate.Invoke();

            if (ShouldFireTheGun())
            {
                FireInput();
            }

            if (_weaponReadyForReload && _player.inputManager.ReloadInputAction.WasPressedThisFrame())
            {
                ReloadInput();
            }

            if (_player.IsAiming == false && _player.inputManager.AimInputAction.IsPressed())
            {
                AimInput(true);
            }
            else if (_player.IsAiming && _player.inputManager.AimInputAction.IsPressed() == false)
            {
                AimInput(false);
            }

            UpdateFiringSpread();
            FocusADS();
        }


        // ========================================================= INIT

        private void InitStartingVariables()
		{
			_availableMagazines = _equippedWeapon.startMagazineCount;
			_lastFired = -_equippedWeapon.fireRate;
		}


        // ========================================================= INPUT TRIGGERS

        public void FireInput()
        {
            if (_reloading == false)
            {
                if (_equippedWeapon.currentAvailableAmmo > 0)
                {
                    if (Time.time > _equippedWeapon.fireRate + _lastFired)
                    {
                        _lastFired = Time.time;
                        Fire();
                    }
                }
            }
        }

        public void ReloadInput()
        {
            if (_reloading == false && _availableMagazines > 0)
            {
                _reloading = true;

                // TODO: Refactor this to be handled in Tick/Update
                Invoke(nameof(EndReload), _equippedWeapon.reloadTime);
            }
        }

        public void AimInput(bool aimIn)
        {
            if (aimIn)
            {
                _player.IsAiming = true;
            }
            else if (_player.IsAiming)
            {
                _player.IsAiming = false;
            }
        }


        // ========================================================= VALIDATORS

        private bool ShouldFireTheGun()
		{
			// Tapping button for semi-auto, holding for full auto and gun pos needs to be either normal, or aiming
			return ((_equippedWeapon.isAutomatic && _player.inputManager.FireInputAction.IsPressed()) ||
				(_equippedWeapon.isAutomatic == false && _player.inputManager.FireInputAction.WasPressedThisFrame())) &&
				(_weaponAtTheReady);
		}


        // ========================================================= ACTIONS

        private void FocusADS()
		{
			if (_player.IsAiming)
			{
				_player.aimingMultiplier = _aimMultiplier;
			}
			else
			{
				_player.aimingMultiplier = 1.0f;
			}
		}

		private void Fire()
		{
			var maxAngle = _currentOverallAngleSpread;

			if (_player.IsAiming == false)
			{
				maxAngle += _equippedWeapon.defaultHipFireAngleSpread;
			}

			var xAngle = UnityEngine.Random.Range(0, maxAngle);
			var yAngle = UnityEngine.Random.Range(0, maxAngle);

            if (UnityEngine.Random.Range(0, 2) == 1)
            {
                xAngle *= -1f;
            }

            if (UnityEngine.Random.Range(0, 2) == 1)
            {
                yAngle *= -1f;
            }

			_bulletSpawnPoint.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);
			_equippedWeapon.Fire(_player.ThisTarget, _bulletSpawnPoint, _shotLayerMask, _ragdollBodyLayerIndex);
			_player.ThisTarget.LastTimeFired = Time.time;
			_angleSpreadFromShooting += _player.IsAiming ? _equippedWeapon.angleSpreadPerShotADS : _equippedWeapon.angleSpreadPerShot;

			OnFire?.Invoke();
		}

        private void EndReload()
        {
            _reloading = false;
            _equippedWeapon.currentAvailableAmmo = _equippedWeapon.maxAmmoInMagazine;
            _availableMagazines--;

            OnReload?.Invoke();
        }


        // ========================================================= TICK

        private void UpdateFiringSpread()
		{
			if (_player.IsMoving())
			{
				_currentSpreadFromMoving = Mathf.Clamp(_currentSpreadFromMoving + _equippedWeapon.maxAngleSpreadWhenMoving / _timeToReachFullMoveSpread * Time.deltaTime, 0, _equippedWeapon.maxAngleSpreadWhenMoving);
			}
			else
			{
				_currentSpreadFromMoving = Mathf.Clamp(_currentSpreadFromMoving - _equippedWeapon.maxAngleSpreadWhenMoving / _timeToReachFullMoveSpread * Time.deltaTime, 0, _equippedWeapon.maxAngleSpreadWhenMoving);
			}

			if (_angleSpreadFromShooting > 0)
			{
				var targetMaxSpreadFromShooting = _player.IsAiming ? _equippedWeapon.maxAngleSpreadWhenShootingADS : _equippedWeapon.maxAngleSpreadWhenShooting;
				_angleSpreadFromShooting = Mathf.Clamp(_angleSpreadFromShooting - _equippedWeapon.spreadStabilityGain.Evaluate(Time.time - _lastFired) * Time.deltaTime, 0, targetMaxSpreadFromShooting);
			}

			_currentOverallAngleSpread = Mathf.Clamp(_currentSpreadFromMoving + _currentSpreadFromMoving + _angleSpreadFromShooting, 0, _maxAngleSpread);
		}
	}
}
