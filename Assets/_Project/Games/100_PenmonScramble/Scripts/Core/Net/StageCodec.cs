using System.IO;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 通信で送る中身（スナップショット・入力）とバイト列の変換。
    /// 通信のライブラリに依存させず、ここだけでテストできるようにする
    /// </summary>
    public static class StageCodec
    {
        /// <summary>
        /// 中身の並びを変えたら上げる。違う版どうしで部屋に入っても、読み違えて変な画面になるより無視する方がよい
        /// </summary>
        public const byte FormatVersion = 1;

        private const byte Skill1Flag = 1;
        private const byte Skill2Flag = 2;

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

            writer.Write(snapshot.Units.Count);
            foreach (var unit in snapshot.Units)
            {
                writer.Write(unit.Id);
                writer.Write((byte)unit.Team);
                writer.Write((sbyte)unit.Seat);
                Write(writer, unit.Position);
                Write(writer, unit.Facing);
                writer.Write(unit.Hp);
                writer.Write(unit.MaxHp);
                writer.Write(unit.Radius);
            }

            writer.Write(snapshot.Projectiles.Count);
            foreach (var projectile in snapshot.Projectiles)
            {
                writer.Write(projectile.Id);
                Write(writer, projectile.Position);
                writer.Write(projectile.Radius);
            }

            WriteEvents(writer, snapshot);
            return stream.ToArray();
        }

        private static void WriteEvents(BinaryWriter writer, StageSnapshot snapshot)
        {
            writer.Write(snapshot.Hits.Count);
            foreach (var hit in snapshot.Hits)
            {
                writer.Write(hit.AttackerId);
                writer.Write(hit.TargetId);
                writer.Write(hit.Damage);
                Write(writer, hit.Position);
            }

            writer.Write(snapshot.Defeats.Count);
            foreach (var defeat in snapshot.Defeats)
            {
                writer.Write(defeat.UnitId);
                writer.Write((byte)defeat.Team);
                Write(writer, defeat.Position);
            }

            writer.Write(snapshot.SkillsUsed.Count);
            foreach (var used in snapshot.SkillsUsed)
            {
                writer.Write(used.UnitId);
                writer.Write((byte)used.Slot);
                writer.Write((byte)used.Shape);
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
                ReadProjectiles(reader, snapshot);
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
                var position = ReadVec2(reader);
                var facing = ReadVec2(reader);
                var hp = reader.ReadInt32();
                var maxHp = reader.ReadInt32();
                var radius = reader.ReadSingle();
                snapshot.Units.Add(new UnitState(id, team, seat, position, facing, hp, maxHp, radius));
            }
        }

        private static void ReadProjectiles(BinaryReader reader, StageSnapshot snapshot)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var id = reader.ReadInt32();
                var position = ReadVec2(reader);
                var radius = reader.ReadSingle();
                snapshot.Projectiles.Add(new ProjectileState(id, position, radius));
            }
        }

        private static void ReadEvents(BinaryReader reader, StageSnapshot snapshot)
        {
            var hits = reader.ReadInt32();
            for (var i = 0; i < hits; i++)
            {
                var attackerId = reader.ReadInt32();
                var targetId = reader.ReadInt32();
                var damage = reader.ReadInt32();
                snapshot.Hits.Add(new HitEvent(attackerId, targetId, damage, ReadVec2(reader)));
            }

            var defeats = reader.ReadInt32();
            for (var i = 0; i < defeats; i++)
            {
                var unitId = reader.ReadInt32();
                var team = (Team)reader.ReadByte();
                snapshot.Defeats.Add(new DefeatedEvent(unitId, team, ReadVec2(reader)));
            }

            var skills = reader.ReadInt32();
            for (var i = 0; i < skills; i++)
            {
                var unitId = reader.ReadInt32();
                var slot = (int)reader.ReadByte();
                snapshot.SkillsUsed.Add(new SkillUsedEvent(unitId, slot, (SkillShape)reader.ReadByte()));
            }
        }

        public static byte[] EncodeInput(PlayerInput input)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(FormatVersion);
            Write(writer, input.Move);
            var flags = (byte)((input.Skill1 ? Skill1Flag : 0) | (input.Skill2 ? Skill2Flag : 0));
            writer.Write(flags);
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
                input = new PlayerInput(move, (flags & Skill1Flag) != 0, (flags & Skill2Flag) != 0);
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
