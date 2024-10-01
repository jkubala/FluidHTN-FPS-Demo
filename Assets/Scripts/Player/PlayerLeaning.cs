using UnityEngine;

namespace FPSDemo.Player
{
    public class PlayerLeaning : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [Header("Leaning")]
        [Tooltip("Distance left or right the player can lean.")]
        [SerializeField] private float _leanDistance = 0.75f;

        [Tooltip("Pecentage the player can lean while standing.")]
        [SerializeField] private float _standLeanAmt = 1f;

        [Tooltip("Pecentage the player can lean while crouching.")]
        [SerializeField] private float _crouchLeanAmt = 0.75f;

        [SerializeField] private float _rotationLeanAmt = 20f;

        [SerializeField] private Player _player;


        // ========================================================= PRIVATE FIELDS

        private float _leanFactorAmt = 1f;
        private float _currentLeanVelocity;
        private Vector3 _leanCheckPos;


        // ========================================================= PROPERTIES

        public float RotationLeanAmt => _rotationLeanAmt;
        public float LeanAmt { get; set; } = 0f;
        public float LeanPos { get; set; }
        public bool IsLeaning { get; set; }


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
            LeanAmt = 0f;
            IsLeaning = false;
            if (_player.IsSprinting == false && 
                _player.IsGrounded && 
                Mathf.Abs(_player.inputManager.GetMovementInput().y) < 0.2f &&
                (_player.inputManager.LeanLeftInputAction.IsPressed() || 
                 _player.inputManager.LeanRightInputAction.IsPressed()))
            {
                int direction;

                // lean left
                if (_player.inputManager.LeanLeftInputAction.IsPressed() && 
                    _player.inputManager.LeanRightInputAction.IsPressed() == false) 
                {
                    direction = -1;
                }
                // lean right
                else if (_player.inputManager.LeanRightInputAction.IsPressed() && 
                         _player.inputManager.LeanLeftInputAction.IsPressed() == false) 
                {
                    direction = 1;
                }
                else
                {
                    return;
                }

                _leanCheckPos = transform.position + Vector3.up * (_player.CurrentFloatingColliderHeight + _player.DistanceToFloat - _player.Capsule.radius);
                _leanFactorAmt = Mathf.Lerp(_standLeanAmt, _crouchLeanAmt, _player.CrouchPercentage);

                // Offset a bit to the opposite side so that the cast does not miss a wall the player is touching
                var offset = -direction * 0.05f * transform.right;
                var origin = _leanCheckPos + offset;
                var radius = _player.Capsule.radius * 0.6f;
                var dir = transform.right * direction;
                var maxDistance = _leanDistance * _leanFactorAmt;
                var layerMask = _player.GroundMask.value;

                if (Physics.SphereCast(origin, radius, dir, out _, maxDistance, layerMask) == false)
                {
                    LeanAmt = _leanDistance * _leanFactorAmt * direction;
                    IsLeaning = true;
                }
            }

            // smooth position between leanAmt values
            LeanPos = Mathf.SmoothDamp(LeanPos, LeanAmt, ref _currentLeanVelocity, 0.1f, Mathf.Infinity, Time.deltaTime);
        }
    }
}
