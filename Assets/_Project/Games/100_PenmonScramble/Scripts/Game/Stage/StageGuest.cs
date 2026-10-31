using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// オンラインのゲスト側。計算はせず、入力をホストへ送り、届いたスナップショットの間を補間して描くだけ
    /// </summary>
    public sealed class StageGuest
    {
        private readonly StageView _view;
        private readonly StageLink _link;

        private float _sinceSnapshot;
        private float _snapshotInterval = Balance.TickSeconds * Balance.SnapshotIntervalTicks;
        private float _sendTimer;
        private PlayerInput _lastSent;
        private bool _hasSent;

        public StageGuest(StageView view, StageLink link)
        {
            _view = view;
            _link = link;
            _link.SnapshotReceived += OnSnapshot;
        }

        /// <summary>まだ1つも届いていなければ null</summary>
        public StageSnapshot Latest { get; private set; }

        public void Dispose()
        {
            _link.SnapshotReceived -= OnSnapshot;
        }

        public void Update(float deltaTime, LocalInputSampler localInput)
        {
            // ホストの更新と同じ間隔でまとめて送る。毎フレーム送ると、速い端末ほど通信が増えるため
            _sendTimer += deltaTime;
            if (_sendTimer >= Balance.TickSeconds)
            {
                _sendTimer = Mathf.Min(_sendTimer - Balance.TickSeconds, Balance.TickSeconds);
                SendIfChanged(localInput.Consume());
            }

            if (Latest == null)
            {
                return;
            }

            _sinceSnapshot += deltaTime;
            _view.Render(Mathf.Clamp01(_sinceSnapshot / _snapshotInterval));
        }

        /// <summary>届く方で送っているので、変わったときだけ送れば、ホストは最後の入力を持ち続けられる</summary>
        private void SendIfChanged(PlayerInput input)
        {
            if (_hasSent && IsSame(input, _lastSent))
            {
                return;
            }

            _link.SendInput(StageCodec.EncodeInput(input));
            _lastSent = input;
            _hasSent = true;
        }

        private static bool IsSame(PlayerInput a, PlayerInput b)
        {
            return a.Move.Equals(b.Move) && a.Skill1 == b.Skill1 && a.Skill2 == b.Skill2 && a.Special == b.Special && a.Switch == b.Switch;
        }

        private void OnSnapshot(byte[] data)
        {
            var snapshot = StageCodec.DecodeSnapshot(data);
            if (snapshot == null)
            {
                return;
            }

            // 次の補間にかける時間を、実際に進んだ更新の数から決める（間隔が一定でなくても速さが変わって見えないように）
            var sameRound = Latest != null && Latest.Round == snapshot.Round;
            var ticks = sameRound ? Mathf.Max(1, snapshot.Tick - Latest.Tick) : Balance.SnapshotIntervalTicks;
            _snapshotInterval = ticks * Balance.TickSeconds;
            _sinceSnapshot = 0f;
            _view.Apply(snapshot);
            Latest = snapshot;
        }
    }
}
