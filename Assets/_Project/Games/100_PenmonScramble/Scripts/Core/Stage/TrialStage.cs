namespace PenmonScramble.Core
{
    /// <summary>
    /// 1回の挑戦の作り方: 選んだステージに、プレイヤー1人につき編成の3体を置く
    /// </summary>
    public static class TrialStage
    {
        // プレイヤーはエリア0の左寄りから始める（敵は右寄りに出る）
        private const float PlayerSpawnXRatio = -0.7f;

        // 協力のときにプレイヤーを縦に並べる間隔（マス）。体の直径より広くして、最初から重ならないようにする
        private const float PlayerSpawnSpacing = 1.5f;

        /// <summary>
        /// seats の席ごとにプレイヤーを1人ずつ置き、席の順に Unit を返す。parties は席の順の編成。
        /// 敵の種類と位置は seed で決まり、HP は人数で増える
        /// </summary>
        public static Unit[] Create(StageDef stage, int seed, int[] seats, UnitStats[][] parties, out StageWorld world)
        {
            world = new StageWorld(stage, seats.Length, seed);
            var x = Balance.AreaWidth * 0.5f * PlayerSpawnXRatio;
            var players = new Unit[seats.Length];
            for (var i = 0; i < seats.Length; i++)
            {
                var y = (i - (seats.Length - 1) * 0.5f) * PlayerSpawnSpacing;
                players[i] = world.AddUnit(Team.Player, parties[i], new Vec2(x, y), seats[i]);
            }

            return players;
        }

        /// <summary>テスト用: 1ステージ目に、round で順に替わる3体（RotatingParty）を連れていく</summary>
        public static Unit[] Create(int seed, int[] seats, int round, out StageWorld world)
        {
            var parties = new UnitStats[seats.Length][];
            for (var i = 0; i < seats.Length; i++)
            {
                parties[i] = PartyStats(RotatingParty(round, i, seats.Length));
            }

            return Create(StageTable.Stage1, seed, seats, parties, out world);
        }

        /// <summary>
        /// round 回目の i 人目が連れていく3体。全員・全回で番号を3つずつ進めるので、ソロなら7回で20種類がひと回りする。
        /// 編成が届かなかった仲間の代わりにも使う
        /// </summary>
        public static SpeciesDef[] RotatingParty(int round, int playerIndex, int playerCount)
        {
            var start = ((round - 1) * playerCount + playerIndex) * Balance.PartySize;
            var party = new SpeciesDef[Balance.PartySize];
            for (var i = 0; i < party.Length; i++)
            {
                var index = ((start + i) % SpeciesTable.Count + SpeciesTable.Count) % SpeciesTable.Count;
                party[i] = SpeciesTable.Species[index];
            }

            return party;
        }

        public static UnitStats[] PartyStats(SpeciesDef[] party)
        {
            var stats = new UnitStats[party.Length];
            for (var i = 0; i < party.Length; i++)
            {
                stats[i] = SpeciesTable.PlayerStats(party[i], Balance.TrialPower);
            }

            return stats;
        }
    }
}
