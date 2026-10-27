using PenmonScramble.Common.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Common.Input
{
    /// <summary>
    /// キーボード入力と仮想UI入力を統合し、統一された入力を提供するマネージャー
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        [Header("Virtual Controls (Optional)")]
        [SerializeField] private VirtualJoystick _virtualJoystick;
        [SerializeField] private VirtualButton _virtualAction1Button;
        [SerializeField] private VirtualButton _virtualAction2Button;
        [SerializeField] private VirtualButton _virtualAction3Button;
        [SerializeField] private VirtualButton _virtualAction4Button;

        [Header("Settings")]
        [SerializeField] private bool _inputEnabled = true;

        public Vector2 MoveVector { get; private set; }
        public bool IsPauseDown { get; private set; }

        private void Update()
        {
            var keyboard = Keyboard.current;

            if (!_inputEnabled)
            {
                ResetAllInputs();
                // ポーズ入力のみ無効化時も受け取れるようにする（ポーズ解除等）
                IsPauseDown = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
                return;
            }

            ProcessMoveInput(keyboard);
            ProcessSystemInputs(keyboard);
        }

        private void ProcessMoveInput(Keyboard keyboard)
        {
            Vector2 keyboardInput = Vector2.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardInput.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardInput.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardInput.x += 1f;
            }

            if (keyboardInput.sqrMagnitude > 1f)
            {
                keyboardInput.Normalize();
            }

            Vector2 virtualInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

            // キーボードまたはバーチャルジョイスティックのいずれか大きい方を採用
            Vector2 combined = keyboardInput.sqrMagnitude > virtualInput.sqrMagnitude ? keyboardInput : virtualInput;
            MoveVector = Vector2.ClampMagnitude(combined, 1f);
        }

        private void ProcessSystemInputs(Keyboard keyboard)
        {
            IsPauseDown = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
        }

        public void RegisterVirtualControls(
            VirtualJoystick joystick,
            VirtualButton btn1 = null,
            VirtualButton btn2 = null,
            VirtualButton btn3 = null,
            VirtualButton btn4 = null)
        {
            _virtualJoystick = joystick;
            _virtualAction1Button = btn1;
            _virtualAction2Button = btn2;
            _virtualAction3Button = btn3;
            _virtualAction4Button = btn4;
        }

        public void UnregisterVirtualControls()
        {
            _virtualJoystick = null;
            _virtualAction1Button = null;
            _virtualAction2Button = null;
            _virtualAction3Button = null;
            _virtualAction4Button = null;
        }

        private void ResetAllInputs()
        {
            MoveVector = Vector2.zero;
            IsPauseDown = false;

            if (_virtualJoystick != null) _virtualJoystick.ResetInput();
            if (_virtualAction1Button != null) _virtualAction1Button.ResetButton();
            if (_virtualAction2Button != null) _virtualAction2Button.ResetButton();
            if (_virtualAction3Button != null) _virtualAction3Button.ResetButton();
            if (_virtualAction4Button != null) _virtualAction4Button.ResetButton();
        }
    }
}
