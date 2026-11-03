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
    /// 左下: いまの番の人の3体のカード（押すとその子で打つ）。右: パワーゲージと、スマホでは打つ大ボタン。まん中: 知らせ。
    /// 組み立ては GolfSceneBuilder.Hud.cs
    /// </summary>
    public class GolfHud : MonoBehaviour
    {
        // 持ち時間がこれより少なくなったら赤くして脈打たせる（秒）
        private const float HurrySeconds = 5f;
        private static readonly Color TimerColor = new Color(1f, 1f, 1f);
        private static readonly Color HurryColor = new Color(1f, 0.38f, 0.32f);

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
        public void Show(GolfHoleWorld world, GolfTeam team, GolfShotControl control, bool canAct)
        {
            var aiming = world.Phase == GolfHolePhase.Aiming && canAct;
            ShowHole(world);
            ShowTimer(world, aiming && !control.Striking);
            ShowRows(world);
            ShowCards(team, world.Phase != GolfHolePhase.Finished);
            _gauge.Show(aiming, control.Gauge.Power, control.Gauge.Running);
            _touch.SetShown(aiming && !control.Striking);
        }

        public void Flash(string message, Color color, float seconds) => _banner.Show(message, color, seconds);

        private void ShowHole(GolfHoleWorld world)
        {
            var hole = world.Hole;
            _holeName.text = hole.Name;
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

        private void ShowCards(GolfTeam team, bool visible)
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                if (!visible || i >= team.Members.Count)
                {
                    _cards[i].Hide();
                    continue;
                }

                _cards[i].Show(team.Members[i], i == team.Selected, false);
            }
        }
    }
}
