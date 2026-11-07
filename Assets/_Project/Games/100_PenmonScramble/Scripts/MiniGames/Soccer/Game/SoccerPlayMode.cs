using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Soccer.Game
{
    public enum SoccerRole
    {
        /// <summary>ひとりで（開発用の試しのピッチ・PK戦の確認も）</summary>
        Solo,
        /// <summary>みんなでのホスト: 試合を計算して全員へ配る</summary>
        Host,
        /// <summary>みんなでのゲスト: ホストから届いた試合を写し、自分の入力をホストへ送る</summary>
        Guest,
    }

    /// <summary>ゲストの入力をホストへ送る口（SoccerOnlinePlay が持つ）</summary>
    public interface ISoccerInputSender
    {
        /// <summary>毎フレーム渡すので、送るかどうか（変わったか・間隔）は送る側が決める</summary>
        void SendInput(SoccerNetInput input);
    }

    /// <summary>試合をどう遊ぶか（ひとり・ホスト・ゲスト）と、この端末の席</summary>
    public sealed class SoccerPlayMode
    {
        private SoccerPlayMode(SoccerRole role, int localSeat, ISoccerInputSender sender)
        {
            Role = role;
            LocalSeat = localSeat;
            Sender = sender;
        }

        public static SoccerPlayMode Solo { get; } = new SoccerPlayMode(SoccerRole.Solo, 0, null);

        public static SoccerPlayMode Host() => new SoccerPlayMode(SoccerRole.Host, 0, null);

        public static SoccerPlayMode Guest(int seat, ISoccerInputSender sender) => new SoccerPlayMode(SoccerRole.Guest, seat, sender);

        public SoccerRole Role { get; }

        /// <summary>この端末で操作する席（ひとり・ホストは 0）</summary>
        public int LocalSeat { get; }

        /// <summary>ゲストのみ: 入力をホストへ送る口</summary>
        public ISoccerInputSender Sender { get; }

        public bool IsOnline => Role != SoccerRole.Solo;

        public bool IsGuest => Role == SoccerRole.Guest;

        public bool IsHost => Role == SoccerRole.Host;
    }
}
