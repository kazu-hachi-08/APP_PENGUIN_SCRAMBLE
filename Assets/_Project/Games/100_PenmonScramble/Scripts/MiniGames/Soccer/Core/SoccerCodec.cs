using PenmonScramble.MiniGames.Core;
using System;
using System.Collections.Generic;
using System.IO;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// サッカーのオンラインで送る中身（スナップショット・入力・編成）とバイト列の変換。
    /// 通信のライブラリに依存させず、ここだけでテストできるようにする（ゴルフの GolfCodec・本編の StageCodec と同じ考え）
    /// </summary>
    public static class SoccerCodec
    {
        /// <summary>中身の並びを変えたら上げる。違う版どうしで部屋に入っても、読み違えるより無視する方がよい</summary>
        public const byte FormatVersion = 2;

        // 壊れた値が届いても、配列を作りすぎないようにする
        private const int MaxSeats = 4;
        private const int MaxPerTeam = 6;
        private const int MaxKeepers = SoccerRules.TeamCount;
        private const int MaxEvents = 512;
        private const int MaxPartySize = 8;
        private const int MaxPenalties = SoccerRules.PenaltyMaxRounds + 1;

        private const byte ShootFlag = 1;
        private const byte PassFlag = 2;
        private const byte SpecialFlag = 4;

        // ---- スナップショット ----

        public static byte[] EncodeSnapshot(SoccerSnapshot s)
        {
            return Encode(writer =>
            {
                writer.Write((byte)s.Stadium);
                writer.Write((byte)s.Mode);
                writer.Write((byte)s.PerTeam);
                writer.Write((byte)s.SeatCount);
                for (var seat = 0; seat < s.SeatCount; seat++)
                {
                    writer.Write((sbyte)s.SeatTeam[seat]);
                    writer.Write((sbyte)s.SeatControlled[seat]);
                    writer.Write((byte)Math.Min(s.SeatGoals[seat], byte.MaxValue));
                }

                foreach (var species in s.Species)
                {
                    writer.Write((ushort)species);
                }

                WriteMatch(writer, s);
                WriteBall(writer, s.Ball);
                WriteSpecial(writer, s.Special);
                foreach (var player in s.Players)
                {
                    WritePlayer(writer, player);
                }

                writer.Write((byte)s.Keepers.Length);
                foreach (var keeper in s.Keepers)
                {
                    WriteKeeper(writer, keeper);
                }

                WriteShootout(writer, s.Shootout);
                writer.Write((ushort)Math.Min(s.Events.Count, MaxEvents));
                for (var i = 0; i < s.Events.Count && i < MaxEvents; i++)
                {
                    WriteEvent(writer, s.Events[i]);
                }
            });
        }

        /// <summary>版が違う・壊れているときは null</summary>
        public static SoccerSnapshot DecodeSnapshot(byte[] data)
        {
            return TryRead(data, ReadSnapshot, out var snapshot) ? snapshot : null;
        }

        private static SoccerSnapshot ReadSnapshot(BinaryReader reader)
        {
            var s = new SoccerSnapshot
            {
                Stadium = reader.ReadByte(),
                Mode = reader.ReadByte() == (byte)MiniGameOnlineMode.Coop ? MiniGameOnlineMode.Coop : MiniGameOnlineMode.Versus,
                PerTeam = reader.ReadByte(),
            };
            var seats = reader.ReadByte();
            if (s.PerTeam < 1 || s.PerTeam > MaxPerTeam || seats < 1 || seats > MaxSeats)
            {
                return null;
            }

            var players = s.PerTeam * SoccerRules.TeamCount;
            s.SeatTeam = new int[seats];
            s.SeatControlled = new int[seats];
            s.SeatGoals = new int[seats];
            for (var seat = 0; seat < seats; seat++)
            {
                s.SeatTeam[seat] = Team(reader.ReadSByte());
                s.SeatControlled[seat] = Player(reader.ReadSByte(), players);
                s.SeatGoals[seat] = reader.ReadByte();
            }

            s.Species = new int[players];
            for (var i = 0; i < players; i++)
            {
                s.Species[i] = reader.ReadUInt16();
            }

            ReadMatch(reader, s);
            s.Ball = ReadBall(reader, players);
            s.Special = ReadSpecial(reader, players);
            s.Players = new SoccerPlayerSnap[players];
            for (var i = 0; i < players; i++)
            {
                s.Players[i] = ReadPlayer(reader);
            }

            var keepers = reader.ReadByte();
            if (keepers > MaxKeepers)
            {
                return null;
            }

            s.Keepers = new SoccerKeeperSnap[keepers];
            for (var i = 0; i < keepers; i++)
            {
                s.Keepers[i] = ReadKeeper(reader);
            }

            s.Shootout = ReadShootout(reader, players);
            var events = Math.Min((int)reader.ReadUInt16(), MaxEvents);
            for (var i = 0; i < events; i++)
            {
                s.Events.Add(ReadEvent(reader, players));
            }

            return s;
        }

        private static void WriteMatch(BinaryWriter writer, SoccerSnapshot s)
        {
            writer.Write((byte)s.Phase);
            writer.Write(s.PhaseSeconds);
            writer.Write(s.TimeLeft);
            writer.Write((sbyte)s.Winner);
            writer.Write(s.WonByShootout);
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                writer.Write((byte)Math.Min(s.Score[team], byte.MaxValue));
                writer.Write(s.Gauge[team]);
            }

            writer.Write(s.CutInSeconds);
            writer.Write(s.GoalScored);
            writer.Write(s.WorldTime);
        }

        private static void ReadMatch(BinaryReader reader, SoccerSnapshot s)
        {
            s.Phase = (SoccerMatchPhase)Math.Min(reader.ReadByte(), (byte)SoccerMatchPhase.Finished);
            s.PhaseSeconds = Finite(reader.ReadSingle());
            s.TimeLeft = Math.Max(0f, Finite(reader.ReadSingle()));
            s.Winner = Team(reader.ReadSByte());
            s.WonByShootout = reader.ReadBoolean();
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                s.Score[team] = reader.ReadByte();
                s.Gauge[team] = Math.Clamp(Finite(reader.ReadSingle()), 0f, 1f);
            }

            s.CutInSeconds = Math.Max(0f, Finite(reader.ReadSingle()));
            s.GoalScored = reader.ReadBoolean();
            s.WorldTime = Math.Max(0f, Finite(reader.ReadSingle()));
        }

        private static void WriteBall(BinaryWriter writer, SoccerBallSnap ball)
        {
            Write(writer, ball.Position);
            Write(writer, ball.Velocity);
            writer.Write(ball.Height);
            writer.Write(ball.VerticalSpeed);
            writer.Write((sbyte)ball.Holder);
            writer.Write((sbyte)ball.KeeperTeam);
            writer.Write((sbyte)ball.LastToucher);
        }

        private static SoccerBallSnap ReadBall(BinaryReader reader, int players)
        {
            return new SoccerBallSnap
            {
                Position = ReadVec2(reader),
                Velocity = ReadVec2(reader),
                Height = Math.Max(0f, Finite(reader.ReadSingle())),
                VerticalSpeed = Finite(reader.ReadSingle()),
                Holder = Player(reader.ReadSByte(), players),
                KeeperTeam = Team(reader.ReadSByte()),
                LastToucher = Player(reader.ReadSByte(), players),
            };
        }

        private static void WriteSpecial(BinaryWriter writer, SoccerSpecialSnap? special)
        {
            writer.Write(special.HasValue);
            if (!special.HasValue)
            {
                return;
            }

            var s = special.Value;
            writer.Write((byte)s.Shooter);
            writer.Write((byte)s.Team);
            writer.Write((byte)s.Element);
            writer.Write((byte)s.Stars);
            writer.Write(s.Hidden);
        }

        private static SoccerSpecialSnap? ReadSpecial(BinaryReader reader, int players)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var shooter = reader.ReadByte();
            var team = reader.ReadByte();
            var element = reader.ReadByte();
            var stars = reader.ReadByte();
            var hidden = reader.ReadBoolean();
            if (shooter >= players || !Enum.IsDefined(typeof(Element), (Element)element))
            {
                return null;
            }

            return new SoccerSpecialSnap { Shooter = shooter, Team = Team((sbyte)team), Element = (Element)element, Stars = Math.Max(1, (int)stars), Hidden = hidden };
        }

        private static void WritePlayer(BinaryWriter writer, SoccerPlayerSnap p)
        {
            Write(writer, p.Position);
            Write(writer, p.Velocity);
            Write(writer, p.Facing);
            writer.Write((byte)p.State);
            writer.Write(p.StateSeconds);
            writer.Write(p.IsCharging);
            writer.Write(p.ChargeSeconds);
            writer.Write(p.SlideCooldown);
            writer.Write(p.DashCooldown);
            writer.Write(p.FrozenSeconds);
        }

        private static SoccerPlayerSnap ReadPlayer(BinaryReader reader)
        {
            return new SoccerPlayerSnap
            {
                Position = ReadVec2(reader),
                Velocity = ReadVec2(reader),
                Facing = ReadVec2(reader),
                State = (SoccerPlayerState)Math.Min(reader.ReadByte(), (byte)SoccerPlayerState.Frozen),
                StateSeconds = Finite(reader.ReadSingle()),
                IsCharging = reader.ReadBoolean(),
                ChargeSeconds = Math.Max(0f, Finite(reader.ReadSingle())),
                SlideCooldown = Math.Max(0f, Finite(reader.ReadSingle())),
                DashCooldown = Math.Max(0f, Finite(reader.ReadSingle())),
                FrozenSeconds = Math.Max(0f, Finite(reader.ReadSingle())),
            };
        }

        private static void WriteKeeper(BinaryWriter writer, SoccerKeeperSnap k)
        {
            Write(writer, k.Position);
            writer.Write(k.Speed);
            writer.Write((byte)k.State);
            writer.Write(k.StateSeconds);
            writer.Write(k.DiveSide);
        }

        private static SoccerKeeperSnap ReadKeeper(BinaryReader reader)
        {
            return new SoccerKeeperSnap
            {
                Position = ReadVec2(reader),
                Speed = Finite(reader.ReadSingle()),
                State = (SoccerKeeperState)Math.Min(reader.ReadByte(), (byte)SoccerKeeperState.Hold),
                StateSeconds = Finite(reader.ReadSingle()),
                DiveSide = Finite(reader.ReadSingle()),
            };
        }

        private static void WriteShootout(BinaryWriter writer, SoccerShootoutSnap s)
        {
            writer.Write(s != null);
            if (s == null)
            {
                return;
            }

            writer.Write((byte)s.Phase);
            writer.Write(s.PhaseSeconds);
            writer.Write((byte)s.KickingTeam);
            writer.Write((sbyte)s.Kicker);
            writer.Write(s.Charge01);
            writer.Write(s.IsCharging);
            writer.Write((sbyte)s.KeeperSide);
            writer.Write((byte)s.LastResult);
            writer.Write((sbyte)s.Winner);
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                var results = s.Results[team];
                var count = Math.Min(results.Count, MaxPenalties);
                writer.Write((byte)count);
                for (var i = 0; i < count; i++)
                {
                    writer.Write((byte)results[i]);
                }
            }
        }

        private static SoccerShootoutSnap ReadShootout(BinaryReader reader, int players)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var s = new SoccerShootoutSnap
            {
                Phase = (SoccerShootoutPhase)Math.Min(reader.ReadByte(), (byte)SoccerShootoutPhase.Finished),
                PhaseSeconds = Finite(reader.ReadSingle()),
                KickingTeam = Team((sbyte)reader.ReadByte()),
                Kicker = Player(reader.ReadSByte(), players),
                Charge01 = Math.Clamp(Finite(reader.ReadSingle()), 0f, 1f),
                IsCharging = reader.ReadBoolean(),
                KeeperSide = Math.Clamp((int)reader.ReadSByte(), -1, 1),
                LastResult = (SoccerPenaltyResult)Math.Min(reader.ReadByte(), (byte)SoccerPenaltyResult.Missed),
                Winner = Team(reader.ReadSByte()),
            };
            if (s.KickingTeam < 0)
            {
                s.KickingTeam = SoccerRules.Red;
            }

            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                var count = Math.Min((int)reader.ReadByte(), MaxPenalties);
                for (var i = 0; i < count; i++)
                {
                    s.Results[team].Add((SoccerPenaltyResult)Math.Min(reader.ReadByte(), (byte)SoccerPenaltyResult.Missed));
                }
            }

            return s;
        }

        private static void WriteEvent(BinaryWriter writer, SoccerEvent e)
        {
            writer.Write((byte)e.Kind);
            writer.Write((sbyte)e.Player);
            writer.Write((sbyte)e.Team);
            writer.Write((sbyte)e.Other);
            Write(writer, e.Position);
            writer.Write(e.Strength);
            writer.Write(e.Lofted);
        }

        private static SoccerEvent ReadEvent(BinaryReader reader, int players)
        {
            var kind = (SoccerEventKind)Math.Min(reader.ReadByte(), (byte)SoccerEventKind.MatchEnd);
            var player = Player(reader.ReadSByte(), players);
            var team = Team(reader.ReadSByte());
            var other = Player(reader.ReadSByte(), players);
            var position = ReadVec2(reader);
            var strength = Finite(reader.ReadSingle());
            var lofted = reader.ReadBoolean();
            return new SoccerEvent(kind, player, team, position, strength, lofted, other);
        }

        // ---- 入力（ゲスト → ホスト）----

        public static byte[] EncodeInput(SoccerNetInput input)
        {
            return Encode(writer =>
            {
                writer.Write(SoccerNetInput.Quantize(input.Move.X));
                writer.Write(SoccerNetInput.Quantize(input.Move.Y));
                var flags = (byte)((input.Shoot ? ShootFlag : 0) | (input.Pass ? PassFlag : 0) | (input.Special ? SpecialFlag : 0));
                writer.Write(flags);
                writer.Write(input.SwitchCount);
            });
        }

        public static bool TryDecodeInput(byte[] data, out SoccerNetInput input)
        {
            input = default;
            if (!TryRead(data, ReadInput, out var read) || !read.HasValue)
            {
                return false;
            }

            input = read.Value;
            return true;
        }

        private static SoccerNetInput? ReadInput(BinaryReader reader)
        {
            var x = reader.ReadSByte() / 127f;
            var y = reader.ReadSByte() / 127f;
            var flags = reader.ReadByte();
            var switches = reader.ReadByte();
            return new SoccerNetInput(new Vec2(x, y), (flags & ShootFlag) != 0, (flags & PassFlag) != 0, (flags & SpecialFlag) != 0, switches);
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

        // ---- 共通 ----

        private static int Team(sbyte value) => value == SoccerRules.Red || value == SoccerRules.Blue ? value : -1;

        private static int Player(sbyte value, int players) => value >= 0 && value < players ? value : SoccerBall.NoPlayer;

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

        private static void Write(BinaryWriter writer, Vec2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        private static Vec2 ReadVec2(BinaryReader reader) => new Vec2(Finite(reader.ReadSingle()), Finite(reader.ReadSingle()));
    }
}
