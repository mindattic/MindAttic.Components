using System;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Tracks a stock uGUI <see cref="Selectable"/>'s hover, press, focus and interactable state (its own colour
    /// transitions are switched off) so a MindAttic component can draw the state with theme colours. Also
    /// selects the control on hover (<see cref="MaInput.SelectOnHover"/>) and records pointer vs keyboard use.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MaStateRelay : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler, IMoveHandler
    {
        [SerializeField] Selectable selectable;

        bool _hovered, _pressed, _focused, _interactable = true;
        float _pulseUntil;

        /// <summary>Raised whenever the visible state changes.</summary>
        public event Action Changed;

        /// <summary>The control being tracked.</summary>
        public Selectable Selectable
        {
            get => selectable;
            set => selectable = value;
        }

        /// <summary>Under the pointer.</summary>
        public bool Hovered => _hovered;

        /// <summary>Selected (keyboard/gamepad focus).</summary>
        public bool Focused => _focused;

        /// <summary>The focus ring should show: focused, and the player is not using a pointer.</summary>
        public bool ShowFocusRing => _focused && !MaInput.PointerModality;

        /// <summary>The pointer state as a <see cref="ControlState"/>.</summary>
        public ControlState State
        {
            get
            {
                if (selectable != null && !selectable.IsInteractable()) return ControlState.Disabled;
                if (_pressed || _pulseUntil > Time.unscaledTime) return ControlState.Pressed;
                if (_hovered || (_focused && !MaInput.PointerModality)) return ControlState.Hover;
                return ControlState.Normal;
            }
        }

        /// <summary>Attaches a relay to a selectable and turns the selectable's own transitions off.</summary>
        public static MaStateRelay Attach(Selectable s)
        {
            s.transition = Selectable.Transition.None;
            var r = MaUi.GetOrAdd<MaStateRelay>(s.gameObject);
            r.selectable = s;
            return r;
        }

        void OnDisable()
        {
            _hovered = _pressed = false;
            _focused = false;
        }

        void Update()
        {
            bool interactable = selectable == null || selectable.IsInteractable();
            bool focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            bool pulseEnded = _pulseUntil > 0 && _pulseUntil <= Time.unscaledTime;
            if (pulseEnded) _pulseUntil = 0;
            if (interactable != _interactable || focused != _focused || pulseEnded)
            {
                _interactable = interactable;
                _focused = focused;
                Changed?.Invoke();
            }
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData e)
        {
            _hovered = true;
            if (MaInput.SelectOnHover && selectable != null && selectable.IsInteractable() && Application.isPlaying &&
                EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            {
                MaInput.PointerModality = true;
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData e)
        {
            _hovered = false;
            _pressed = false;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            MaInput.PointerModality = true;
            _pressed = true;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData e)
        {
            _pressed = false;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnSelect(BaseEventData e)
        {
            _focused = true;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnDeselect(BaseEventData e)
        {
            _focused = false;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnSubmit(BaseEventData e)
        {
            MaInput.PointerModality = false;
            _pulseUntil = Time.unscaledTime + 0.1f;
            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void OnMove(AxisEventData e)
        {
            if (MaInput.PointerModality)
            {
                MaInput.PointerModality = false;
                Changed?.Invoke();
            }
        }

        /// <summary>Re-reads the state now (after setting interactable from code).</summary>
        public void Refresh()
        {
            _interactable = selectable == null || selectable.IsInteractable();
            Changed?.Invoke();
        }
    }
}
