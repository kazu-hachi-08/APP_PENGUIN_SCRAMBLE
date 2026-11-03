using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 打つ子のカード1枚: 属性の色の丸に顔・下に★。いま打つ子は大きくして金の輪。押すとその子に替える。
    /// 特殊ショットを使った子は暗くする（特殊ショットはフェーズ 4。いまは used がいつも false）
    /// </summary>
    public class GolfMemberCard : MonoBehaviour
    {
        private const string StarOn = "★";
        private const string StarOff = "☆";
        private const int MaxStars = 3;
        private static readonly Color UsedTint = new Color(0.45f, 0.45f, 0.5f);

        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _visual;
        [SerializeField] private Image _disc;
        [SerializeField] private Image _face;
        [SerializeField] private Text _stars;
        [SerializeField] private GameObject _selectedRing;
        [SerializeField] private float _selectedScale = 1.18f;
        [SerializeField] private float _scaleSpeed = 14f;
        [SerializeField] private float _bounceSeconds = 0.25f;
        [SerializeField] private float _bounceScale = 0.2f;

        private int _species = -1;
        private bool _selected;
        private float _scale = 1f;
        private float _bounce;

        public Button Button => _button;

        public void Show(GolfMember member, bool selected, bool used)
        {
            gameObject.SetActive(true);
            if (member.SpeciesNo != _species)
            {
                _species = member.SpeciesNo;
                CreatureArt.ShowFace(_face, member.SpeciesNo);
                _stars.text = Stars(member.Stars);
            }

            if (selected && !_selected)
            {
                _bounce = _bounceSeconds;
            }

            _selected = selected;
            _selectedRing.SetActive(selected);
            var tint = used ? UsedTint : Color.white;
            _disc.color = PenguinPalette.ElementColor(member.Element) * tint;
            _face.color = tint;
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            var dt = Time.deltaTime;
            _scale = Mathf.Lerp(_scale, _selected ? _selectedScale : 1f, 1f - Mathf.Exp(-_scaleSpeed * dt));
            _bounce = Mathf.Max(0f, _bounce - dt);
            var bounce = _bounce > 0f ? Mathf.Sin((1f - _bounce / _bounceSeconds) * Mathf.PI) * _bounceScale : 0f;
            _visual.localScale = Vector3.one * (_scale + bounce);
        }

        private static string Stars(int stars)
        {
            var count = Mathf.Clamp(stars, 0, MaxStars);
            return new string(StarOn[0], count) + new string(StarOff[0], MaxStars - count);
        }
    }
}
