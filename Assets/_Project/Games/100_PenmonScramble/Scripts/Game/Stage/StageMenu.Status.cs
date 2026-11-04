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

        [SerializeField] private Text _headingText;
        [SerializeField] private Text _noteText;
        [SerializeField] private Text _stageText;
        [SerializeField] private HudAreaPips _areaPips;
        [SerializeField] private StageMenuParty _party;

        private StageSnapshot _snapshot;
        private string _caption = string.Empty;
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

        /// <summary>
        /// ステージの様子（StageSnapshot）が無い所（ミニゲーム）で、上の帯の札に出す字。この字を出す間はエリアのつぶを隠す
        /// </summary>
        public void SetCaption(string caption)
        {
            _caption = caption ?? string.Empty;
            if (_shown)
            {
                ShowStatus(snap: false);
            }
        }

        private void BindStatus()
        {
            _headingText.text = _solo ? SoloHeading : CoopHeading;
            _noteText.text = _solo ? string.Empty : _coopNote;
        }

        private void ShowStatusNow() => ShowStatus(snap: true);

        private void ShowStatus(bool snap)
        {
            if (_snapshot != null)
            {
                _stageText.text = Stages.Get(_snapshot.StageNo)?.Name ?? string.Empty;
                _areaPips.Set(_snapshot.AreaIndex, _snapshot.AreaCount, StageHud.IsRoundStage(_snapshot));
            }
            else if (_caption.Length > 0)
            {
                _stageText.text = _caption;
                _areaPips.gameObject.SetActive(false);
            }

            // 右の編成の欄はステージだけ（ミニゲームでは作らない）
            if (_party != null)
            {
                _party.Show(_partyState, _fallen, snap);
            }
        }
    }
}
