using System;
using UnityEngine;

namespace FPSDemo.Player
{
    public class PlayerJumping : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [Tooltip("Vertical speed of player jump.")]
        [SerializeField] private float _jumpSpeed = 4f;

        [Tooltip("Time in seconds allowed between player jumps.")]
        [SerializeField] private float _delayBetweenJumps = 1f;

        [Tooltip("Vertical velocity to play landing effect.")]
        [SerializeField] private float _velocityLandThreshold = 5f;

        [SerializeField] private float _jumpBufferTime = 0.5f;
        [SerializeField] private float _coyotteTimeGrace = 0.3f;

        [SerializeField] private Player _player;


        // ========================================================= PRIVATE FIELDS

        private float _lastLandedFromJumpTime = Mathf.NegativeInfinity;
        private float _lastTryToJumpTime = Mathf.NegativeInfinity;
        private float _lastJumpTime = Mathf.NegativeInfinity;
        private bool _startJumping = false;


        // ========================================================= PROPERTIES

        public Action OnHardLanding { get; set; }


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponent<Player>();
            }
        }

        void Awake()
        {
            //_lastTryToJumpTime = -_jumpBufferTime;
        }

        void OnEnable()
        {
            _player.OnPlayerUpdate += OnPlayerUpdate;
            _player.OnLanding += OnLanding;
            _player.OnBeforeMove += OnBeforeMove;
        }
        void OnDisable()
        {
            _player.OnPlayerUpdate -= OnPlayerUpdate;
            _player.OnLanding -= OnLanding;
            _player.OnBeforeMove -= OnBeforeMove;
        }


        // ========================================================= CALLBACKS

        public void OnPlayerUpdate()
        {
            // Cache last keypress the player tried to jump
            if (_player.inputManager.JumpInputAction.WasPerformedThisFrame() && _player.IsClimbing == false)
            {
                _lastTryToJumpTime = Time.time;
            }

            // The player must have been grounded for the last "_coyotteTimeGrace" seconds - this enables the player to
            // jump even after contact with ground has been lost for that duration - relaxing the time window makes the game feel snappier
            var groundCheck = _player.lastOnGroundPositionTime + _coyotteTimeGrace > Time.time && _player.IsSlidingDownSlope == false;

            // This makes player jump even in cases he hit the ground a fraction of second after pressing jump button, thus relaxing
            // the time window, but in a different way groundCheck does
            var shouldTryToJump = _lastTryToJumpTime + _jumpBufferTime > Time.time;
            var canJumpAgain = _lastLandedFromJumpTime + _delayBetweenJumps < Time.time;
            var lastJumpWindowOver = _lastJumpTime + 0.5f < Time.time;

            if ((_player.IsGrounded || _player.IsSlidingDownSlope) && lastJumpWindowOver && _player.IsJumping)
            {
                _player.IsJumping = false;
            }
            if (shouldTryToJump
                && groundCheck
                && canJumpAgain
                && _player.IsJumping == false
                && _player.IsCrouching == false
               )
            {
                // apply the jump velocity to the player rigidbody
                _startJumping = true;
            }
        }

        public void OnBeforeMove()
        {
            if (_startJumping)
            {
                _player.IsJumping = true;
                _player.gravityForce += _jumpSpeed;
                _lastJumpTime = Time.time;
                _startJumping = false;
            }
        }

        void OnLanding(float velocity)
        {
            if (_player.IsJumping)
            {
                _lastLandedFromJumpTime = Time.time;
                _player.IsJumping = false;
            }

            if (velocity > _velocityLandThreshold)
            {
                OnHardLanding?.Invoke();
            }
        }
    }
}
