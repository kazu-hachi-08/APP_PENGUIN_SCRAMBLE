using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 遊び方の操作の絵の、キー1つ（コントローラーのボタン1つ）。押している間は光って少し縮む。
    /// 絵を見ながら実際に押して、どのキーが何かをその場で確かめられるようにする
    /// </summary>
    public class HowToPlayKey : MonoBehaviour
    {
        // スティックは少し倒しただけでは光らせない（触っていないのに光らないように）
        private const float ActuationThreshold = 0.5f;
        private const float PressedScale = 0.92f;

        [SerializeField] private Image _face;
        [SerializeField] private Key[] _keys = new Key[0];
        /// <summary>Gamepad の子のコントロール名（"buttonSouth"・"leftStick" など）</summary>
        [SerializeField] private string[] _padControls = new string[0];
        [SerializeField] private Color _litColor = new Color(1f, 0.82f, 0.30f);

        private Color _idleColor;

        private void Awake()
        {
            _idleColor = _face.color;
        }

        private void OnDisable()
        {
            Show(false);
        }

        private void Update()
        {
            Show(IsPressed());
        }

        private void Show(bool lit)
        {
            _face.color = lit ? _litColor : _idleColor;
            transform.localScale = Vector3.one * (lit ? PressedScale : 1f);
        }

        private bool IsPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                foreach (var key in _keys)
                {
                    if (keyboard[key].isPressed)
                    {
                        return true;
                    }
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            foreach (var path in _padControls)
            {
                var control = gamepad.TryGetChildControl(path);
                if (control != null && control.IsActuated(ActuationThreshold))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
