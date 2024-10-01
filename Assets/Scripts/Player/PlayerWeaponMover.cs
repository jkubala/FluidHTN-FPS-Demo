using FPSDemo.Target;
using FPSDemo.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace FPSDemo.Player
{
	public class PlayerWeaponMover : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

		[SerializeField] private float _weaponPosChangeSpeed = 3f;
		[SerializeField] private float _weaponRotChangeSpeed = 3f;

		[Header("Weapon Sway")]
		[SerializeField] private Vector3 _lookSway = Vector3.zero;
		[SerializeField] private Vector3 _movementSway = Vector3.zero;
		[SerializeField] private float _maxSway = 4f;
		[SerializeField] private float _swaySmoothness = 0.02f;

		[Header("Bob Motion")]
		[SerializeField] private Vector3 _crouchAimBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] private Vector3 _crouchAimBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] private Vector3 _aimBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] private Vector3 _aimBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] private Vector3 _crouchBobAmount = new Vector3(1f, 1f, 0f);
		[SerializeField] private Vector3 _crouchBobSpeed = new Vector3(8f, 4f, 0f);
		[Space(10)]
		[SerializeField] private Vector3 _walkBobAmount = new Vector3(2f, 2f, 0f);
		[SerializeField] private Vector3 _walkBobSpeed = new Vector3(16f, 8f, 0f);
		[Space(10)]
		[SerializeField] private Vector3 _runBobAmount = new Vector3(5f, 5f, 1f);
		[SerializeField] private Vector3 _runBobSpeed = new Vector3(18f, 7f, 7f);
		[Space(10)]
		[SerializeField] private float _bobDamp = 0.1f;
		[SerializeField] private float _resetSpeed = 6.0f;
		

		[Header("Breath Sway")]
		[SerializeField] private Vector3 _breathSwayAmount = Vector3.zero;
		[SerializeField] private Vector3 _breathSwaySpeed = Vector3.zero;

        [Header("Dependencies")]
        [SerializeField] private CameraMovement _cameraMovement;
        [SerializeField] private Player _player;
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private HealthSystem _healthSystem;


        // ========================================================= PRIVATE FIELDS

        private GunPosition _gunPosition = GunPosition.normal;

        private Vector3 _currentWeaponPos;
        private Vector3 _currentWeaponRot;
        private Vector3 _targetWeaponPos;
        private Vector3 _targetWeaponRot;
        private bool _weaponObstructed;

		// recoil
        private Vector3 _currentRecoilRotation = Vector3.zero;
        private Vector3 _targetRecoilRotation = Vector3.zero;

		// weapon sway
        private Vector3 _currentSway;
        private Vector3 _weaponSway = Vector3.zero;
        private Vector3 _smoothedWeaponSway = Vector3.zero;

		// bob
        private Vector3 _targetBobAmount;
        private Vector3 _targetBobSpeed;
        private Vector3 _finalBob = Vector3.zero;
        private Vector3 _bobVelocity = Vector3.zero;
        private Vector3 _bobSmoothed = Vector3.zero;

        // breath sway
        private Vector3 _targetBreathSway = Vector3.zero;
        private Vector3 _targetBreathSwaySmoothed = Vector3.zero;


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

		void Start()
		{
			_targetWeaponPos = _weaponController.EquippedWeapon.normalPos;
			_targetWeaponRot = _weaponController.EquippedWeapon.normalRot;
			_currentWeaponPos = _targetWeaponPos;
			_currentWeaponRot = _targetWeaponRot;
		}

		private void OnEnable()
		{
			_weaponController.EquippedWeapon.weaponCollisionDetector.CollisionEntered += OnLowerWeapon;
			_weaponController.EquippedWeapon.weaponCollisionDetector.CollisionExited += OnRaiseWeapon;
			_weaponController.OnUpdate += OnMoveWeapon;
			_weaponController.OnFire += OnAddRecoilToTheTargetRotation;
			_healthSystem.OnDeath += OnDeath;
		}

		private void OnDisable()
		{
			_weaponController.EquippedWeapon.weaponCollisionDetector.CollisionEntered -= OnLowerWeapon;
			_weaponController.EquippedWeapon.weaponCollisionDetector.CollisionExited -= OnRaiseWeapon;
			_weaponController.OnUpdate -= OnMoveWeapon;
			_weaponController.OnFire -= OnAddRecoilToTheTargetRotation;
			_healthSystem.OnDeath -= OnDeath;
		}


        // ========================================================= CALLBACKS

        private void OnLowerWeapon()
		{
			_weaponObstructed = true;
		}

		private void OnRaiseWeapon()
		{
			_weaponObstructed = false;
		}

		private void OnMoveWeapon()
		{
			UpdateWeaponPosition();
			UpdateThePositionOfCollisionDetector();
			UpdateWeaponSway();
			WeaponBreathingSway();
		}

        private void OnAddRecoilToTheTargetRotation()
        {
            Vector3 recoilToAdd = _player.IsAiming ? _weaponController.EquippedWeapon.adsRecoil : _weaponController.EquippedWeapon.recoil;
            _targetRecoilRotation += new Vector3(recoilToAdd.x, UnityEngine.Random.Range(recoilToAdd.y, -recoilToAdd.y), UnityEngine.Random.Range(recoilToAdd.z, -recoilToAdd.z));
        }

        private void OnDeath()
		{
			ChangeGunPos(GunPosition.away);
		}

		private void ChangeGunPos(GunPosition newGunPos)
		{
			_gunPosition = newGunPos;
			_weaponController.WeaponReadyForReload = false;
			_weaponController.WeaponAtTheReady = false;
			switch (newGunPos)
			{
				case GunPosition.normal:
					_targetWeaponPos = _weaponController.EquippedWeapon.normalPos;
					_targetWeaponRot = _weaponController.EquippedWeapon.normalRot;
					_weaponController.WeaponReadyForReload = true;
					_weaponController.WeaponAtTheReady = true;
					break;
				case GunPosition.aiming:
					_targetWeaponPos = _weaponController.EquippedWeapon.ADSPos;
					_targetWeaponRot = _weaponController.EquippedWeapon.ADSRot;
					_weaponController.WeaponAtTheReady = true;
					break;
				case GunPosition.run:
					_targetWeaponPos = _weaponController.EquippedWeapon.runPos;
					_targetWeaponRot = _weaponController.EquippedWeapon.runRot;
					break;
				case GunPosition.away:
					_targetWeaponPos = _weaponController.EquippedWeapon.awayPos;
					_targetWeaponRot = _weaponController.EquippedWeapon.awayRot;
					break;
			}
		}


        // ========================================================= TICK

        void UpdateThePositionOfCollisionDetector()
		{
			if (_player.IsAiming)
			{
				_weaponController.EquippedWeapon.weaponCollisionDetector.transform.localPosition = _weaponController.EquippedWeapon.ADSPos;
			}
			else
			{
				_weaponController.EquippedWeapon.weaponCollisionDetector.transform.localPosition = _weaponController.EquippedWeapon.normalPos;
			}
		}

		void UpdateWeaponPosition()
		{
			if (_weaponObstructed || _player.IsClimbing || _player.MoveTowardsFinished == false)
			{
				ChangeGunPos(GunPosition.away);
			}
			else if ((_player.IsSprinting || _player.IsGrounded == false) && _player.IsAiming == false)
			{
				ChangeGunPos(GunPosition.run);
			}
			else if (_gunPosition != GunPosition.normal || _gunPosition != GunPosition.aiming)
			{
				if (_player.IsAiming)
				{
                    ChangeGunPos(GunPosition.aiming);
				}
				else
				{
                    ChangeGunPos(GunPosition.normal);
                }
			}

			_targetRecoilRotation = Vector3.Lerp(_targetRecoilRotation, Vector3.zero, _weaponController.EquippedWeapon.recoilRecoverSpeed * Time.deltaTime);
			_currentRecoilRotation = Vector3.Slerp(_currentRecoilRotation, _targetRecoilRotation, _weaponController.EquippedWeapon.blowbackForce * Time.deltaTime);

			_currentWeaponPos = Vector3.Lerp(_currentWeaponPos, _targetWeaponPos, _weaponPosChangeSpeed * Time.deltaTime);
			_currentWeaponRot = Vector3.Slerp(_currentWeaponRot, _targetWeaponRot, _weaponRotChangeSpeed * Time.deltaTime);

			_weaponController.EquippedWeapon.transform.SetLocalPositionAndRotation(_currentWeaponPos, Quaternion.Euler(_currentWeaponRot + _currentRecoilRotation + _smoothedWeaponSway + BobbingCalculation() + _targetBreathSwaySmoothed));
		}

		void UpdateWeaponSway()
		{
			var moveInput = _player.InputManager.GetMovementInput();
			var lookInput = _cameraMovement.CameraMovementThisFrame;

			_weaponSway.x = lookInput.y * _lookSway.x + moveInput.y * _movementSway.x;
			_weaponSway.y = lookInput.x * _lookSway.y + moveInput.x * _movementSway.y;
			_weaponSway.z = lookInput.x * _lookSway.z - moveInput.x * _movementSway.z;
			_weaponSway = Vector3.ClampMagnitude(_weaponSway, _maxSway);

			_smoothedWeaponSway = Vector3.SmoothDamp(_smoothedWeaponSway, _weaponSway, ref _currentSway, _swaySmoothness);
		}

        void WeaponBreathingSway()
        {
            if (_player.IsAiming)
            {
                if (_targetBreathSway != Vector3.zero)
                {
                    _targetBreathSway = Vector3.MoveTowards(_targetBreathSway, Vector3.zero, Time.deltaTime * _resetSpeed);
                }
            }
            else
            {
                _targetBreathSway.x = Mathf.Sin(Time.time * _breathSwaySpeed.x) * _breathSwayAmount.x;
                _targetBreathSway.y = Mathf.Sin(Time.time * _breathSwaySpeed.y) * _breathSwayAmount.y;
                _targetBreathSway.z = Mathf.Sin(Time.time * _breathSwaySpeed.z) * _breathSwayAmount.z;
            }
            _targetBreathSwaySmoothed = Vector3.Lerp(_targetBreathSwaySmoothed, _targetBreathSway, Time.deltaTime);
        }


        // ========================================================= CALCULATIONS

        public Vector3 BobbingCalculation()
		{
			if (_player.IsMoving() == false || _player.IsGrounded == false)
			{
				_bobSmoothed = Vector3.Lerp(_bobSmoothed, Vector3.zero, Time.deltaTime * _resetSpeed);
			}
			else
			{
				SetTargetBobAmountAndSpeed();

				_finalBob = new Vector3(
					Mathf.Sin(Time.time * _targetBobSpeed.x) * _targetBobAmount.x,
					Mathf.Sin(Time.time * _targetBobSpeed.y) * _targetBobAmount.y,
					Mathf.Sin(Time.time * _targetBobSpeed.z) * _targetBobAmount.z);

				_bobSmoothed = Vector3.SmoothDamp(_bobSmoothed, _finalBob, ref _bobVelocity, _bobDamp);
			}
			return _bobSmoothed;
		}


        // ========================================================= SETTERS

        void SetTargetBobAmountAndSpeed()
		{
			if (_player.IsSprinting)
			{
				_targetBobAmount = _runBobAmount;
				_targetBobSpeed = _runBobSpeed;
			}
			else if (_player.IsMoving())
			{
				if (_player.IsAiming)
				{
					if (_player.IsCrouching)
					{
						_targetBobAmount = _crouchAimBobAmount;
						_targetBobSpeed = _crouchAimBobSpeed;
					}
					else
					{
						_targetBobAmount = _aimBobAmount;
						_targetBobSpeed = _aimBobSpeed;
					}
				}
				else if (_player.IsCrouching)
				{
					_targetBobAmount = _crouchBobAmount;
					_targetBobSpeed = _crouchBobSpeed;
				}
				else
				{
					_targetBobAmount = _walkBobAmount;
					_targetBobSpeed = _walkBobSpeed;
				}
			}
		}


        // ========================================================= PRIVATE ENUMS

        private enum GunPosition { away, aiming, run, normal }
    }
}
