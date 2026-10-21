using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Where Esc / gamepad B (and gamepad Start) go, in order: a text field being edited or an open dropdown keeps
    /// it; else the top <see cref="MaModal"/> cancels; else the most recently enabled open <see cref="MenuStack"/>
    /// goes back; else a closed pause-menu stack opens. A hidden driver polls the input each frame; call
    /// <see cref="HandleBack"/> yourself to route a back press from anywhere else (an on-screen button, tests).
    /// </summary>
    public static class MaBackRouter
    {
        static readonly List<MenuStack> s_stacks = new List<MenuStack>();
        static GameObject s_driver;

        /// <summary>Turns the automatic Esc/B handling off (e.g. while a minigame owns Esc). On by default.</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Routes one back press; true when something used it.</summary>
        public static bool HandleBack()
        {
            if (MaTextbox.AnyEditing || MaSelect.AnyExpanded) return false;
            var modal = MaModal.Top;
            if (modal != null) return modal.HandleBack();
            for (int i = s_stacks.Count - 1; i >= 0; i--)
            {
                var s = s_stacks[i];
                if (s == null || !s.isActiveAndEnabled || !s.EscapeEnabled || !s.IsOpen) continue;
                return s.HandleBack() != BackOutcome.None;
            }
            for (int i = s_stacks.Count - 1; i >= 0; i--)
            {
                var s = s_stacks[i];
                if (s == null || !s.isActiveAndEnabled || !s.EscapeEnabled || s.IsOpen || !s.OpenOnBack) continue;
                return s.HandleBack() == BackOutcome.Opened;
            }
            return false;
        }

        /// <summary>Routes a gamepad Start press: toggles the most recent pause-menu stack (when no modal is open).</summary>
        public static bool HandleMenu()
        {
            if (MaModal.Top != null || MaTextbox.AnyEditing) return false;
            for (int i = s_stacks.Count - 1; i >= 0; i--)
            {
                var s = s_stacks[i];
                if (s == null || !s.isActiveAndEnabled || !s.EscapeEnabled || !s.OpenOnBack) continue;
                s.Toggle();
                return true;
            }
            return false;
        }

        internal static void Register(MenuStack stack)
        {
            s_stacks.Remove(stack);
            s_stacks.Add(stack);
            EnsureDriver();
        }

        internal static void Unregister(MenuStack stack) => s_stacks.Remove(stack);

        internal static void EnsureDriver()
        {
            if (s_driver != null || !Application.isPlaying) return;
            s_driver = new GameObject("MindAttic Back Router") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(s_driver);
            s_driver.AddComponent<MaBackRouterDriver>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_stacks.Clear();
            s_driver = null;
            Enabled = true;
        }
    }

    /// <summary>Polls back/menu input once per frame for <see cref="MaBackRouter"/>.</summary>
    [AddComponentMenu("")]
    internal sealed class MaBackRouterDriver : MonoBehaviour
    {
        int _handledFrame = -1;

        void Update()
        {
            if (!MaBackRouter.Enabled || _handledFrame == Time.frameCount) return;
            if (MaInput.BackPressedThisFrame())
            {
                _handledFrame = Time.frameCount;
                MaInput.PointerModality = false;
                MaBackRouter.HandleBack();
            }
            else if (MaInput.MenuPressedThisFrame())
            {
                _handledFrame = Time.frameCount;
                MaBackRouter.HandleMenu();
            }
        }
    }
}
