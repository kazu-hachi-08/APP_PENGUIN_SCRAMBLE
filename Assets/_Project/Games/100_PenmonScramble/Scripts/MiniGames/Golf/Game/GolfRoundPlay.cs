using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 1コース（3ホール）を回る進行役。打つ子は本編の編成の3体（開発用の2人のときも同じ3体。オンラインは各自の3体）。
    /// ラウンド（Core）を毎フレーム進め、操作・見た目・カメラ・HUD・音へ渡す。
    /// 起きたことの見せ方は GolfRoundPlay.Events.cs、ホールの間のスコアカードと結果・メニューは GolfRoundPlay.Flow.cs、
    /// オンライン（ほかの人の狙い・ゲストの写し・ホストへの入力）は GolfRoundPlay.Online.cs
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後、StageMenu が開け閉めした後に動く
    [DefaultExecutionOrder(60)]
    public partial class GolfRoundPlay : MonoBehaviour
    {
        private const int ShootAction = 0;

        [SerializeField] private GolfHoleView _view;
        [SerializeField] private GolfCamera _camera;
        [SerializeField] private GolfShotControl _control;
        [SerializeField] private GolfHud _hud;

        // ホールの始めに全体を見せる秒（打つボタンで飛ばせる）
        [SerializeField] private float _introSeconds = 2.4f;

        private GolfRound _round;
        private GolfPlayMode _mode = GolfPlayMode.Solo;
        private Phase _phase;
        private float _intro;
        // カップイン・池の後、喜ぶ・頭を抱えるのを見せる間は、次の人の番を始めない（持ち時間も減らさない）
        private float _hold;
        // 番が替わったことを知らせる人（-1 なら知らせない）
        private int _turnBanner = -1;
        // このフレームにラウンドの時間を進めたか（進めなかったフレームも、動く物の時刻だけは進める）
        private bool _ticked;
        // 持ち時間のカチッを最後に鳴らした秒（-1 は数えていない）
        private int _hurrySecond = -1;

        /// <summary>ひとりで（開発用は players 人が1台で交代に）回る。打つ子は全員、本編の編成の3体</summary>
        /// <param name="record">結果を記録に書く（開発用の試しのホールでは書かない）</param>
        public void BeginSolo(GolfCourseDef course, int players, bool record)
        {
            var count = Mathf.Clamp(players, 1, GolfRules.MaxPlayers);
            var teams = new List<GolfTeam>();
            for (var i = 0; i < count; i++)
            {
                teams.Add(new GolfTeam(PartySpecies()));
            }

            Begin(new GolfRound(course, teams), record, GolfPlayMode.Solo);
        }

        /// <summary>作ったラウンドを回り始める（オンラインのホストは各自の3体で作ったもの、ゲストは写しのラウンド）</summary>
        public void Begin(GolfRound round, bool record, GolfPlayMode mode)
        {
            _record = record;
            _mode = mode;
            _round = round;
            _round.Happened += OnHappened;
            _round.HoleStarted += _ => BuildHole();
            _round.HoleCompleted += OnHoleCompleted;
            _hud.gameObject.SetActive(true);
            _hud.CardPressed += index => _control.SelectMember(index);
            _control.MemberChanged += () => GolfSounds.Play(GolfSound.Select);
            _control.SpecialChanged += () => GolfSounds.Play(_control.SpecialArmed ? GolfSound.Special : GolfSound.Select);
            BindShots();
            BindFlow();
            // ラウンドは作った時点で1ホール目が始まっている（見た目はここで組む）
            BuildHole();
        }

        public static List<int> PartySpecies()
        {
            var species = new List<int>();
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                species.Add(penguin.speciesNo);
            }

            return species;
        }

        /// <summary>ホールの見た目を組み直して、全体を見せるところから始める（次のホール・やり直し・もう一度）</summary>
        private void BuildHole()
        {
            var hole = _round.Hole;
            _view.Build(hole, _round.PlayerCount, _round.Course.World);
            _control.Bind(_round, _view.Shooter, _mode.LocalSeat);
            _camera.ShowHole(hole.Ground);
            _phase = Phase.Intro;
            _intro = _introSeconds;
            _hold = 0f;
            _turnBanner = -1;
            ResetSwingHold();
            _hud.SetPlaying(true);
            HidePanels();
            var label = _round.HoleCount > 1 ? $"ホール {_round.HoleIndex + 1}  パー {hole.Par}" : $"{hole.Name}  パー {hole.Par}";
            _hud.Flash(label, Color.white, _introSeconds - 0.3f);
            GolfSounds.Play(GolfSound.HoleStart);
            UpdateMenuCaption();
        }

        private void Update()
        {
            if (_round == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            _ticked = false;
            if (!IsMenuOpen && (_phase == Phase.Intro || _phase == Phase.Play))
            {
                _camera.Orbit(_control.TakeLook(dt));
                if (_phase == Phase.Intro)
                {
                    TickIntro(dt);
                }
                else
                {
                    TickPlay(dt);
                }
            }
            else if (_phase == Phase.HoleOver)
            {
                TickHoleOver(dt);
            }

            // 全体を見せている間・反応を見せている間・ホールの間も、動く物は止めずに動かす
            // （ひとりでメニューを開いている間は止める。オンラインは止めない＝ホストの時刻はみんなの時刻）
            if (!_ticked && (!IsMenuOpen || _mode.IsOnline))
            {
                _round.Idle(dt);
            }

            // ゲストは計算しないので、メニューやホールの間でも、届いた位置へ球を寄せ続ける
            _round.TickMirror(dt, GolfNetRules.SnapshotSeconds);
            _view.SyncBalls(_round.World.Balls, dt);
            _view.SyncGimmicks(World.Time);
            if (_phase == Phase.Intro || _phase == Phase.Play)
            {
                _hud.Show(_round, DisplayAim, CanAct, _control.IsLocalTurn, _control.BoostAvailable);
            }

            TickHurrySound();
        }

        private GolfHoleWorld World => _round.World;

        private GolfTeam CurrentTeam => _round.Teams[World.CurrentPlayer];

        /// <summary>番の人が操作できる（全体を見せている間・反応を見せている間でない）</summary>
        private bool CanAct => _phase == Phase.Play && _hold <= 0f;

        /// <summary>ホールの全体を見せる。打つボタンで飛ばせる（飛ばしたボタンでゲージが動き出さないよう、このフレームは操作を読まない）</summary>
        private void TickIntro(float dt)
        {
            _intro -= dt;
            var input = InputManager.HasInstance ? InputManager.Instance : null;
            // オンラインは全員で同じ時間だけ見せる（ひとりだけ飛ばして先に打ち始めないように）
            if (!_mode.IsOnline && input != null && input.IsActionDown(ShootAction))
            {
                _intro = 0f;
            }

            if (_intro <= 0f)
            {
                _phase = Phase.Play;
            }

            _view.ShowGuide(null, dt);
        }

        private void TickPlay(float dt)
        {
            var aiming = World.Phase == GolfHolePhase.Aiming;
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

            TickSwingHold(dt);
            _control.Tick(dt);
            SendLocalAim();
            // 振り下ろしている間は持ち時間を止める（ゲージを止めたのに、当たる前に時間切れで自動の1打にならないように）
            if (!(aiming && _control.Striking))
            {
                _round.Tick(dt);
                _ticked = true;
            }

            if (_phase != Phase.Play)
            {
                return;
            }

            var ball = World.Balls[World.CurrentPlayer];
            var point = _view.BallPoint(ball);
            if (World.Phase == GolfHolePhase.Aiming)
            {
                Aim(point);
            }
            else
            {
                Watch(ball, point);
            }
        }

        /// <summary>狙っている間: 打つ子を球の横に構えさせ、カメラを球の後ろへ、ガイド線を出す（特殊を ON にしていれば水色・風は曲がる）</summary>
        private void Aim(Vector3 point)
        {
            var aim = GolfSpace.ToWorld(GolfHoleWorld.Direction(World.AimAngle));
            _camera.Aim(point, aim);
            // 振り下ろしている間・打った球が動き出すのを待つ間（オンライン）は、構え直さずに振り抜いたまま
            if (_control.Striking || SwingHeld)
            {
                _view.ShowGuide(null, Time.deltaTime);
                return;
            }

            var hands = DisplayAim;
            _view.Shooter.Address(CurrentTeam.Current, point, aim, _view.Surface, hands.GaugeRunning ? hands.Power : 0f);
            var special = hands.Special ? GolfSpecial.For(CurrentTeam.Current, hands.Curve) : GolfSpecial.None;
            var guide = GolfGuide.Compute(World.Hole, World.Balls, World.CurrentPlayer, World.AimAngle, special);
            _view.ShowGuide(guide, Time.deltaTime, !special.IsNone);
        }

        /// <summary>転がっている間: 球を追う。氷は通り道を凍らせ、雷は加速する向きの線を出す</summary>
        private void Watch(GolfBall ball, Vector3 point)
        {
            _camera.Follow(point);
            _view.Shooter.Watch(point);
            _view.TrackIce(ball);
            if (_control.BoostAvailable)
            {
                var guide = GolfGuide.Compute(World.Hole, World.Balls, World.CurrentPlayer, _control.BoostAngle);
                _view.ShowGuide(guide, Time.deltaTime, true);
                return;
            }

            _view.ShowGuide(null, Time.deltaTime);
        }

        private enum Phase
        {
            /// <summary>ホールの全体を見せている</summary>
            Intro,
            /// <summary>打っている</summary>
            Play,
            /// <summary>全員が終わって、最後の反応を見せている（その後スコアカードか結果）</summary>
            HoleOver,
            /// <summary>ホールの間のスコアカード</summary>
            Scorecard,
            /// <summary>結果の画面</summary>
            Result,
        }
    }
}
