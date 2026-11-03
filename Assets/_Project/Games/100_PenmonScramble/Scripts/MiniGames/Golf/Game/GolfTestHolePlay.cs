using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 試しの1ホールを遊ぶ（開発用。Tools/PenmonScramble/Golf から開く）。
    /// ホールの進行（Core）を毎フレーム進め、操作・見た目・HUD へ渡す。K / ○ でホールをやり直す
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に動く
    [DefaultExecutionOrder(50)]
    public class GolfTestHolePlay : MonoBehaviour
    {
        private const int RestartAction = 1;

        [SerializeField] private GolfHoleView _view;
        [SerializeField] private GolfShotControl _control;
        [SerializeField] private GolfDevHud _hud;

        private GolfHoleWorld _world;
        private int _players;

        public void Begin(int players)
        {
            _players = Mathf.Clamp(players, 1, GolfRules.MaxPlayers);
            _hud.gameObject.SetActive(true);
            Restart();
        }

        private void Restart()
        {
            if (_world != null)
            {
                _world.Happened -= OnHappened;
            }

            var hole = GolfTestHoles.TestHole;
            _world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(_players));
            _world.Happened += OnHappened;
            _view.Build(hole, _players);
            _control.Bind(_world);
            _hud.Flash($"{hole.Name}  パー {hole.Par}");
        }

        private void Update()
        {
            if (_world == null)
            {
                return;
            }

            if (InputManager.HasInstance && InputManager.Instance.IsActionDown(RestartAction))
            {
                Restart();
                return;
            }

            _control.Tick(Time.deltaTime);
            _world.Tick(Time.deltaTime);
            _view.Sync(_world.Balls);
            _view.ShowGuide(_world.Phase == GolfHolePhase.Aiming
                ? GolfGuide.Compute(_world.Hole, _world.Balls, _world.CurrentPlayer, _world.AimAngle)
                : (GolfGuideLine?)null);
            _hud.Show(_world, _control);
        }

        private void OnHappened(GolfEvent e)
        {
            var message = Message(e);
            if (message != null)
            {
                _hud.Flash(message);
            }
        }

        private string Message(GolfEvent e)
        {
            var who = $"P{e.Player + 1}";
            switch (e.Kind)
            {
                case GolfEventKind.CupIn:
                    return _world.Strokes(e.Player) == 1 ? $"{who} ホールインワン！" : $"{who} カップイン！（{_world.Strokes(e.Player)} 打）";
                case GolfEventKind.Fell:
                    return e.Player == _world.CurrentPlayer ? $"{who} 池ポチャ（1 打たす）" : $"{who} 落ちた（もとの位置へ）";
                case GolfEventKind.LipOut:
                    return "おしい！";
                case GolfEventKind.GaveUp:
                    return $"{who} 上限で終わり";
                case GolfEventKind.HoleFinished:
                    return "ホール終了  K / ○ でもう一度";
                default:
                    return null;
            }
        }
    }
}
