using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>サッカーのオンラインの間隔</summary>
    public static class SoccerNetRules
    {
        /// <summary>ホストがスナップショットを送る間隔（秒。毎秒15回）。ゲストはこの間をかけて届いた位置へ寄せて描く</summary>
        public const float SnapshotSeconds = 1f / 15f;

        /// <summary>
        /// ゲストがスティックの向きの変わりを送る間隔の下限（秒。ホストの1歩と同じ）。ボタン・交代は変わったらすぐ送る
        /// （押した瞬間を遅らせない・取りこぼさないため）
        /// </summary>
        public const float MoveSendSeconds = SoccerRules.StepSeconds;

        /// <summary>ゲストがシーンに入ってから「準備できた（編成）」を送り直す間隔（秒）。最初のスナップショットが届くまで</summary>
        public const float ReadyResendSeconds = 0.5f;

        /// <summary>ホストが全員の準備を待つ上限（秒）。過ぎたら、届いていない人は仮の3体で始める</summary>
        public const float ReadyTimeoutSeconds = 6f;

        /// <summary>ゲストがホストの最初のスナップショットを待つ上限（秒。ホストのシーンが始まらないまま、切断も届かないとき）</summary>
        public const float GuestGiveUpSeconds = 20f;
    }

    /// <summary>
    /// ゲストがホストへ送る入力（その時点で押しているボタンとスティック）。交代ボタンは押した回数を数えて送る
    /// （押した瞬間だけの物なので、回数が増えたことで「押した」とわかるようにする）
    /// </summary>
    public readonly struct SoccerNetInput
    {
        public readonly Vec2 Move;
        public readonly bool Shoot;
        public readonly bool Pass;
        public readonly bool Special;

        /// <summary>交代ボタンを押した回数（255 の次は 0 に戻る）</summary>
        public readonly byte SwitchCount;

        public SoccerNetInput(Vec2 move, bool shoot, bool pass, bool special, byte switchCount)
        {
            Move = move.ClampLength(1f);
            Shoot = shoot;
            Pass = pass;
            Special = special;
            SwitchCount = switchCount;
        }

        /// <summary>送るときの細かさ（向きの各成分を −127〜127 にする）。同じなら送り直さない</summary>
        public static sbyte Quantize(float value) => (sbyte)System.Math.Round(System.Math.Clamp(value, -1f, 1f) * 127f);

        public bool SameButtons(SoccerNetInput other)
        {
            return Shoot == other.Shoot && Pass == other.Pass && Special == other.Special && SwitchCount == other.SwitchCount;
        }

        public bool SameMove(SoccerNetInput other)
        {
            return Quantize(Move.X) == Quantize(other.Move.X) && Quantize(Move.Y) == Quantize(other.Move.Y);
        }
    }

    /// <summary>
    /// ホストが受けた、ゲスト1人の入力。届いた中身を、ホストの1歩ずつの入力にする。
    /// 1歩の間に「押して離した」が両方届いても押したことを落とさないよう、押されたボタンはその歩だけ押している扱いにする
    /// （次の歩で離した扱いになる。短く押したシュート・スライディングが消えないように）
    /// </summary>
    public sealed class SoccerRemoteInput
    {
        private SoccerNetInput _held;
        private bool _received;
        private bool _shootLatch;
        private bool _passLatch;
        private bool _specialLatch;
        private bool _switchPending;

        public void Receive(SoccerNetInput input)
        {
            // 初めて届いた回数はそのまま覚える（前の回数が無いので、押したとは数えない）
            _switchPending |= _received && input.SwitchCount != _held.SwitchCount;
            _shootLatch |= input.Shoot;
            _passLatch |= input.Pass;
            _specialLatch |= input.Special;
            _held = input;
            _received = true;
        }

        /// <summary>この歩の入力。押された印はここで消す</summary>
        public SoccerInput Take(out bool switchPressed)
        {
            var input = new SoccerInput(_held.Move, _held.Shoot || _shootLatch, _held.Pass || _passLatch, _held.Special || _specialLatch);
            switchPressed = _switchPending;
            _shootLatch = false;
            _passLatch = false;
            _specialLatch = false;
            _switchPending = false;
            return input;
        }
    }
}
