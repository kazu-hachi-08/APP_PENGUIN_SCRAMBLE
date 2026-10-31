using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// HUD の3体の小さな顔1つ: 属性の色の丸に顔・下に HP の細いバー。
    /// 場に出ている子は金の輪、次に出る子は白い輪。交代を待つ間は次の子の顔に暗い扇がかかって時計回りに晴れ、明けたら1回弾む。倒れた子は灰色に×
    /// </summary>
    public class HudMemberSlot : MonoBehaviour
    {
        private const float FaintedFaceGray = 0.35f;
        private static readonly Color FaintedDiscColor = new Color(0.35f, 0.37f, 0.42f);

        [SerializeField] private RectTransform _visual;
        [SerializeField] private Image _disc;
        [SerializeField] private Image _face;
        [SerializeField] private Image _cooldown;
        [SerializeField] private GameObject _cross;
        [SerializeField] private GameObject _activeRing;
        [SerializeField] private Image _nextRing;
        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private float _bounceSeconds = 0.3f;
        [SerializeField] private float _bounceScale = 0.3f;
        [SerializeField] private float _nextPulsePerSecond = 1.5f;

        private int _species = -1;
        private bool _wasCooling;
        // 弾みの残り時間。0 以下なら弾まない
        private float _bounce;
        private float _elapsed;

        /// <param name="cooldown">交代の待ちの残り（1 で始まったばかり、0 で替われる）。次に出る子にだけ出す</param>
        public void Show(int speciesNo, float hpRatio, bool active, bool next, float cooldown)
        {
            gameObject.SetActive(true);
            if (speciesNo != _species)
            {
                _species = speciesNo;
                CreatureArt.ShowFace(_face, speciesNo);
            }

            var fainted = hpRatio <= 0f;
            _disc.color = fainted ? FaintedDiscColor : PenguinPalette.SpeciesElementColor(speciesNo);
            _face.color = fainted ? new Color(FaintedFaceGray, FaintedFaceGray, FaintedFaceGray) : Color.white;
            _cross.SetActive(fainted);
            _activeRing.SetActive(active);
            var isNext = next && !active && !fainted;
            _nextRing.gameObject.SetActive(isNext);
            _cooldown.fillAmount = isNext ? Mathf.Clamp01(cooldown) : 0f;
            _hpFill.anchorMax = new Vector2(Mathf.Clamp01(hpRatio), 1f);

            var cooling = isNext && cooldown > 0f;
            if (_wasCooling && !cooling && isNext)
            {
                _bounce = _bounceSeconds;
            }

            _wasCooling = cooling;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            UpdateBounce(dt);
            if (_nextRing.gameObject.activeSelf)
            {
                // 替われる間だけ脈打たせて「今なら交代できる」と見せる
                var ready = _cooldown.fillAmount <= 0f;
                var wave = ready ? 0.5f + 0.5f * Mathf.Sin(_elapsed * _nextPulsePerSecond * Mathf.PI * 2f) : 0f;
                _nextRing.color = new Color(1f, 1f, 1f, ready ? Mathf.Lerp(0.55f, 1f, wave) : 0.4f);
            }
        }

        private void UpdateBounce(float dt)
        {
            if (_bounce <= 0f)
            {
                _visual.localScale = Vector3.one;
                return;
            }

            _bounce -= dt;
            var t = 1f - Mathf.Clamp01(_bounce / _bounceSeconds);
            _visual.localScale = Vector3.one * (1f + _bounceScale * Mathf.Sin(t * Mathf.PI));
        }
    }
}
