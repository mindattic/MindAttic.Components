using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// The few raw inputs the package reads itself (everything else comes through the EventSystem): back
    /// (Esc, gamepad B/East) and menu (gamepad Start), from whichever input backend is active, and whether the
    /// player is using a pointer or keyboard/gamepad (focus rings show only for the latter, like the web's
    /// <c>:focus-visible</c>).
    /// </summary>
    public static class MaInput
    {
        /// <summary>Hovering a control with the mouse also selects it, so keyboard/gamepad navigation continues from there. On by default.</summary>
        public static bool SelectOnHover { get; set; } = true;

        /// <summary>True after a pointer press, false after keyboard/gamepad navigation or submit.</summary>
        public static bool PointerModality { get; internal set; }

        /// <summary>True on the frame Esc or gamepad B (East) went down.</summary>
        public static bool BackPressedThisFrame()
        {
#if MINDATTIC_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && gp.buttonEast.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) return true;
#endif
            return false;
        }

        /// <summary>True on the frame gamepad Start went down.</summary>
        public static bool MenuPressedThisFrame()
        {
#if MINDATTIC_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if (gp != null && gp.startButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.JoystickButton7)) return true;
#endif
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PointerModality = false;
            SelectOnHover = true;
        }
    }
}
