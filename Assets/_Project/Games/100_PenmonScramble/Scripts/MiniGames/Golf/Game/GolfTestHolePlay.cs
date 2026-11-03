using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 試しの1ホールを遊ぶ（開発用。Tools/PenmonScramble/Golf から開く）。打つ子は本編の編成の3体（2人のときも同じ3体）。
    /// ホールの進行（Core）を毎フレーム進め、操作・見た目・カメラ・HUD・音へ渡す。起きたことの見せ方は GolfTestHolePlay.Events.cs。
    /// Space / LB でホールをやり直す。記録には何も書かない
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に動く
    [DefaultExecutionOrder(50)]
    public partial class GolfTestHolePlay : MonoBehaviour
    {
        private const int ShootAction = 0;
        private const int RestartAction = 3;

        // 試しのホールの景色（海岸）
        private const int TestWorld = 1;

        [SerializeField] private GolfHoleView _view;
        [SerializeField] private GolfCamera _camera;
        [SerializeField] private GolfShotControl _control;
        [SerializeField] private GolfHud _hud;

        // ホールの始めに全体を見せる秒（打つボタンで飛ばせる）
        [SerializeField] private float _introSeconds = 2.4f;

        private readonly List<GolfTeam> _teams = new List<GolfTeam>();
        private GolfHoleWorld _world;
        private float _intro;
        // カップイン・池の後、喜ぶ・頭を抱えるのを見せる間は、次の人の番を始めない（持ち時間も減らさない）
        private float _hold;
        // 番が替わったことを知らせる人（-1 なら知らせない）
        private int _turnBanner = -1;

        public void Begin(int players)
        {
            var count = Mathf.Clamp(players, 1, GolfRules.MaxPlayers);
            _teams.Clear();
            var party = PartySpecies();
            for (var i = 0; i < count; i++)
            {
                _teams.Add(new GolfTeam(party));
            }

            _hud.gameObject.SetActive(true);
            _hud.CardPressed += index => _control.SelectMember(index);
            _control.MemberChanged += () => GolfSounds.Play(GolfSound.Select);
            Restart();
        }

        private static List<int> PartySpecies()
        {
            var species = new List<int>();
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                species.Add(penguin.speciesNo);
            }

            return species;
        }

        private void Restart()
        {
            if (_world != null)
            {
                _world.Happened -= OnHappened;
            }

            var hole = GolfTestHoles.TestHole;
            _world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(_teams.Count));
            _world.Happened += OnHappened;
            _view.Build(hole, _teams.Count, TestWorld);
            _control.Bind(_world, _teams, _view.Shooter);
            _camera.ShowHole(hole.Ground);
            _intro = _introSeconds;
            _hold = 0f;
            _turnBanner = -1;
            _hud.Flash($"{hole.Name}  パー {hole.Par}", Color.white, _introSeconds - 0.3f);
        }

        private void Update()
        {
            if (_world == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            var input = InputManager.HasInstance ? InputManager.Instance : null;
            if (input != null && input.IsActionDown(RestartAction))
            {
                Restart();
                return;
            }

            _camera.Orbit(_control.TakeLook(dt));
            if (_intro > 0f)
            {
                TickIntro(dt, input);
            }
            else
            {
                TickPlay(dt);
            }

            _view.SyncBalls(_world.Balls, dt);
            _hud.Show(_world, CurrentTeam, _control, CanAct);
        }

        private GolfTeam CurrentTeam => _teams[_world.CurrentPlayer];

        /// <summary>番の人が操作できる（全体を見せている間・反応を見せている間でない）</summary>
        private bool CanAct => _intro <= 0f && _hold <= 0f;

        /// <summary>ホールの全体を見せる。打つボタンで飛ばせる（飛ばしたボタンでゲージが動き出さないよう、このフレームは操作を読まない）</summary>
        private void TickIntro(float dt, InputManager input)
        {
            _intro -= dt;
            if (input != null && input.IsActionDown(ShootAction))
            {
                _intro = 0f;
            }

            _view.ShowGuide(null, dt);
        }

        private void TickPlay(float dt)
        {
            var aiming = _world.Phase == GolfHolePhase.Aiming;
            if (aiming && _hold > 0f)
            {
                _hold -= dt;
                _view.ShowGuide(null, dt);
                return;
            }

            if (aiming)
            {
                ShowTurnBanner();
            }

            _control.Tick(dt);
            // 振り下ろしている間は持ち時間を止める（ゲージを止めたのに、当たる前に時間切れで自動の1打にならないように）
            if (!(aiming && _control.Striking))
            {
                _world.Tick(dt);
            }

            var ball = _world.Balls[_world.CurrentPlayer];
            var point = _view.BallPoint(ball);
            if (_world.Phase == GolfHolePhase.Aiming)
            {
                Aim(point);
            }
            else
            {
                _camera.Follow(point);
                _view.Shooter.Watch(point);
                _view.ShowGuide(null, dt);
            }
        }

        /// <summary>狙っている間: 打つ子を球の横に構えさせ、カメラを球の後ろへ、ガイド線を出す</summary>
        private void Aim(Vector3 point)
        {
            var aim = GolfSpace.ToWorld(GolfHoleWorld.Direction(_world.AimAngle));
            _camera.Aim(point, aim);
            if (_control.Striking)
            {
                _view.ShowGuide(null, Time.deltaTime);
                return;
            }

            _view.Shooter.Address(CurrentTeam.Current, point, aim, _view.Surface, _control.Gauge.Power);
            _view.ShowGuide(GolfGuide.Compute(_world.Hole, _world.Balls, _world.CurrentPlayer, _world.AimAngle), Time.deltaTime);
        }
    }
}
