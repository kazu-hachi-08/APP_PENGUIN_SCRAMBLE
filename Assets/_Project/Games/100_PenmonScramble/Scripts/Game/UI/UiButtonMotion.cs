using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// メニューのボタンの手ごたえ: マウスを乗せると少し大きく、押している間は縮み、押したら音を鳴らす。
    /// 押せないときは全体（色の面・つや・縁・文字）をまとめて薄くする（Button の色の変化は色の面にしか掛からないため）
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UiButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _hoverScale = 1.04f;
        [SerializeField] private float _pressedScale = 0.93f;
        [SerializeField] private float _scalePerSecond = 18f;
        [SerializeField] private float _disabledAlpha = 0.45f;

        private Button _button;
        private CanvasGroup _group;
        private bool _held;
        private bool _hovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }

            _button.onClick.AddListener(() => GameSounds.Play(GameSound.Click));
        }

        private void OnDisable()
        {
            // 押したまま画面が閉じても、次に開いたときに縮んだままにならないように
            _held = false;
            _hovered = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData) => _held = true;

        public void OnPointerUp(PointerEventData eventData) => _held = false;

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        private void Update()
        {
            var interactable = _button.IsInteractable();
            _group.alpha = interactable ? 1f : _disabledAlpha;
            var target = !interactable ? 1f : _held ? _pressedScale : _hovered ? _hoverScale : 1f;
            // メニューは止めた時間（Time.timeScale = 0）の中でも開くので、実時間で動かす
            var scale = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-_scalePerSecond * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * scale;
        }
    }
}
