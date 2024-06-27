using UnityEngine;

namespace FPSDemo.FPSController
{
    [RequireComponent(typeof(Player))]
    public class PlayerSprinting : MonoBehaviour
    {
        Player player;
        [Tooltip("Speed that player moves when sprinting.")]
        [SerializeField] float sprintSpeed = 9f;


        void Awake()
        {
            player = GetComponent<Player>();
        }

        void OnEnable()
        {
            player.OnBeforeMove += OnBeforeMove;
		}
		void OnDisable()
        {
            player.OnBeforeMove -= OnBeforeMove;
        }

        public void OnBeforeMove()
        {
            Vector2 move = player.inputManager.GetMovementInput();
            // If the player is trying to sprint and criteria for sprinting are met, begin sprinting
            if (player.inputManager.SprintInputAction.IsPressed() && move.y > 0f && Mathf.Approximately(move.x, 0f) && player.IsGrounded && !player.IsClimbing && !player.IsCrouching && !player.IsAiming)
            {
                player.desiredTargetSpeed = sprintSpeed;
            }
            else
            {
				player.desiredTargetSpeed = player.WalkSpeed;
            }
        }
    }
}
