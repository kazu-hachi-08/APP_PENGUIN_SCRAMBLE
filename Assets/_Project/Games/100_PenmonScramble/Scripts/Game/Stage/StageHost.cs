using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの計算を進める側（ソロ・オンラインのホスト）。全員の入力を Core に入れて固定の時間刻みで更新し、
    /// 結果のスナップショットを自分の画面に描いて、ゲストへも送る（§8）。ソロは link が null なだけで同じ流れ
    /// </summary>
    public sealed class StageHost
    {
        public const int LocalSeat = 0;

        // 重い端末で処理が追いつかないときに、追いつこうとして余計に重くならないよう上限を決める
        private const int MaxTicksPerFrame = 5;

        private readonly StageView _view;
        private readonly StageLink _link;
        private readonly List<int> _seats = new List<int>();
        private readonly Dictionary<int, InputLatch> _remoteInputs = new Dictionary<int, InputLatch>();

        private StageWorld _world;
        private StageRecorder _recorder;
        private Unit[] _players;
        private int _round;
        private float _accumulator;
        private StageSnapshot _unsent;

        public StageHost(StageView view, StageLink link, int playerCount)
        {
            _view = view;
            _link = link;
            for (var seat = 0; seat < playerCount; seat++)
            {
                _seats.Add(seat);
                if (seat != LocalSeat)
                {
                    _remoteInputs.Add(seat, new InputLatch());
                }
            }
        }

        public StageSnapshot Latest { get; private set; }

        private bool IsFinished => Latest.Result != StageResult.Playing;

        /// <summary>最初の開始と「もう一度」。いま部屋にいる人数で作り直す</summary>
        public void StartRound()
        {
            _recorder?.Detach();
            _round++;
            _players = TrialStage.Create(System.Environment.TickCount, _seats.ToArray(), _round, out _world);
            _recorder = new StageRecorder(_world, _round);
            _accumulator = 0f;
            _unsent = null;
            Publish(_recorder.Capture());
            _view.Render(1f);
        }

        public void ReceiveInput(int seat, PlayerInput input)
        {
            if (_remoteInputs.TryGetValue(seat, out var latch))
            {
                latch.Receive(input);
            }
        }

        /// <summary>ゲストが抜けた。その人のペンギンを消して、残りで続ける（§8）</summary>
        public void RemovePlayer(int seat)
        {
            _seats.Remove(seat);
            _remoteInputs.Remove(seat);
            foreach (var player in _players)
            {
                if (player.Seat == seat)
                {
                    _world.RemoveUnit(player.Id);
                }
            }
        }

        public void Update(float deltaTime, LocalInputSampler localInput)
        {
            if (IsFinished)
            {
                _view.Render(1f);
                return;
            }

            _accumulator += deltaTime;
            var ticks = 0;
            while (_accumulator >= Balance.TickSeconds && ticks < MaxTicksPerFrame && !IsFinished)
            {
                ApplyInputs(localInput);
                _world.Tick();
                Publish(_recorder.Capture());
                _accumulator -= Balance.TickSeconds;
                ticks++;
            }

            // 上限で打ち切った分は捨てる（あとで一気に進んで見えるより、少し遅くなる方が遊びやすい）
            _accumulator = Mathf.Min(_accumulator, Balance.TickSeconds);
            _view.Render(IsFinished ? 1f : _accumulator / Balance.TickSeconds);
        }

        private void ApplyInputs(LocalInputSampler localInput)
        {
            // 倒れていても毎回読む。押したままの記録が残って、次の挑戦の最初に技が出ないようにするため
            var local = localInput.Consume();
            foreach (var player in _players)
            {
                var input = player.Seat == LocalSeat ? local : ConsumeRemote(player.Seat);
                _world.SetInput(player.Id, input);
            }
        }

        private PlayerInput ConsumeRemote(int seat)
        {
            return _remoteInputs.TryGetValue(seat, out var latch) ? latch.Consume() : default;
        }

        private void Publish(StageSnapshot snapshot)
        {
            _view.Apply(snapshot);
            Latest = snapshot;
            if (_link == null)
            {
                return;
            }

            // 自分の画面に描いた後で足す（自分の画面で同じ出来事を2回出さないため）
            if (_unsent != null)
            {
                snapshot.PrependEvents(_unsent);
            }

            var isDue = snapshot.Tick % Balance.SnapshotIntervalTicks == 0 || snapshot.Result != StageResult.Playing;
            if (!isDue)
            {
                _unsent = snapshot;
                return;
            }

            _link.SendSnapshot(StageCodec.EncodeSnapshot(snapshot));
            _unsent = null;
        }
    }
}
