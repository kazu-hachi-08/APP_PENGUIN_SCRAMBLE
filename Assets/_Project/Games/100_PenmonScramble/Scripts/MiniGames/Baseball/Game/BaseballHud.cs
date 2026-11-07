using System;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球の HUD（仮。スコアボード・打順のカードはフェーズ 4）: 左上にカウント（B・S・O の丸）、まん中に結果の大きな文字。
    /// 投げる番では下に球種の札3枚（押すと投げる。PC ではキーの印つき）と1球の持ち時間。
    /// PC では下に操作の説明、スマホでは打つ番に「振る」「バント」のボタン、投げる番に曲げるスティックを出す
    /// </summary>
    public class BaseballHud : MonoBehaviour
    {
        private static readonly Color LightOff = new Color(0.22f, 0.24f, 0.30f);
        private static readonly Color BallOn = new Color(0.35f, 0.88f, 0.40f);
        private static readonly Color StrikeOn = new Color(1f, 0.82f, 0.22f);
        private static readonly Color OutOn = new Color(1f, 0.32f, 0.28f);
        private static readonly Color UrgentColor = new Color(1f, 0.4f, 0.35f);

        [Header("カウント（左から順に光る）")]
        [SerializeField] private Image[] _ballLights;
        [SerializeField] private Image[] _strikeLights;
        [SerializeField] private Image[] _outLights;

        [Header("知らせ")]
        [SerializeField] private Text _banner;
        [SerializeField] private float _bannerPopSeconds = 0.25f;
        [SerializeField] private float _bannerPopScale = 1.6f;
        [SerializeField] private float _bannerFadeSeconds = 0.3f;

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
        [Tooltip("スマホの曲げるスティック（投げる番だけ）")]
        [SerializeField] private GameObject _bendStick;
        [SerializeField, TextArea] private string _batHelp = "振る J / ×（タイミングよく）　バント K / ○（押している間）　もどる Esc";
        [SerializeField, TextArea] private string _pitchHelp = "投げる J ストレート / K カーブ / L チェンジアップ（× ○ △）　投げた後に曲げる ←→ / 左スティック　もどる Esc";

        private bool _isTouch;
        private float _bannerLeft;
        private float _bannerTime;
        private int _shownClock = -1;

        /// <summary>球種の札を押した（投げる番の構えの間だけ）</summary>
        public event Action<BaseballPitchType> PitchChosen;

        private void Awake()
        {
            _banner.gameObject.SetActive(false);
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
        }

        /// <summary>打つ番か投げる番かで、説明とスマホのボタンを替える</summary>
        public void SetSide(bool batting)
        {
            gameObject.SetActive(true);
            _help.text = batting ? _batHelp : _pitchHelp;
            _help.gameObject.SetActive(!_isTouch);
            foreach (var button in _batButtons)
            {
                button.SetActive(batting);
            }

            _bendStick.SetActive(!batting);
            _touch.SetShown(true);
        }

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

        /// <summary>まん中に大きく出して、seconds 秒で消す</summary>
        public void ShowBanner(string text, Color color, float seconds)
        {
            _banner.text = text;
            _banner.color = color;
            _banner.gameObject.SetActive(true);
            _bannerLeft = seconds;
            _bannerTime = 0f;
        }

        private void Update() => TickBanner(Time.deltaTime);

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
    }
}
