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
            return Encode(writer =>
            {
                writer.Write((byte)party.Count);
                foreach (var penguin in party)
                {
                    writer.Write((byte)penguin.speciesNo);
                    writer.Write((short)Math.Clamp(penguin.power, 1, MaxPower));
                }
            });
        }

        /// <summary>
        /// 版が違う・壊れている・知らない種類が入っているときは false。
        /// 人数0は空の編成を party に残して false を返す（呼び出し側は true のときだけ使う）。
        /// 知らない種類のときは party を null のままにする
        /// </summary>
        public static bool TryDecodeParty(byte[] data, out UnitStats[] party)
        {
            TryRead(data, ReadParty, out party);
            return party != null && party.Length > 0;
        }

        /// <summary>知らない種類が入っていたら null を返す（読み取りはそこで止める）</summary>
        private static UnitStats[] ReadParty(BinaryReader reader)
        {
            var count = Math.Min((int)reader.ReadByte(), Balance.PartySize);
            var result = new UnitStats[count];
            for (var i = 0; i < count; i++)
            {
                var species = SpeciesTable.Get(reader.ReadByte());
                var power = Math.Clamp((int)reader.ReadInt16(), 1, MaxPower);
                if (species == null)
                {
                    return null;
                }

                result[i] = SpeciesTable.PlayerStats(species, power);
            }

            return result;
        }
    }
}
