using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace FPSDemo.Player
{
    [RequireComponent(typeof(Player))]
    public class PlayerSprinting : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS
        
        [Tooltip("Speed that player moves when sprinting.")]
        [SerializeField] float _sprintSpeed = 9f;
        [SerializeField] private Player _player;


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

        public void OnBeforeMove()
        {
            var move = _player.InputManager.GetMovementInput();

            // If the player is trying to sprint and criteria for sprinting are met, begin sprinting
            if (_player.IsGrounded && 
                _player.IsClimbing == false && 
                _player.IsCrouching == false &&
                _player.IsAiming == false &&
                move.y > 0f &&
                Mathf.Approximately(move.x, 0f) &&
                _player.InputManager.SprintInputAction.IsPressed())
            {
                _player.DesiredTargetSpeed = _sprintSpeed;
            }
            else
            {
				_player.DesiredTargetSpeed = _player.WalkSpeed;
            }
        }
    }
}
