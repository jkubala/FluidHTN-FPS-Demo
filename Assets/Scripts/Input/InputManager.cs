using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSDemo.Input
{
	public class InputManager : MonoBehaviour
	{
		public class InputActionNames
		{
			public const string Movement = "Movement";
			public const string MouseLook = "Mouse Look";
			public const string MouseWheel = "Mouse Wheel";
			public const string Sprint = "Sprint";
			public const string Jump = "Jump";
			public const string Crouch = "Crouch";
			public const string Interact = "Interact";
			public const string Reload = "Reload";
			public const string LeanLeft = "Lean Left";
			public const string LeanRight = "Lean Right";
			public const string Fire = "Fire";
			public const string Aim = "Aim";
		}

		public InputActionAsset inputActions;

		[Tooltip("Reverse vertical input for look.")]
		[SerializeField] bool invertVerticalLook;

		// Axes
		InputAction MouseLookInputAction { get; set; }
		InputAction MovementInputAction { get; set; }
		InputAction MouseWheelInputAction { get; set; }

		// Buttons
		public InputAction SprintInputAction { get; private set; }
		public InputAction JumpInputAction { get; private set; }
		public InputAction CrouchInputAction { get; private set; }
		public InputAction InteractInputAction { get; private set; }
		public InputAction ReloadInputAction { get; private set; }
		public InputAction LeanLeftInputAction { get; private set; }
		public InputAction LeanRightInputAction { get; private set; }
		public InputAction FireInputAction { get; private set; }
		public InputAction AimInputAction { get; private set; }

		void Awake()
		{
			MovementInputAction = inputActions.FindAction(InputActionNames.Movement);
			MouseLookInputAction = inputActions.FindAction(InputActionNames.MouseLook);
			MouseWheelInputAction = inputActions.FindAction(InputActionNames.MouseWheel);
			SprintInputAction = inputActions.FindAction(InputActionNames.Sprint);
			JumpInputAction = inputActions.FindAction(InputActionNames.Jump);
			CrouchInputAction = inputActions.FindAction(InputActionNames.Crouch);
			InteractInputAction = inputActions.FindAction(InputActionNames.Interact);
			ReloadInputAction = inputActions.FindAction(InputActionNames.Reload);
			LeanLeftInputAction = inputActions.FindAction(InputActionNames.LeanLeft);
			LeanRightInputAction = inputActions.FindAction(InputActionNames.LeanRight);
			FireInputAction = inputActions.FindAction(InputActionNames.Fire);
			AimInputAction = inputActions.FindAction(InputActionNames.Aim);

			SetActiveAllGameplayControls(true);
		}

		public void SetActiveAllGameplayControls(bool value)
		{
			if (value)
			{
				MouseLookInputAction.Enable();
				MovementInputAction.Enable();
				MouseWheelInputAction.Enable();
				SprintInputAction.Enable();
				JumpInputAction.Enable();
				CrouchInputAction.Enable();
				InteractInputAction.Enable();
				ReloadInputAction.Enable();
				LeanLeftInputAction.Enable();
				LeanRightInputAction.Enable();
				FireInputAction.Enable();
				AimInputAction.Enable();
			}
			else
			{
				MouseLookInputAction.Disable();
				MovementInputAction.Disable();
				MouseWheelInputAction.Disable();
				SprintInputAction.Disable();
				JumpInputAction.Disable();
				CrouchInputAction.Disable();
				InteractInputAction.Disable();
				ReloadInputAction.Disable();
				LeanLeftInputAction.Disable();
				LeanRightInputAction.Disable();
				FireInputAction.Disable();
				AimInputAction.Disable();
			}
		}

		public virtual Vector2 GetLookInput()
		{
			Vector2 value = MouseLookInputAction.ReadValue<Vector2>();

			if (invertVerticalLook)
			{
				value.y = -value.y;
			}

			return value;
		}

		public Vector2 GetMovementInput()
		{
			if (MovementInputAction != null)
			{
				return MovementInputAction.ReadValue<Vector2>();
			}
			else
			{
				return Vector2.zero;
			}
		}

		public bool CrouchToggled
		{
			get
			{
				if (CrouchInputAction.WasReleasedThisFrame() && MouseWheelInputAction.ReadValue<float>() == 0f)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool DecreaseCrouchLevel
		{
			get
			{
				if (CrouchInputAction.IsPressed() && MouseWheelInputAction.ReadValue<float>() > 0f)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool IncreaseCrouchLevel
		{
			get
			{
				if (CrouchInputAction.IsPressed() && MouseWheelInputAction.ReadValue<float>() < 0f)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
	}
}
