using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーの HUD（仮。試合の流れを作るフェーズ 4 で作り直す）: 上のまん中に赤・青の点と時間、まん中に知らせ（「ゴール！」など）、
    /// PC では下に操作の説明、スマホでは仮想スティックとシュートのボタン
    /// </summary>
    public class SoccerHud : MonoBehaviour
    {
        [SerializeField] private Text _redScore;
        [SerializeField] private Text _blueScore;
        [SerializeField] private Text _time;
        [SerializeField] private Text _banner;
        [SerializeField] private GameObject _help;
        [SerializeField] private TouchControlsVisibility _touch;

        [Header("知らせ")]
        [SerializeField] private float _bannerPopSeconds = 0.25f;
        [SerializeField] private float _bannerPopScale = 1.6f;
        [SerializeField] private float _bannerFadeSeconds = 0.3f;

        [Header("点")]
        [SerializeField] private float _scorePopSeconds = 0.4f;
        [SerializeField] private float _scorePopScale = 1.6f;

        private int _red = -1;
        private int _blue = -1;
        private int _shownSeconds = -1;
        private float _bannerLeft;
        private float _bannerTime;
        private float _redPop = 1f;
        private float _bluePop = 1f;

        private void Awake()
        {
            _banner.gameObject.SetActive(false);
            // スマホ（触れる画面）では説明の代わりにボタンを出す
            _help.SetActive(!(Application.isMobilePlatform || Touchscreen.current != null));
        }

        public void SetPlaying(bool playing)
        {
            gameObject.SetActive(playing);
            _touch.SetShown(playing);
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

        /// <summary>始まってからの時間（m:ss）</summary>
        public void SetTime(float seconds)
        {
            var whole = Mathf.FloorToInt(seconds);
            if (whole == _shownSeconds)
            {
                return;
            }

            _shownSeconds = whole;
            _time.text = $"{whole / 60}:{whole % 60:00}";
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
            TickBanner(dt);
            _redPop = Pop(_redScore.transform, _redPop, dt);
            _bluePop = Pop(_blueScore.transform, _bluePop, dt);
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

        private float Pop(Transform target, float progress, float dt)
        {
            progress = Mathf.Min(1f, progress + dt / _scorePopSeconds);
            target.localScale = Vector3.one * Mathf.Lerp(_scorePopScale, 1f, progress);
            return progress;
        }
    }
}
