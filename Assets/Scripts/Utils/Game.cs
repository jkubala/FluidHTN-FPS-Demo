using UnityEngine;

namespace FPSDemo.FPSController
{
	public class Game : MonoBehaviour
	{

		void Awake()
		{
			FocusTheGameWindow();
			ToggleCursor(false);
		}

		void ToggleCursor(bool value)
		{
			if (value)
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.Confined;

			}
			else
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
		}

		void FocusTheGameWindow()
		{
#if UNITY_EDITOR
			UnityEditor.EditorWindow gameViewWindow = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"));
			gameViewWindow.Focus();
			Event focusMouseClick = new()
			{
				button = 0,
				clickCount = 1,
				type = EventType.MouseDown,
				mousePosition = gameViewWindow.rootVisualElement.contentRect.center
			};
			gameViewWindow.SendEvent(focusMouseClick);
#endif
		}
	}
}