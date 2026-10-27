using System;
using System.Text;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中の画面表示: 場に出ている子の HP・3体の様子・必殺技のゲージ・残りの敵・結果
    /// </summary>
    public class StageHud : MonoBehaviour
    {
        // ゲージ満タンのときに光らせる速さ（§3「ゲージ満タンで光る」）
        private const float ReadyPulsePerSecond = 3f;
        private const float IdleButtonAlpha = 0.35f;

        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _partyText;
        [SerializeField] private RectTransform _gaugeFill;
        [SerializeField] private Image _gaugeFillImage;
        [SerializeField] private Text _gaugeText;
        [SerializeField] private Color _gaugeColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color _gaugeReadyColor = new Color(1f, 0.95f, 0.4f);
        [SerializeField] private Image _specialButtonImage;
        [SerializeField] private Text _enemiesText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        private readonly StringBuilder _builder = new StringBuilder();
        private string _playerLabel = string.Empty;
        private bool _specialReady;

        public event Action RetryRequested;
        public event Action TitleRequested;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            _titleButton.onClick.AddListener(() => TitleRequested?.Invoke());
        }

        /// <summary>バーを自分のペンギンと同じ色にして、協力のときにどれが自分か分かるようにする</summary>
        public void SetPlayer(string label, Color color)
        {
            _hpFillImage.color = color;
            _playerLabel = label;
        }

        /// <param name="fallen">3体とも倒れて場から消えた。最後に見た3体を HP 0 で出す</param>
        public void SetParty(PlayerState state, bool fallen)
        {
            var active = state.Members[state.ActiveIndex];
            var hp = fallen ? 0 : active.Hp;
            var ratio = active.MaxHp > 0 ? (float)hp / active.MaxHp : 0f;
            _hpFill.anchorMax = new Vector2(ratio, 1f);
            _hpText.text = $"{_playerLabel}  {PenguinPalette.SpeciesName(active.SpeciesNo)}  HP {hp} / {active.MaxHp}";
            _partyText.text = PartyLine(state, fallen);
            SetGauge(fallen ? 0f : state.SpecialGauge);
        }

        /// <summary>「▶アデリン 120 ｜ ヒゲジイ 130 ｜ ×ジェンツ　交代OK」のように、3体の HP と交代できるかを1行で出す</summary>
        private string PartyLine(PlayerState state, bool fallen)
        {
            _builder.Clear();
            for (var i = 0; i < state.Members.Count; i++)
            {
                var member = state.Members[i];
                var hp = fallen ? 0 : member.Hp;
                if (i > 0)
                {
                    _builder.Append(" ｜ ");
                }

                _builder.Append(i == state.ActiveIndex ? "▶" : hp <= 0 ? "×" : "　");
                _builder.Append(PenguinPalette.SpeciesName(member.SpeciesNo)).Append(' ').Append(hp);
            }

            if (!fallen)
            {
                _builder.Append(state.SwitchCooldown > 0f ? $"　交代 {state.SwitchCooldown:0.0}" : "　交代OK");
            }

            return _builder.ToString();
        }

        private void SetGauge(float gauge)
        {
            _gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(gauge), 1f);
            _specialReady = gauge >= 1f;
            _gaugeText.text = _specialReady ? "必殺技 OK！" : "必殺技";
        }

        private void Update()
        {
            var pulse = _specialReady ? 0.5f + 0.5f * Mathf.Sin(Time.time * ReadyPulsePerSecond * Mathf.PI * 2f) : 0f;
            _gaugeFillImage.color = _specialReady ? Color.Lerp(_gaugeReadyColor, Color.white, pulse) : _gaugeColor;
            if (_specialButtonImage != null)
            {
                var color = _specialButtonImage.color;
                color.a = _specialReady ? Mathf.Lerp(0.7f, 1f, pulse) : IdleButtonAlpha;
                _specialButtonImage.color = color;
            }
        }

        public void SetEnemiesLeft(int count)
        {
            _enemiesText.text = $"のこり {count} 体";
        }

        /// <param name="canRetry">「もう一度」を押せるのはホストだけ（ゲストはホストが始め直すのを待つ）</param>
        public void ShowResult(bool cleared, bool canRetry)
        {
            var title = cleared ? "クリア！" : "やられた…";
            _resultText.text = canRetry ? title : $"{title}\n<size=48>ホストが「もう一度」を押すと始まります</size>";
            _retryButton.gameObject.SetActive(canRetry);
            _resultPanel.SetActive(true);
        }

        public void HideResult()
        {
            _resultPanel.SetActive(false);
        }
    }
}
