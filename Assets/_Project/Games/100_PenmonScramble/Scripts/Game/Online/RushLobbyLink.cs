using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using Unity.Collections;
using Unity.Netcode;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ボスラッシュの部屋の待ちの窓での通信: ホストが選んだステージと難易度を、ゲストへ届ける。
    /// ゲストは受け取る準備ができてから「いまの選択を教えて」と頼み、ホストは頼まれたときと選び直したときに全員へ送る
    /// （ホストから先に送ると、ゲストの受け取る準備より先に届いて捨てられることがあるため。OnlineSession の名前交換と同じ考え）
    /// </summary>
    public sealed class RushLobbyLink
    {
        private const string ChoiceMessage = "scramble.rush.choice";
        private const string AskMessage = "scramble.rush.ask";
        private const int MessageSize = 64;

        private readonly List<ulong> _guestIds = new List<ulong>();
        private bool _open;

        /// <summary>ゲストのみ: ホストの選択が届いた</summary>
        public event Action<RushChoice> ChoiceReceived;

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

            messaging.RegisterNamedMessageHandler(AskMessage, (_, _) => AskReceived?.Invoke());
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

            messaging.RegisterNamedMessageHandler(ChoiceMessage, (_, reader) =>
            {
                reader.ReadValueSafe(out byte[] data);
                if (StageCodec.TryDecodeRushChoice(data, out var choice))
                {
                    ChoiceReceived?.Invoke(choice);
                }
            });
            _open = true;
            using var writer = new FastBufferWriter(MessageSize, Allocator.Temp);
            messaging.SendNamedMessage(AskMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            var messaging = Messaging;
            messaging?.UnregisterNamedMessageHandler(ChoiceMessage);
            messaging?.UnregisterNamedMessageHandler(AskMessage);
        }

        /// <summary>ホストのみ: 部屋にいるゲスト全員へ送る</summary>
        public void SendChoice(RushChoice choice)
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
            writer.WriteValueSafe(StageCodec.EncodeRushChoice(choice));
            messaging.SendNamedMessage(ChoiceMessage, _guestIds, writer, NetworkDelivery.ReliableSequenced);
        }
    }
}
