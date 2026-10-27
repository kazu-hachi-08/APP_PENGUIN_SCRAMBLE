using System.IO;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 通信で送る中身（スナップショット・入力）とバイト列の変換。
    /// 通信のライブラリに依存させず、ここだけでテストできるようにする
    /// </summary>
    public static partial class StageCodec
    {
        /// <summary>
        /// 中身の並びを変えたら上げる。違う版どうしで部屋に入っても、読み違えて変な画面になるより無視する方がよい
        /// </summary>
        public const byte FormatVersion = 2;

        private const byte Skill1Flag = 1;
        private const byte Skill2Flag = 2;
        private const byte SpecialFlag = 4;
        private const byte SwitchFlag = 8;

        public static byte[] EncodeSnapshot(StageSnapshot snapshot)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(FormatVersion);
            writer.Write(snapshot.Round);
            writer.Write(snapshot.Tick);
            writer.Write((byte)snapshot.Result);
            Write(writer, snapshot.AreaMin);
            Write(writer, snapshot.AreaMax);
            WriteUnits(writer, snapshot);
            WriteObjects(writer, snapshot);
            WritePlayers(writer, snapshot);
            WriteEvents(writer, snapshot);
            return stream.ToArray();
        }

        private static void WriteUnits(BinaryWriter writer, StageSnapshot snapshot)
        {
            writer.Write(snapshot.Units.Count);
            foreach (var unit in snapshot.Units)
            {
                writer.Write(unit.Id);
                writer.Write((byte)unit.Team);
                writer.Write((sbyte)unit.Seat);
                writer.Write((byte)unit.SpeciesNo);
                Write(writer, unit.Position);
                Write(writer, unit.Facing);
                writer.Write(unit.Hp);
                writer.Write(unit.MaxHp);
                writer.Write(unit.Radius);
                writer.Write((byte)unit.Flags);
            }
        }

        private static void WriteObjects(BinaryWriter writer, StageSnapshot snapshot)
        {
            writer.Write(snapshot.Projectiles.Count);
            foreach (var projectile in snapshot.Projectiles)
            {
                writer.Write(projectile.Id);
                writer.Write((byte)projectile.Team);
                writer.Write((byte)projectile.Element);
                Write(writer, projectile.Position);
                writer.Write(projectile.Radius);
            }

            writer.Write(snapshot.Effects.Count);
            foreach (var effect in snapshot.Effects)
            {
                writer.Write(effect.Id);
                writer.Write((byte)effect.Kind);
                writer.Write((byte)effect.Element);
                Write(writer, effect.Position);
                writer.Write(effect.Radius);
            }
        }

        private static void WritePlayers(BinaryWriter writer, StageSnapshot snapshot)
        {
            writer.Write(snapshot.Players.Count);
            foreach (var player in snapshot.Players)
            {
                writer.Write((sbyte)player.Seat);
                writer.Write((byte)player.ActiveIndex);
                writer.Write(player.SpecialGauge);
                writer.Write(player.SwitchCooldown);
                writer.Write((byte)player.Members.Count);
                foreach (var member in player.Members)
                {
                    writer.Write((byte)member.SpeciesNo);
                    writer.Write(member.Hp);
                    writer.Write(member.MaxHp);
                }
            }
        }

        /// <summary>版が違う・壊れているときは null（その回は描かずに次を待つ）</summary>
        public static StageSnapshot DecodeSnapshot(byte[] data)
        {
            try
            {
                using var reader = new BinaryReader(new MemoryStream(data));
                if (reader.ReadByte() != FormatVersion)
                {
                    return null;
                }

                var snapshot = new StageSnapshot
                {
                    Round = reader.ReadInt32(),
                    Tick = reader.ReadInt32(),
                    Result = (StageResult)reader.ReadByte(),
                    AreaMin = ReadVec2(reader),
                    AreaMax = ReadVec2(reader),
                };
                ReadUnits(reader, snapshot);
                ReadObjects(reader, snapshot);
                ReadPlayers(reader, snapshot);
                ReadEvents(reader, snapshot);
                return snapshot;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        private static void ReadUnits(BinaryReader reader, StageSnapshot snapshot)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadInt32();
                var team = (Team)reader.ReadByte();
                var seat = (int)reader.ReadSByte();
                var species = (int)reader.ReadByte();
                var position = ReadVec2(reader);
                var facing = ReadVec2(reader);
                var hp = reader.ReadInt32();
                var maxHp = reader.ReadInt32();
                var radius = reader.ReadSingle();
                var flags = (UnitFlags)reader.ReadByte();
                snapshot.Units.Add(new UnitState(id, team, seat, species, position, facing, hp, maxHp, radius, flags));
            }
        }

        private static void ReadObjects(BinaryReader reader, StageSnapshot snapshot)
        {
            var projectiles = reader.ReadInt32();
            for (var i = 0; i < projectiles; i++)
            {
                var id = reader.ReadInt32();
                var team = (Team)reader.ReadByte();
                var element = (Element)reader.ReadByte();
                var position = ReadVec2(reader);
                snapshot.Projectiles.Add(new ProjectileState(id, team, element, position, reader.ReadSingle()));
            }

            var effects = reader.ReadInt32();
            for (var i = 0; i < effects; i++)
            {
                var id = reader.ReadInt32();
                var kind = (AreaEffectKind)reader.ReadByte();
                var element = (Element)reader.ReadByte();
                var position = ReadVec2(reader);
                snapshot.Effects.Add(new EffectState(id, kind, element, position, reader.ReadSingle()));
            }
        }

        private static void ReadPlayers(BinaryReader reader, StageSnapshot snapshot)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var player = new PlayerState
                {
                    Seat = reader.ReadSByte(),
                    ActiveIndex = reader.ReadByte(),
                    SpecialGauge = reader.ReadSingle(),
                    SwitchCooldown = reader.ReadSingle(),
                };
                var members = reader.ReadByte();
                for (var m = 0; m < members; m++)
                {
                    var species = (int)reader.ReadByte();
                    var hp = reader.ReadInt32();
                    player.Members.Add(new MemberState(species, hp, reader.ReadInt32()));
                }

                snapshot.Players.Add(player);
            }
        }

        public static byte[] EncodeInput(PlayerInput input)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(FormatVersion);
            Write(writer, input.Move);
            var flags = (input.Skill1 ? Skill1Flag : 0) | (input.Skill2 ? Skill2Flag : 0)
                | (input.Special ? SpecialFlag : 0) | (input.Switch ? SwitchFlag : 0);
            writer.Write((byte)flags);
            return stream.ToArray();
        }

        /// <summary>版が違う・壊れているときは false</summary>
        public static bool TryDecodeInput(byte[] data, out PlayerInput input)
        {
            input = default;
            try
            {
                using var reader = new BinaryReader(new MemoryStream(data));
                if (reader.ReadByte() != FormatVersion)
                {
                    return false;
                }

                var move = ReadVec2(reader);
                var flags = reader.ReadByte();
                input = new PlayerInput(move, (flags & Skill1Flag) != 0, (flags & Skill2Flag) != 0,
                    (flags & SpecialFlag) != 0, (flags & SwitchFlag) != 0);
                return true;
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

        private static Vec2 ReadVec2(BinaryReader reader)
        {
            var x = reader.ReadSingle();
            return new Vec2(x, reader.ReadSingle());
        }
    }
}
