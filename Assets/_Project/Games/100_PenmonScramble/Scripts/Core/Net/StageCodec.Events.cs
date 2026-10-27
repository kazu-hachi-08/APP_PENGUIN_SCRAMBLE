using System.IO;

namespace PenmonScramble.Core
{
    /// <summary>スナップショットのうち、前回からの間に起きたこと（演出に使う）</summary>
    public static partial class StageCodec
    {
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

            writer.Write(snapshot.SpecialsUsed.Count);
            foreach (var special in snapshot.SpecialsUsed)
            {
                writer.Write(special.UnitId);
                writer.Write((byte)special.SpeciesNo);
                writer.Write((byte)special.Element);
            }

            writer.Write(snapshot.Bursts.Count);
            foreach (var burst in snapshot.Bursts)
            {
                writer.Write((byte)burst.Shape);
                writer.Write((byte)burst.Element);
                Write(writer, burst.Position);
                writer.Write(burst.Radius);
                writer.Write(burst.IsSpecial);
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

            var specials = reader.ReadInt32();
            for (var i = 0; i < specials; i++)
            {
                var unitId = reader.ReadInt32();
                var species = (int)reader.ReadByte();
                snapshot.SpecialsUsed.Add(new SpecialUsedEvent(unitId, species, (Element)reader.ReadByte()));
            }

            var bursts = reader.ReadInt32();
            for (var i = 0; i < bursts; i++)
            {
                var shape = (BurstShape)reader.ReadByte();
                var element = (Element)reader.ReadByte();
                var position = ReadVec2(reader);
                var radius = reader.ReadSingle();
                snapshot.Bursts.Add(new BurstEvent(shape, element, position, radius, reader.ReadBoolean()));
            }
        }
    }
}
