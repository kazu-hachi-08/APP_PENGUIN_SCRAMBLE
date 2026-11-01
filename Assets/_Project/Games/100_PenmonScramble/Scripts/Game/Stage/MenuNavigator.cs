using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Game
{
    /// <summary>
    /// メニューを方向キー・WASD・十字キー・スティックで選び、Enter / ×（南のボタン）で決める入力。
    /// 押し続けると、少し待ってから一定の間隔で進む（スティックを倒しっぱなしでも1つずつ選べるように）。
    /// 時間は止めた時間（Time.timeScale = 0）の中でも進む実時間で測る
    /// </summary>
    public sealed class MenuNavigator
    {
        private const float StickThreshold = 0.5f;
        private const float RepeatDelay = 0.4f;
        private const float RepeatInterval = 0.12f;

        private Vector2Int _held;
        private float _nextRepeatAt;

        /// <summary>このフレームに進んだ向き（上が +y）。進まなければ 0</summary>
        public Vector2Int Step { get; private set; }

        /// <summary>このフレームに決定を押した</summary>
        public bool Submit { get; private set; }

        public void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            UpdateStep(ReadDirection(keyboard, gamepad));
            Submit = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>開いた直後など、前から押していた向きで勝手に進まないようにする</summary>
        public void Reset()
        {
            // いま倒している向きは「押し始め」にしない（押し続けたときの繰り返しも、待ちからやり直す）
            _held = ReadDirection(Keyboard.current, Gamepad.current);
            _nextRepeatAt = Time.unscaledTime + RepeatDelay;
            Step = Vector2Int.zero;
            Submit = false;
        }

        private void UpdateStep(Vector2Int direction)
        {
            Step = Vector2Int.zero;
            var now = Time.unscaledTime;
            if (direction != _held)
            {
                _held = direction;
                Step = direction;
                _nextRepeatAt = now + RepeatDelay;
                return;
            }

            if (direction != Vector2Int.zero && now >= _nextRepeatAt)
            {
                Step = direction;
                _nextRepeatAt = now + RepeatInterval;
            }
        }

        /// <summary>縦と横を同時に倒したときは、大きく倒した方だけにする（斜めで2つ動かないように）</summary>
        private static Vector2Int ReadDirection(Keyboard keyboard, Gamepad gamepad)
        {
            var value = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) value.y += 1f;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) value.y -= 1f;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) value.x -= 1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) value.x += 1f;
            }

            if (gamepad != null)
            {
                value += gamepad.dpad.ReadValue() + gamepad.leftStick.ReadValue();
            }

            if (Mathf.Abs(value.y) >= Mathf.Abs(value.x))
            {
                return new Vector2Int(0, Mathf.Abs(value.y) >= StickThreshold ? (int)Mathf.Sign(value.y) : 0);
            }

            return new Vector2Int(Mathf.Abs(value.x) >= StickThreshold ? (int)Mathf.Sign(value.x) : 0, 0);
        }
    }
}
