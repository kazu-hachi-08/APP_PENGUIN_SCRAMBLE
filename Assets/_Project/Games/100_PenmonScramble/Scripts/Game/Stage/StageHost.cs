using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの計算を進める側（ソロ・オンラインのホスト）。全員の入力を Core に入れて固定の時間刻みで更新し、
    /// 結果のスナップショットを自分の画面に描いて、ゲストへも送る。ソロは link が null なだけで同じ流れ。
    /// 最初の挑戦は、ゲスト全員の編成が届いてから始める
    /// </summary>
    public sealed class StageHost
    {
        public const int LocalSeat = 0;

        // 重い端末で処理が追いつかないときに、追いつこうとして余計に重くならないよう上限を決める（ボスの計測・技の小窓も同じ上限で進める）
        public const int MaxTicksPerFrame = 5;

        // ゲストの編成がこれだけ待っても届かなければ、代わりの3体（TrialStage.RotatingParty）で始める（いつまでも始まらない、を防ぐ）
        private const float PartyWaitSeconds = 3f;

        private readonly StageView _view;
        private readonly StageLink _link;
        private readonly List<int> _seats = new List<int>();
        private readonly Dictionary<int, InputLatch> _remoteInputs = new Dictionary<int, InputLatch>();
        private readonly Dictionary<int, UnitStats[]> _parties = new Dictionary<int, UnitStats[]>();
        private readonly StageDef _stage;

        private StageWorld _world;
        private StageRecorder _recorder;
        private Unit[] _players;
        private int _round;
        private float _accumulator;
        private float _partyWait;
        private StageSnapshot _unsent;

        public StageHost(StageView view, StageLink link, int playerCount, StageDef stage, UnitStats[] localParty)
        {
            _view = view;
            _link = link;
            _stage = stage;
            _parties[LocalSeat] = localParty;
            for (var seat = 0; seat < playerCount; seat++)
            {
                _seats.Add(seat);
                if (seat != LocalSeat)
                {
                    _remoteInputs.Add(seat, new InputLatch());
                }
            }
        }

        /// <summary>最初の挑戦が始まるまでは null</summary>
        public StageSnapshot Latest { get; private set; }

        private bool IsFinished => Latest.Result != StageResult.Playing;

        private bool HasAllParties => _seats.TrueForAll(_parties.ContainsKey);

        /// <summary>「もう一度」。いま部屋にいる人数で作り直す（最初の挑戦は Update で始まる）</summary>
        public void StartRound()
        {
            _recorder?.Detach();
            _round++;
            var seats = _seats.ToArray();
            _players = TrialStage.Create(_stage, System.Environment.TickCount, seats, PartiesOf(seats), out _world);
            _recorder = new StageRecorder(_world, _round);
            _accumulator = 0f;
            _unsent = null;
            Publish(_recorder.Capture());
            _view.Render(1f);
        }

        /// <summary>ゲストの編成が届いた。次に始める挑戦から使う</summary>
        public void ReceiveParty(int seat, UnitStats[] party)
        {
            if (_seats.Contains(seat))
            {
                _parties[seat] = party;
            }
        }

        private UnitStats[][] PartiesOf(int[] seats)
        {
            var parties = new UnitStats[seats.Length][];
            for (var i = 0; i < seats.Length; i++)
            {
                parties[i] = _parties.TryGetValue(seats[i], out var party) && party.Length > 0
                    ? party
                    : TrialStage.PartyStats(TrialStage.RotatingParty(1, i, seats.Length));
            }

            return parties;
        }

        public void ReceiveInput(int seat, PlayerInput input)
        {
            if (_remoteInputs.TryGetValue(seat, out var latch))
            {
                latch.Receive(input);
            }
        }

        /// <summary>ゲストが抜けた。その人のペンギンを消して、残りで続ける</summary>
        public void RemovePlayer(int seat)
        {
            _seats.Remove(seat);
            _remoteInputs.Remove(seat);
            if (_players == null)
            {
                return;
            }

            foreach (var player in _players)
            {
                if (player.Seat == seat)
                {
                    _world.RemoveUnit(player.Id);
                }
            }
        }

        /// <summary>
        /// 固定の時間刻み（Balance.TickSeconds）で溜まった分だけ Tick に進める。戻り値は描画用の補間値（0〜1）。
        /// ステージ・ベンチマーク・技の小窓で同じ骨組みを使うので、ここに一つだけ置く
        /// </summary>
        /// <param name="canTick">null なら刻みと上限だけで止まる。StageHost は終わったら進めないために使う</param>
        public static float StepFixedTicks(ref float accumulator, float dt, Func<bool> canTick, Action tick)
        {
            accumulator += dt;
            var ticks = 0;
            while (accumulator >= Balance.TickSeconds && ticks < MaxTicksPerFrame && (canTick == null || canTick()))
            {
                tick();
                accumulator -= Balance.TickSeconds;
                ticks++;
            }

            // 上限で打ち切った分は捨てる（あとで一気に進んで見えるより、少し遅くなる方が遊びやすい）
            accumulator = Mathf.Min(accumulator, Balance.TickSeconds);
            return accumulator / Balance.TickSeconds;
        }

        public void Update(float deltaTime, LocalInputSampler localInput)
        {
            if (Latest == null)
            {
                WaitForParties(deltaTime, localInput);
                return;
            }

            if (IsFinished)
            {
                _view.Render(1f);
                return;
            }

            var interpolation = StepFixedTicks(ref _accumulator, deltaTime, () => !IsFinished, () =>
            {
                ApplyInputs(localInput);
                _world.Tick();
                Publish(_recorder.Capture());
            });
            _view.Render(IsFinished ? 1f : interpolation);
        }

        private void WaitForParties(float deltaTime, LocalInputSampler localInput)
        {
            // 待っている間に押したボタンで、始まった瞬間に技が出ないように読み捨てる
            localInput.Consume();
            _partyWait += deltaTime;
            if (HasAllParties || _partyWait >= PartyWaitSeconds)
            {
                StartRound();
            }
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
