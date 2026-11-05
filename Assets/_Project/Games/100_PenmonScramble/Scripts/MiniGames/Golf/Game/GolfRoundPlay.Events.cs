using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ホールで起きたことの見せ方: 音・知らせ・打つ子の反応（喜ぶ・頭を抱える）・バンパーが弾む・水しぶき・旗が跳ねる・
    /// 特殊ショット（名前の知らせ・水切りの輪・雷の輪）・ギミック（間欠泉・ワープの泡・動く物からよけた球）
    /// </summary>
    public partial class GolfRoundPlay
    {
        // 喜ぶ・頭を抱えるのを見せる秒（この間は次の番を始めない）
        private const float CheerHoldSeconds = 2.2f;
        private const float DespairHoldSeconds = 1.5f;
        private const float MessageSeconds = 1.6f;
        private const float TurnMessageSeconds = 1.1f;
        // カップインで喜ぶ子を出す所（カップからカメラの方へ）
        private const float CheerDistance = 0.8f;

        private static readonly Color GoodColor = new Color(1f, 0.88f, 0.3f);
        private static readonly Color BadColor = new Color(0.65f, 0.85f, 1f);

        private void OnHappened(GolfEvent e)
        {
            switch (e.Kind)
            {
                case GolfEventKind.Shot:
                    GolfSounds.PlayShot(e.Strength);
                    _view.Shooter.OnShot(e.Strength);
                    ResetSwingHold();
                    break;
                case GolfEventKind.Special:
                    OnSpecial(e);
                    break;
                case GolfEventKind.Boost:
                    GolfSounds.Play(GolfSound.Boost);
                    _view.BoostAt(e.Position);
                    break;
                case GolfEventKind.Skip:
                    GolfSounds.Play(e.Strength > 0f ? GolfSound.Splash : GolfSound.Skip);
                    _view.SkipAt(e.Position, e.Strength > 0f);
                    break;
                case GolfEventKind.WallHit:
                    GolfSounds.PlayWall(e.Strength);
                    _view.PopWallNear(e.Position);
                    break;
                case GolfEventKind.BallHit:
                    GolfSounds.Play(GolfSound.BallHit);
                    break;
                case GolfEventKind.Jump:
                    GolfSounds.Play(GolfSound.Jump);
                    break;
                case GolfEventKind.Land:
                    GolfSounds.Play(GolfSound.Land);
                    break;
                case GolfEventKind.Geyser:
                    GolfSounds.Play(GolfSound.Geyser);
                    _view.PuffAt(e.Position);
                    break;
                case GolfEventKind.Warp:
                    GolfSounds.Play(GolfSound.Warp);
                    _view.WarpAt((int)e.Strength);
                    break;
                case GolfEventKind.Nudged:
                    GolfSounds.Play(GolfSound.Nudge);
                    _view.PuffAt(e.Position);
                    break;
                case GolfEventKind.LipOut:
                    GolfSounds.Play(GolfSound.LipOut);
                    _hud.Flash("おしい！", BadColor, MessageSeconds);
                    break;
                case GolfEventKind.CupIn:
                    OnCupIn(e);
                    break;
                case GolfEventKind.Fell:
                    OnFell(e);
                    break;
                case GolfEventKind.GaveUp:
                    GolfSounds.Play(GolfSound.GaveUp);
                    _hud.Flash($"{GolfSeats.Label(e.Player)}  上限で終わり", BadColor, MessageSeconds);
                    break;
                case GolfEventKind.TurnStart:
                    OnTurnStart(e);
                    break;
            }
        }

        /// <summary>特殊ショットの名前を、打った子の属性の色で出す</summary>
        private void OnSpecial(GolfEvent e)
        {
            var kind = (GolfSpecialKind)e.Other;
            GolfSounds.Play(GolfSound.Special);
            var color = PenguinPalette.ElementColor(_round.Teams[e.Player].Current.Element);
            _hud.Flash($"{GolfSpecial.NameOf(kind)}！", Color.Lerp(color, Color.white, 0.35f), MessageSeconds);
        }

        private void OnCupIn(GolfEvent e)
        {
            var strokes = World.Strokes(e.Player);
            // ホールインワンは特別なジングル（ふつうのカップインと聞き分けられるように）
            GolfSounds.Play(strokes == 1 ? GolfSound.HoleInOne : GolfSound.CupIn);
            _view.CelebrateCup();
            var name = GolfScoreNames.ForStrokes(strokes, World.Hole.Par);
            _hud.Flash($"{name}！", GoodColor, MessageSeconds + 0.6f);
            // 打った本人が入れたときだけ喜ぶ（弾かれて入った人の子はいま出ていない）
            if (e.Player != World.CurrentPlayer)
            {
                return;
            }

            var cup = _view.GroundPoint(World.Hole.Cup);
            var toCamera = Camera.main != null ? Camera.main.transform.position - cup : Vector3.back;
            toCamera.y = 0f;
            var at = cup + (toCamera.sqrMagnitude > 0f ? toCamera.normalized : Vector3.back) * CheerDistance;
            _view.Shooter.Cheer(at, at + toCamera);
            _hold = CheerHoldSeconds;
        }

        private void OnFell(GolfEvent e)
        {
            GolfSounds.Play(GolfSound.Splash);
            _view.SplashAt(e.Position);
            if (e.Player != World.CurrentPlayer)
            {
                _hud.Flash($"{GolfSeats.Label(e.Player)} の球が落ちた（もとの位置へ）", BadColor, MessageSeconds);
                return;
            }

            _hud.Flash("ポチャン！  1打たす", BadColor, MessageSeconds);
            _view.Shooter.Despair();
            _hold = DespairHoldSeconds;
        }

        /// <summary>番が替わった。誰の番かの知らせは、喜ぶ・頭を抱えるのを見せ終わってから出す（カップインの知らせを消さないように）</summary>
        private void OnTurnStart(GolfEvent e)
        {
            _camera.ResetOrbit();
            _control.ResetForTurn();
            _view.MeltIce();
            ResetSwingHold();
            ResetRemoteAim(e.Player);
            _turnBanner = _round.PlayerCount > 1 ? e.Player : -1;
        }

        private void ShowTurnBanner()
        {
            if (_turnBanner < 0)
            {
                return;
            }

            var label = _mode.IsOnline && _turnBanner == _mode.LocalSeat ? "あなたの番" : $"{GolfSeats.Label(_turnBanner)} の番";
            _hud.Flash(label, GolfSeats.ColorOf(_turnBanner), TurnMessageSeconds);
            GolfSounds.Play(GolfSound.Turn);
            _turnBanner = -1;
        }

        /// <summary>
        /// 自分の番の持ち時間が残り少ないとき、1 秒ごとにカチッと鳴らす（持ち時間の輪が赤く脈打つのと同じ秒から）。
        /// ほかの人の番では鳴らさない（自分が急がされているように聞こえないように）
        /// </summary>
        private void TickHurrySound()
        {
            var counting = _phase == Phase.Play && CanAct && _control.IsLocalTurn
                && World.Phase == GolfHolePhase.Aiming && !DisplayAim.Striking
                && World.ShotTimeLeft <= GolfHud.HurrySeconds;
            var second = counting ? Mathf.CeilToInt(World.ShotTimeLeft) : -1;
            if (second >= 1 && second != _hurrySecond)
            {
                GolfSounds.Play(GolfSound.Tick);
            }

            _hurrySecond = second;
        }
    }
}
