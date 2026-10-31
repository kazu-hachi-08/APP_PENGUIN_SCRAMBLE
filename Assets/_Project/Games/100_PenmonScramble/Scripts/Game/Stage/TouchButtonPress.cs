using PenmonScramble.Common.Input;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 押している間だけ、ボタンの面（Top）を厚みのぶん沈めて少し縮める。押せたことが指の下でも分かるように。
    /// 動かすのは見た目（子）だけで、触れる範囲は変えない
    /// </summary>
    [RequireComponent(typeof(VirtualButton))]
    public class TouchButtonPress : MonoBehaviour
    {
        [SerializeField] private RectTransform _visual;
        [Tooltip("押すと沈む面。下に残る厚み（Base）と影はそのまま")]
        [SerializeField] private RectTransform _top;
        [SerializeField] private float _pressDepth = 12f;
        [SerializeField] private float _pressedScale = 0.94f;
        [SerializeField] private float _speed = 24f;

        private VirtualButton _button;
        private float _pressed;

        private void Awake()
        {
            _button = GetComponent<VirtualButton>();
        }

        private void Update()
        {
            var target = _button.IsHeld ? 1f : 0f;
            _pressed = Mathf.Lerp(_pressed, target, 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime));
            _visual.localScale = Vector3.one * Mathf.Lerp(1f, _pressedScale, _pressed);
            _top.anchoredPosition = new Vector2(0f, -_pressDepth * _pressed);
        }
    }
}
