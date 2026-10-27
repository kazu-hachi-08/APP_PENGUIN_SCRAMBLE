namespace PenmonScramble.Core
{
    /// <summary>
    /// お試しエリア: プレイヤー1人につき3体と、野生のペンギンの群れ。
    /// フェーズ4でステージ定義、フェーズ6で編成（自分で選んだ3体）に置き換える
    /// </summary>
    public static class TrialStage
    {
        public const int EnemyCount = 10;

        // 敵はエリアの右側に出す。最初からプレイヤーに重ならないようにするため
        private const float EnemySpawnMinXRatio = 0.1f;
        private const float EnemySpawnMarginRatio = 0.1f;
        private const float PlayerSpawnXRatio = -0.7f;

        // 協力のときにプレイヤーを縦に並べる間隔（マス）。体の直径より広くして、最初から重ならないようにする
        private const float PlayerSpawnSpacing = 1.5f;

        /// <summary>
        /// seats の席ごとにプレイヤーを1人ずつ置き、席の順に Unit を返す。敵の種類と位置は seed で決まり、HP は人数で増える。
        /// 連れていく3体は round で順に替わる（編成ができるまで、「もう一度」で20種類を全部試せるようにするため）
        /// </summary>
        public static Unit[] Create(int seed, int[] seats, int round, out StageWorld world)
        {
            var half = new Vec2(Balance.AreaWidth * 0.5f, Balance.AreaHeight * 0.5f);
            world = new StageWorld(-half, half, seed);
            var players = new Unit[seats.Length];
            for (var i = 0; i < seats.Length; i++)
            {
                var y = (i - (seats.Length - 1) * 0.5f) * PlayerSpawnSpacing;
                var party = PartyStats(RotatingParty(round, i, seats.Length));
                players[i] = world.AddUnit(Team.Player, party, new Vec2(half.X * PlayerSpawnXRatio, y), seats[i]);
            }

            AddEnemies(world, seed, half, seats.Length);
            return players;
        }

        /// <summary>
        /// round 回目の i 人目が連れていく3体。全員・全回で番号を3つずつ進めるので、
        /// ソロなら7回で20種類がひと回りする
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

        private static UnitStats[] PartyStats(SpeciesDef[] party)
        {
            var stats = new UnitStats[party.Length];
            for (var i = 0; i < party.Length; i++)
            {
                stats[i] = SpeciesTable.PlayerStats(party[i], Balance.TrialPower);
            }

            return stats;
        }

        private static void AddEnemies(StageWorld world, int seed, Vec2 half, int playerCount)
        {
            var random = new System.Random(seed);
            var minX = half.X * EnemySpawnMinXRatio;
            var maxX = half.X * (1f - EnemySpawnMarginRatio);
            var maxY = half.Y * (1f - EnemySpawnMarginRatio);
            for (var i = 0; i < EnemyCount; i++)
            {
                var x = minX + (float)random.NextDouble() * (maxX - minX);
                var y = -maxY + (float)random.NextDouble() * maxY * 2f;
                var species = SpeciesTable.Species[random.Next(SpeciesTable.Count)];
                world.AddUnit(Team.Enemy, SpeciesTable.EnemyStats(species, playerCount), new Vec2(x, y));
            }
        }
    }
}
