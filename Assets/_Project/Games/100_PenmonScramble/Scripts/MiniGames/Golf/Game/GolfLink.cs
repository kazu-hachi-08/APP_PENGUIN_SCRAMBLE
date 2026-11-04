using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフの通信路: ホスト → 全員にスナップショット、ゲスト → ホストに「準備できた（編成）」と入力。
    /// 接続は共通基盤の OnlineSession に任せ、ここはバイト列の受け渡しだけを持つ（中身は Core の GolfCodec。本編の StageLink と同じ形）
    /// </summary>
    public sealed class GolfLink
    {
        // メッセージの名前は配った版どうしで合わせるので変えない
        private const string SnapshotMessage = "scramble.golf.snapshot";
        private const string InputMessage = "scramble.golf.input";
        private const string ReadyMessage = "scramble.golf.ready";
        private const int InitialMessageSize = 512;
        // 1つのスナップショットは数百バイト。出来事が溜まっても届くよう、MTU を超える分は分割して送る
        private const int MaxMessageSize = 16 * 1024;

        private readonly List<ulong> _guestIds = new List<ulong>();
        private bool _open;

        /// <summary>ゲストのみ: ホストからスナップショットが届いた</summary>
        public event Action<byte[]> SnapshotReceived;

        /// <summary>ホストのみ: ゲストから入力が届いた。引数は送り主の clientId</summary>
        public event Action<ulong, byte[]> InputReceived;

        /// <summary>ホストのみ: ゲストがシーンに入って準備できた（中身は編成）。引数は送り主の clientId</summary>
        public event Action<ulong, byte[]> ReadyReceived;

        private static NetworkManager Network => NetworkManager.Singleton;
        private static CustomMessagingManager Messaging => Network != null ? Network.CustomMessagingManager : null;

        public void Open()
        {
            var messaging = Messaging;
            if (messaging == null)
            {
                return;
            }

            messaging.RegisterNamedMessageHandler(SnapshotMessage, (_, reader) => SnapshotReceived?.Invoke(ReadBytes(reader)));
            messaging.RegisterNamedMessageHandler(InputMessage, (sender, reader) => InputReceived?.Invoke(sender, ReadBytes(reader)));
            messaging.RegisterNamedMessageHandler(ReadyMessage, (sender, reader) => ReadyReceived?.Invoke(sender, ReadBytes(reader)));
            _open = true;
        }

        public void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            var messaging = Messaging;
            messaging?.UnregisterNamedMessageHandler(SnapshotMessage);
            messaging?.UnregisterNamedMessageHandler(InputMessage);
            messaging?.UnregisterNamedMessageHandler(ReadyMessage);
        }

        /// <summary>ホストのみ: 部屋にいるゲスト全員へ</summary>
        public void SendSnapshot(byte[] data)
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

            using var writer = Write(data);
            // 起きたこと（音・知らせ）を落とさないよう、届く方で送る
            messaging.SendNamedMessage(SnapshotMessage, _guestIds, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendInput(byte[] data) => SendToHost(InputMessage, data);

        public void SendReady(byte[] data) => SendToHost(ReadyMessage, data);

        private void SendToHost(string message, byte[] data)
        {
            var messaging = Messaging;
            if (!_open || messaging == null)
            {
                return;
            }

            using var writer = Write(data);
            messaging.SendNamedMessage(message, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
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
