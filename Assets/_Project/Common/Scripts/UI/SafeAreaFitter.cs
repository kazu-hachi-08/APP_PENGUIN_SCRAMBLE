using UnityEngine;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// RectTransform を Screen.safeArea に合わせる
    /// ノッチやホームバーにボタンが被ってスマホで押しにくくなるのを防ぐため
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private Vector2Int _appliedScreenSize;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        // 画面回転や分割表示で safeArea が途中で変わるため、差分があるときだけ再適用する
        private void Update()
        {
            if (Screen.safeArea != _appliedArea
                || Screen.width != _appliedScreenSize.x
                || Screen.height != _appliedScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var min = _appliedArea.position;
            var max = _appliedArea.position + _appliedArea.size;
            _rect.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
            _rect.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
