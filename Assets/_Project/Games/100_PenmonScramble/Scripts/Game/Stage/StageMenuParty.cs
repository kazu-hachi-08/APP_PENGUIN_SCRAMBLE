using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニューの右の欄: 編成の3体（属性の色の丸に顔・名前・HP のバーと数）。
    /// 場に出ている子は金の輪と「出ている」の札、倒れた子は灰色に×。
    /// 開いた瞬間の HP はすぐに合わせる（前に見た値から減っていくように見せない）。協力は止まらないので、開いている間に減ればバーが揺れる
    /// </summary>
    public class StageMenuParty : MonoBehaviour
    {
        private const float FaintedGray = 0.35f;
        private static readonly Color FaintedDiscColor = new Color(0.35f, 0.37f, 0.42f);

        [Serializable]
        private class Row
        {
            public GameObject Root;
            public Image Disc;
            public Image Face;
            public Text Name;
            public HudBar Hp;
            public Text HpText;
            public GameObject Cross;
            public GameObject ActiveMark;
        }

        [SerializeField] private Row[] _rows;

        /// <param name="fallen">3体とも倒れて場から消えた。最後に見た3体を HP 0 で出す</param>
        /// <param name="snap">開いた瞬間。HP のバーを動かさずに合わせる</param>
        public void Show(PlayerState state, bool fallen, bool snap)
        {
            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                var has = state != null && i < state.Members.Count;
                row.Root.SetActive(has);
                if (has)
                {
                    ShowMember(row, state.Members[i], fallen, !fallen && i == state.ActiveIndex, snap);
                }
            }
        }

        private static void ShowMember(Row row, MemberState member, bool fallen, bool active, bool snap)
        {
            var hp = fallen ? 0 : member.Hp;
            var fainted = hp <= 0;
            CreatureArt.ShowFace(row.Face, member.SpeciesNo);
            row.Disc.color = fainted ? FaintedDiscColor : PenguinPalette.SpeciesElementColor(member.SpeciesNo);
            row.Face.color = fainted ? new Color(FaintedGray, FaintedGray, FaintedGray) : Color.white;
            row.Cross.SetActive(fainted);
            row.ActiveMark.SetActive(active);
            row.Name.text = PenguinPalette.SpeciesName(member.SpeciesNo);
            var ratio = member.MaxHp > 0 ? Mathf.Clamp01((float)hp / member.MaxHp) : 0f;
            if (snap)
            {
                row.Hp.Snap(ratio);
            }
            else
            {
                row.Hp.SetRatio(ratio);
            }

            row.HpText.text = $"{hp} / {member.MaxHp}";
        }
    }
}
