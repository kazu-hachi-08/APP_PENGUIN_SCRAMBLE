using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中の通信路: ホスト → 全員にスナップショット、ゲスト → ホストに入力。
    /// 接続は共通基盤の OnlineSession に任せ、ここはバイト列の受け渡しだけを持つ（中身は Core の StageCodec）
    /// </summary>
    public sealed class StageLink
    {
        private const string SnapshotMessage = "scramble.snapshot";
        private const string InputMessage = "scramble.input";
        private const int InitialMessageSize = 1024;
        // 敵や弾が多い瞬間でも数KB。MTU を超える分は分割して送る
        private const int MaxMessageSize = 64 * 1024;

        private readonly List<ulong> _guestIds = new List<ulong>();

        /// <summary>ゲストのみ: ホストからスナップショットが届いた</summary>
        public event Action<byte[]> SnapshotReceived;

        /// <summary>ホストのみ: ゲストから入力が届いた。引数は送り主の clientId</summary>
        public event Action<ulong, byte[]> InputReceived;

        private static NetworkManager Network => NetworkManager.Singleton;
        private static CustomMessagingManager Messaging => Network != null ? Network.CustomMessagingManager : null;

        /// <summary>試合開始のコールバックの中で、すぐに呼ぶこと（ホストは開始直後から送ってくるため）</summary>
        public void Open()
        {
            var messaging = Messaging;
            messaging?.RegisterNamedMessageHandler(SnapshotMessage, (_, reader) => SnapshotReceived?.Invoke(ReadBytes(reader)));
            messaging?.RegisterNamedMessageHandler(InputMessage, (sender, reader) => InputReceived?.Invoke(sender, ReadBytes(reader)));
        }

        public void Close()
        {
            var messaging = Messaging;
            messaging?.UnregisterNamedMessageHandler(SnapshotMessage);
            messaging?.UnregisterNamedMessageHandler(InputMessage);
        }

        public void SendSnapshot(byte[] data)
        {
            var messaging = Messaging;
            if (messaging == null)
            {
                return;
            }

            // ホスト自身には送らない（自分の画面は送る前のスナップショットで描いている）
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

            using var writer = Write(data);
            // 位置は最新だけで足りるが、ダメージ数字などの出来事は落とすと見えなくなるので届く方にする
            messaging.SendNamedMessage(SnapshotMessage, _guestIds, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendInput(byte[] data)
        {
            var messaging = Messaging;
            if (messaging == null)
            {
                return;
            }

            using var writer = Write(data);
            messaging.SendNamedMessage(InputMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        private static FastBufferWriter Write(byte[] data)
        {
            var writer = new FastBufferWriter(InitialMessageSize, Allocator.Temp, MaxMessageSize);
            writer.WriteValueSafe(data);
            return writer;
        }

        private static byte[] ReadBytes(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] data);
            return data;
        }
    }
}
