using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>試合を始める用意: ピッチと子を作り、子ごとのペンモンの種類から属性と★（必殺シュートの種類と強さ）を決める</summary>
    public static class SoccerMatchSetup
    {
        /// <summary>種類の表に無い番号（新しい版で増えた子など）のときに使う種類</summary>
        public const int FallbackSpecies = 1;

        /// <param name="species">子ごとのペンモンの種類（赤の子の後に青の子。perTeam ずつ）</param>
        public static SoccerWorld CreateWorld(SoccerPitchDef pitch, IReadOnlyList<int> species, int seed, int perTeam = SoccerRules.PlayersPerTeam)
        {
            var world = new SoccerWorld(pitch, perTeam, perTeam, keepers: true, seed: seed);
            for (var i = 0; i < world.Players.Count; i++)
            {
                var def = SpeciesTable.Get(i < species.Count ? species[i] : FallbackSpecies) ?? SpeciesTable.Get(FallbackSpecies);
                world.SetKind(i, def.Element, def.Stars);
            }

            return world;
        }
    }
}
