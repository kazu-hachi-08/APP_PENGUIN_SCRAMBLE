using System;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフの画面の表示。左上: ホールの札（ホール・パー・何打目）と打順の表。上のまん中: 持ち時間の輪。
    /// 左下: いまの番の人の3体のカード（押すとその子で打つ）と、その上に特殊ショットの札（名前・★・ON / OFF。雷は転がっている間に加速の案内）。
    /// 右: パワーゲージと、スマホでは打つ大ボタンと特殊のボタン。まん中: 知らせ。
    /// 組み立ては GolfSceneBuilder.Hud.cs
    /// </summary>
    public class GolfHud : MonoBehaviour
    {
        // 持ち時間がこれより少なくなったら赤くして脈打たせる（秒）
        private const float HurrySeconds = 5f;
        private static readonly Color TimerColor = new Color(1f, 1f, 1f);
        private static readonly Color HurryColor = new Color(1f, 0.38f, 0.32f);
        private static readonly Color ArmedColor = new Color(0.45f, 0.95f, 1f);
        private static readonly Color UsedColor = new Color(0.7f, 0.7f, 0.75f);
        private const string UsedLabel = "このコースでは使った";
        // スマホは操作の説明を出さない（_help が隠れている）ので、ボタンの名前で案内する
        private string OffLabel => _help.activeSelf ? "OFF（L / △ で ON）" : "OFF（特殊 のボタンで ON）";
        private const string BoostHint = "←→ で向き　J / × / 打つ で加速";

        [SerializeField] private Text _holeName;
        [SerializeField] private Text _par;
        [SerializeField] private Text _stroke;
        [SerializeField] private GameObject _timer;
        [SerializeField] private RectTransform _timerVisual;
        [SerializeField] private Image _timerFill;
        [SerializeField] private Text _timerText;
        [SerializeField] private GolfTurnRow[] _rows;
        [SerializeField] private GolfMemberCard[] _cards;
        [SerializeField] private GolfGaugeView _gauge;
        [SerializeField] private GameObject _specialPlate;
        [SerializeField] private Image _specialPlateImage;
        [SerializeField] private Text _specialName;
        [SerializeField] private Text _specialState;
        [Tooltip("特殊のボタン（スマホ）。押すと L / △ と同じ")]
        [SerializeField] private GameObject _specialButton;
        [SerializeField] private GolfBanner _banner;
        [SerializeField] private GameObject _help;
        [SerializeField] private TouchControlsVisibility _touch;
        [SerializeField] private float _hurryPulseRate = 2f;
        [SerializeField] private float _hurryPulseScale = 0.12f;

        /// <summary>カードが押された（何枚目か）</summary>
        public event Action<int> CardPressed;

        private void Awake()
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                var index = i;
                _cards[i].Button.onClick.AddListener(() => OnCardClicked(index));
            }

            // スマホでは操作の説明の代わりに、打つボタンとカードが見えている
            _help.SetActive(!(Application.isMobilePlatform || Touchscreen.current != null));
        }

        /// <summary>
        /// 押したカードを選んだままにしない（選んだままだと、打つ ×（南）が UI の「決定」にもなって、そのカードの子へ戻ってしまう）
        /// </summary>
        private void OnCardClicked(int index)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            CardPressed?.Invoke(index);
        }

        /// <param name="canAct">いまの番の人が操作できる（ホールの始めの全体を見せている間・喜んでいる間は false）</param>
        public void Show(GolfRound round, GolfShotControl control, bool canAct)
        {
            var world = round.World;
            var team = round.Teams[world.CurrentPlayer];
            var aiming = world.Phase == GolfHolePhase.Aiming && canAct;
            var boost = canAct && control.BoostAvailable;
            ShowHole(round);
            ShowTimer(world, aiming && !control.Striking);
            ShowRows(world);
            ShowCards(team, control, world.Phase != GolfHolePhase.Finished);
            ShowSpecial(team, control, aiming, boost);
            _gauge.Show(aiming, control.Gauge.Power, control.Gauge.Running);
            // 雷の加速は打つボタンで押すので、転がっている間も打つボタンを出す（特殊のボタンは狙う間だけ）
            _touch.SetShown((aiming && !control.Striking) || boost);
            _specialButton.SetActive(aiming && team.CanUseSpecial(team.Selected));
        }

        /// <summary>ホールの間・結果の間は、ホールの札と打順の表だけ残して隠す（スコアカードと重ねない）</summary>
        public void SetPlaying(bool playing)
        {
            _gauge.Show(false, 0f, false);
            _timer.SetActive(false);
            _specialPlate.SetActive(false);
            _touch.SetShown(false);
            if (!playing)
            {
                ShowCards(null, null, false);
            }
        }

        public void Flash(string message, Color color, float seconds) => _banner.Show(message, color, seconds);

        private void ShowHole(GolfRound round)
        {
            var world = round.World;
            var hole = world.Hole;
            _holeName.text = round.HoleCount > 1 ? $"{round.HoleIndex + 1}. {hole.Name}" : hole.Name;
            _par.text = $"パー {hole.Par}";
            if (world.Phase == GolfHolePhase.Finished)
            {
                _stroke.text = "ホール終了";
                return;
            }

            var player = world.CurrentPlayer;
            var next = world.Phase == GolfHolePhase.Aiming ? world.Strokes(player) + 1 : world.Strokes(player);
            _stroke.text = $"{next} 打目（上限 {hole.StrokeLimit}）";
        }

        private void ShowTimer(GolfHoleWorld world, bool visible)
        {
            _timer.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var left = world.ShotTimeLeft;
            var hurry = left <= HurrySeconds;
            _timerFill.fillAmount = Mathf.Clamp01(left / GolfRules.ShotSeconds);
            _timerFill.color = hurry ? HurryColor : TimerColor;
            _timerText.text = Mathf.CeilToInt(left).ToString();
            var pulse = hurry ? Mathf.Abs(Mathf.Sin(left * _hurryPulseRate * Mathf.PI)) * _hurryPulseScale : 0f;
            _timerVisual.localScale = Vector3.one * (1f + pulse);
        }

        private void ShowRows(GolfHoleWorld world)
        {
            var playing = world.Phase != GolfHolePhase.Finished;
            for (var i = 0; i < _rows.Length; i++)
            {
                if (i >= world.PlayerCount)
                {
                    _rows[i].Hide();
                    continue;
                }

                var done = world.IsDone(i);
                _rows[i].Show(GolfSeats.Label(i), GolfSeats.ColorOf(i), world.Strokes(i), playing && world.CurrentPlayer == i,
                    world.HoledOut(i), done && !world.HoledOut(i));
            }
        }

        private void ShowCards(GolfTeam team, GolfShotControl control, bool visible)
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                if (!visible || team == null || i >= team.Members.Count)
                {
                    _cards[i].Hide();
                    continue;
                }

                var selected = i == team.Selected;
                _cards[i].Show(team.Members[i], selected, team.IsSpecialUsed(i), selected && control.SpecialArmed);
            }
        }

        /// <summary>
        /// 特殊ショットの札: いま選んでいる子の特殊の名前と★、ON / OFF（風は曲げる向き）。使った子は「使った」。
        /// 雷の加速が使える間は、その案内に替える
        /// </summary>
        private void ShowSpecial(GolfTeam team, GolfShotControl control, bool aiming, bool boost)
        {
            _specialPlate.SetActive(aiming || boost);
            if (!aiming && !boost)
            {
                return;
            }

            var member = team.Current;
            var kind = GolfSpecial.KindOf(member.Element);
            _specialPlateImage.color = PenguinPalette.ElementColor(member.Element);
            if (boost)
            {
                _specialName.text = $"{GolfSpecial.NameOf(GolfSpecialKind.ThunderBoost)}！";
                _specialState.text = BoostHint;
                _specialState.color = ArmedColor;
                return;
            }

            _specialName.text = $"{GolfSpecial.NameOf(kind)} {new string('★', Mathf.Clamp(member.Stars, 0, 3))}";
            var used = team.IsSpecialUsed(team.Selected);
            _specialState.text = used ? UsedLabel : !control.SpecialArmed ? OffLabel : StateLabel(kind, control.Curve);
            _specialState.color = control.SpecialArmed ? ArmedColor : used ? UsedColor : Color.white;
        }

        private static string StateLabel(GolfSpecialKind kind, int curve)
        {
            if (kind == GolfSpecialKind.WindCurve)
            {
                return curve > 0 ? "ON  → 右へ曲げる" : "ON  ← 左へ曲げる";
            }

            return kind == GolfSpecialKind.ThunderBoost ? "ON  打った後にもう一度押す" : "ON";
        }
    }
}
