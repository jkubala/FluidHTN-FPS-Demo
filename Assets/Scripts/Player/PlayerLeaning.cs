using UnityEngine;

namespace FPSDemo.Player
{
    public class PlayerLeaning : MonoBehaviour
    {
        [Header("Leaning")]
        [Tooltip("Distance left or right the player can lean.")]
        [SerializeField] float leanDistance = 0.75f;
        [Tooltip("Pecentage the player can lean while standing.")]
        [SerializeField] float standLeanAmt = 1f;
        [Tooltip("Pecentage the player can lean while crouching.")]
        [SerializeField] float crouchLeanAmt = 0.75f;
        public float rotationLeanAmt = 20f;



        public float LeanAmt { get; set; } = 0f;
        public float LeanPos { get; set; }
        public bool IsLeaning { get; set; }

        Player player;
        float leanFactorAmt = 1f;
        float currentLeanVelocity;
        Vector3 leanCheckPos;
        // Start is called before the first frame update
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

        void OnBeforeMove()
        {
            LeanAmt = 0f;
            IsLeaning = false;
            if (!player.IsSprinting && player.IsGrounded && Mathf.Abs(player.inputManager.GetMovementInput().y) < 0.2f &&
            (player.inputManager.LeanLeftInputAction.IsPressed() || player.inputManager.LeanRightInputAction.IsPressed()))
            {
                int direction;
                if (player.inputManager.LeanLeftInputAction.IsPressed() && !player.inputManager.LeanRightInputAction.IsPressed()) // lean left
                {
                    direction = -1;
                }
                else if (player.inputManager.LeanRightInputAction.IsPressed() && !player.inputManager.LeanLeftInputAction.IsPressed()) // lean right
                {
                    direction = 1;
                }
                else
                {
                    return;
                }
                leanCheckPos = transform.position + Vector3.up * (player.CurrentFloatingColliderHeight + player.DistanceToFloat - player.Capsule.radius);
                leanFactorAmt = Mathf.Lerp(standLeanAmt, crouchLeanAmt, player.CrouchPercentage);

                // Offset a bit to the opposite side so that the cast does not miss a wall the player is touching
                Vector3 offset = -direction * 0.05f * transform.right;
                if (!Physics.SphereCast(leanCheckPos + offset, player.Capsule.radius * 0.6f, transform.right * direction, out _, leanDistance * leanFactorAmt, player.GroundMask.value))
                {
                    LeanAmt = leanDistance * leanFactorAmt * direction;
                    IsLeaning = true;
                }
            }
            // smooth position between leanAmt values
            LeanPos = Mathf.SmoothDamp(LeanPos, LeanAmt, ref currentLeanVelocity, 0.1f, Mathf.Infinity, Time.deltaTime);
        }
    }
}
