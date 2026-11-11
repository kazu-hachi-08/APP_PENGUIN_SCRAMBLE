using System.Collections;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 強化した瞬間の演出: 素材の顔が強化する子へ吸い込まれる → 顔が光る → パワーの数字が数え上がる →
    /// 上限解放なら◆が1つずつ金に灯る。全部で 1.5 秒ほど。どこを押してもとばせる（最後の状態にする）
    /// </summary>
    public partial class EnhanceScreen
    {
        [Header("演出")]
        [SerializeField] private Image _glow;
        [SerializeField] private Image _flyTemplate;
        [Tooltip("演出の間だけ出す、画面全体の透明なボタン（押すととばす）")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private float _flySeconds = 0.38f;
        [SerializeField] private float _flyStagger = 0.06f;
        [SerializeField] private float _glowSeconds = 0.45f;
        [SerializeField] private float _countSeconds = 0.6f;
        [SerializeField] private float _countSoundInterval = 0.08f;
        [SerializeField] private float _pipSeconds = 0.28f;
        [SerializeField] private float _pipStagger = 0.15f;
        [SerializeField] private float _holdSeconds = 0.3f;

        private const float FlyEndScale = 0.35f;
        private const float GlowEndScale = 1.7f;
        private const float FacePunchScale = 1.15f;
        private const float PipPopScale = 2f;
        private const float PreviewBlinkSpeed = 3f;

        private readonly List<Image> _flyers = new List<Image>();
        private bool[] _pipPreview;
        private Coroutine _playing;
        private bool _skip;

        private bool IsPlaying => _playing != null;

        /// <summary>吸い込まれる顔1つ分（消える前のカードの位置と種類）</summary>
        private readonly struct Flight
        {
            public Flight(int speciesNo, Vector3 from)
            {
                SpeciesNo = speciesNo;
                From = from;
            }

            public int SpeciesNo { get; }
            public Vector3 From { get; }
        }

        private void AwakeEffects()
        {
            _pipPreview = new bool[_pips.Length];
            _flyTemplate.gameObject.SetActive(false);
            _skipButton.onClick.AddListener(() => _skip = true);
            _skipButton.gameObject.SetActive(false);
            _glow.color = Clear(_glow.color);
        }

        private void OnDisable()
        {
            // 演出の途中で閉じられても（シーンの切り替えなど）、次に開いたときに残りが出ないように
            if (_playing != null)
            {
                StopCoroutine(_playing);
                _playing = null;
            }

            ClearFlyers();
            _skipButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsPlaying)
            {
                return;
            }

            // これから灯る◆は明滅させて、上限解放になることを知らせる
            var blink = ToyEasing.Pulse01(Time.unscaledTime, PreviewBlinkSpeed);
            for (var i = 0; i < _pips.Length; i++)
            {
                if (_pipPreview[i])
                {
                    _pips[i].color = Color.Lerp(_pipOffColor, _pipOnColor, blink);
                }
            }
        }

        private void SetPipPreview(int index, bool preview)
        {
            _pipPreview[index] = preview;
        }

        private List<Flight> CollectFlights()
        {
            var flights = new List<Flight>();
            foreach (var card in _cards)
            {
                var penguin = card.gameObject.activeSelf ? Record.Find(card.PenguinId) : null;
                if (penguin != null && _selected.Contains(penguin.id))
                {
                    flights.Add(new Flight(penguin.speciesNo, card.transform.position));
                }
            }

            return flights;
        }

        private void PlayEnhance(EnhancePlan plan, List<Flight> flights)
        {
            _skip = false;
            _skipButton.gameObject.SetActive(true);
            _skipButton.transform.SetAsLastSibling();
            _enhanceButton.interactable = false;
            ShowNumbers(plan, plan.PowerBefore, true);
            ShowPips(plan.LimitBreakBefore, plan.LimitBreakBefore);
            _playing = StartCoroutine(Play(plan, flights));
        }

        private IEnumerator Play(EnhancePlan plan, List<Flight> flights)
        {
            yield return FlyIn(flights);
            if (!_skip)
            {
                GameSounds.Play(GameSound.ResultUnlock);
            }

            yield return GlowAndCount(plan);
            yield return LightPips(plan);
            yield return Wait(_holdSeconds);
            Finish();
        }

        private IEnumerator FlyIn(List<Flight> flights)
        {
            var target = _face.transform.position;
            foreach (var flight in flights)
            {
                var flyer = Instantiate(_flyTemplate, transform);
                flyer.gameObject.SetActive(true);
                flyer.transform.position = flight.From;
                CreatureArt.ShowFace(flyer, flight.SpeciesNo);
                _flyers.Add(flyer);
            }

            var total = _flySeconds + _flyStagger * Mathf.Max(0, flights.Count - 1);
            var landed = 0;
            for (var t = 0f; t < total && !_skip; t += Time.unscaledDeltaTime)
            {
                for (var i = 0; i < _flyers.Count; i++)
                {
                    var p = Mathf.Clamp01((t - _flyStagger * i) / _flySeconds);
                    // だんだん速く（吸い込まれる）
                    var eased = p * p;
                    var rect = _flyers[i].rectTransform;
                    rect.position = Vector3.Lerp(flights[i].From, target, eased);
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, FlyEndScale, eased);
                    _flyers[i].enabled = p < 1f;
                }

                while (landed < _flyers.Count && t >= _flyStagger * landed + _flySeconds)
                {
                    landed++;
                    GameSounds.Play(GameSound.ResultCoin);
                }

                yield return null;
            }

            ClearFlyers();
        }

        private IEnumerator GlowAndCount(EnhancePlan plan)
        {
            var nextSound = 0f;
            var seconds = Mathf.Max(_glowSeconds, _countSeconds);
            for (var t = 0f; t < seconds && !_skip; t += Time.unscaledDeltaTime)
            {
                var g = Mathf.Clamp01(t / _glowSeconds);
                _glow.color = WithAlpha(_glow.color, 1f - g);
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(1f, GlowEndScale, g);
                _face.transform.localScale = Vector3.one * Mathf.Lerp(FacePunchScale, 1f, g);

                var c = Mathf.Clamp01(t / _countSeconds);
                var shown = Mathf.RoundToInt(Mathf.Lerp(plan.PowerBefore, plan.PowerAfter, 1f - (1f - c) * (1f - c)));
                ShowNumbers(plan, shown, true);
                if (t >= nextSound && c < 1f && plan.PowerAfter > plan.PowerBefore)
                {
                    GameSounds.Play(GameSound.ResultCount);
                    nextSound = t + _countSoundInterval;
                }

                yield return null;
            }
        }

        private IEnumerator LightPips(EnhancePlan plan)
        {
            for (var i = plan.LimitBreakBefore; i < plan.LimitBreakAfter && i < _pips.Length && !_skip; i++)
            {
                _pips[i].text = "◆";
                _pips[i].color = _pipOnColor;
                GameSounds.Play(GameSound.Star);
                for (var t = 0f; t < _pipSeconds && !_skip; t += Time.unscaledDeltaTime)
                {
                    _pips[i].transform.localScale = Vector3.one * Mathf.Lerp(PipPopScale, 1f, t / _pipSeconds);
                    yield return null;
                }

                _pips[i].transform.localScale = Vector3.one;
                yield return Wait(_pipStagger);
            }
        }

        private IEnumerator Wait(float seconds)
        {
            for (var t = 0f; t < seconds && !_skip; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        /// <summary>最後の状態にそろえる（とばしたときも、ふつうに終わったときも同じ）</summary>
        private void Finish()
        {
            _playing = null;
            ClearFlyers();
            _glow.color = Clear(_glow.color);
            _glow.transform.localScale = Vector3.one;
            _face.transform.localScale = Vector3.one;
            _skipButton.gameObject.SetActive(false);
            Refresh();
        }

        private void ClearFlyers()
        {
            foreach (var flyer in _flyers)
            {
                if (flyer != null)
                {
                    Destroy(flyer.gameObject);
                }
            }

            _flyers.Clear();
        }

        private static Color Clear(Color color) => WithAlpha(color, 0f);

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
