using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>ボットのうまさ。よけ始めるまでの遅れで分ける</summary>
    public enum BotSkill : byte
    {
        /// <summary>よけない・交代しない・起こしに行かない。近い敵へ歩いて技を出し続けるだけ（重さの計測・スクショ用）</summary>
        Brawler,

        /// <summary>ふつうの人。予告に気づいてから動き出すまでに少しかかる</summary>
        Casual,

        /// <summary>うまい人。予告を見てすぐ動く</summary>
        Expert,
    }

    /// <summary>
    /// 人の代わりに戦うプレイヤーの入力（バランスの試験場・スクショの台本・重さの計測で使う）。
    /// 赤い予告・敵の構え・敵の竜巻などの中にいたら外へ歩き、HP が減ったら交代し、倒れた仲間を起こしに行く。
    /// それ以外は近い敵へ歩いて技1を出し続け、ときどき技2、ゲージがたまれば必殺技を出す。
    /// 1人に1つ作る（気づいてからの遅れと、交代のボタンの押し離しを覚えておくため）
    /// </summary>
    public sealed partial class StageBot
    {
        // この距離まで近づいたら止まって技を出す（相手の体の縁から、マス）
        private const float AttackGap = 1.2f;
        // 技2はときどき押す（ずっと押すと技1が出ない）
        private const float Skill2Period = 1.5f;
        private const float Skill2Hold = 0.2f;
        // 場に出ている子の HP がこの割合を切ったら、もっと元気な控えと交代する
        private const float SwitchHpRatio = 0.3f;
        // 気づいてから動き出すまで（秒）。人の反応の速さの目安（見てから押すまで 0.2〜0.4 秒ほど）
        private const float ExpertReactionSeconds = 0.15f;
        private const float CasualReactionSeconds = 0.35f;
        // ふつうの人は、たくさんの敵に気を取られて、危ないものをこの割合で見落とす（気づかずに当たる）
        private const double CasualOverlookChance = 0.2;

        private readonly BotSkill _skill;
        // 見落とすかどうかを決める乱数。種を決めておけば、同じ試験は何度回しても同じ結果になる
        private readonly Random _random;
        // 危ないもの（予告・構え・範囲の Id）を初めて見た時刻。気づくまでの遅れを測る
        private readonly Dictionary<int, float> _firstSeen = new Dictionary<int, float>();
        private readonly List<int> _stale = new List<int>();
        private bool _switchHeld;
        // いまの判断をしている時刻（危ないものに気づいたかを見るため）
        private float _now;

        /// <param name="seed">見落としの乱数の種（ふつうの人だけが使う）</param>
        public StageBot(BotSkill skill, int seed = 0)
        {
            _skill = skill;
            _random = new Random(seed);
        }

        private float ReactionSeconds => _skill == BotSkill.Expert ? ExpertReactionSeconds : CasualReactionSeconds;

        /// <summary>よけないボット（重さの計測・スクショ用）。覚えておくものが無いので作らずに呼べる（毎フレーム作ると、計測中にごみが出るため）</summary>
        public static PlayerInput InputFor(StageWorld world, Unit player, float time)
        {
            var skill2 = IsSkill2Time(time);
            return new PlayerInput(Approach(world, player), skill1: !skill2, skill2: skill2, special: true);
        }

        /// <param name="time">ステージが始まってからの秒数</param>
        public PlayerInput Next(StageWorld world, Unit player, float time)
        {
            _now = time;
            var skill2 = IsSkill2Time(time);
            var move = _skill == BotSkill.Brawler ? Approach(world, player) : Decide(world, player, time);
            var swap = _skill != BotSkill.Brawler && WantsSwitch(player) && !_switchHeld;
            _switchHeld = swap;
            return new PlayerInput(move, skill1: !skill2, skill2: skill2, special: true, swap: swap);
        }

        private static bool IsSkill2Time(float time) => time % Skill2Period < Skill2Hold;

        /// <summary>よける → 起こす → 戦う → 次のエリアへ、の順に決める</summary>
        private Vec2 Decide(StageWorld world, Unit player, float time)
        {
            if (TryEscape(world, player, time, out var escape))
            {
                return escape;
            }

            if (TryFindDownedAlly(world, player, out var downed))
            {
                return MoveToward(player.Position, downed, Balance.ReviveRadius * 0.5f);
            }

            if (HasEnemy(world))
            {
                return AvoidSteppingIn(world, player, Approach(world, player));
            }

            return world.IsGateOpen ? TowardNextArea(world, player) : Vec2.Zero;
        }

        private static Vec2 Approach(StageWorld world, Unit player)
        {
            var enemy = NearestEnemy(world, player.Position);
            if (enemy == null)
            {
                return Vec2.Zero;
            }

            return MoveToward(player.Position, enemy.Position, enemy.Radius + player.Radius + AttackGap);
        }

        /// <summary>出入口が開いたら、広がった範囲の右端（次のエリア）へ。通り道は真ん中の高さにある</summary>
        private static Vec2 TowardNextArea(StageWorld world, Unit player)
        {
            var target = new Vec2(world.AreaMax.X, (world.AreaMin.Y + world.AreaMax.Y) * 0.5f);
            var toTarget = target - player.Position;
            // 通り道の高さに寄せてから進む（楕円の闘技場の手前で、とがった先の柵に引っかからないように）
            var y = MathF.Abs(toTarget.Y) > Balance.ArenaDoorHalfWidth * 0.5f ? toTarget.Y : 0f;
            return new Vec2(1f, y).Normalized;
        }

        private bool WantsSwitch(Unit player)
        {
            if (player.SwitchCooldown > 0f || player.Hp >= player.MaxHp * SwitchHpRatio)
            {
                return false;
            }

            var ratio = (float)player.Hp / player.MaxHp;
            foreach (var member in player.Members)
            {
                if (member != player.Active && member.IsAlive && (float)member.Hp / member.MaxHp > ratio + SwitchHpRatio)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindDownedAlly(StageWorld world, Unit player, out Vec2 position)
        {
            position = default;
            var best = float.MaxValue;
            foreach (var unit in world.Units)
            {
                var distance = (unit.Position - player.Position).LengthSquared;
                if (unit != player && unit.IsDown && distance < best)
                {
                    best = distance;
                    position = unit.Position;
                }
            }

            return best < float.MaxValue;
        }

        private static bool HasEnemy(StageWorld world) => world.CountAlive(Team.Enemy) > 0;

        private static Unit NearestEnemy(StageWorld world, Vec2 from)
        {
            Unit nearest = null;
            var best = float.MaxValue;
            foreach (var unit in world.Units)
            {
                var distance = (unit.Position - from).LengthSquared;
                if (unit.Team == Team.Enemy && unit.IsAlive && distance < best)
                {
                    best = distance;
                    nearest = unit;
                }
            }

            return nearest;
        }

        /// <summary>target から stopDistance の所まで歩く（もう着いていれば止まる）</summary>
        private static Vec2 MoveToward(Vec2 from, Vec2 target, float stopDistance)
        {
            var delta = target - from;
            return delta.LengthSquared > stopDistance * stopDistance ? delta.Normalized : Vec2.Zero;
        }
    }
}
