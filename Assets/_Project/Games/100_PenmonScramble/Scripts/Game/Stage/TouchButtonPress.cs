using PenmonScramble.Common.Input;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 押している間だけボタンの見た目を少し縮める。押せたことが指の下でも分かるように。
    /// 縮めるのは見た目（子）だけで、触れる範囲は変えない
    /// </summary>
    [RequireComponent(typeof(VirtualButton))]
    public class TouchButtonPress : MonoBehaviour
    {
        [SerializeField] private RectTransform _visual;
        [SerializeField] private float _pressedScale = 0.88f;
        [SerializeField] private float _scalePerSecond = 20f;

        private VirtualButton _button;

        private void Awake()
        {
            _button = GetComponent<VirtualButton>();
        }

        private void Update()
        {
            var target = _button.IsHeld ? _pressedScale : 1f;
            var scale = Mathf.Lerp(_visual.localScale.x, target, 1f - Mathf.Exp(-_scalePerSecond * Time.unscaledDeltaTime));
            _visual.localScale = Vector3.one * scale;
        }
    }
}
