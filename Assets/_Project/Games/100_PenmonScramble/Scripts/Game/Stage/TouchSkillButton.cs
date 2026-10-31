using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スマホの技・交代のボタンの見た目: 色の面と下の厚み・中のアイコン・待ちの残りを暗い扇で見せ、明けた瞬間に光る。
    /// 何を出すかは TouchControlsView が決める
    /// </summary>
    public class TouchSkillButton : MonoBehaviour
    {
        // 厚みと縁取りは面の色を濃くした色。どの属性の色でもボタンの形が分かるように
        private const float BaseDarken = 0.45f;
        private const float OutlineDarken = 0.55f;
        // 押せない（交代できる子がいない）ときに面を灰色へ寄せる割合
        private const float UnusableGray = 0.65f;
        private static readonly Color Gray = new Color(0.45f, 0.47f, 0.52f);

        [SerializeField] private Image _face;
        [SerializeField] private Image _base;
        [SerializeField] private Image _icon;
        [SerializeField] private Outline _iconOutline;
        [Tooltip("待ちの残り。Filled（Radial360）で、残りの割合だけ暗くする")]
        [SerializeField] private Image _cooldown;
        [SerializeField] private Image _flash;
        [SerializeField] private float _flashSeconds = 0.35f;
        [SerializeField] private float _flashGrow = 0.3f;

        private Color _color = Color.white;
        private bool _usable = true;
        private float _lastCooldown;
        private float _flashLeft;

        private void Awake()
        {
            _color = _face.color;
        }

        public void SetColor(Color color)
        {
            if (color == _color)
            {
                return;
            }

            _color = color;
            ApplyColor();
        }

        /// <summary>技の形のアイコン（白で描いた物）。中の白を縁取りで面から浮かせる</summary>
        public void SetIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
            _icon.color = Color.white;
            _icon.enabled = sprite != null;
        }

        /// <summary>交代のボタンは、次に出る子の顔を出す（縁取りはしない）</summary>
        public void SetFace(int speciesNo)
        {
            CreatureArt.ShowFace(_icon, speciesNo);
        }

        public void SetUsable(bool usable)
        {
            if (usable == _usable)
            {
                return;
            }

            _usable = usable;
            ApplyColor();
        }

        /// <param name="ratio">待ちの残り（0〜1。0 で出せる）</param>
        public void SetCooldown(float ratio)
        {
            _cooldown.fillAmount = ratio;
            _cooldown.enabled = ratio > 0f;
            if (_lastCooldown > 0f && ratio <= 0f)
            {
                _flashLeft = _flashSeconds;
            }

            _lastCooldown = ratio;
        }

        private void ApplyColor()
        {
            var face = _usable ? _color : Color.Lerp(_color, Gray, UnusableGray);
            _face.color = face;
            _base.color = Color.Lerp(face, Color.black, BaseDarken);
            if (_iconOutline != null)
            {
                _iconOutline.effectColor = Color.Lerp(face, Color.black, OutlineDarken);
            }
        }

        /// <summary>待ちが明けたら、白い光がふくらみながら消える（出せるようになったと目の端で分かるように）</summary>
        private void Update()
        {
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            var t = _flashLeft / _flashSeconds;
            _flash.enabled = t > 0f;
            _flash.color = new Color(1f, 1f, 1f, t);
            _flash.rectTransform.localScale = Vector3.one * (1f + _flashGrow * (1f - t));
        }
    }
}
