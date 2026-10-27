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

        /// <summary>ペンギンの体の半径（マス）。種類で変えると交代した瞬間に重なりが動いて見えるので、全員同じにする</summary>
        public const float PenguinRadius = 0.45f;

        /// <summary>ガチャ（フェーズ6）ができるまで、連れていくペンギンのパワー（§7 のワールド1の真ん中）</summary>
        public const int TrialPower = 100;

        /// <summary>連れていくペンギンの数（§4.1）</summary>
        public const int PartySize = 3;

        /// <summary>交代した後、次に交代できるまで（秒）。交代を連打して技の待ち時間をごまかせないようにする</summary>
        public const float SwitchCooldownSeconds = 2f;

        // 野生の敵（§4.7）。プレイヤーより弱く・遅くして、たくさん出しても囲まれて負けないようにする
        /// <summary>敵の HP ＝ 種類の HP の倍率 × これ × 人数の倍率</summary>
        public const int EnemyPower = 25;
        /// <summary>敵の攻撃 ＝ 種類の攻撃の倍率 × これ</summary>
        public const float EnemyAttackScale = 0.5f;
        public const float EnemySpeedScale = 0.45f;
        /// <summary>敵は同じ技でも、プレイヤーのこの倍の間をあけて使う</summary>
        public const float EnemyCooldownScale = 5f;

        /// <summary>落下の技で、届く所に敵がいないときに落とす前方の距離（マス）</summary>
        public const float DropFallbackDistance = 2f;

        /// <summary>敵1体分の HP を削ったときにたまる必殺技のゲージ（満タン=1）。5体分で満タン</summary>
        public const float SpecialGainPerEnemy = 0.2f;

        /// <summary>必殺技のカットインの長さ（更新の回数）。0.6秒（§4.3）。この間は全体が止まる</summary>
        public const int SpecialCutInTicks = 18;

        /// <summary>必殺技を出した本人が無敵でいる時間（秒）。カットインと効き目が終わるまで</summary>
        public const float SpecialInvincibleSeconds = 3f;

        // ステージの進み方（§6.1）
        /// <summary>エリアに入ってから最初の敵が出るまで（秒）。出入口が閉じたのを見せてから出すため</summary>
        public const float FirstWaveDelaySeconds = 1f;
        /// <summary>敵がこの数まで減ったら次の波を出す。全滅まで待つと間が空いて退屈になるため</summary>
        public const int NextWaveEnemiesLeft = 2;
        /// <summary>開いた出入口から次のエリアへこれだけ入ると、次のエリアが始まる（マス）。出入口の上を行き来しただけでは始めない</summary>
        public const float AreaEnterDepth = 2f;
        /// <summary>敵はプレイヤーからこれだけ離れた所に出す（マス）。出た瞬間に殴られないように</summary>
        public const float SpawnMinDistance = 4f;
        /// <summary>敵を出す場所の、壁からの余白（マス）</summary>
        public const float SpawnWallMargin = 1f;

        // ステージのボス（§6.1）。同じ種類の雑魚を、大きく・硬く・強くする
        public const float BossHpScale = 15f;
        public const float BossAttackScale = 2f;
        public const float BossRadiusScale = 2f;
        /// <summary>雑魚（EnemyCooldownScale）より短い間で技を使う</summary>
        public const float BossCooldownScale = 2f;
        /// <summary>ボスが飛ばされる強さの割合。雑魚と同じだけ飛ぶと、強い敵に見えないため</summary>
        public const float BossKnockbackScale = 0.25f;

        // ダウンと復活（§4.5）
        /// <summary>仲間の体がこの範囲にかかっていると、復活の時間がたまる（マス）</summary>
        public const float ReviveRadius = 1.5f;
        public const float ReviveSeconds = 3f;
        /// <summary>復活した先頭の子の HP の割合</summary>
        public const float ReviveHpRatio = 0.3f;
        /// <summary>復活した直後の無敵（秒）。囲まれたまま起きて、すぐまた倒れる、を防ぐ</summary>
        public const float ReviveInvincibleSeconds = 2f;

        // 吹っ飛びの連鎖（§4.4）
        /// <summary>この速さ以上で飛んでいる敵だけが、ほかの敵にぶつかってダメージを与える（マス/秒）。弱い技で押しただけでは連鎖させない</summary>
        public const float ChainMinSpeed = 6f;
        /// <summary>連鎖のダメージ ＝ ぶつかった速さ × これ</summary>
        public const float ChainDamagePerSpeed = 1f;
        /// <summary>ぶつけられた敵が飛ぶ速さの割合</summary>
        public const float ChainTransferRatio = 0.8f;
        /// <summary>ぶつかった側に残る速さの割合。ぶつかるたびに弱まって、連鎖がいつまでも続かないようにする</summary>
        public const float ChainKeepRatio = 0.5f;

        // 合体技（§4.6）
        /// <summary>別のプレイヤーの技がこの秒数以内に同じ敵へ当たると、合体技が起きる</summary>
        public const float ComboWindowSeconds = 1f;
        /// <summary>同じ敵で1回起きたら、この秒数は起きない（連続で起き続けないため）</summary>
        public const float ComboCooldownSeconds = 2f;
        /// <summary>合体技が起きたときに全体を止める長さ（更新の回数）。0.1秒だけ止めて「決まった」感じを出す</summary>
        public const int ComboHitStopTicks = 3;
        /// <summary>足が遅くなっている間の速さの割合（合体技「吹雪」）</summary>
        public const float SlowSpeedRatio = 0.5f;

        // 協力の人数ごとの敵の HP の倍率（§4.7）。添字は人数-1
        private static readonly float[] EnemyHpScales = { 1.0f, 1.6f, 2.2f, 2.8f };

        public static float EnemyHpScale(int playerCount)
        {
            var index = Math.Clamp(playerCount, 1, MaxPlayers) - 1;
            return EnemyHpScales[index];
        }
    }
}
