using System.Collections;
using UnityEngine;

namespace FPSDemo.Player
{
	public class CameraMovement : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private float _standingCamHeight = 1.65f;
		[SerializeField] private float _targetCamYPos = 1.65f;

		[Tooltip("Mouse look sensitivity/camera move speed.")]
		[SerializeField] private float _mouseSensitivity = 0.2f;

		[Tooltip("Mouse look sensitivity/camera move speed in ADS.")]
		[SerializeField] private float _mouseSensitivityADS = 0.1f;

		[Tooltip("Maximum pitch of camera for mouselook.")]
		[SerializeField] private float _defaultYAngleLimit = 89f;

		[Tooltip("Maximum yaw of camera for mouselook.")]
		[SerializeField] private float _defaultXAngleLimit = 360f;

		[Tooltip("Smooth speed of camera angles for mouse look.")]
		[SerializeField] private float _smoothSpeed = 50f;

		[SerializeField] private float _lookTowardsSpeed = 50f;
		[SerializeField] private float _camLeanMoveTime = 0.075f;
		[SerializeField] private float _camDampSpeed = 0.1f;
		[SerializeField] private float _tiltHeadForward = 0.1f;
		[SerializeField] private float _tiltHeadBack = -0.15f;

        [SerializeField] float _cameraDefaultFOV = 65f;

        [SerializeField] private Camera _camera;
        [SerializeField] private Player _player;
        [SerializeField] private PlayerLeaning _playerLeaning;


        // ========================================================= PRIVATE FIELDS

        private Coroutine _rotateTowardsRoutine = null;
		private Quaternion _originalRotation;
		private Transform _cameraOffsetPoint;
		private Vector3 _currentCamXZPos;
        private Vector3 _camXVelocity;
        private Vector2 _cameraMovementLastFrame = Vector2.zero;

        private float _currentMaxXAngle = 360f;
        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private float _rotationZ = 0f;
        private float _currentCamYPos;
		private float _camYVelocity;
		private float _targetFOV;
        private float _targetMouseSensitivity;

        // ========================================================= PROPERTIES

        public Transform CameraBase { get; private set; }
        public bool CanRotateCamera { get; set; } = true;
        public bool RotateTowardsFinished { get; private set; } = true;
        public Vector2 CameraMovementThisFrame { get; private set; } = Vector2.zero;
		public float CurrentFOV { get; set; } = 45f;
        public float NormalFOV { get { return _cameraDefaultFOV; } private set { _cameraDefaultFOV = value; } }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<Player>();
            }

            if (_playerLeaning == null)
            {
                _playerLeaning = GetComponentInParent<PlayerLeaning>();
            }

            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }
        }

        void Awake()
		{
			CameraBase = _camera.transform.parent;
			_cameraOffsetPoint = CameraBase.transform.parent;
			_originalRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
			_targetCamYPos = _standingCamHeight;
			_currentCamXZPos = transform.position;
			CurrentFOV = NormalFOV;
			_camera.fieldOfView = CurrentFOV;
			_targetMouseSensitivity = _mouseSensitivity;
			_currentCamYPos = _targetCamYPos + _player.transform.position.y;
		}

		void Start()
		{
			transform.SetParent(null);
			transform.position = new Vector3(transform.position.x, _currentCamYPos, transform.position.z);
		}

		void OnEnable()
		{
			_player.OnBeforeMove += OnUpdateCameraPosition;
			_player.OnAfterMove += OnUpdateCamera;
			_player.OnTeleport += OnTeleportYCamPosUpdate;
			_player.OnTeleportRotate += OnUpdateRotation;
		}

		void OnDisable()
		{
			_player.OnBeforeMove -= OnUpdateCameraPosition;
			_player.OnAfterMove -= OnUpdateCamera;
			_player.OnTeleport -= OnTeleportYCamPosUpdate;
			_player.OnTeleportRotate -= OnUpdateRotation;
		}


        // ========================================================= TICK

        

        private void RotateCamera()
		{
			_targetMouseSensitivity = _player.IsAiming ? _mouseSensitivityADS : _mouseSensitivity;
			_rotationX += _player.inputManager.GetLookInput().x * _targetMouseSensitivity;
			_rotationY -= _player.inputManager.GetLookInput().y * _targetMouseSensitivity;

			_rotationX = Mathf.Clamp(_rotationX %= 360f, -_currentMaxXAngle, _currentMaxXAngle);
			_rotationY = Mathf.Clamp(_rotationY %= 360f, -_defaultYAngleLimit, _defaultYAngleLimit);
			_rotationZ = -_playerLeaning.LeanPos * _playerLeaning.RotationLeanAmt;

			CameraMovementThisFrame = new Vector2(_rotationX - _cameraMovementLastFrame.x, _rotationY - _cameraMovementLastFrame.y);
			
            transform.rotation = Quaternion.Lerp(transform.rotation, _originalRotation * Quaternion.AngleAxis(_rotationX, Vector3.up), _smoothSpeed * Time.deltaTime);
			
            CameraBase.transform.rotation = Quaternion.Lerp(CameraBase.transform.rotation, transform.rotation * Quaternion.AngleAxis(_rotationY, Vector3.right) * Quaternion.AngleAxis(_rotationZ, Vector3.forward), _smoothSpeed * Time.deltaTime);
			
            _cameraMovementLastFrame.x = _rotationX;
			_cameraMovementLastFrame.y = _rotationY;
		}

		private void MoveCameraLean()
		{
			var targetPos = _player.transform.position;

			_currentCamXZPos = Vector3.SmoothDamp(_currentCamXZPos, targetPos, ref _camXVelocity, _camLeanMoveTime);
			_currentCamYPos = Mathf.SmoothDamp(_currentCamYPos, _targetCamYPos, ref _camYVelocity, _camDampSpeed);

			transform.position = new Vector3(_currentCamXZPos.x, _currentCamYPos, _currentCamXZPos.z);

			var headTiltValue = Vector3.Dot(-transform.up, _camera.transform.forward);
			var headTilt = transform.forward * Mathf.Lerp(_tiltHeadBack, _tiltHeadForward, (headTiltValue + 1f) / 2f);

			CameraBase.transform.position = _cameraOffsetPoint.position + _player.transform.right * _playerLeaning.LeanPos + headTilt;
		}


        // ========================================================= GETTERS

        public float GetCameraPitch()
        {
            float angleToReturn = CameraBase.localRotation.eulerAngles.x;
            if (angleToReturn > 180)
            {
                angleToReturn -= 360;
            }
            return angleToReturn;
        }


        // ========================================================= PUBLIC METHODS

        public void ResetXAngle()
		{
			_currentMaxXAngle = _defaultXAngleLimit;
		}

		public void ClampXAngle(float angle)
		{
			_currentMaxXAngle = angle;
		}


		public void StartRotatingCameraTowards(Quaternion targetRotation, float targetCameraPitch)
		{
			if (_rotateTowardsRoutine != null)
			{
				StopCoroutine(_rotateTowardsRoutine);
			}
			CanRotateCamera = false;
			RotateTowardsFinished = false;
			Vector3 modifiedRotation = targetRotation.eulerAngles;
			modifiedRotation.x = 0;
			_rotateTowardsRoutine = StartCoroutine(RotateCameraTowards(Quaternion.Euler(modifiedRotation), targetCameraPitch));
		}


        // ========================================================= COROUTINES

        private IEnumerator RotateCameraTowards(Quaternion targetRot, float targetCameraPitch)
		{
			while (targetRot != transform.rotation || !Mathf.Approximately(GetCameraPitch(), targetCameraPitch))
			{
				CameraBase.localRotation = Quaternion.Euler(new Vector3(Mathf.MoveTowards(GetCameraPitch(), targetCameraPitch, Time.deltaTime * _lookTowardsSpeed), 0, 0));
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * _lookTowardsSpeed);
				yield return null;
			}
			_originalRotation = targetRot;
			UpdateRotation(transform.rotation);
			UpdateCameraPitch(GetCameraPitch());
			_cameraMovementLastFrame = new Vector2(_rotationX, _rotationY);
			RotateTowardsFinished = true;
		}


        // ========================================================= PLAYER CALLBACKS

        private void OnUpdateCameraPosition()
        {
            if (_player.ThisTarget.IsDead)
            {
                // Set it to the middle of the collider
                _targetCamYPos = _player.CurrentFloatingColliderHeight / 2 + _player.transform.position.y;
            }
            else
            {
                _targetCamYPos = _standingCamHeight - (_player.standingFloatingColliderHeight - _player.CurrentFloatingColliderHeight) + _player.transform.position.y;
                _player.rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * transform.eulerAngles.y));
            }
        }

        private void OnUpdateCamera()
        {
            if (CanRotateCamera)
            {
                RotateCamera();
            }

            MoveCameraLean();
        }

        private void OnUpdateRotation(Quaternion newRotation)
        {
            UpdateRotation(newRotation);

        }

        private void OnTeleportYCamPosUpdate(float newPosition)
		{
			_currentCamYPos = _standingCamHeight + newPosition;
			_targetCamYPos = _standingCamHeight - (_player.standingFloatingColliderHeight - _player.CurrentFloatingColliderHeight) + newPosition;
			_camYVelocity = 0f;
			UpdateCameraPitch(0);
			_currentCamXZPos = _player.transform.position;
			transform.position = new Vector3(_currentCamXZPos.x, _currentCamYPos, _currentCamXZPos.z);
		}


        // ========================================================= SIMULATION

        private void UpdateRotation(Quaternion newRotation)
        {
            _originalRotation = newRotation;
            _rotationX = newRotation.x;
            _rotationZ = newRotation.z;
        }

        private void UpdateCameraPitch(float angle)
		{
			_rotationY = angle;
		}
	}
}
