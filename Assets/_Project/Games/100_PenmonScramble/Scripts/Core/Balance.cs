using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ゲームの調整値。数値はここにまとめ、ルールのコードには直接書かない。
    /// </summary>
    public static class Balance
    {
        /// <summary>
        /// 1回の更新で進める時間（秒）。オンラインでホストとゲストの結果をそろえるため、描画のフレームとは切り離して固定にする
        /// </summary>
        public const float TickSeconds = 1f / 30f;

        /// <summary>1エリアの広さ（マス）。横画面 16:9 にちょうど収まる大きさ</summary>
        public const float AreaWidth = 16f;
        public const float AreaHeight = 9f;

        /// <summary>吹っ飛びの減速（マス/秒²）。大きいほどすぐ止まる</summary>
        public const float KnockbackDeceleration = 18f;

        /// <summary>この速さより速く飛ばされている間は動けない（マス/秒）</summary>
        public const float StunSpeed = 1f;

        /// <summary>壁に当たって跳ね返るときに残る速さの割合</summary>
        public const float WallRestitution = 0.6f;

        /// <summary>どんなに弱い技でも、当たれば最低これだけ減る（当たったのに0、を見せないため）</summary>
        public const int MinDamage = 1;

        /// <summary>敵は技が届く距離のこの割合まで近づいてから殴る（届くか届かないかの境目で空振りしないため）</summary>
        public const float EnemyAttackRangeRatio = 0.8f;

        /// <summary>協力の最大人数（§1）</summary>
        public const int MaxPlayers = 4;

        /// <summary>
        /// ホストが全員へ状態を送る間隔（更新の回数）。2回ごと＝毎秒15回。
        /// 毎回送ると通信量が倍になる割に、補間して描くので見た目はほとんど変わらない
        /// </summary>
        public const int SnapshotIntervalTicks = 2;

        // 協力の人数ごとの敵の HP の倍率（§4.7）。添字は人数-1
        private static readonly float[] EnemyHpScales = { 1.0f, 1.6f, 2.2f, 2.8f };

        public static float EnemyHpScale(int playerCount)
        {
            var index = Math.Clamp(playerCount, 1, MaxPlayers) - 1;
            return EnemyHpScales[index];
        }
    }
}
