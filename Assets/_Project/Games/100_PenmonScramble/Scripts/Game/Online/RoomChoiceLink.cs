using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 部屋の待ちの窓での「ホストが選んだもの」を、ゲストへ届ける通信路（中身はバイト列。読み書きは使う側）。
    /// ゲストは受け取る準備ができてから「いまの選択を教えて」と頼み、ホストは頼まれたときと選び直したときに全員へ送る
    /// （ホストから先に送ると、ゲストの受け取る準備より先に届いて捨てられることがあるため。OnlineSession の名前交換と同じ考え）。
    /// 部屋の遊び（RoomModeSelector）・ボスラッシュのステージ（RushLobbyLink）・ゴルフのコースが、名前を変えて1つずつ使う
    /// </summary>
    public sealed class RoomChoiceLink
    {
        private const int MessageSize = 64;

        private readonly string _choiceMessage;
        private readonly string _askMessage;
        private readonly List<ulong> _guestIds = new List<ulong>();
        private bool _open;

        /// <param name="name">メッセージの名前のもと（「scramble.rush」なら scramble.rush.choice / scramble.rush.ask）</param>
        public RoomChoiceLink(string name)
        {
            _choiceMessage = name + ".choice";
            _askMessage = name + ".ask";
        }

        /// <summary>ゲストのみ: ホストの選択が届いた（読めるかは受け取った側が決める）</summary>
        public event Action<byte[]> ChoiceReceived;

        /// <summary>ホストのみ: ゲストが選択を知りたがっている</summary>
        public event Action AskReceived;

        private static NetworkManager Network => NetworkManager.Singleton;
        private static CustomMessagingManager Messaging => Network != null ? Network.CustomMessagingManager : null;

        public void OpenAsHost()
        {
            Close();
            var messaging = Messaging;
            if (messaging == null)
            {
                return;
            }

            messaging.RegisterNamedMessageHandler(_askMessage, (_, _) => AskReceived?.Invoke());
            _open = true;
        }

        /// <summary>受け取れるようにしてから、ホストに今の選択を頼む</summary>
        public void OpenAsGuest()
        {
            Close();
            var messaging = Messaging;
            if (messaging == null)
            {
                return;
            }

            messaging.RegisterNamedMessageHandler(_choiceMessage, (_, reader) =>
            {
                reader.ReadValueSafe(out byte[] data);
                ChoiceReceived?.Invoke(data);
            });
            _open = true;
            using var writer = new FastBufferWriter(MessageSize, Allocator.Temp);
            messaging.SendNamedMessage(_askMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            var messaging = Messaging;
            messaging?.UnregisterNamedMessageHandler(_choiceMessage);
            messaging?.UnregisterNamedMessageHandler(_askMessage);
        }

        /// <summary>ホストのみ: 部屋にいるゲスト全員へ送る</summary>
        public void Send(byte[] data)
        {
            var messaging = Messaging;
            if (!_open || messaging == null || !Network.IsServer)
            {
                return;
            }

            _guestIds.Clear();
            foreach (var id in Network.ConnectedClientsIds)
            {
                if (id != Network.LocalClientId)
                {
                    _guestIds.Add(id);
                }
            }

            if (_guestIds.Count == 0)
            {
                return;
            }

            using var writer = new FastBufferWriter(MessageSize, Allocator.Temp);
            writer.WriteValueSafe(data);
            messaging.SendNamedMessage(_choiceMessage, _guestIds, writer, NetworkDelivery.ReliableSequenced);
        }
    }
}
