using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// PK戦の HUD: 点の下に、チームごとに5つの丸（決めたらチームの色に ○、止められた・外したら灰色に ×、まだなら薄い丸）。
    /// 5本を過ぎたら新しい5本を見せる。人が蹴るときは下にためゲージ（上の赤い所は強いが外れることがある）と持ち時間と狙い方の一言、
    /// 人が守るときは飛ぶ向きの一言
    /// </summary>
    public partial class SoccerHud
    {
        public const int ShootoutMarksPerTeam = 5;

        private const string KickHint = "スティックで狙う（左右・上で高い所）　シュートをためて離す";
        private const string KeepHint = "スティックの左右で飛ぶ向きを選ぶ（倒さなければ真ん中）";
        private const string SuddenDeathLabel = "サドンデス";
        private const string ShootoutLabel = "PK戦";

        private static readonly Color EmptyMarkColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color FailMarkColor = new Color(0.45f, 0.47f, 0.52f);

        [Header("PK戦")]
        [SerializeField] private GameObject _shootoutBoard;
        [SerializeField] private Text _shootoutLabel;
        [Tooltip("赤の5つ、続けて青の5つ")]
        [SerializeField] private Image[] _shootoutMarks;
        [SerializeField] private Text[] _shootoutMarkTexts;
        [SerializeField] private GameObject _penaltyPanel;
        [SerializeField] private Image _penaltyGauge;
        [SerializeField] private GameObject _penaltyGaugeGroup;
        [SerializeField] private Text _penaltyTimer;
        [SerializeField] private Text _penaltyHint;

        private int _shownKicks = -1;

        private void AwakeShootout()
        {
            _shootoutBoard.SetActive(false);
            _penaltyPanel.SetActive(false);
        }

        /// <summary>PK戦の表（null なら隠す）。本数が変わったときだけ描き直す</summary>
        public void ShowShootout(SoccerShootout shootout)
        {
            _shootoutBoard.SetActive(shootout != null);
            _gaugeGroup.SetActive(shootout == null);
            if (shootout == null)
            {
                _shownKicks = -1;
                return;
            }

            var kicks = shootout.TotalKicks;
            if (kicks == _shownKicks)
            {
                return;
            }

            _shownKicks = kicks;
            _shootoutLabel.text = shootout.IsSuddenDeath ? SuddenDeathLabel : ShootoutLabel;
            // 両チームとも同じ5本の組を見せる（サドンデスで6本目からは、6〜10本目の組）
            var page = Mathf.Max(0, Mathf.Max(shootout.Results(0).Count, shootout.Results(1).Count) - 1) / ShootoutMarksPerTeam;
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                ShowMarks(team, shootout, page * ShootoutMarksPerTeam);
            }
        }

        private void ShowMarks(int team, SoccerShootout shootout, int first)
        {
            var results = shootout.Results(team);
            for (var i = 0; i < ShootoutMarksPerTeam; i++)
            {
                var slot = team * ShootoutMarksPerTeam + i;
                var kick = first + i;
                if (kick >= results.Count)
                {
                    _shootoutMarks[slot].color = EmptyMarkColor;
                    _shootoutMarkTexts[slot].text = string.Empty;
                    continue;
                }

                var scored = results[kick] == SoccerPenaltyResult.Goal;
                _shootoutMarks[slot].color = scored ? SoccerColors.Team(team) : FailMarkColor;
                _shootoutMarkTexts[slot].text = scored ? "○" : "×";
            }
        }

        /// <summary>
        /// 人が蹴る（kicking）ならためゲージ・持ち時間・狙い方、人が守る（keeping）なら飛ぶ向きの一言。どちらでもなければ隠す
        /// </summary>
        public void ShowPenaltyAim(SoccerShootout shootout, bool kicking, bool keeping)
        {
            var aiming = shootout != null && shootout.Phase == SoccerShootoutPhase.Aim && (kicking || keeping);
            _penaltyPanel.SetActive(aiming);
            if (!aiming)
            {
                return;
            }

            _penaltyGaugeGroup.SetActive(kicking);
            _penaltyTimer.gameObject.SetActive(kicking);
            _penaltyHint.text = kicking ? KickHint : KeepHint;
            if (kicking)
            {
                _penaltyGauge.fillAmount = shootout.Charge01;
                _penaltyTimer.text = Mathf.CeilToInt(shootout.AimSecondsLeft).ToString();
            }
        }

        private void HidePenaltyAim() => _penaltyPanel.SetActive(false);
    }
}
