using System.Collections.Generic;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// スマホのボタンの中身: 動かしている子がボールを持っているかで、技1は シュート ⇔ スライディング、技2は パス ⇔ ダッシュ の絵に替わる。
    /// 持っていないときは、スライディング・ダッシュの待ちを扇で出す。交代のボタンには、押したら替わる子の顔を出す（替われる子がいなければ灰色）
    /// </summary>
    public class SoccerTouchView : MonoBehaviour
    {
        [SerializeField] private TouchSkillButton _shoot;
        [SerializeField] private TouchSkillButton _pass;
        [SerializeField] private TouchSkillButton _switch;
        [SerializeField] private Sprite _shootIcon;
        [SerializeField] private Sprite _slideIcon;
        [SerializeField] private Sprite _passIcon;
        [SerializeField] private Sprite _dashIcon;

        private bool? _holding;
        private int _face = int.MinValue;

        /// <param name="species">子ごとのペンモンの種類（world.Players と同じ並び）</param>
        public void Show(SoccerWorld world, SoccerControl control, IReadOnlyList<int> species)
        {
            var player = world.Players[control.Controlled];
            var holding = world.Ball.Holder == player.Index;
            if (_holding != holding)
            {
                _holding = holding;
                _shoot.SetIcon(holding ? _shootIcon : _slideIcon);
                _pass.SetIcon(holding ? _passIcon : _dashIcon);
            }

            _shoot.SetCooldown(holding ? 0f : player.SlideCooldown / SoccerBalance.SlideCooldownSeconds);
            _pass.SetCooldown(holding ? 0f : player.DashCooldown / SoccerBalance.DashCooldownSeconds);
            ShowSwitchFace(control.NextSwitchTarget(world), species);
        }

        private void ShowSwitchFace(int target, IReadOnlyList<int> species)
        {
            if (target == _face)
            {
                return;
            }

            _face = target;
            _switch.SetUsable(target != SoccerBall.NoPlayer);
            if (target != SoccerBall.NoPlayer && target < species.Count)
            {
                _switch.SetFace(species[target]);
            }
        }
    }
}
