using System;
using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ボスラッシュの部屋の待ちの窓での通信: ホストが選んだステージと難易度を、ゲストへ届ける（受け渡しは RoomChoiceLink）
    /// </summary>
    public sealed class RushLobbyLink
    {
        // メッセージの名前（scramble.rush.choice / scramble.rush.ask）は配った版どうしで合わせるので変えない
        private readonly RoomChoiceLink _link = new RoomChoiceLink("scramble.rush");

        public RushLobbyLink()
        {
            _link.AskReceived += () => AskReceived?.Invoke();
            _link.ChoiceReceived += data =>
            {
                if (StageCodec.TryDecodeRushChoice(data, out var choice))
                {
                    ChoiceReceived?.Invoke(choice);
                }
            };
        }

        /// <summary>ゲストのみ: ホストの選択が届いた</summary>
        public event Action<RushChoice> ChoiceReceived;

        /// <summary>ホストのみ: ゲストが選択を知りたがっている</summary>
        public event Action AskReceived;

        public void OpenAsHost() => _link.OpenAsHost();

        /// <summary>受け取れるようにしてから、ホストに今の選択を頼む</summary>
        public void OpenAsGuest() => _link.OpenAsGuest();

        public void Close() => _link.Close();

        /// <summary>ホストのみ: 部屋にいるゲスト全員へ送る</summary>
        public void SendChoice(RushChoice choice) => _link.Send(StageCodec.EncodeRushChoice(choice));
    }
}
