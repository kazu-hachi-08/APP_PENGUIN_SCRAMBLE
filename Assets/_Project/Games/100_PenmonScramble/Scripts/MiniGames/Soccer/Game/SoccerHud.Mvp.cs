using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// みんなでの結果の左に出す MVP の板（点を一番入れた人の名前を席の色で、その下に入れた点）。
    /// 結果の欄の左は各ミニゲームが使う所なので、サッカーの HUD に持つ
    /// </summary>
    public partial class SoccerHud
    {
        [Header("MVP（みんなでの結果）")]
        [SerializeField] private GameObject _mvp;
        [SerializeField] private Text _mvpName;
        [SerializeField] private Text _mvpGoals;
        [Tooltip("名前の下に敷く帯（席の色）")]
        [SerializeField] private Image _mvpBand;

        /// <param name="names">点を一番入れた人（並んだら「・」でつなぐ）</param>
        public void ShowMvp(string names, int goals, Color seatColor)
        {
            _mvp.SetActive(true);
            _mvpName.text = names;
            _mvpGoals.text = $"{goals} 点";
            _mvpBand.color = seatColor;
        }

        public void HideMvp() => _mvp.SetActive(false);
    }
}
