using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// みんなでの結果の左に出す MVP の板（打点の一番多い人の名前を席の色で、その下に打点）。
    /// 結果の欄の左はスコアボードと分け合うので、スコアボードの下に置く（サッカーの MVP の板と同じ見た目）
    /// </summary>
    public partial class BaseballHud
    {
        [Header("MVP（みんなでの結果）")]
        [SerializeField] private GameObject _mvp;
        [SerializeField] private Text _mvpName;
        [SerializeField] private Text _mvpRbi;
        [Tooltip("名前の下に敷く帯（席の色）")]
        [SerializeField] private Image _mvpBand;

        /// <param name="names">打点が一番多い人（並んだら「・」でつなぐ）</param>
        public void ShowMvp(string names, int rbi, Color seatColor)
        {
            _mvp.SetActive(true);
            _mvpName.text = names;
            _mvpRbi.text = $"{rbi} 打点";
            _mvpBand.color = seatColor;
        }

        public void HideMvp() => _mvp.SetActive(false);
    }
}
