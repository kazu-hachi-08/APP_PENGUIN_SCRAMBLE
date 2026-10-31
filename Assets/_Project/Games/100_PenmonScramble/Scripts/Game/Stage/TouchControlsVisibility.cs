using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 仮想スティック・ボタンは、タッチできる端末でだけ出す（PC では画面が狭くなるだけなので）。
    /// 出すのはステージで遊んでいる間だけ（タイトル・ロビー・結果では隠す）。出し入れは短くフェードする
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TouchControlsVisibility : MonoBehaviour
    {
        [SerializeField] private float _fadeSeconds = 0.2f;

        private CanvasGroup _group;
        private bool _shown;

        private void Awake()
        {
            gameObject.SetActive(Application.isMobilePlatform || Touchscreen.current != null);
            _group = GetComponent<CanvasGroup>();
            Apply(0f);
        }

        public void SetShown(bool shown)
        {
            _shown = shown;
        }

        private void Update()
        {
            var target = _shown ? 1f : 0f;
            Apply(Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / _fadeSeconds));
        }

        private void Apply(float alpha)
        {
            _group.alpha = alpha;
            // 消えかけのボタンを押せないよう、押せるかどうかはフェードを待たずに切り替える
            _group.blocksRaycasts = _shown;
            _group.interactable = _shown;
        }
    }
}
