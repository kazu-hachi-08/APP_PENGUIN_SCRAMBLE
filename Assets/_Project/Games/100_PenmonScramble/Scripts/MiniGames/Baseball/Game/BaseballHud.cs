using System;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球の HUD: 左上にカウント（B・S・O の丸）、まん中に結果の大きな文字（ホームラン・チェンジ・ゲームセットは帯つき）。
    /// 上のまん中のスコアボード（BaseballHud.Match.cs）、左右の打順のカードと魔球・必殺打の札（BaseballHud.Lineup.cs）。
    /// 投げる番では下に球種の札3枚（押すと投げる。PC ではキーの印つき）と1球の持ち時間。
    /// PC では下に操作の説明、スマホでは打つ番に「振る」「バント」のボタン、投げる番に曲げるスティックを出す
    /// </summary>
    public partial class BaseballHud : MonoBehaviour
    {
        private static readonly Color LightOff = new Color(0.22f, 0.24f, 0.30f);
        private static readonly Color BallOn = new Color(0.35f, 0.88f, 0.40f);
        private static readonly Color StrikeOn = new Color(1f, 0.82f, 0.22f);
        private static readonly Color OutOn = new Color(1f, 0.32f, 0.28f);
        private static readonly Color UrgentColor = new Color(1f, 0.4f, 0.35f);

        [Header("カウント（左から順に光る）")]
        [SerializeField] private GameObject _countPlate;
        [SerializeField] private Image[] _ballLights;
        [SerializeField] private Image[] _strikeLights;
        [SerializeField] private Image[] _outLights;

        [Header("知らせ")]
        [SerializeField] private Text _banner;
        [Tooltip("ホームラン・チェンジ・ゲームセットのときだけ、字の後ろに横いっぱいの帯を出す")]
        [SerializeField] private Image _bannerBand;
        [SerializeField] private float _bannerPopSeconds = 0.25f;
        [SerializeField] private float _bannerPopScale = 1.6f;
        [SerializeField] private float _bannerFadeSeconds = 0.3f;
        [Tooltip("帯が横から伸びきるまでの秒")]
        [SerializeField] private float _bandOpenSeconds = 0.18f;

        [Header("投げる番")]
        [Tooltip("球種の札をまとめた物（投げる番の構えの間だけ出す）")]
        [SerializeField] private GameObject _pitchCards;
        [Tooltip("ストレート・カーブ・チェンジアップの順")]
        [SerializeField] private Button[] _pitchButtons;
        [Tooltip("札の下のキーの印（スマホでは隠す）")]
        [SerializeField] private GameObject[] _keyHints;
        [SerializeField] private Text _clock;
        [Tooltip("残りがこの秒以下になったら持ち時間を赤くする")]
        [SerializeField] private float _clockUrgentSeconds = 3f;

        [Header("操作")]
        [SerializeField] private Text _help;
        [SerializeField] private TouchControlsVisibility _touch;
        [Tooltip("スマホの「振る」「バント」のボタン（打つ番だけ）")]
        [SerializeField] private GameObject[] _batButtons;
        [Tooltip("スマホの曲げるスティック（投げる番と、追い風ヒットで打った後）")]
        [SerializeField] private GameObject _bendStick;
        [SerializeField, TextArea] private string _batHelp = "振る J / ×（タイミングよく）　バント K / ○（押している間）　必殺打 Space / L1（投げる前に）　メニュー Esc";
        [SerializeField, TextArea] private string _pitchHelp = "投げる J ストレート / K カーブ / L チェンジアップ（× ○ △）　投げた後に曲げる ←→　魔球 Space / L1（投げる前に）　投げる子 ↑↓　メニュー Esc";

        private bool _isTouch;
        private bool _batting;
        private bool _steering;
        private float _bannerLeft;
        private float _bannerTime;
        private Color _bandColor;
        private int _shownClock = -1;

        /// <summary>球種の札を押した（投げる番の構えの間だけ）</summary>
        public event Action<BaseballPitchType> PitchChosen;

        private void Awake()
        {
            _banner.gameObject.SetActive(false);
            _bannerBand.gameObject.SetActive(false);
            _isTouch = TouchControlsVisibility.IsTouchDevice;
            for (var i = 0; i < _pitchButtons.Length; i++)
            {
                var type = (BaseballPitchType)i;
                _pitchButtons[i].onClick.AddListener(() => PitchChosen?.Invoke(type));
            }

            foreach (var hint in _keyHints)
            {
                hint.SetActive(!_isTouch);
            }

            SetPitchCards(false);
            AwakeLineup();
        }

        /// <summary>打つ番か投げる番かで、説明とスマホのボタンを替える</summary>
        public void SetSide(bool batting)
        {
            gameObject.SetActive(true);
            _batting = batting;
            _steering = false;
            _help.text = batting ? _batHelp : _pitchHelp;
            _help.gameObject.SetActive(!_isTouch);
            foreach (var button in _batButtons)
            {
                button.SetActive(batting);
            }

            ApplyStick();
            _touch.SetShown(true);
        }

        /// <summary>追い風ヒットで打った後だけ、打つ番でも曲げるスティックを出す</summary>
        public void SetSteerStick(bool steering)
        {
            _steering = steering;
            ApplyStick();
        }

        private void ApplyStick() => _bendStick.SetActive(!_batting || _steering);

        public void ShowCount(BaseballCount count)
        {
            Light(_ballLights, count.Balls, BallOn);
            Light(_strikeLights, count.Strikes, StrikeOn);
            Light(_outLights, count.Outs, OutOn);
        }

        private static void Light(Image[] lights, int on, Color color)
        {
            for (var i = 0; i < lights.Length; i++)
            {
                lights[i].color = i < on ? color : LightOff;
            }
        }

        /// <summary>球種の札を出す・隠す（出すときは持ち時間も出す）</summary>
        public void SetPitchCards(bool shown)
        {
            _pitchCards.SetActive(shown);
            _shownClock = -1;
        }

        /// <summary>1球の持ち時間の残り（秒は切り上げ）。残りわずかで赤く</summary>
        public void SetClock(float seconds)
        {
            var whole = Mathf.CeilToInt(seconds);
            if (whole == _shownClock)
            {
                return;
            }

            _shownClock = whole;
            _clock.text = $"のこり {whole}";
            _clock.color = whole <= _clockUrgentSeconds ? UrgentColor : Color.white;
        }

        /// <summary>まん中に大きく出して、seconds 秒で消す。band を渡すと、字の後ろに横いっぱいの帯を出す（大きな場面だけ）</summary>
        public void ShowBanner(string text, Color color, float seconds, Color? band = null)
        {
            _banner.text = text;
            _banner.color = color;
            _banner.gameObject.SetActive(true);
            _bannerLeft = seconds;
            _bannerTime = 0f;
            _bannerBand.gameObject.SetActive(band.HasValue);
            if (band.HasValue)
            {
                _bandColor = band.Value;
                _bannerBand.color = _bandColor;
                _bannerBand.transform.localScale = new Vector3(0f, 1f, 1f);
            }
        }

        private void Update() => TickBanner(Time.deltaTime);

        /// <summary>大きく出てから縮んで止まり、最後に薄くなって消える（帯はまん中から左右へ伸びて、字と一緒に薄くなる）</summary>
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
            var fade = Mathf.Clamp01(_bannerLeft / _bannerFadeSeconds);
            var color = _banner.color;
            color.a = fade;
            _banner.color = color;
            if (_bannerBand.gameObject.activeSelf)
            {
                var open = Mathf.Clamp01(_bannerTime / _bandOpenSeconds);
                _bannerBand.transform.localScale = new Vector3(1f - (1f - open) * (1f - open), 1f, 1f);
                var band = _bandColor;
                band.a *= fade;
                _bannerBand.color = band;
            }

            if (_bannerLeft <= 0f)
            {
                _banner.gameObject.SetActive(false);
                _bannerBand.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 結果を出している間は、スコアボードだけを左に寄せて残し（右に結果の欄が出る）、ほかは隠す。false で試合中の並びに戻す
        /// </summary>
        public void SetResult(bool result)
        {
            _countPlate.SetActive(!result);
            _help.gameObject.SetActive(!result && !_isTouch);
            _touch.SetShown(!result);
            SetLineupsShown(!result);
            PlaceScoreboard(result);
            if (!result)
            {
                return;
            }

            SetPitchCards(false);
            HideSpecialToggle();
            _banner.gameObject.SetActive(false);
            _bannerBand.gameObject.SetActive(false);
        }
    }
}
