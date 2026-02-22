using System.Collections;
using UnityEngine;

namespace FPSDemo.Player
{
    public class CameraMovement : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [SerializeField]
        private float _standingCamHeight = 1.65f;

        [Tooltip("Mouse look sensitivity/camera move speed.")]
        [SerializeField]
        private float _mouseSensitivity = 0.2f;

        [Tooltip("Mouse look sensitivity/camera move speed in ADS.")]
        [SerializeField]
        private float _mouseSensitivityADS = 0.1f;

        [Tooltip("Maximum pitch of camera for mouselook.")]
        [SerializeField]
        private float _defaultYAngleLimit = 89f;

        [Tooltip("Maximum yaw of camera for mouselook.")]
        [SerializeField]
        private float _defaultXAngleLimit = 360f;

        [Tooltip("Smooth speed of camera angles for mouse look.")]
        [SerializeField]
        private float _smoothSpeed = 50f;

        [SerializeField]
        private float _lookTowardsSpeed = 10f;
        [SerializeField]
        private float _camDampSpeed = 0.1f;
        [SerializeField]
        private float _tiltHeadForward = 0.1f;
        [SerializeField]
        private float _tiltHeadBack = -0.15f;

        [SerializeField]
        private float _rotationLeaning = 20f;

        [SerializeField]
        float _defaultFOV = 65f;
        [SerializeField]
        float _rotationSnapThreshold = 0.1f;
        // ========================================================= PRIVATE FIELDS

        private float _targetCamYPos;
        private Coroutine _rotateTowardsRoutine;
        private Quaternion _originalRotation;
        private Vector3 _currentCamXZPos;
        private Vector3 _camXVelocity;

        private float _currentMaxXAngle;
        private Vector3 _cameraRotation;
        private Vector3 _previousCameraRotation;
        private float _currentCamYPos;
        private float _camYVelocity;
        private float _currentFOV;
        private float _targetFOV;
        private Quaternion _baseCameraRotation;

        private Camera _camera;
        private Rigidbody _rb;
        private Player _player;
        private PlayerLeaning _playerLeaning;

        // ========================================================= PROPERTIES

        public Transform CameraOffset { get; private set; }
        public bool CanRotateCamera { get; set; } = true;
        public bool RotateTowardsFinished { get; private set; } = true;
        public Vector2 CameraMovementThisFrame { get; private set; } = Vector2.zero;

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
            CameraOffset = _camera.transform.parent;
            _originalRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
            _targetCamYPos = _standingCamHeight;
            _currentCamXZPos = transform.position;
            _currentFOV = _defaultFOV;
            _camera.fieldOfView = _currentFOV;
            _currentCamYPos = _targetCamYPos + _player.transform.position.y;
            _baseCameraRotation = CameraOffset.rotation;
            _currentMaxXAngle = _defaultXAngleLimit;
            _rb = _player.GetComponent<Rigidbody>();
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
            _player.OnTeleportRotate += UpdateRotation;
        }

        void OnDisable()
        {
            _player.OnBeforeMove -= OnUpdateCameraPosition;
            _player.OnAfterMove -= OnUpdateCamera;
            _player.OnTeleport -= OnTeleportYCamPosUpdate;
            _player.OnTeleportRotate -= UpdateRotation;
        }

        // ========================================================= TICK

        private void RotateCamera()
        {
            float targetMouseSensitivity = _player.IsAiming ? _mouseSensitivityADS : _mouseSensitivity;
            _cameraRotation.x += _player.InputManager.GetLookInput().x * targetMouseSensitivity;
            _cameraRotation.y -= _player.InputManager.GetLookInput().y * targetMouseSensitivity;

            _cameraRotation.x = Mathf.Clamp(_cameraRotation.x %= 360f, -_currentMaxXAngle, _currentMaxXAngle);
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y %= 360f, -_defaultYAngleLimit, _defaultYAngleLimit);
            _cameraRotation.z = -_playerLeaning.CurrentLeanDistance * _rotationLeaning;

            CameraMovementThisFrame = new Vector2(_cameraRotation.x - _previousCameraRotation.x,
                _cameraRotation.y - _previousCameraRotation.y);

            transform.rotation = Quaternion.Lerp(transform.rotation,
                _originalRotation * Quaternion.AngleAxis(_cameraRotation.x, Vector3.up), _smoothSpeed * Time.deltaTime);

            _baseCameraRotation = Quaternion.Lerp(_baseCameraRotation,
                transform.rotation * Quaternion.AngleAxis(_cameraRotation.y, Vector3.right),
                _smoothSpeed * Time.deltaTime);
            CameraOffset.rotation = _baseCameraRotation * Quaternion.AngleAxis(_cameraRotation.z, Vector3.forward);

            _previousCameraRotation = _cameraRotation;
        }

        private void MoveCameraLean()
        {
            var targetPos = _player.transform.position;

            _currentCamXZPos = Vector3.SmoothDamp(_currentCamXZPos, targetPos, ref _camXVelocity, _camDampSpeed);
            _currentCamYPos = Mathf.SmoothDamp(_currentCamYPos, _targetCamYPos, ref _camYVelocity, _camDampSpeed);

            transform.position = new Vector3(_currentCamXZPos.x, _currentCamYPos, _currentCamXZPos.z);

            var headTiltValue = Vector3.Dot(-transform.up, _camera.transform.forward);
            var headTilt = transform.forward * Mathf.Lerp(_tiltHeadBack, _tiltHeadForward, (headTiltValue + 1f) / 2f);

            CameraOffset.position = transform.position + _player.transform.right * _playerLeaning.CurrentLeanDistance +
                                    headTilt;
        }

        // ========================================================= GETTERS

        private float GetCameraPitch()
        {
            float angleToReturn = CameraOffset.localRotation.eulerAngles.x;
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
            _rotateTowardsRoutine = StartCoroutine(RotateCameraTowards(
                Quaternion.Euler(0, targetRotation.eulerAngles.y, targetRotation.eulerAngles.z), targetCameraPitch));
        }

        // ========================================================= COROUTINES

        private IEnumerator RotateCameraTowards(Quaternion targetRot, float targetCameraPitch)
        {
            while (Quaternion.Angle(transform.rotation, targetRot) > _rotationSnapThreshold
                   || Mathf.Abs(GetCameraPitch() - targetCameraPitch) > _rotationSnapThreshold)
            {
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRot, _lookTowardsSpeed * Time.deltaTime);
                CameraOffset.localRotation = Quaternion.Slerp(CameraOffset.localRotation,
                    Quaternion.Euler(targetCameraPitch, 0, 0), Time.deltaTime * _lookTowardsSpeed);
                yield return null;
            }

            UpdateRotation(transform.rotation);
            _cameraRotation.y = GetCameraPitch();
            _baseCameraRotation = CameraOffset.rotation;
            _previousCameraRotation = _cameraRotation;
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
                _targetCamYPos = _standingCamHeight -
                                 (_player.StandingFloatingColliderHeight - _player.CurrentFloatingColliderHeight) +
                                 _player.transform.position.y;
                _rb.MoveRotation(Quaternion.Euler(Vector3.up * transform.eulerAngles.y));
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

        private void OnTeleportYCamPosUpdate(float newPosition)
        {
            _currentCamYPos = _standingCamHeight + newPosition;
            _targetCamYPos = _standingCamHeight -
                (_player.StandingFloatingColliderHeight - _player.CurrentFloatingColliderHeight) + newPosition;
            _camXVelocity = Vector3.zero;
            _camYVelocity = 0f;
            _cameraRotation.y = 0f;
            _previousCameraRotation.y = 0f;
            _currentCamXZPos = _player.transform.position;
            transform.position = new Vector3(_currentCamXZPos.x, _currentCamYPos, _currentCamXZPos.z);
        }

        private void UpdateRotation(Quaternion newRotation)
        {
            _originalRotation = newRotation;
            _cameraRotation.x = 0f;
            _cameraRotation.z = 0f;
            _previousCameraRotation = _cameraRotation;
        }
    }
}