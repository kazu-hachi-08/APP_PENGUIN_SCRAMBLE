using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニューの札1枚（HUD と同じ斜めに切った札）。選ばれると前（右）へ出て大きくなり、後ろが光って左に ▶ が出る。
    /// 開くときは外からすべり込み（少し行き過ぎて戻る）、閉じるときは外へ抜けていく。動きは止めた時間の中でも実時間で進める。
    /// 押したか・マウスが乗ったかは StageMenu / StageMenuConfirm へ知らせるだけで、選ぶのはそちらが決める
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StageMenuCard : MonoBehaviour, IPointerEnterHandler
    {
        private const float DimBrightness = 0.55f;

        [SerializeField] private RectTransform _body;
        [SerializeField] private Image _plate;
        [SerializeField] private Image _glow;
        [SerializeField] private GameObject _arrow;
        [SerializeField] private Text _label;
        [SerializeField] private Color _color = Color.white;
        [Tooltip("外のどこからすべり込むか（いる場所からのずれ）")]
        [SerializeField] private Vector2 _slideFrom = new Vector2(-1100f, 0f);
        [SerializeField] private float _inSeconds = 0.3f;
        [SerializeField] private float _outSeconds = 0.14f;
        [SerializeField] private float _overshoot = 1.4f;
        [Tooltip("選ばれたときに前へ出る量")]
        [SerializeField] private Vector2 _selectedShift = new Vector2(44f, 0f);
        [SerializeField] private float _selectedScale = 0.1f;
        [SerializeField] private float _selectPerSecond = 16f;
        [SerializeField] private float _glowPulsePerSecond = 1.2f;

        private Button _button;
        private RectTransform _rect;
        private Vector2 _home;
        private bool _selected;
        private float _selectAmount;
        private float _elapsed;
        private float _delay;
        private bool _leaving;

        public event Action<StageMenuCard> Hovered;
        public event Action<StageMenuCard> Pressed;

        private void Awake() => Init();

        /// <summary>閉じた入れ物の中にあって Awake がまだでも、StageMenu が先に並べ直せるように</summary>
        private void Init()
        {
            if (_rect != null)
            {
                return;
            }

            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => Pressed?.Invoke(this));
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);

        /// <summary>いる場所。やり直しを出さないときに、残りの札を詰めて並べ直すため</summary>
        public void Place(Vector2 home)
        {
            Init();
            _home = home;
            Apply();
        }

        public void SetLabel(string label) => _label.text = label;

        public void SetSelected(bool selected) => _selected = selected;

        /// <summary>選び直しをすぐに見た目へ出す（開いた瞬間に、前の選びから動いて見えないように）</summary>
        public void SnapSelection()
        {
            Init();
            _selectAmount = _selected ? 1f : 0f;
            Apply();
        }

        public void PlayIn(float delay)
        {
            Init();
            gameObject.SetActive(true);
            _leaving = false;
            _delay = delay;
            _elapsed = 0f;
            Apply();
        }

        public void PlayOut(float delay)
        {
            _leaving = true;
            _delay = delay;
            _elapsed = 0f;
        }

        /// <summary>閉じ始めてから抜け終わるまでの秒</summary>
        public float OutDuration(float delay) => delay + _outSeconds;

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            _selectAmount = Mathf.MoveTowards(_selectAmount, _selected ? 1f : 0f, _selectPerSecond * dt);
            Apply();
        }

        private void Apply()
        {
            var ease = EaseSelect(_selectAmount);
            _rect.anchoredPosition = _home + _selectedShift * ease + _slideFrom * SlideAway();
            _body.localScale = Vector3.one * (1f + _selectedScale * ease);
            _plate.color = Color.Lerp(Dim(_color), _color, ease);
            _arrow.SetActive(_selected);
            var glow = _glow.color;
            var pulse = ToyEasing.Pulse01(Time.unscaledTime, _glowPulsePerSecond);
            glow.a = ease * Mathf.Lerp(0.55f, 1f, pulse);
            _glow.color = glow;
        }

        /// <summary>いる場所から外までの割合（0 でいる場所、1 で外）</summary>
        private float SlideAway()
        {
            var seconds = _leaving ? _outSeconds : _inSeconds;
            var t = Mathf.Clamp01((_elapsed - _delay) / seconds);
            // 抜けるときは加速して出ていく。入るときは行き過ぎて戻る
            return _leaving ? t * t : 1f - BackOut(t);
        }

        private float BackOut(float t)
        {
            var u = t - 1f;
            return 1f + (_overshoot + 1f) * u * u * u + _overshoot * u * u;
        }

        private static float EaseSelect(float t) => 1f - (1f - t) * (1f - t);

        private static Color Dim(Color color)
        {
            return new Color(color.r * DimBrightness, color.g * DimBrightness, color.b * DimBrightness, color.a);
        }
    }
}
