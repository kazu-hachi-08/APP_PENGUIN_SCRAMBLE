using PenmonScramble.Common.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace PenmonScramble.Common.Input
{
    /// <summary>
    /// キーボード・コントローラー・仮想UI入力を統合し、統一された入力を提供するマネージャー
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        public const int ActionCount = 4;

        // スティックの遊び。これより小さい倒し方は無視する（触っていないのに歩き出さないため）
        private const float GamepadDeadZone = 0.2f;

        [Header("Virtual Controls (Optional)")]
        [SerializeField] private VirtualJoystick _virtualJoystick;
        [SerializeField] private VirtualButton _virtualAction1Button;
        [SerializeField] private VirtualButton _virtualAction2Button;
        [SerializeField] private VirtualButton _virtualAction3Button;
        [SerializeField] private VirtualButton _virtualAction4Button;

        [Header("Action Keys (Action1〜4)")]
        [SerializeField] private Key[] _actionKeys = { Key.J, Key.K, Key.L, Key.Space };

        [Header("Settings")]
        [SerializeField] private bool _inputEnabled = true;

        private readonly bool[] _actionHeld = new bool[ActionCount];
        private readonly bool[] _actionDown = new bool[ActionCount];

        public Vector2 MoveVector { get; private set; }
        public bool IsPauseDown { get; private set; }

        /// <summary>index は 0〜3（Action1〜4）</summary>
        public bool IsActionHeld(int index) => _actionHeld[index];

        /// <summary>このフレームに押した</summary>
        public bool IsActionDown(int index) => _actionDown[index];

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

            var gamepad = Gamepad.current;
            ProcessMoveInput(keyboard, gamepad);
            ProcessActionInputs(keyboard, gamepad);
            ProcessSystemInputs(keyboard, gamepad);
        }

        private void ProcessMoveInput(Keyboard keyboard, Gamepad gamepad)
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

            // キーボード・コントローラー・バーチャルジョイスティックのうち、一番大きく倒しているものを採用
            Vector2 combined = keyboardInput.sqrMagnitude > virtualInput.sqrMagnitude ? keyboardInput : virtualInput;
            Vector2 gamepadInput = ReadGamepadMove(gamepad);
            if (gamepadInput.sqrMagnitude > combined.sqrMagnitude)
            {
                combined = gamepadInput;
            }

            MoveVector = Vector2.ClampMagnitude(combined, 1f);
        }

        private static Vector2 ReadGamepadMove(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                return Vector2.zero;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < GamepadDeadZone)
            {
                stick = Vector2.zero;
            }

            Vector2 dpad = gamepad.dpad.ReadValue();
            return stick.sqrMagnitude > dpad.sqrMagnitude ? stick : Vector2.ClampMagnitude(dpad, 1f);
        }

        private void ProcessActionInputs(Keyboard keyboard, Gamepad gamepad)
        {
            for (int i = 0; i < ActionCount; i++)
            {
                KeyControl key = keyboard != null && i < _actionKeys.Length ? keyboard[_actionKeys[i]] : null;
                ButtonControl pad = GetGamepadAction(gamepad, i);
                VirtualButton virtualButton = GetVirtualAction(i);

                _actionHeld[i] = (key != null && key.isPressed) || (pad != null && pad.isPressed) || (virtualButton != null && virtualButton.IsHeld);
                _actionDown[i] = (key != null && key.wasPressedThisFrame) || (pad != null && pad.wasPressedThisFrame) || (virtualButton != null && virtualButton.IsDown);
            }
        }

        /// <summary>Action1〜4 = 南・東・北・LB（多くのゲームで決定・キャンセルに当たる南と東を、よく使う技にする）</summary>
        private static ButtonControl GetGamepadAction(Gamepad gamepad, int index)
        {
            if (gamepad == null)
            {
                return null;
            }

            switch (index)
            {
                case 0: return gamepad.buttonSouth;
                case 1: return gamepad.buttonEast;
                case 2: return gamepad.buttonNorth;
                case 3: return gamepad.leftShoulder;
                default: return null;
            }
        }

        private VirtualButton GetVirtualAction(int index)
        {
            switch (index)
            {
                case 0: return _virtualAction1Button;
                case 1: return _virtualAction2Button;
                case 2: return _virtualAction3Button;
                case 3: return _virtualAction4Button;
                default: return null;
            }
        }

        private void ProcessSystemInputs(Keyboard keyboard, Gamepad gamepad)
        {
            IsPauseDown = (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
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
            System.Array.Clear(_actionHeld, 0, ActionCount);
            System.Array.Clear(_actionDown, 0, ActionCount);

            if (_virtualJoystick != null) _virtualJoystick.ResetInput();
            if (_virtualAction1Button != null) _virtualAction1Button.ResetButton();
            if (_virtualAction2Button != null) _virtualAction2Button.ResetButton();
            if (_virtualAction3Button != null) _virtualAction3Button.ResetButton();
            if (_virtualAction4Button != null) _virtualAction4Button.ResetButton();
        }
    }
}
