using System.Collections.Generic;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーの HUD: 上のまん中に赤・青の点とその間の残り時間（残り10秒から赤く弾む）、点の下にチームの必殺ゲージ、
    /// まん中に知らせ（「ゴール！」など）、PC では下に操作の説明と、ゲージが満タンのときの「L / △ で必殺シュート！」、
    /// スマホでは仮想スティックと技1・技2・必殺・交代のボタン。PK戦の間は点の下に ○× の表（SoccerHud.Shootout.cs）
    /// </summary>
    public partial class SoccerHud : MonoBehaviour
    {
        private static readonly Color UrgentColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color GaugeReadyColor = new Color(1f, 0.85f, 0.25f);

        [SerializeField] private Text _redScore;
        [SerializeField] private Text _blueScore;
        [SerializeField] private Text _time;
        [SerializeField] private Text _banner;
        [SerializeField] private GameObject _help;
        [SerializeField] private TouchControlsVisibility _touch;
        [SerializeField] private SoccerTouchView _touchView;

        [Header("必殺ゲージ（赤・青の順）")]
        [SerializeField] private Image[] _gauges;
        [Tooltip("ゲージ（と後ろの板）をまとめた物。PK戦の間は表と入れ替えて隠す")]
        [SerializeField] private GameObject _gaugeGroup;
        [Tooltip("PC で自分のチームのゲージが満タンのときに出す一言")]
        [SerializeField] private Text _specialHint;
        [SerializeField] private float _gaugePulsePerSecond = 3f;

        [Header("知らせ")]
        [SerializeField] private float _bannerPopSeconds = 0.25f;
        [SerializeField] private float _bannerPopScale = 1.6f;
        [SerializeField] private float _bannerFadeSeconds = 0.3f;

        [Header("点・時間")]
        [SerializeField] private float _scorePopSeconds = 0.4f;
        [SerializeField] private float _scorePopScale = 1.6f;
        [SerializeField] private float _timePopScale = 1.35f;

        private Color[] _gaugeColors;
        private bool _isTouch;
        private int _red = -1;
        private int _blue = -1;
        private int _shownSeconds = -1;
        private float _bannerLeft;
        private float _bannerTime;
        private float _redPop = 1f;
        private float _bluePop = 1f;
        private float _timePop = 1f;
        private float _gaugeTime;

        private void Awake()
        {
            _banner.gameObject.SetActive(false);
            // スマホ（触れる画面）では説明の代わりにボタンを出す
            _isTouch = Application.isMobilePlatform || Touchscreen.current != null;
            _help.SetActive(!_isTouch);
            _specialHint.gameObject.SetActive(false);
            _gaugeColors = new Color[_gauges.Length];
            for (var i = 0; i < _gauges.Length; i++)
            {
                _gaugeColors[i] = _gauges[i].color;
            }

            AwakeShootout();
        }

        public void SetPlaying(bool playing)
        {
            gameObject.SetActive(playing);
            _touch.SetShown(playing);
            // 結果で隠した説明を、もう一度の試合で出し直す
            _help.SetActive(playing && !_isTouch);
        }

        /// <summary>結果を出す間: 点（と PK戦の表）だけ残し、ボタン・説明・ためゲージを隠す</summary>
        public void SetResult()
        {
            _touch.SetShown(false);
            _help.SetActive(false);
            _specialHint.gameObject.SetActive(false);
            HidePenaltyAim();
        }

        /// <summary>点が増えた側の数字を弾ませる</summary>
        public void SetScore(int red, int blue)
        {
            if (_red >= 0 && red > _red)
            {
                _redPop = 0f;
            }

            if (_blue >= 0 && blue > _blue)
            {
                _bluePop = 0f;
            }

            _red = red;
            _blue = blue;
            _redScore.text = red.ToString();
            _blueScore.text = blue.ToString();
        }

        /// <summary>残り時間（m:ss。秒は切り上げ）。残り10秒からは赤く</summary>
        public void SetTimeLeft(float seconds)
        {
            var whole = Mathf.CeilToInt(seconds);
            if (whole == _shownSeconds)
            {
                return;
            }

            _shownSeconds = whole;
            _time.text = $"{whole / 60}:{whole % 60:00}";
            _time.color = whole <= SoccerRules.CountdownFrom ? UrgentColor : Color.white;
        }

        /// <summary>残り10秒からの1秒ごとに、時間を弾ませる</summary>
        public void PulseTime() => _timePop = 0f;

        /// <summary>
        /// チームのゲージ（0〜1）。満タンのゲージは金色で脈打つ。ownTeam のゲージが満タンなら、PC では押すボタンを出す
        /// </summary>
        public void SetGauges(SoccerWorld world, int ownTeam, bool canUse)
        {
            for (var i = 0; i < _gauges.Length && i < SoccerRules.TeamCount; i++)
            {
                var gauge = world.Gauge(i);
                _gauges[i].fillAmount = gauge;
                var pulse = 0.5f + 0.5f * Mathf.Sin(_gaugeTime * _gaugePulsePerSecond * Mathf.PI * 2f);
                _gauges[i].color = gauge >= 1f ? Color.Lerp(GaugeReadyColor, Color.white, pulse * 0.6f) : _gaugeColors[i];
            }

            _specialHint.gameObject.SetActive(!_isTouch && canUse && world.IsSpecialReady(ownTeam));
        }

        /// <summary>スマホのボタンの中身を、動かしている子に合わせる（毎フレーム）</summary>
        public void ShowControls(SoccerWorld world, SoccerControl control, IReadOnlyList<int> species)
        {
            _touchView.Show(world, control, species);
        }

        /// <summary>まん中に大きく出して、seconds 秒で消す</summary>
        public void ShowBanner(string text, float seconds)
        {
            _banner.text = text;
            _banner.gameObject.SetActive(true);
            _bannerLeft = seconds;
            _bannerTime = 0f;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _gaugeTime += dt;
            TickBanner(dt);
            _redPop = Pop(_redScore.transform, _redPop, dt, _scorePopScale);
            _bluePop = Pop(_blueScore.transform, _bluePop, dt, _scorePopScale);
            _timePop = Pop(_time.transform, _timePop, dt, _timePopScale);
        }

        /// <summary>大きく出てから縮んで止まり、最後に薄くなって消える</summary>
        private void TickBanner(float dt)
        {
            if (!_banner.gameObject.activeSelf)
            {
                return;
            }

            _bannerTime += dt;
            _bannerLeft -= dt;
            var pop = Mathf.Clamp01(_bannerTime / _bannerPopSeconds);
            _banner.transform.localScale = Vector3.one * Mathf.Lerp(_bannerPopScale, 1f, 1f - (1f - pop) * (1f - pop));
            var color = _banner.color;
            color.a = Mathf.Clamp01(_bannerLeft / _bannerFadeSeconds);
            _banner.color = color;
            if (_bannerLeft <= 0f)
            {
                _banner.gameObject.SetActive(false);
            }
        }

        private float Pop(Transform target, float progress, float dt, float scale)
        {
            progress = Mathf.Min(1f, progress + dt / _scorePopSeconds);
            target.localScale = Vector3.one * Mathf.Lerp(scale, 1f, progress);
            return progress;
        }
    }
}
