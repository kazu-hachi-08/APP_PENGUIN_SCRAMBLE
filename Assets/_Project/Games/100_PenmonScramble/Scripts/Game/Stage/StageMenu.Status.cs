using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// メニューの上の帯（見出し・ステージ名・エリアのつぶ）と右の編成の欄に、いまの様子を出す。
    /// StageRunner が毎フレーム渡し、出している間だけ書き換える。
    /// 協力はゲームが止まらないので、見出しを「メニュー」にして、その下に止まらないことを書く
    /// </summary>
    public partial class StageMenu
    {
        private const string SoloHeading = "いちじていし";
        private const string CoopHeading = "メニュー";
        private const string CoopNote = "協力中は ゲームが止まりません";

        [SerializeField] private Text _headingText;
        [SerializeField] private Text _noteText;
        [SerializeField] private Text _stageText;
        [SerializeField] private HudAreaPips _areaPips;
        [SerializeField] private StageMenuParty _party;

        private StageSnapshot _snapshot;
        private PlayerState _partyState;
        private bool _fallen;

        /// <param name="party">自分の3体。まだ届いていなければ null</param>
        /// <param name="fallen">3体とも倒れて場から消えた</param>
        public void SetStatus(StageSnapshot snapshot, PlayerState party, bool fallen)
        {
            _snapshot = snapshot;
            _partyState = party;
            _fallen = fallen;
            if (_shown)
            {
                ShowStatus(snap: false);
            }
        }

        private void BindStatus()
        {
            _headingText.text = _solo ? SoloHeading : CoopHeading;
            _noteText.text = _solo ? string.Empty : CoopNote;
        }

        private void ShowStatusNow() => ShowStatus(snap: true);

        private void ShowStatus(bool snap)
        {
            if (_snapshot != null)
            {
                _stageText.text = Stages.Get(_snapshot.StageNo)?.Name ?? string.Empty;
                _areaPips.Set(_snapshot.AreaIndex, _snapshot.AreaCount);
            }

            _party.Show(_partyState, _fallen, snap);
        }
    }
}
