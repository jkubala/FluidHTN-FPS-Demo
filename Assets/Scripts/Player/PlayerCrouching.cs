using UnityEngine;

namespace FPSDemo.Player
{
	public class PlayerCrouching : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [Header("Crouching")]
		[SerializeField] private float _crouchSpeed = 10f;
		[SerializeField] private float _crouchSpeedMultiplier = 0.4f;
		[SerializeField] private int _numberOfStepsToCrouch = 3;
		[SerializeField] private float _spaceFromCeilingWhenUncrouching = 0.1f;
		[SerializeField] private float _deadHeight = 0.8f;

        [SerializeField] private Player _player;


        // ========================================================= PRIVATE FIELDS

        private int _currentCrouchLevel = 0;
		private bool _isAdjustingCrouchLevel = false;
		private float _stepSize;
        private Vector3 _castOrigin = Vector3.zero;


        // ========================================================= PROPERTIES

        public float CrouchColliderHeight { get; private set; } = 1.25f;


        // ========================================================= UNITY METHODS

        private void OnValidate()
        {
            if (_player == null)
            {
                _player = GetComponent<Player>();
            }
        }

        private void Awake()
		{
			_player.CrouchFloatingColliderHeight = CrouchColliderHeight - _player.DistanceToFloat;
		}

        private void Start()
		{
			_stepSize = (_player.StandingFloatingColliderHeight - _player.CrouchFloatingColliderHeight) / _numberOfStepsToCrouch;
		}

        private void OnEnable()
		{
			_player.OnPlayerUpdate += OnPlayerUpdate;
			_player.OnBeforeMove += OnBeforeMove;

			OnRestart();
		}
        private void OnDisable()
		{
			_player.OnPlayerUpdate -= OnPlayerUpdate;
			_player.OnBeforeMove -= OnBeforeMove;
		}


        // ========================================================= CLEAR / RESET

        private void OnRestart()
        {
            SetCrouchLevelToMatchHeight(_player.CharacterHeight);
        }


        // ========================================================= CALLBACKS

        private void OnPlayerUpdate()
        {
            UpdateCrouchLevel();
        }

        private void OnBeforeMove()
		{
			float heightTarget;
			if (_player.ThisTarget.IsDead)
			{
				heightTarget = _deadHeight;
			}
			else
			{
				heightTarget = _player.StandingFloatingColliderHeight - (_currentCrouchLevel * _stepSize);
				if (_player.IsCrouching && Mathf.Approximately(heightTarget, _player.CurrentFloatingColliderHeight) == false)
				{
					_castOrigin = _player.PlayerTopSphere();
                    var maxDistance = _player.StandingFloatingColliderHeight - _player.CurrentFloatingColliderHeight;
                    var layerMask = ~LayerMask.GetMask("Player");

                    if (Physics.SphereCast(_castOrigin, _player.Radius, Vector3.up, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
					{
						var distanceToHit = Mathf.Clamp(hit.point.y - _castOrigin.y - _player.Radius, 0, Mathf.Infinity);
						heightTarget = Mathf.Clamp(Mathf.Min(_player.CurrentFloatingColliderHeight + distanceToHit - _spaceFromCeilingWhenUncrouching, heightTarget), _player.CrouchFloatingColliderHeight, _player.StandingFloatingColliderHeight);
					}
				}
			}

			// Gradually adjust current height to target height
			if (Mathf.Abs(heightTarget - _player.CurrentFloatingColliderHeight) > 0.001f)
			{
				_player.CurrentFloatingColliderHeight = Mathf.Lerp(_player.CurrentFloatingColliderHeight, heightTarget, _crouchSpeed * Time.deltaTime);
			}
			else
			{
				_player.CurrentFloatingColliderHeight = heightTarget;
			}

			// Calculate crouch speed
			if (_player.IsCrouching)
			{
				_player.CrouchSpeedMultiplier = Mathf.Lerp(1f, _crouchSpeedMultiplier, _player.CrouchPercentage);
			}
			else
			{
				_player.CrouchSpeedMultiplier = 1f;
			}
		}


        // ========================================================= SIMULATION

        private void UpdateCrouchLevel()
		{
			if (!_player.IsSprinting)
			{
				if (_player.InputManager.IncreaseCrouchLevel)
				{
					_currentCrouchLevel = Mathf.Clamp(++_currentCrouchLevel, 0, _numberOfStepsToCrouch);
					_isAdjustingCrouchLevel = true;
				}
				else if (_player.InputManager.DecreaseCrouchLevel)
				{
					_currentCrouchLevel = Mathf.Clamp(--_currentCrouchLevel, 0, _numberOfStepsToCrouch);
					_isAdjustingCrouchLevel = true;
				}
				else if (_player.InputManager.CrouchToggled)
				{
					if (_isAdjustingCrouchLevel)
					{
						_isAdjustingCrouchLevel = false;
					}
					else
					{
						if (_currentCrouchLevel > 0)
						{
							_currentCrouchLevel = 0;
						}
						else
						{
							_currentCrouchLevel = _numberOfStepsToCrouch;
						}
					}
				}
			}
			else
			{
				_currentCrouchLevel = 0;
			}
		}


        // ========================================================= SETTERS

        public void SetCrouchLevelToMatchHeight(float heightToMatch)
		{
			int crouchLevelToSet;
			if (Mathf.Approximately(heightToMatch, _player.CharacterHeight))
			{
				crouchLevelToSet = 0;
			}
			else
			{
				crouchLevelToSet = _numberOfStepsToCrouch - Mathf.FloorToInt((heightToMatch - CrouchColliderHeight - _spaceFromCeilingWhenUncrouching) / _stepSize);
			}

            _currentCrouchLevel = crouchLevelToSet;
        }
	}
}
