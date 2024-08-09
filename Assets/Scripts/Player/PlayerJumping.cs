using System;
using UnityEngine;

namespace FPSDemo.Player
{
    public class PlayerJumping : MonoBehaviour
    {
        [Tooltip("Vertical speed of player jump.")]
        [SerializeField] float jumpSpeed = 4f;
        [Tooltip("Time in seconds allowed between player jumps.")]
        [SerializeField] float delayBetweenJumps = 1f;
        [Tooltip("Vertical velocity to play landing effect.")]
        [SerializeField] float velocityLandThreshold = 5f;
        [SerializeField] float jumpBufferTime = 0.5f;
        [SerializeField] float coyotteTimeGrace = 0.3f;
        float lastLandedFromJumpTime = Mathf.NegativeInfinity;
        float lastTryToJumpTime = Mathf.NegativeInfinity;
        float lastJumpTime = Mathf.NegativeInfinity;
        bool startJumping = false;
        Player player;
        public event Action OnHardLanding;

        void Awake()
        {
            player = GetComponent<Player>();
            //lastTryToJumpTime = -jumpBufferTime;
        }

        void OnEnable()
        {
            player.OnPlayerUpdate += OnPlayerUpdate;
            player.OnLanding += Landing;
            player.OnBeforeMove += OnBeforeMove;
        }
        void OnDisable()
        {
            player.OnPlayerUpdate -= OnPlayerUpdate;
            player.OnLanding -= Landing;
            player.OnBeforeMove -= OnBeforeMove;
        }

        void Landing(float velocity)
        {
            if (player.IsJumping)
            {
                lastLandedFromJumpTime = Time.time;
                player.IsJumping = false;
            }

            if (velocity > velocityLandThreshold)
            {
                OnHardLanding?.Invoke();
            }
        }

        public void OnPlayerUpdate()
        {
            // Cache last keypress the player tried to jump
            if (player.inputManager.JumpInputAction.WasPerformedThisFrame() && !player.IsClimbing)
            {
                lastTryToJumpTime = Time.time;
            }

            /* The player must have been grounded for the last "coyotteTimeGrace" seconds - this enables the player to
            jump even after contact with ground has been lost for that duration - relaxing the time window makes the game feel snappier */
            bool groundCheck = player.lastOnGroundPositionTime + coyotteTimeGrace > Time.time && !player.IsSlidingDownSlope;

            /* This makes player jump even in cases he hit the ground a fraction of second after pressing jump button, thus relaxing
            the time window, but in a different way groundCheck does */
            bool shouldTryToJump = lastTryToJumpTime + jumpBufferTime > Time.time;
            bool canJumpAgain = lastLandedFromJumpTime + delayBetweenJumps < Time.time;
            bool lastJumpWindowOver = lastJumpTime + 0.5f < Time.time;

            if ((player.IsGrounded || player.IsSlidingDownSlope) && lastJumpWindowOver && player.IsJumping)
            {
                player.IsJumping = false;
            }
            if (shouldTryToJump
                && groundCheck
                && canJumpAgain
                && !player.IsJumping
                && !player.IsCrouching
            )
            {
                // apply the jump velocity to the player rigidbody
                startJumping = true;
            }
        }

        public void OnBeforeMove()
        {
            if (startJumping)
            {
                player.IsJumping = true;
                player.gravityForce += jumpSpeed;
                lastJumpTime = Time.time;
                startJumping = false;
            }
        }
    }
}
