using System;
using System.Collections.Generic;
using System.IO;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 野球のオンラインで送る中身（スナップショット・入力・編成）とバイト列の変換。
    /// 通信のライブラリに依存させず、ここだけでテストできるようにする（サッカーの SoccerCodec と同じ考え）
    /// </summary>
    public static class BaseballCodec
    {
        /// <summary>中身の並びを変えたら上げる。違う版どうしで部屋に入っても、読み違えるより無視する方がよい</summary>
        public const byte FormatVersion = 1;

        // 壊れた値が届いても、配列を作りすぎないようにする
        private const int MaxSeats = MiniGameTeams.MaxSeats;
        private const int MaxInnings = 16;
        private const int MaxMoves = 8;
        private const int MaxPartySize = 8;
        private const int MaxRun = byte.MaxValue;

        // ---- スナップショット（ホスト → ゲスト）----

        public static byte[] EncodeSnapshot(BaseballSnapshot s)
        {
            return Encode(writer =>
            {
                writer.Write((byte)s.Stadium);
                writer.Write((byte)s.Mode);
                writer.Write((byte)s.CpuStars);
                writer.Write((byte)s.SeatCount);
                for (var seat = 0; seat < s.SeatCount; seat++)
                {
                    writer.Write((sbyte)s.SeatTeam[seat]);
                    writer.Write(s.SeatLeft[seat]);
                    writer.Write((byte)Math.Clamp(s.SeatRbi[seat], 0, byte.MaxValue));
                }

                for (var team = 0; team < BaseballRules.TeamCount; team++)
                {
                    writer.Write((byte)s.Lineup[team].Length);
                    for (var slot = 0; slot < s.Lineup[team].Length; slot++)
                    {
                        writer.Write((ushort)s.Lineup[team][slot]);
                        writer.Write((sbyte)s.Owner[team][slot]);
                    }
                }

                WriteState(writer, s.State);
                writer.Write((byte)s.Stage);
                writer.Write((ushort)s.ThrowCount);
                writer.Write((sbyte)s.PitcherSeat);
                writer.Write(s.HasThrow);
                if (s.HasThrow)
                {
                    WriteThrow(writer, s.Throw);
                    WriteLog(writer, s.PitcherLog);
                    WriteLog(writer, s.BatterLog);
                    writer.Write(s.Committed);
                    writer.Write((ushort)Math.Max(0, s.DoneStep));
                    writer.Write((byte)s.Outcome.Kind);
                    writer.Write((byte)s.Outcome.Hit);
                    writer.Write(s.Outcome.Bunt);
                    writer.Write((byte)s.Plate);
                    writer.Write((byte)Math.Clamp(s.Runs, 0, byte.MaxValue));
                }
            });
        }

        /// <summary>版が違う・壊れているときは null</summary>
        public static BaseballSnapshot DecodeSnapshot(byte[] data) => TryRead(data, ReadSnapshot, out var snapshot) ? snapshot : null;

        private static BaseballSnapshot ReadSnapshot(BinaryReader reader)
        {
            var s = new BaseballSnapshot
            {
                Stadium = reader.ReadByte(),
                Mode = reader.ReadByte() == (byte)MiniGameOnlineMode.Coop ? MiniGameOnlineMode.Coop : MiniGameOnlineMode.Versus,
                CpuStars = reader.ReadByte(),
            };
            var seats = reader.ReadByte();
            if (seats < 1 || seats > MaxSeats)
            {
                return null;
            }

            s.SeatTeam = new int[seats];
            s.SeatLeft = new bool[seats];
            s.SeatRbi = new int[seats];
            for (var seat = 0; seat < seats; seat++)
            {
                var team = reader.ReadSByte();
                s.SeatTeam[seat] = MiniGameTeams.IsTeam(team) ? team : -1;
                s.SeatLeft[seat] = reader.ReadBoolean();
                s.SeatRbi[seat] = reader.ReadByte();
            }

            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                var count = reader.ReadByte();
                if (count < 1 || count > BaseballNetRules.MaxLineup)
                {
                    return null;
                }

                s.Lineup[team] = new int[count];
                s.Owner[team] = new int[count];
                for (var slot = 0; slot < count; slot++)
                {
                    s.Lineup[team][slot] = reader.ReadUInt16();
                    var owner = reader.ReadSByte();
                    s.Owner[team][slot] = owner >= 0 && owner < seats ? owner : -1;
                }
            }

            s.State = ReadState(reader, s.Lineup[0].Length, s.Lineup[1].Length);
            if (s.State == null)
            {
                return null;
            }

            var stage = reader.ReadByte();
            if (stage > (byte)BaseballStage.Over)
            {
                return null;
            }

            s.Stage = (BaseballStage)stage;
            s.ThrowCount = reader.ReadUInt16();
            var pitcher = reader.ReadSByte();
            s.PitcherSeat = pitcher >= 0 && pitcher < seats ? pitcher : -1;
            s.HasThrow = reader.ReadBoolean();
            if (s.HasThrow)
            {
                s.Throw = ReadThrow(reader);
                s.PitcherLog = ReadLog(reader);
                s.BatterLog = ReadLog(reader);
                s.Committed = reader.ReadBoolean();
                s.DoneStep = Math.Min((int)reader.ReadUInt16(), BaseballRoleLog.MaxSteps);
                var kind = reader.ReadByte();
                var hit = reader.ReadByte();
                var bunt = reader.ReadBoolean();
                var plate = reader.ReadByte();
                s.Runs = reader.ReadByte();
                if (kind > (byte)BaseballPitchOutcomeKind.InPlay || hit > (byte)BaseballHitResult.Foul || plate > (byte)BaseballPlateResult.Out)
                {
                    return null;
                }

                s.Outcome = new BaseballPitchOutcome((BaseballPitchOutcomeKind)kind, (BaseballHitResult)hit, bunt);
                s.Plate = (BaseballPlateResult)plate;
            }

            return s;
        }

        // ---- 入力（ゲスト → ホスト）----

        public static byte[] EncodeInput(BaseballNetInput input)
        {
            return Encode(writer =>
            {
                writer.Write((ushort)input.ThrowId);
                writer.Write((sbyte)input.PitcherSlot);
                writer.Write(input.SpecialOn);
                writer.Write(input.Thrown);
                writer.Write((byte)input.Type);
                writer.Write(input.MagicOn);
                writer.Write(input.FreezeSeconds);
                writer.Write(input.WobblePhase);
                WriteLog(writer, input.Log);
            });
        }

        public static bool TryDecodeInput(byte[] data, out BaseballNetInput input) => TryRead(data, ReadInput, out input);

        private static BaseballNetInput ReadInput(BinaryReader reader)
        {
            var input = new BaseballNetInput
            {
                ThrowId = reader.ReadUInt16(),
                PitcherSlot = reader.ReadSByte(),
                SpecialOn = reader.ReadBoolean(),
                Thrown = reader.ReadBoolean(),
            };
            var type = reader.ReadByte();
            input.Type = type <= (byte)BaseballPitchType.Changeup ? (BaseballPitchType)type : BaseballPitchType.Straight;
            input.MagicOn = reader.ReadBoolean();
            input.FreezeSeconds = Finite(reader.ReadSingle());
            input.WobblePhase = Finite(reader.ReadSingle());
            input.Log = ReadLog(reader);
            return input;
        }

        // ---- 編成（ゲスト → ホスト。シーンに入ったときの「準備できた」）----

        public static byte[] EncodeParty(IReadOnlyList<int> species)
        {
            return Encode(writer =>
            {
                var count = Math.Min(species?.Count ?? 0, MaxPartySize);
                writer.Write((byte)count);
                for (var i = 0; i < count; i++)
                {
                    writer.Write((ushort)species[i]);
                }
            });
        }

        public static bool TryDecodeParty(byte[] data, out int[] species)
        {
            return TryRead(data, reader =>
            {
                var count = reader.ReadByte();
                if (count > MaxPartySize)
                {
                    return null;
                }

                var list = new int[count];
                for (var i = 0; i < count; i++)
                {
                    list[i] = reader.ReadUInt16();
                }

                return list;
            }, out species);
        }

        // ---- 試合の様子 ----

        private const byte TopFlag = 1;
        private const byte HalfOverFlag = 2;
        private const byte OverFlag = 4;
        private const byte MercyFlag = 8;

        private static void WriteState(BinaryWriter writer, BaseballGameState state)
        {
            writer.Write((byte)state.Inning);
            var flags = (state.IsTop ? TopFlag : 0) | (state.IsHalfOver ? HalfOverFlag : 0) | (state.IsOver ? OverFlag : 0) | (state.EndedByMercy ? MercyFlag : 0);
            writer.Write((byte)flags);
            writer.Write((ushort)state.Pitches);
            writer.Write((byte)Math.Min(state.PitchesThisPlate, byte.MaxValue));
            writer.Write((byte)state.Balls);
            writer.Write((byte)state.Strikes);
            writer.Write((byte)state.Outs);
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                writer.Write((byte)state.Batter[team]);
                writer.Write((byte)state.Pitcher[team]);
                var runs = state.InningRuns[team];
                writer.Write((byte)Math.Min(runs.Count, MaxInnings));
                for (var i = 0; i < runs.Count && i < MaxInnings; i++)
                {
                    writer.Write((byte)Math.Clamp(runs[i], 0, byte.MaxValue));
                }

                writer.Write(Bits(state.MagicUsed[team]));
                writer.Write(Bits(state.HitUsed[team]));
            }

            foreach (var runner in state.Runners)
            {
                writer.Write((sbyte)runner);
            }

            writer.Write((byte)Math.Min(state.LastMoves.Count, MaxMoves));
            for (var i = 0; i < state.LastMoves.Count && i < MaxMoves; i++)
            {
                var move = state.LastMoves[i];
                writer.Write((sbyte)move.Runner);
                writer.Write((byte)move.From);
                writer.Write((byte)move.To);
                writer.Write(move.Out);
            }
        }

        private static BaseballGameState ReadState(BinaryReader reader, int awayLineup, int homeLineup)
        {
            var lineups = new[] { awayLineup, homeLineup };
            var state = new BaseballGameState { Inning = reader.ReadByte() };
            var flags = reader.ReadByte();
            state.IsTop = (flags & TopFlag) != 0;
            state.IsHalfOver = (flags & HalfOverFlag) != 0;
            state.IsOver = (flags & OverFlag) != 0;
            state.EndedByMercy = (flags & MercyFlag) != 0;
            state.Pitches = reader.ReadUInt16();
            state.PitchesThisPlate = reader.ReadByte();
            state.Balls = reader.ReadByte();
            state.Strikes = reader.ReadByte();
            state.Outs = reader.ReadByte();
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                state.Batter[team] = reader.ReadByte();
                state.Pitcher[team] = reader.ReadByte();
                var innings = reader.ReadByte();
                if (innings > MaxInnings)
                {
                    return null;
                }

                for (var i = 0; i < innings; i++)
                {
                    state.InningRuns[team].Add(reader.ReadByte());
                }

                state.MagicUsed[team] = Flags(reader.ReadByte(), lineups[team]);
                state.HitUsed[team] = Flags(reader.ReadByte(), lineups[team]);
            }

            for (var b = 0; b < BaseballRules.BaseCount; b++)
            {
                var runner = reader.ReadSByte();
                state.Runners[b] = runner >= 0 && runner < BaseballNetRules.MaxLineup ? runner : BaseballBases.Empty;
            }

            var moves = reader.ReadByte();
            if (moves > MaxMoves)
            {
                return null;
            }

            for (var i = 0; i < moves; i++)
            {
                var runner = reader.ReadSByte();
                var from = reader.ReadByte();
                var to = reader.ReadByte();
                var isOut = reader.ReadBoolean();
                state.LastMoves.Add(new BaseballRunnerMove(runner, Math.Min((int)from, BaseballRunnerMove.ScoredBase), Math.Min((int)to, BaseballRunnerMove.ScoredBase), isOut));
            }

            return state;
        }

        // ---- 投げ方と記録 ----

        private static void WriteThrow(BinaryWriter writer, BaseballThrow pitch)
        {
            writer.Write((ushort)pitch.Id);
            writer.Write((byte)pitch.Type);
            WriteSpecial(writer, pitch.Magic.Special);
            writer.Write(pitch.Magic.FreezeSeconds);
            writer.Write(pitch.Magic.WobblePhase);
            WriteSpecial(writer, pitch.HitSpecial);
            writer.Write(pitch.Plan.HasValue);
            if (pitch.Plan.HasValue)
            {
                var plan = pitch.Plan.Value;
                writer.Write((byte)plan.Type);
                writer.Write((byte)plan.Bend);
                writer.Write(plan.TargetX);
            }
        }

        private static BaseballThrow ReadThrow(BinaryReader reader)
        {
            var id = reader.ReadUInt16();
            var type = PitchType(reader.ReadByte());
            var magicSpecial = ReadSpecial(reader);
            var freeze = Finite(reader.ReadSingle());
            var phase = Finite(reader.ReadSingle());
            var magic = magicSpecial.IsActive ? new BaseballPitchMagic(magicSpecial, freeze, phase) : BaseballPitchMagic.None;
            var hit = ReadSpecial(reader);
            BaseballPitchPlan? plan = null;
            if (reader.ReadBoolean())
            {
                var planType = PitchType(reader.ReadByte());
                var bend = reader.ReadByte();
                var target = Finite(reader.ReadSingle());
                plan = new BaseballPitchPlan(planType, bend <= (byte)BaseballBendPlan.Late ? (BaseballBendPlan)bend : BaseballBendPlan.Straight, target);
            }

            return new BaseballThrow(id, type, magic, hit, plan);
        }

        private static void WriteSpecial(BinaryWriter writer, BaseballSpecial special)
        {
            writer.Write(special.IsActive);
            writer.Write((byte)special.Element);
            writer.Write((byte)special.Stars);
        }

        private static BaseballSpecial ReadSpecial(BinaryReader reader)
        {
            var active = reader.ReadBoolean();
            var element = reader.ReadByte();
            var stars = reader.ReadByte();
            return active && Enum.IsDefined(typeof(Element), element) ? new BaseballSpecial((Element)element, stars) : BaseballSpecial.None;
        }

        /// <summary>記録: 歩の数、曲げる入力とバントの構えはどちらも同じ値の続く長さで詰める（ほとんど同じ値が続くため）、振った歩と時刻</summary>
        private static void WriteLog(BinaryWriter writer, BaseballRoleLog log)
        {
            writer.Write((ushort)log.Progress);
            WriteRuns(writer, log.Bends, b => (byte)b);
            WriteRuns(writer, log.Bunts, b => b ? (byte)1 : (byte)0);
            writer.Write((short)log.SwingStep);
            writer.Write(log.SwingTime);
        }

        private static BaseballRoleLog ReadLog(BinaryReader reader)
        {
            var progress = Math.Min((int)reader.ReadUInt16(), BaseballRoleLog.MaxSteps);
            var bends = ReadRuns(reader, progress, v => (sbyte)v);
            var bunts = ReadRuns(reader, progress, v => v != 0);
            var swingStep = reader.ReadInt16();
            var swingTime = reader.ReadSByte();
            return BaseballRoleLog.FromParts(bends, bunts, swingStep, swingTime);
        }

        private static void WriteRuns<T>(BinaryWriter writer, IReadOnlyList<T> values, Func<T, byte> toByte) where T : IEquatable<T>
        {
            var runs = new List<(byte Length, byte Value)>();
            var i = 0;
            while (i < values.Count)
            {
                var length = 1;
                while (i + length < values.Count && length < MaxRun && values[i + length].Equals(values[i]))
                {
                    length++;
                }

                runs.Add(((byte)length, toByte(values[i])));
                i += length;
            }

            writer.Write((ushort)runs.Count);
            foreach (var (length, value) in runs)
            {
                writer.Write(length);
                writer.Write(value);
            }
        }

        private static List<T> ReadRuns<T>(BinaryReader reader, int total, Func<byte, T> fromByte)
        {
            var list = new List<T>(total);
            var runs = reader.ReadUInt16();
            for (var r = 0; r < runs; r++)
            {
                var length = reader.ReadByte();
                var value = fromByte(reader.ReadByte());
                for (var i = 0; i < length && list.Count < total; i++)
                {
                    list.Add(value);
                }
            }

            return list;
        }

        // ---- 共通 ----

        private static BaseballPitchType PitchType(byte value) => value <= (byte)BaseballPitchType.Changeup ? (BaseballPitchType)value : BaseballPitchType.Straight;

        private static byte Bits(bool[] flags)
        {
            var bits = 0;
            for (var i = 0; i < flags.Length && i < BaseballNetRules.MaxLineup; i++)
            {
                if (flags[i])
                {
                    bits |= 1 << i;
                }
            }

            return (byte)bits;
        }

        private static bool[] Flags(byte bits, int count)
        {
            var flags = new bool[count];
            for (var i = 0; i < count; i++)
            {
                flags[i] = (bits & (1 << i)) != 0;
            }

            return flags;
        }

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;

        private static byte[] Encode(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(FormatVersion);
            write(writer);
            return stream.ToArray();
        }

        private static bool TryRead<T>(byte[] data, Func<BinaryReader, T> read, out T value)
        {
            value = default;
            if (data == null || data.Length == 0)
            {
                return false;
            }

            try
            {
                using var reader = new BinaryReader(new MemoryStream(data));
                if (reader.ReadByte() != FormatVersion)
                {
                    return false;
                }

                value = read(reader);
                return value != null;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }
    }
}
