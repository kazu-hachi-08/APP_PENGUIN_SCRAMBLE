using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 1球の投げ方（投げた瞬間に決まる物）。番号は試合の中で投げた順（1 から）。
    /// CPU が投げる球は組み立て（Plan）を持ち、曲げる入力はどの端末でも組み立てから計算する（送らない）
    /// </summary>
    public readonly struct BaseballThrow
    {
        public BaseballThrow(int id, BaseballPitchType type, BaseballPitchMagic magic, BaseballSpecial hitSpecial, BaseballPitchPlan? plan)
        {
            Id = id;
            Type = type;
            Magic = magic;
            HitSpecial = hitSpecial;
            Plan = plan;
        }

        public int Id { get; }
        public BaseballPitchType Type { get; }
        public BaseballPitchMagic Magic { get; }

        /// <summary>打つ人が出した必殺打（出していなければ None）</summary>
        public BaseballSpecial HitSpecial { get; }

        /// <summary>CPU が投げる球の組み立て（人が投げる球は null）</summary>
        public BaseballPitchPlan? Plan { get; }

        public BaseballThrow WithHitSpecial(BaseballSpecial hitSpecial) => new BaseballThrow(Id, Type, Magic, hitSpecial, Plan);

        /// <summary>投げた球そのものが同じか（必殺打は打つ人の物なので比べない）</summary>
        public bool SamePitch(BaseballThrow other)
        {
            return Id == other.Id && Type == other.Type && Magic.Special.Element == other.Magic.Special.Element
                && Magic.IsActive == other.Magic.IsActive && Magic.Special.Stars == other.Magic.Special.Stars
                && Magic.FreezeSeconds == other.Magic.FreezeSeconds && Magic.WobblePhase == other.Magic.WobblePhase
                && Plan.HasValue == other.Plan.HasValue;
        }
    }

    /// <summary>
    /// 1球を、投げ方と2人の入力の記録（BaseballRoleLog）から計算する。どの端末も同じ投げ方・同じ記録なら同じ1球になる
    /// （BaseballBoard は入力の時刻だけで決まる。CPU の曲げる入力・追い風ヒットの曲げも盤から計算する）。
    /// オンラインでは相手の入力が遅れて届くので、届くまでは最後の値が続くと見なして進め、
    /// 届いた記録が使った値と違えば投げた所から計算し直す（1球は数百歩なので、一瞬で終わる）。
    /// 計算し直したときは、前に出した起きたことの続きだけを出す（音・演出を2度鳴らさない）。
    /// 結果を決めてよいか（InputsComplete）は、結果が出た歩までの入力が全員ぶんそろったかで見る（ホストが決める）
    /// </summary>
    public sealed class BaseballPitchSession
    {
        private readonly List<(int Step, BaseballEvent Event)> _emitted = new List<(int, BaseballEvent)>();
        private readonly List<(int Step, BaseballEvent Event)> _replayed = new List<(int, BaseballEvent)>();
        private readonly List<BaseballEvent> _stepEvents = new List<BaseballEvent>();
        private readonly List<Used> _used = new List<Used>();
        private bool _dirty;

        public BaseballPitchSession(BaseballFieldDef field)
        {
            Board = new BaseballBoard(field);
        }

        public BaseballBoard Board { get; }

        public BaseballThrow Throw { get; private set; }

        public BaseballRoleLog PitcherLog { get; } = new BaseballRoleLog();

        public BaseballRoleLog BatterLog { get; } = new BaseballRoleLog();

        /// <summary>投げてから進めた歩の数</summary>
        public int Step { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>1球の結果が出た歩の数（まだなら −1）</summary>
        public int DoneStep { get; private set; } = -1;

        /// <summary>投げる人の記録を、この歩の数まで使った（これより先の曲げは結果に効かない）</summary>
        public int PitcherNeeded { get; private set; }

        /// <summary>打つ人の記録を、この歩の数まで使った</summary>
        public int BatterNeeded { get; private set; }

        public bool IsDone => DoneStep >= 0;

        /// <summary>投げた所から計算し直した回数（描く側が、跳んだ球を寄せるのに使う）</summary>
        public int Replays { get; private set; }

        /// <summary>結果が出ていて、そこまでに使った入力が全員ぶん届いている（ホストが結果を試合に入れてよい）</summary>
        public bool InputsComplete => IsDone && !_dirty && PitcherLog.Progress >= PitcherNeeded && BatterLog.Progress >= BatterNeeded;

        /// <summary>投げる（記録を空にして、盤を構えから投げる）。投げた起きたことを events に足す</summary>
        public void Begin(BaseballThrow pitch, List<BaseballEvent> events)
        {
            Throw = pitch;
            PitcherLog.Clear();
            BatterLog.Clear();
            _emitted.Clear();
            IsActive = true;
            Step = 0;
            _dirty = false;
            _stepEvents.Clear();
            Restart(_stepEvents);
            foreach (var e in _stepEvents)
            {
                _emitted.Add((-1, e));
                events.Add(e);
            }
        }

        /// <summary>球が終わった（次の球の構えへ）</summary>
        public void End()
        {
            IsActive = false;
            _dirty = false;
            Board.Reset();
        }

        /// <summary>打つ人の必殺打を後から知った（投げる人の端末は、投げた後にホストから届く）。違えば計算し直す</summary>
        public void SetHitSpecial(BaseballSpecial hitSpecial)
        {
            if (Throw.HitSpecial.IsActive == hitSpecial.IsActive && Throw.HitSpecial.Element == hitSpecial.Element && Throw.HitSpecial.Stars == hitSpecial.Stars)
            {
                return;
            }

            Throw = Throw.WithHitSpecial(hitSpecial);
            _dirty = true;
        }

        /// <summary>相手の記録が届いた（その人の物で置き換える）。使った値と違えば、次に進めるときに計算し直す</summary>
        public void MergePitcher(BaseballRoleLog log)
        {
            if (PitcherLog.Merge(log))
            {
                CheckDiverged();
            }
        }

        public void MergeBatter(BaseballRoleLog log)
        {
            if (BatterLog.Merge(log))
            {
                CheckDiverged();
            }
        }

        /// <summary>ホストの決めた記録で置き換える（結果が決まったとき。自分の記録も含めて、ホストと同じ1球にそろえる）</summary>
        public void Adopt(BaseballRoleLog pitcher, BaseballRoleLog batter, int doneStep)
        {
            PitcherLog.CopyFrom(pitcher);
            BatterLog.CopyFrom(batter);
            Step = Math.Max(Step, doneStep);
            _dirty = true;
        }

        /// <summary>1歩進める（届いた記録が前の計算と違っていれば、先に投げた所から計算し直す）。新しく起きたことを events に足す</summary>
        public void Advance(List<BaseballEvent> events)
        {
            if (!IsActive)
            {
                return;
            }

            if (_dirty)
            {
                Replay(events);
            }

            _stepEvents.Clear();
            StepOnce(_stepEvents);
            foreach (var e in _stepEvents)
            {
                _emitted.Add((Step - 1, e));
                events.Add(e);
            }
        }

        /// <summary>計算し直すだけ（進めない。結果が決まったときに、届いた記録で盤をそろえる）</summary>
        public void Settle(List<BaseballEvent> events)
        {
            if (IsActive && _dirty)
            {
                Replay(events);
            }
        }

        private void Restart(List<BaseballEvent> events)
        {
            Board.Reset();
            _used.Clear();
            DoneStep = -1;
            PitcherNeeded = 0;
            BatterNeeded = 0;
            Board.Throw(Throw.Type, Throw.Magic, Throw.HitSpecial, events, BaseballClock.AtThrow(Throw.Id));
        }

        /// <summary>投げた所から今の歩まで計算し直し、前に出した起きたことと同じ所から先だけを出す</summary>
        private void Replay(List<BaseballEvent> events)
        {
            _dirty = false;
            Replays++;
            var target = Step;
            _replayed.Clear();
            _stepEvents.Clear();
            Restart(_stepEvents);
            foreach (var e in _stepEvents)
            {
                _replayed.Add((-1, e));
            }

            Step = 0;
            while (Step < target)
            {
                _stepEvents.Clear();
                StepOnce(_stepEvents);
                foreach (var e in _stepEvents)
                {
                    _replayed.Add((Step - 1, e));
                }
            }

            var same = 0;
            while (same < _emitted.Count && same < _replayed.Count && Same(_emitted[same], _replayed[same]))
            {
                same++;
            }

            for (var i = same; i < _replayed.Count; i++)
            {
                events.Add(_replayed[i].Event);
            }

            _emitted.Clear();
            _emitted.AddRange(_replayed);
        }

        private void StepOnce(List<BaseballEvent> events)
        {
            var input = InputAt(Step, out var used);
            _used.Add(used);
            Board.Step(input, events);
            Step++;
            if (DoneStep < 0 && Board.Phase == BaseballBoardPhase.Done)
            {
                DoneStep = Step;
            }
        }

        /// <summary>
        /// その歩の入力: 投げた球が来ている間は、曲げるのは投げる人（CPU は組み立てから）、左右・振る・バントは打つ人
        /// （打つ人の左右は、記録の曲げる入力の欄を使う。CPU の分もホストが記録する）。
        /// 打球が出たら、曲げるのは打つ人（追い風ヒット。CPU の打った球は盤から計算する）
        /// </summary>
        private BaseballInput InputAt(int step, out Used used)
        {
            var board = Board;
            if (board.Phase == BaseballBoardPhase.Pitching)
            {
                var source = UsedBend.None;
                var bend = 0f;
                if (Throw.Plan.HasValue)
                {
                    bend = BaseballAi.Bend(board.Pitch, Throw.Plan.Value, board.Field);
                }
                else if (board.Pitch.CanBend)
                {
                    bend = PitcherLog.BendAt(step);
                    source = UsedBend.Pitcher;
                    PitcherNeeded = Math.Max(PitcherNeeded, step + 1);
                }

                var swing = BatterLog.SwingAt(step);
                var bunt = BatterLog.BuntAt(step);
                var time = BatterLog.SwingTimeAt(step);
                var move = BatterLog.BendAt(step);
                BatterNeeded = Math.Max(BatterNeeded, step + 1);
                used = new Used(source, bend, true, swing, bunt, time, move);
                return new BaseballInput(bend, swing, bunt, time, move);
            }

            if (board.Phase == BaseballBoardPhase.BallInPlay && board.Ball.CanSteer)
            {
                if (!SteerFromLog)
                {
                    used = default;
                    return new BaseballInput(BaseballAi.Steer(board), false, false);
                }

                var steer = BatterLog.BendAt(step);
                BatterNeeded = Math.Max(BatterNeeded, step + 1);
                used = new Used(UsedBend.Batter, steer, false);
                return new BaseballInput(steer, false, false);
            }

            used = default;
            return BaseballInput.None;
        }

        /// <summary>打球を曲げるのが人か（CPU の打った追い風ヒットは盤から計算する）。投げる前に決める</summary>
        public bool SteerFromLog { get; set; } = true;

        /// <summary>届いた記録が、計算に使った値と違う歩があるか（投げる人の曲げ・打つ人の左右 / 振る / バント / 曲げ）</summary>
        private void CheckDiverged()
        {
            for (var step = 0; step < _used.Count; step++)
            {
                var used = _used[step];
                var changed = used.Source == UsedBend.Pitcher && PitcherLog.BendAt(step) != used.Bend
                    || used.Source == UsedBend.Batter && BatterLog.BendAt(step) != used.Bend
                    || used.UsedBat && (BatterLog.SwingAt(step) != used.Swing || BatterLog.BuntAt(step) != used.Bunt || BatterLog.SwingTimeAt(step) != used.SwingTime
                        || BatterLog.BendAt(step) != used.Move);
                if (changed)
                {
                    _dirty = true;
                    return;
                }
            }
        }

        private static bool Same((int Step, BaseballEvent Event) a, (int Step, BaseballEvent Event) b)
        {
            return a.Step == b.Step && a.Event.Kind == b.Event.Kind && a.Event.Index == b.Event.Index && a.Event.Result == b.Event.Result;
        }

        private enum UsedBend
        {
            None,
            Pitcher,
            Batter,
        }

        /// <summary>その歩の計算に使った、記録から読んだ値（届いた記録と比べて、計算し直すかを決める）</summary>
        private readonly struct Used
        {
            public Used(UsedBend source, float bend, bool usedBat, bool swing = false, bool bunt = false, float swingTime = 0f, float move = 0f)
            {
                Source = source;
                Bend = bend;
                UsedBat = usedBat;
                Swing = swing;
                Bunt = bunt;
                SwingTime = swingTime;
                Move = move;
            }

            public UsedBend Source { get; }
            public float Bend { get; }

            /// <summary>打つ人の左右・振る・バントを読んだ（投げた球が来ている間）</summary>
            public bool UsedBat { get; }

            public bool Swing { get; }
            public bool Bunt { get; }
            public float SwingTime { get; }

            /// <summary>打つ人の左右（記録の曲げる入力の欄）</summary>
            public float Move { get; }
        }
    }
}
