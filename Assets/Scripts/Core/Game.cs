using UnityEngine;
using FPSDemo.NPC.Utilities;

namespace FPSDemo.Core
{
	public class Game : MonoBehaviour
    {
        // -------------------------------------------- STATIC PROPERTIES
        private static Game Instance { get; set; }
        public static AISettings AISettings => Instance != null ? Instance._aiSettings : null;
        public static TacticalGeneratorSettings TacticalSettings => Instance != null ? Instance._tacticalSettings : null;


        // -------------------------------------------- INSPECTOR FIELDS

        [SerializeField] private AISettings _aiSettings;
        [SerializeField] private TacticalGeneratorSettings _tacticalSettings;


        // ========================================================= UNITY METHODS

        private void Awake()
		{
			Instance = this;
			FocusTheGameWindow();
			ToggleCursor(false);
		}


        // ========================================================= TOGGLES

        private static void ToggleCursor(bool value)
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


        // ========================================================= EDITOR / DEBUG

        private static void FocusTheGameWindow()
		{
#if UNITY_EDITOR
			var gameViewWindow = UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"));
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