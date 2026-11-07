using System;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// AI の強さ1段ぶん。足の速さは変えない（速さで勝負が決まらないように）。
    /// 気づくまでの遅れ・シュートの狙いのぶれ・スライディングの出しやすさ・パスを選ぶ賢さ・ダッシュの使い方で差をつける
    /// </summary>
    public readonly struct SoccerAiLevel
    {
        /// <summary>ボールの動きに気づくまでの遅れ（秒）。この秒だけ前のボールが、そのまま進んだと思って動く（蹴られた・奪われたのに気づくのが遅れる）</summary>
        public readonly float ReactionSeconds;

        /// <summary>シュート・パス・スライディングを考え直す間隔の秒</summary>
        public readonly float DecisionSeconds;

        /// <summary>シュートの狙いのぶれ（度。正規分布のおよその幅）</summary>
        public readonly float AimErrorDegrees;

        /// <summary>届く所で、スライディングを出す割合（考え直すたびに決める）</summary>
        public readonly float SlideChance;

        /// <summary>前の空いた味方がいるときに、パスを選ぶ割合（囲まれたときは強さに関係なくパスを探す）</summary>
        public readonly float PassSense;

        /// <summary>シュートを狙い始める、ゴールまでの距離</summary>
        public readonly float ShootRange;

        /// <summary>遠くのボールへダッシュする割合（考え直すたびに決める）</summary>
        public readonly float DashChance;

        /// <summary>PK戦で蹴るとき、狙う強さのぶれ（小さいほど赤い所に入れず、強く蹴れる）</summary>
        public readonly float PenaltyPowerSpread;

        /// <summary>PK戦で守るとき、蹴る子がため始めたのを見て狙いを読む割合</summary>
        public readonly float PenaltyReadChance;

        public SoccerAiLevel(float reactionSeconds, float decisionSeconds, float aimErrorDegrees, float slideChance, float passSense, float shootRange, float dashChance, float penaltyPowerSpread, float penaltyReadChance)
        {
            PenaltyPowerSpread = penaltyPowerSpread;
            PenaltyReadChance = penaltyReadChance;
            ReactionSeconds = reactionSeconds;
            DecisionSeconds = decisionSeconds;
            AimErrorDegrees = aimErrorDegrees;
            SlideChance = slideChance;
            PassSense = passSense;
            ShootRange = shootRange;
            DashChance = dashChance;
        }
    }

    /// <summary>AI の強さの段と、AI の動きの調整値</summary>
    public static partial class SoccerBalance
    {
        public const int AiMinStars = 1;
        public const int AiMaxStars = 5;

        /// <summary>人のチームの、人が動かしていない味方の強さ（「ふつう」で固定。案）</summary>
        public const int TeammateAiStars = 3;

        /// <summary>協力で人が1人増えるごとに、CPU の強さを上げる段の数（案。人が多いほど1人ずつの子が少なく、人の判断が多く入って強いため）</summary>
        public const int CoopStarsPerExtraHuman = 1;

        /// <summary>協力の CPU の強さ: スタジアムの強さに、2人目からの人数ぶんを足す（★5 まで）</summary>
        public static int CoopCpuStars(int stadiumStars, int humans)
        {
            return Math.Clamp(stadiumStars + Math.Max(0, humans - 1) * CoopStarsPerExtraHuman, AiMinStars, AiMaxStars);
        }

        /// <summary>気づくまでの遅れの上限（秒）。これより前のボールは覚えておかない</summary>
        public const float AiMaxReactionSeconds = 0.5f;

        // ★1〜★5（スタジアム 1〜5 の CPU）。★4・★5 は ★3 との間を詰めてある: 等間隔の段では、全スタジアムの確認で
        // ★3（人のチームの代わり）が嵐の崖に 18%・火山の入り江に 2% しか勝てず、最後のスタジアムがほぼ勝てない壁になったため
        // （詰めた後は 35%・20%。人が入ると勝ちやすくなるので、試遊で外れていたらここを直す）
        private static readonly SoccerAiLevel[] AiLevels =
        {
            new SoccerAiLevel(0.22f, 0.4f, 10f, 0.1f, 0.4f, 9f, 0.25f, 0.22f, 0.05f),
            new SoccerAiLevel(0.18f, 0.35f, 8.5f, 0.13f, 0.48f, 9.5f, 0.3f, 0.18f, 0.1f),
            new SoccerAiLevel(0.15f, 0.3f, 7f, 0.16f, 0.55f, 10f, 0.35f, 0.15f, 0.15f),
            new SoccerAiLevel(0.14f, 0.28f, 6.5f, 0.17f, 0.58f, 10.2f, 0.37f, 0.14f, 0.17f),
            new SoccerAiLevel(0.12f, 0.25f, 5.5f, 0.19f, 0.65f, 10.5f, 0.4f, 0.12f, 0.2f),
        };

        public static SoccerAiLevel AiLevel(int stars) => AiLevels[Math.Clamp(stars, AiMinStars, AiMaxStars) - AiMinStars];

        // ---- AI の動き（強さに関係なく同じ）----

        /// <summary>考え直す間隔を、子ごとにこの割合だけばらす（全員が同じ歩で一斉に動き出さないように）</summary>
        public const float AiDecisionJitter = 0.25f;

        /// <summary>持ち場の取り合いを決め直す間隔の、考え直す間隔に対する倍率（持ち場がころころ替わらないように）</summary>
        public const float AiRoleHoldScale = 2f;

        /// <summary>ボールを取りに行く子を替えるのは、今の子より着くのがこの秒以上早い子がいるときだけ</summary>
        public const float AiChaserMargin = 0.35f;

        /// <summary>ボールに追いつける所を読む、いちばん先の秒と刻み（SoccerReach。AI と人の自動の切り替えで使う）</summary>
        public const float InterceptHorizon = 3f;
        public const float InterceptStep = 0.1f;

        /// <summary>持ち場に着いたとみなす距離と、この距離から歩きをゆるめる</summary>
        public const float AiArriveDistance = 0.35f;
        public const float AiSlowDistance = 1.5f;

        /// <summary>シュート・パスのコースに相手がいないとみなす、コースの線からの距離</summary>
        public const float AiShotLaneClearance = 0.7f;
        public const float AiPassLaneClearance = 1.1f;

        /// <summary>シュートの的はゴールの口の端からこれだけ内側（柱に当てない）</summary>
        public const float AiShotPostInset = 0.7f;

        /// <summary>これより近ければ、コースが空いていなくても蹴る</summary>
        public const float AiForceShootDistance = 4f;

        /// <summary>キーパーがこれより近ければ、取られる前に蹴る</summary>
        public const float AiKeeperPanicDistance = 2.2f;

        /// <summary>ゴールの線からこれより近くて、口の外（角度が無い所）にいるときは蹴らずに中へ運ぶ</summary>
        public const float AiNoAngleDepth = 1.5f;

        /// <summary>ため（0〜1）: 近くは AiChargeMin、1 マス遠いごとに AiChargePerMeter 足し、AiChargeMax まで（満タンの浮いた球は蹴らない）</summary>
        public const float AiChargeMin = 0.6f;
        public const float AiChargePerMeter = 0.03f;
        public const float AiChargeMax = 0.95f;

        /// <summary>この距離に相手がいたら、囲まれたとみなしてパスを探す</summary>
        public const float AiPressureRadius = 2.2f;

        /// <summary>パスを出す相手までの距離の範囲</summary>
        public const float AiPassMinDistance = 3f;
        public const float AiPassMaxDistance = 15f;

        /// <summary>
        /// パスの良さ ＝ 前へ進む距離 ＋ 受ける子のまわりの空き × AiPassOpenWeight（空きは AiPassOpenCap まで）。
        /// 囲まれていないときは、これが AiPassWorthScore 以上のときだけパスする
        /// </summary>
        public const float AiPassOpenWeight = 0.8f;
        public const float AiPassOpenCap = 4f;
        public const float AiPassWorthScore = 3f;

        /// <summary>持って運ぶとき、前のこの距離にいる相手をよける</summary>
        public const float AiDribbleAvoidRadius = 3.5f;
        public const float AiDribbleAvoidWeight = 1.5f;

        /// <summary>縦の壁からこの距離に入ったら、運ぶ向きを内へ寄せる</summary>
        public const float AiWallAvoidDistance = 1.8f;
        public const float AiWallAvoidWeight = 0.8f;

        /// <summary>持っている相手の動く先をこの秒だけ読んで寄る</summary>
        public const float AiPressLeadSeconds = 0.3f;

        /// <summary>この距離に入って、向きが合っていればスライディングを出す</summary>
        public const float AiSlideDistance = 1.6f;
        public const float AiSlideFacingCos = 0.7f;

        /// <summary>持っている相手へ寄るとき、相手の進む先からこれだけ自分のゴール側に立つ</summary>
        public const float AiGoalSideOffset = 0.8f;

        /// <summary>持ち場の子でも、相手が持っているボールがこの距離に来たら奪いに行く</summary>
        public const float AiEngageDistance = 2.5f;

        /// <summary>ボールがこれより遠いときだけダッシュする</summary>
        public const float AiDashDistance = 4f;
    }
}
