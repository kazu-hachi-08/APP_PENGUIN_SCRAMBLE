using System;
using System.Collections.Generic;
using System.IO;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// ゴルフのオンラインで送る中身（スナップショット・入力・編成）とバイト列の変換。
    /// 通信のライブラリに依存させず、ここだけでテストできるようにする（本編の StageCodec と同じ考え）
    /// </summary>
    public static class GolfCodec
    {
        /// <summary>中身の並びを変えたら上げる。違う版どうしで部屋に入っても、読み違えるより無視する方がよい</summary>
        public const byte FormatVersion = 1;

        // 壊れた値が届いても、配列を作りすぎないようにする
        private const int MaxHoles = 16;
        private const int MaxEvents = 512;

        private const byte GaugeFlag = 1;
        private const byte StrikingFlag = 2;
        private const byte SpecialFlag = 4;
        private const byte LeftCurveFlag = 8;

        private const byte InCupFlag = 1;
        private const byte ActiveFlag = 2;
        private const byte BoostFlag = 4;
        private const byte DoneFlag = 8;
        private const byte RemovedFlag = 16;

        // ---- スナップショット ----

        public static byte[] EncodeSnapshot(GolfSnapshot snapshot)
        {
            return Encode(writer =>
            {
                var players = snapshot.PlayerCount;
                var holes = snapshot.Recorded.Length;
                writer.Write((byte)snapshot.CourseNo);
                writer.Write((byte)snapshot.HoleIndex);
                writer.Write((byte)holes);
                writer.Write((byte)players);
                writer.Write((byte)snapshot.Phase);
                writer.Write((byte)snapshot.CurrentPlayer);
                for (var i = 0; i < players; i++)
                {
                    writer.Write((byte)snapshot.TurnOrder[i]);
                }

                writer.Write(snapshot.ShotTimeLeft);
                WriteAim(writer, snapshot.Aim);
                for (var player = 0; player < players; player++)
                {
                    WritePlayer(writer, snapshot, player);
                }

                for (var hole = 0; hole < holes; hole++)
                {
                    writer.Write(snapshot.Recorded[hole]);
                    for (var player = 0; player < players; player++)
                    {
                        writer.Write((short)snapshot.Card[hole, player]);
                    }
                }

                writer.Write((ushort)snapshot.Events.Count);
                foreach (var e in snapshot.Events)
                {
                    WriteEvent(writer, e);
                }
            });
        }

        /// <summary>版が違う・壊れているときは null</summary>
        public static GolfSnapshot DecodeSnapshot(byte[] data)
        {
            return TryRead(data, ReadSnapshot, out var snapshot) ? snapshot : null;
        }

        private static GolfSnapshot ReadSnapshot(BinaryReader reader)
        {
            var snapshot = new GolfSnapshot
            {
                CourseNo = reader.ReadByte(),
                HoleIndex = reader.ReadByte(),
            };
            var holes = reader.ReadByte();
            var players = reader.ReadByte();
            if (holes < 1 || holes > MaxHoles || players < 1 || players > GolfRules.MaxPlayers || snapshot.HoleIndex >= holes)
            {
                return null;
            }

            snapshot.Phase = (GolfHolePhase)Math.Min(reader.ReadByte(), (byte)GolfHolePhase.Finished);
            snapshot.CurrentPlayer = Math.Min(reader.ReadByte(), players - 1);
            snapshot.TurnOrder = new int[players];
            for (var i = 0; i < players; i++)
            {
                snapshot.TurnOrder[i] = reader.ReadByte();
            }

            if (!IsPermutation(snapshot.TurnOrder))
            {
                return null;
            }

            snapshot.ShotTimeLeft = reader.ReadSingle();
            snapshot.Aim = ReadAim(reader);
            snapshot.Balls = new GolfBallState[players];
            snapshot.Strokes = new int[players];
            snapshot.Done = new bool[players];
            snapshot.Removed = new bool[players];
            snapshot.Teams = new GolfTeamState[players];
            for (var player = 0; player < players; player++)
            {
                ReadPlayer(reader, snapshot, player);
            }

            snapshot.Card = new int[holes, players];
            snapshot.Recorded = new bool[holes];
            for (var hole = 0; hole < holes; hole++)
            {
                snapshot.Recorded[hole] = reader.ReadBoolean();
                for (var player = 0; player < players; player++)
                {
                    snapshot.Card[hole, player] = reader.ReadInt16();
                }
            }

            var events = Math.Min((int)reader.ReadUInt16(), MaxEvents);
            for (var i = 0; i < events; i++)
            {
                snapshot.Events.Add(ReadEvent(reader));
            }

            return snapshot;
        }

        private static void WritePlayer(BinaryWriter writer, GolfSnapshot snapshot, int player)
        {
            var ball = snapshot.Balls[player];
            Write(writer, ball.Position);
            Write(writer, ball.Velocity);
            writer.Write(ball.Height);
            writer.Write(ball.IceLeft);
            writer.Write((byte)ball.Special);
            writer.Write((byte)ball.BoostStars);
            var flags = (byte)((ball.InCup ? InCupFlag : 0) | (ball.Active ? ActiveFlag : 0) | (ball.BoostReady ? BoostFlag : 0)
                | (snapshot.Done[player] ? DoneFlag : 0) | (snapshot.Removed[player] ? RemovedFlag : 0));
            writer.Write(flags);
            writer.Write((short)snapshot.Strokes[player]);

            var team = snapshot.Teams[player];
            var count = Math.Min(team.Species.Length, GolfRules.TeamSize);
            writer.Write((byte)count);
            var used = 0;
            for (var i = 0; i < count; i++)
            {
                writer.Write((byte)team.Species[i]);
                used |= team.SpecialUsed[i] ? 1 << i : 0;
            }

            writer.Write((byte)team.Selected);
            writer.Write((byte)used);
        }

        private static void ReadPlayer(BinaryReader reader, GolfSnapshot snapshot, int player)
        {
            var ball = new GolfBallState
            {
                Position = ReadVec2(reader),
                Velocity = ReadVec2(reader),
                Height = reader.ReadSingle(),
                IceLeft = reader.ReadSingle(),
                Special = (GolfSpecialKind)Math.Min(reader.ReadByte(), (byte)GolfSpecialKind.ThunderBoost),
                BoostStars = reader.ReadByte(),
            };
            var flags = reader.ReadByte();
            ball.InCup = (flags & InCupFlag) != 0;
            ball.Active = (flags & ActiveFlag) != 0;
            ball.BoostReady = (flags & BoostFlag) != 0;
            snapshot.Balls[player] = ball;
            snapshot.Done[player] = (flags & DoneFlag) != 0;
            snapshot.Removed[player] = (flags & RemovedFlag) != 0;
            snapshot.Strokes[player] = reader.ReadInt16();

            var count = Math.Min((int)reader.ReadByte(), GolfRules.TeamSize);
            var team = new GolfTeamState { Species = new int[count], SpecialUsed = new bool[count] };
            for (var i = 0; i < count; i++)
            {
                team.Species[i] = reader.ReadByte();
            }

            team.Selected = reader.ReadByte();
            var used = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                team.SpecialUsed[i] = (used & (1 << i)) != 0;
            }

            snapshot.Teams[player] = team;
        }

        private static void WriteEvent(BinaryWriter writer, GolfNetEvent e)
        {
            writer.Write((byte)e.Hole);
            writer.Write((byte)e.Event.Kind);
            writer.Write((sbyte)e.Event.Player);
            writer.Write((sbyte)e.Event.Other);
            Write(writer, e.Event.Position);
            writer.Write(e.Event.Strength);
        }

        private static GolfNetEvent ReadEvent(BinaryReader reader)
        {
            int hole = reader.ReadByte();
            var kind = (GolfEventKind)reader.ReadByte();
            int player = reader.ReadSByte();
            int other = reader.ReadSByte();
            var position = ReadVec2(reader);
            var strength = reader.ReadSingle();
            return new GolfNetEvent(hole, new GolfEvent(kind, player, position, strength, other));
        }

        // ---- 入力 ----

        public static byte[] EncodeInput(GolfInput input)
        {
            return Encode(writer =>
            {
                writer.Write((byte)input.Kind);
                WriteAim(writer, input.Aim);
                writer.Write(input.BoostTurn);
            });
        }

        /// <summary>版が違う・壊れている・知らない種類のときは false</summary>
        public static bool TryDecodeInput(byte[] data, out GolfInput input)
        {
            input = default;
            if (!TryRead(data, ReadInput, out var read) || !read.HasValue)
            {
                return false;
            }

            input = read.Value;
            return true;
        }

        private static GolfInput? ReadInput(BinaryReader reader)
        {
            var kind = (GolfInputKind)reader.ReadByte();
            var aim = ReadAim(reader);
            var turn = reader.ReadSingle();
            switch (kind)
            {
                case GolfInputKind.Aim: return GolfInput.ForAim(aim);
                case GolfInputKind.Strike: return GolfInput.ForStrike(aim);
                case GolfInputKind.Boost: return GolfInput.ForBoost(turn);
                default: return null;
            }
        }

        // ---- 編成（ゲストがシーンに入ったら、打つ子の3体をホストへ送る） ----

        public static byte[] EncodeParty(IReadOnlyList<int> species)
        {
            return Encode(writer =>
            {
                var count = Math.Min(species.Count, GolfRules.TeamSize);
                writer.Write((byte)count);
                for (var i = 0; i < count; i++)
                {
                    writer.Write((byte)species[i]);
                }
            });
        }

        /// <summary>版が違う・壊れているときは false。知らない種類は GolfTeam が外す（空なら仮の3体になる）</summary>
        public static bool TryDecodeParty(byte[] data, out int[] species)
        {
            return TryRead(data, reader =>
            {
                var count = Math.Min((int)reader.ReadByte(), GolfRules.TeamSize);
                var result = new int[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = reader.ReadByte();
                }

                return result;
            }, out species);
        }

        // ---- 道具 ----

        private static void WriteAim(BinaryWriter writer, GolfAim aim)
        {
            writer.Write((byte)Math.Clamp(aim.Member, 0, GolfRules.TeamSize - 1));
            writer.Write(aim.Angle);
            writer.Write(aim.Power);
            var flags = (byte)((aim.GaugeRunning ? GaugeFlag : 0) | (aim.Striking ? StrikingFlag : 0) | (aim.Special ? SpecialFlag : 0)
                | (aim.Curve < 0 ? LeftCurveFlag : 0));
            writer.Write(flags);
        }

        private static GolfAim ReadAim(BinaryReader reader)
        {
            int member = Math.Min(reader.ReadByte(), (byte)(GolfRules.TeamSize - 1));
            var angle = reader.ReadSingle();
            var power = Math.Clamp(reader.ReadSingle(), 0f, 1f);
            var flags = reader.ReadByte();
            return new GolfAim(member, Finite(angle), Finite(power), (flags & GaugeFlag) != 0, (flags & StrikingFlag) != 0,
                (flags & SpecialFlag) != 0, (flags & LeftCurveFlag) != 0 ? -1 : 1);
        }

        /// <summary>壊れた値（NaN・無限）で向きや球が消えないようにする</summary>
        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;

        private static bool IsPermutation(int[] order)
        {
            var seen = new bool[order.Length];
            foreach (var seat in order)
            {
                if (seat < 0 || seat >= order.Length || seen[seat])
                {
                    return false;
                }

                seen[seat] = true;
            }

            return true;
        }

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

        private static void Write(BinaryWriter writer, Vec2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        private static Vec2 ReadVec2(BinaryReader reader) => new Vec2(Finite(reader.ReadSingle()), Finite(reader.ReadSingle()));
    }
}
