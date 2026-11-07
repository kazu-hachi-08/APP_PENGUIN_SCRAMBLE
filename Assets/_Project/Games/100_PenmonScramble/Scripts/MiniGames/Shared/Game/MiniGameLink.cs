using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームのみんなで遊ぶ間の通信路: ホスト → 全員にスナップショット、ゲスト → ホストに「準備できた（編成）」と入力。
    /// 接続は共通基盤の OnlineSession に任せ、ここはバイト列の受け渡しだけを持つ（中身は各ミニゲームの Core の Codec。本編の StageLink と同じ形）。
    /// ミニゲームごとに名前を変えて1つずつ使う（ゴルフは scramble.golf、サッカーは scramble.soccer、野球は scramble.baseball）
    /// </summary>
    public sealed class MiniGameLink
    {
        private const int InitialMessageSize = 512;
        // 1つのスナップショットは数百バイト〜数キロバイト。出来事が溜まっても届くよう、MTU を超える分は分割して送る
        private const int MaxMessageSize = 16 * 1024;

        private readonly string _snapshotMessage;
        private readonly string _inputMessage;
        private readonly string _readyMessage;

        private readonly List<ulong> _guestIds = new List<ulong>();
        private bool _open;

        /// <param name="name">メッセージの名前のもと（「scramble.golf」なら scramble.golf.snapshot / .input / .ready）。配った版どうしで合わせるので変えない</param>
        public MiniGameLink(string name)
        {
            _snapshotMessage = name + ".snapshot";
            _inputMessage = name + ".input";
            _readyMessage = name + ".ready";
        }

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

            messaging.RegisterNamedMessageHandler(_snapshotMessage, (_, reader) => SnapshotReceived?.Invoke(ReadBytes(reader)));
            messaging.RegisterNamedMessageHandler(_inputMessage, (sender, reader) => InputReceived?.Invoke(sender, ReadBytes(reader)));
            messaging.RegisterNamedMessageHandler(_readyMessage, (sender, reader) => ReadyReceived?.Invoke(sender, ReadBytes(reader)));
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
            messaging?.UnregisterNamedMessageHandler(_snapshotMessage);
            messaging?.UnregisterNamedMessageHandler(_inputMessage);
            messaging?.UnregisterNamedMessageHandler(_readyMessage);
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
            messaging.SendNamedMessage(_snapshotMessage, _guestIds, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendInput(byte[] data) => SendToHost(_inputMessage, data);

        public void SendReady(byte[] data) => SendToHost(_readyMessage, data);

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
