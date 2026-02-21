using UnityEngine;

namespace FPSDemo.Player
{
    public class PlayerLeaning : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [Header("Leaning")]
        [Tooltip("Max distance left or right the player can lean when adjusting stance.")]
        [SerializeField] private float _maxLeanDistance = 0.75f;
        
        [Tooltip(("Factor of how much the player can lean while using quick lean (Q or E)."))]
        [SerializeField] private float _leanDistanceMobileFactor = 0.4f;

        [Tooltip("Factor of how much the player can lean while standing.")]
        [SerializeField] private float _standLeanFactor = 1f;
        
        [Tooltip("Factor of how much the player can lean while crouching.")]
        [SerializeField] private float _crouchLeanAmt = 0.75f;

        [SerializeField] private float _leaningSensitivity = 0.01f;
        
        [Header("Spherecast check parameters")]
        [SerializeField] private float _bufferDistanceFromWall = 0.4f;
        [SerializeField] private float _originOffsetDistance = 0.05f;
        [SerializeField] private float _playerRadiusPercentage = 0.6f;
        
        // ========================================================= PRIVATE FIELDS
        
        private float _currentLeanVelocity;
        private Player _player;
        private float _targetLeanDistance;

        // ========================================================= PROPERTIES

        public float CurrentLeanDistance { get; private set; }

        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponent<Player>();
            }
        }

        void OnEnable()
        {
            _player.OnBeforeMove += OnBeforeMove;
        }

        void OnDisable()
        {
            _player.OnBeforeMove -= OnBeforeMove;
        }
        
        // ========================================================= CALLBACKS

        void OnBeforeMove()
        {
            _targetLeanDistance = CalculateCurrentLeanAmt();

            // Smooth current lean to target lean
            CurrentLeanDistance = Mathf.SmoothDamp(CurrentLeanDistance, _targetLeanDistance, ref _currentLeanVelocity, 0.1f, Mathf.Infinity,
                Time.deltaTime);
        }

        float CalculateCurrentLeanAmt()
        {
            if (_player.IsSprinting || !_player.IsGrounded)
            {
                return 0f;
            }

            // Quick lean
            int quickLeanDirection = _player.InputManager.ShouldQuickLean;
            if (quickLeanDirection != 0)
            {
                return CurrentLeanFromQuickLean(quickLeanDirection);
            }

            // Adjust stance lean
            float changeStanceLean = -_player.InputManager.ChangeStanceAmount.x;
            if (changeStanceLean != 0 && Mathf.Abs(_player.InputManager.GetMovementInput().y) == 0f)
            {
                return CurrentLeanFromAdjustStanceLean(changeStanceLean);
            }

            // Maintain lean if adjusting
            if (_player.InputManager.AdjustStanceInputAction.IsPressed())
            {
                return _targetLeanDistance;
            }

            // Otherwise reset lean
            return 0f;
        }

        private float CurrentLeanFromAdjustStanceLean(float changeStanceLean)
        {
            // Max distance player can lean based on current stance
            float maxDistanceToLean = _maxLeanDistance *
                                      Mathf.Lerp(_standLeanFactor, _crouchLeanAmt, _player.CrouchPercentage);
            // Clamped distance based on any obstacles in the way
            float clampedDistanceToLeanLeft = GetMaxDistanceInDirection(-1, maxDistanceToLean);
            float clampedDistanceToLeanRight = GetMaxDistanceInDirection(1, maxDistanceToLean);
            return Mathf.Clamp(CurrentLeanDistance + changeStanceLean * _leaningSensitivity, -clampedDistanceToLeanLeft, clampedDistanceToLeanRight);
        }

        private float CurrentLeanFromQuickLean(int quickLeanDirection)
        {
            float distanceToLean = _maxLeanDistance * _leanDistanceMobileFactor *
                                   Mathf.Lerp(_standLeanFactor, _crouchLeanAmt, _player.CrouchPercentage);
            return GetMaxDistanceInDirection(quickLeanDirection, distanceToLean) * quickLeanDirection;
        }

        private float GetMaxDistanceInDirection(int leanDirection, float distanceToLean)
        {
            var origin = transform.position
                         + Vector3.up * (_player.CurrentFloatingColliderHeight + _player.DistanceToFloat -
                                         _player.Capsule.radius)
                         + transform.right *
                         (-leanDirection *
                          _originOffsetDistance); // Offset a bit to the opposite side so that the cast does not miss a wall the player is touching

            if (Physics.SphereCast(origin, _player.Capsule.radius * _playerRadiusPercentage,
                    transform.right * leanDirection, out RaycastHit hit, distanceToLean + _bufferDistanceFromWall,
                    _player.GroundMask.value))
            {
                return Vector3.Distance(hit.point, origin) - _bufferDistanceFromWall;
            }

            return distanceToLean;
        }
    }
}