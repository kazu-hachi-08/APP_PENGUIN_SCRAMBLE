using PenmonScramble.Golf.Core;

namespace PenmonScramble.Golf.Game
{
    public enum GolfRole
    {
        /// <summary>ひとりで（開発用の2人も、1台で交代に打つのでこれ）</summary>
        Solo,
        /// <summary>みんなでのホスト: ラウンドを計算して全員へ配る</summary>
        Host,
        /// <summary>みんなでのゲスト: ホストから届いたラウンドを写し、自分の番の入力をホストへ送る</summary>
        Guest,
    }

    /// <summary>ゲストの入力をホストへ送る口（GolfOnlinePlay が持つ）</summary>
    public interface IGolfInputSender
    {
        /// <summary>狙っている様子。毎フレーム渡すので、送るかどうか（変わったか・間隔）は送る側が決める</summary>
        void SendAim(GolfAim aim);

        void Send(GolfInput input);
    }

    /// <summary>コースをどう回るか（ひとり・ホスト・ゲスト）と、この端末で操作する席</summary>
    public sealed class GolfPlayMode
    {
        private GolfPlayMode(GolfRole role, int localSeat, IGolfInputSender sender)
        {
            Role = role;
            LocalSeat = localSeat;
            Sender = sender;
        }

        public static GolfPlayMode Solo { get; } = new GolfPlayMode(GolfRole.Solo, -1, null);

        public static GolfPlayMode Host() => new GolfPlayMode(GolfRole.Host, 0, null);

        public static GolfPlayMode Guest(int seat, IGolfInputSender sender) => new GolfPlayMode(GolfRole.Guest, seat, sender);

        public GolfRole Role { get; }

        /// <summary>この端末で操作する席。-1 なら全員（ひとりで・開発用の2人）</summary>
        public int LocalSeat { get; }

        /// <summary>ゲストのみ: 入力をホストへ送る口</summary>
        public IGolfInputSender Sender { get; }

        public bool IsOnline => Role != GolfRole.Solo;

        public bool IsGuest => Role == GolfRole.Guest;

        /// <summary>結果を記録に書く席（ひとりで回るときは席 0）</summary>
        public int RecordSeat => LocalSeat < 0 ? 0 : LocalSeat;
    }
}
