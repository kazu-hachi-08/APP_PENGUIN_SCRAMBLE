using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>結果の画面に出すもの</summary>
    public readonly struct ResultInfo
    {
        public ResultInfo(bool cleared, bool canRetry, string stageName, int enemiesDefeated, StageReward reward, string unlockedStageName)
        {
            Cleared = cleared;
            CanRetry = canRetry;
            StageName = stageName;
            EnemiesDefeated = enemiesDefeated;
            Reward = reward;
            UnlockedStageName = unlockedStageName;
        }

        public bool Cleared { get; }

        /// <summary>「もう一度」を押せるのはホストだけ（ゲストはホストが始め直すのを待つ）</summary>
        public bool CanRetry { get; }

        public string StageName { get; }
        public int EnemiesDefeated { get; }
        public StageReward Reward { get; }

        /// <summary>新しく遊べるようになったステージの名前。無ければ null</summary>
        public string UnlockedStageName { get; }
    }

    /// <summary>
    /// 結果の画面: 見出しがまん中に叩きつけられ（クリアなら紙ふぶき）、カメラがペンモンへ寄るのに合わせて右の列へ移る。
    /// 続けて札が上から順にすべり込み（倒した数 → コイン → 初クリア ボーナス → 次のステージの解放）、最後にボタンが出る。
    /// 途中で画面を押すと（攻撃のボタンでも）、最後の姿まで飛ばす。時間はメニューと同じく実時間で進める
    /// </summary>
    public class ResultScreen : MonoBehaviour, IPointerClickHandler
    {
        // 流れ（秒）: 最後の一撃を少し見せてから見出し → まん中で見せてから右へ → 札を順に → ボタン
        private const float HeadlineAt = 0.3f;
        private const float GlideAt = 1.1f;
        private const float FirstRowAt = 1.5f;
        private const float RowInterval = 0.55f;
        private const float ButtonsDelay = 0.4f;
        private const float ButtonsSlideSeconds = 0.35f;
        private const float ShadeFadeSeconds = 0.5f;
        // コインの札がすべり込んで止まるころに、コインを散らす
        private const float CoinBurstDelay = 0.2f;
        private const float CoinSoundInterval = 0.04f;
        private const string ClearTitle = "クリア！";
        private const string FailTitle = "やられた…";
        private const string GuestNote = "ホストが「もう一度」を押すと始まります";

        [SerializeField] private Image _shade;
        [SerializeField] private Color _clearShadeColor = new Color(0.06f, 0.09f, 0.22f, 0.82f);
        [SerializeField] private Color _failShadeColor = new Color(0.04f, 0.07f, 0.2f, 0.9f);
        [SerializeField] private Color _clearTitleColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color _failTitleColor = new Color(0.7f, 0.82f, 1f);
        [SerializeField] private ResultHeadline _headline;
        [SerializeField] private ResultRow _defeatedRow;
        [SerializeField] private ResultRow _coinRow;
        [SerializeField] private RectTransform _coinIcon;
        [SerializeField] private ResultRow _bonusRow;
        [SerializeField] private ResultRow _unlockRow;
        [SerializeField] private Text _unlockText;
        [Tooltip("札を並べる列の、一番上の札の位置と札の間隔（出さない札は詰める）")]
        [SerializeField] private float _rowTop = 110f;
        [SerializeField] private float _rowSpacing = 96f;
        [SerializeField] private CanvasGroup _buttons;
        [SerializeField] private Vector2 _buttonsSlideFrom = new Vector2(0f, -160f);
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;
        [SerializeField] private Text _guestNote;
        [SerializeField] private ResultConfetti _confetti;
        [SerializeField] private ResultCoinBurst _coinBurst;

        private ResultRow[] _shownRows = Array.Empty<ResultRow>();
        private RectTransform _buttonsRect;
        private Vector2 _buttonsHome;
        private Vector2 _titleButtonHome;
        private ResultInfo _info;
        private float _elapsed = -1f;
        private float _buttonsAt;
        private bool _confettiFired;
        private bool _stampPlayed;
        private bool _coinsFired;
        private int _unlockSoundsPlayed;
        private float _lastCoinSoundAt = float.NegativeInfinity;

        /// <summary>カメラがペンモンへ寄り始める秒。見出しが右へ移るのと同時に、空いたまん中へペンモンが映る</summary>
        public const float StageCueSeconds = GlideAt - 0.1f;

        public event Action RetryRequested;
        public event Action TitleRequested;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>ボタンまで出そろった（ここからは攻撃のボタンで「もう一度」になる）</summary>
        public bool IsSettled => IsShown && _elapsed >= SettledAt;

        /// <summary>出そろってからの秒。出そろう前は負</summary>
        public float SettledFor => IsShown ? _elapsed - SettledAt : -1f;

        private float SettledAt => _buttonsAt + ButtonsSlideSeconds;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            _titleButton.onClick.AddListener(() => TitleRequested?.Invoke());
            _coinBurst.Arrived += OnCoinArrived;
            _buttonsRect = (RectTransform)_buttons.transform;
            _buttonsHome = _buttonsRect.anchoredPosition;
            _titleButtonHome = ((RectTransform)_titleButton.transform).anchoredPosition;
        }

        private void OnDestroy()
        {
            _coinBurst.Arrived -= OnCoinArrived;
        }

        public void Show(ResultInfo info)
        {
            _info = info;
            gameObject.SetActive(true);
            _elapsed = 0f;
            _confettiFired = _stampPlayed = _coinsFired = false;
            _unlockSoundsPlayed = 0;
            _shade.color = info.Cleared ? _clearShadeColor : _failShadeColor;

            var screenCenter = ((RectTransform)transform).TransformPoint(Vector3.zero);
            _headline.Setup(info.Cleared ? ClearTitle : FailTitle, info.Cleared ? _clearTitleColor : _failTitleColor, info.StageName, info.Cleared, screenCenter, GlideAt - HeadlineAt);
            SetupRows(info);
            _retryButton.gameObject.SetActive(info.CanRetry);
            // ゲストは「タイトルへ」だけなので、まん中に置く
            ((RectTransform)_titleButton.transform).anchoredPosition = info.CanRetry ? _titleButtonHome : new Vector2(0f, _titleButtonHome.y);
            _guestNote.gameObject.SetActive(!info.CanRetry);
            _guestNote.text = GuestNote;
            Evaluate(silent: true);
        }

        public void Hide()
        {
            _confetti.Clear();
            _coinBurst.Clear();
            _elapsed = -1f;
            gameObject.SetActive(false);
        }

        /// <summary>演出を最後まで飛ばす（出そろっていれば何もしない）</summary>
        public void Skip()
        {
            if (!IsShown || IsSettled)
            {
                return;
            }

            _elapsed = SettledAt;
            // 飛ばしたら散っている途中のコインは消し、音もまとめて鳴らさない
            _coinsFired = _stampPlayed = true;
            _unlockSoundsPlayed = int.MaxValue;
            _coinBurst.Clear();
            Evaluate(silent: true);
        }

        public void OnPointerClick(PointerEventData eventData) => Skip();

        /// <summary>出す札だけを上から詰めて並べ、出る時刻を決める</summary>
        private void SetupRows(ResultInfo info)
        {
            var reward = info.Reward;
            var hasBonus = reward.FirstClearBonus > 0;
            var hasUnlock = !string.IsNullOrEmpty(info.UnlockedStageName);
            _unlockText.text = hasUnlock ? $"「{info.UnlockedStageName}」解放！" : string.Empty;

            var rows = new[] { _defeatedRow, _coinRow, _bonusRow, _unlockRow };
            var shown = new[] { true, true, hasBonus, hasUnlock };
            var targets = new[] { info.EnemiesDefeated, reward.Coins, reward.FirstClearBonus, -1 };
            var formats = new[] { "{0} 体", "+{0}", "+{0}", string.Empty };
            var list = new System.Collections.Generic.List<ResultRow>();
            for (var i = 0; i < rows.Length; i++)
            {
                var home = new Vector2(0f, _rowTop - _rowSpacing * list.Count);
                rows[i].Setup(shown[i], home, targets[i], formats[i]);
                if (shown[i])
                {
                    list.Add(rows[i]);
                }
            }

            _shownRows = list.ToArray();
            _buttonsAt = RowAt(_shownRows.Length - 1) + RowInterval + ButtonsDelay;
        }

        private static float RowAt(int index) => FirstRowAt + RowInterval * index;

        /// <summary>出そろった後も進め続ける（光の筋が回り、「解放！」の札がふくらみ続ける）。音や演出は一度ずつしか出ない</summary>
        private void Update()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            Evaluate(silent: false);
        }

        private void Evaluate(bool silent)
        {
            _shade.canvasRenderer.SetAlpha(Mathf.Clamp01(_elapsed / ShadeFadeSeconds));
            _headline.Evaluate(_elapsed - HeadlineAt);
            FireHeadlineEffects(silent);
            for (var i = 0; i < _shownRows.Length; i++)
            {
                _shownRows[i].Evaluate(_elapsed - RowAt(i), silent);
            }

            FireRowEffects(silent);
            EvaluateButtons();
        }

        private void FireHeadlineEffects(bool silent)
        {
            if (_elapsed < HeadlineAt)
            {
                return;
            }

            if (!_stampPlayed && !silent)
            {
                _stampPlayed = true;
                GameSounds.Play(GameSound.ResultStamp);
            }

            // 紙ふぶきは飛ばしても見せる（クリアの華やかさは残したいので）
            if (!_confettiFired && _info.Cleared)
            {
                _confettiFired = true;
                _confetti.Burst();
            }
        }

        /// <summary>コインの札が出たらコインを散らす。初クリアのボーナス・解放の札が出たら、そのたびに音を鳴らす</summary>
        private void FireRowEffects(bool silent)
        {
            var coinIndex = Array.IndexOf(_shownRows, _coinRow);
            if (!_coinsFired && coinIndex >= 0 && _elapsed >= RowAt(coinIndex) + CoinBurstDelay && _info.Reward.Coins > 0)
            {
                _coinsFired = true;
                _coinBurst.Play(_headline.transform.position, _coinIcon);
            }

            if (silent)
            {
                return;
            }

            var specials = 0;
            for (var i = 0; i < _shownRows.Length; i++)
            {
                var special = _shownRows[i] == _bonusRow || _shownRows[i] == _unlockRow;
                if (special && _elapsed >= RowAt(i) && ++specials > _unlockSoundsPlayed)
                {
                    _unlockSoundsPlayed = specials;
                    GameSounds.Play(GameSound.ResultUnlock);
                }
            }
        }

        private void OnCoinArrived()
        {
            _coinRow.Bump();
            if (Time.unscaledTime - _lastCoinSoundAt >= CoinSoundInterval)
            {
                _lastCoinSoundAt = Time.unscaledTime;
                GameSounds.Play(GameSound.ResultCoin);
            }
        }

        /// <summary>ボタンは下からすべり込む。出るまでは押せない（見えないボタンを押してしまわないように）</summary>
        private void EvaluateButtons()
        {
            var t = Mathf.Clamp01((_elapsed - _buttonsAt) / ButtonsSlideSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            _buttonsRect.anchoredPosition = _buttonsHome + _buttonsSlideFrom * (1f - eased);
            _buttons.alpha = t;
            _buttons.interactable = _buttons.blocksRaycasts = t >= 1f;
        }
    }
}
