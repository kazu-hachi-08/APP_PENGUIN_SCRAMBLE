using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 必殺技のカットイン（§4.3）。画面の真ん中に帯を横から滑り込ませ、ペンギンの顔（絵ができるまでは色の丸）と技の名前を出す。
    /// 長さは Core のヒットストップと同じにして、止まっている間だけ出す
    /// </summary>
    public class SpecialCutIn : MonoBehaviour
    {
        private const float SlideRatio = 0.2f;

        [SerializeField] private RectTransform _band;
        [SerializeField] private Image _bandImage;
        [SerializeField] private Image _face;
        [SerializeField] private Text _text;

        private float _seconds;
        private float _age = -1f;
        private float _width;

        public void Play(SpecialUsedEvent special)
        {
            var color = PenguinPalette.SpeciesColor(special.SpeciesNo);
            _face.color = color;
            _bandImage.color = Color.Lerp(PenguinPalette.ElementColor(special.Element), Color.black, 0.6f);
            _text.text = $"{PenguinPalette.SpeciesName(special.SpeciesNo)}の必殺技！\n<size=96>{SpecialMoves.For(special.Element).Name}</size>";
            _seconds = Balance.SpecialCutInTicks * Balance.TickSeconds;
            _width = ((RectTransform)transform).rect.width;
            _age = 0f;
            gameObject.SetActive(true);
            Apply();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _seconds)
            {
                gameObject.SetActive(false);
                return;
            }

            Apply();
        }

        /// <summary>左から入って止まり、最後に右へ抜ける</summary>
        private void Apply()
        {
            var t = _age / _seconds;
            float x;
            if (t < SlideRatio)
            {
                x = Mathf.Lerp(-_width, 0f, t / SlideRatio);
            }
            else if (t > 1f - SlideRatio)
            {
                x = Mathf.Lerp(0f, _width, (t - (1f - SlideRatio)) / SlideRatio);
            }
            else
            {
                x = 0f;
            }

            _band.anchoredPosition = new Vector2(x, _band.anchoredPosition.y);
        }
    }
}
