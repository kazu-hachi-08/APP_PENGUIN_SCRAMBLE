using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 必殺技のカットイン。画面の真ん中に帯を横から滑り込ませ、ペンギンの顔（絵が無ければ色の丸）と技の名前を出す。
    /// 長さは Core のヒットストップと同じにして、止まっている間だけ出す。
    /// ステージの画面いっぱいの帯と、ペンモン画面の実演の小窓の中の小さな帯（SkillDemoView）で使う
    /// </summary>
    public class SpecialCutIn : MonoBehaviour
    {
        private const float SlideRatio = 0.2f;

        [SerializeField] private RectTransform _band;
        [SerializeField] private Image _bandImage;
        [SerializeField] private Image _face;
        [SerializeField] private Text _text;
        // 技の名前の文字の大きさ（帯の大きさに合わせる）
        [SerializeField] private int _nameFontSize = 96;

        private float _seconds;
        private float _age = -1f;
        private float _width;

        public void Play(SpecialUsedEvent special)
        {
            Play(special.SpeciesNo, special.Element, "の必殺技！", SpecialMoves.For(special.Element).Name, Balance.SpecialCutInTicks * Balance.TickSeconds);
        }

        /// <summary>
        /// 種類の顔と属性の色の帯に「（名前）（見出し）」と技の名前を出す（ミニゲームの必殺シュートなど、本編の必殺技でない技にも使う）
        /// </summary>
        /// <param name="heading">名前の後ろに付ける一言（「の必殺技！」など）</param>
        public void Play(int speciesNo, Element element, string heading, string moveName, float seconds)
        {
            CreatureArt.ShowFace(_face, speciesNo);
            _bandImage.color = Color.Lerp(PenguinPalette.ElementColor(element), Color.black, 0.6f);
            _text.text = $"{PenguinPalette.SpeciesName(speciesNo)}{heading}\n<size={_nameFontSize}>{moveName}</size>";
            _seconds = seconds;
            _width = ((RectTransform)transform).rect.width;
            _age = 0f;
            gameObject.SetActive(true);
            Apply();
        }

        public void Hide()
        {
            _age = -1f;
            gameObject.SetActive(false);
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
