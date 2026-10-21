using UnityEngine;
using UnityEngine.EventSystems;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Makes sure the scene has an EventSystem with an input module that matches the project: the Input
    /// System's <c>InputSystemUIInputModule</c> when the Input System package is installed and enabled, else the
    /// old <c>StandaloneInputModule</c>. Either way keyboard arrows/WASD, gamepad sticks and d-pad, Enter/A
    /// (submit) and Esc/B (cancel) drive every component.
    /// </summary>
    public static class MaEventSystem
    {
        /// <summary>The scene's EventSystem, created when there is none.</summary>
        public static EventSystem Ensure()
        {
            var current = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
            if (current != null) return current;

            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
#if MINDATTIC_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#endif
            return es;
        }

        /// <summary>The name of the input module the package would add in this project (for diagnostics and tests).</summary>
        public static string PreferredModule
        {
            get
            {
#if MINDATTIC_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                return "InputSystemUIInputModule";
#elif ENABLE_LEGACY_INPUT_MANAGER
                return "StandaloneInputModule";
#else
                return "none";
#endif
            }
        }

        /// <summary>Selects <paramref name="go"/> (null clears the selection) on the current EventSystem.</summary>
        public static void Select(GameObject go)
        {
            var es = EventSystem.current != null ? EventSystem.current : Ensure();
            if (es == null || es.alreadySelecting) return;
            es.SetSelectedGameObject(go);
        }

        /// <summary>The selected GameObject, or null.</summary>
        public static GameObject Selected => EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
    }
}
