using System;
using System.Collections.Generic;
using System.IO;

namespace PenmonScramble.Core
{
    /// <summary>ゲストが参加したときにホストへ送る、自分の編成（種類とパワー）</summary>
    public static partial class StageCodec
    {
        // 壊れた値や改造した値が届いても、ありえない強さにならないようにする
        private const int MaxPower = 9999;

        public static byte[] EncodeParty(IReadOnlyList<OwnedPenguin> party)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(FormatVersion);
            writer.Write((byte)party.Count);
            foreach (var penguin in party)
            {
                writer.Write((byte)penguin.speciesNo);
                writer.Write((short)Math.Clamp(penguin.power, 1, MaxPower));
            }

            return stream.ToArray();
        }

        /// <summary>版が違う・壊れている・知らない種類が入っているときは false</summary>
        public static bool TryDecodeParty(byte[] data, out UnitStats[] party)
        {
            party = null;
            try
            {
                using var reader = new BinaryReader(new MemoryStream(data));
                if (reader.ReadByte() != FormatVersion)
                {
                    return false;
                }

                var count = Math.Min((int)reader.ReadByte(), Balance.PartySize);
                var result = new UnitStats[count];
                for (var i = 0; i < count; i++)
                {
                    var species = SpeciesTable.Get(reader.ReadByte());
                    var power = Math.Clamp((int)reader.ReadInt16(), 1, MaxPower);
                    if (species == null)
                    {
                        return false;
                    }

                    result[i] = SpeciesTable.PlayerStats(species, power);
                }

                party = result;
                return count > 0;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }
    }
}
