using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ペンモン画面で技を見せるための目安（形の名前・特徴・威力・出しやすさ・範囲の3段階）。
    /// 形ごとに数値の意味が違う（設置は何度も当たる、落下は数か所に落ちる など）ので、そのまま比べず
    /// 「1回出したときに1体へ与えるおおよその威力」「当たる床の広さ」に直してから段階に分ける
    /// </summary>
    public static class SkillProfile
    {
        public const int MaxRating = 3;

        // 威力の段階の境目（1体へのおおよその威力）。技1の多く（8前後）が1、技2の多く（20前後）が2になる
        private const float PowerRating2 = 12f;
        private const float PowerRating3 = 26f;

        // 出しやすさの段階の境目（待ち時間・秒）。押しっぱなしで連打できる技が3
        private const float QuickCooldown = 0.5f;
        private const float SlowCooldown = 2.5f;

        // 範囲の段階の境目（当たる床の広さ・マス²）。近接のふつうの振りが1、周囲の輪が2、大きな設置や落下が3
        private const float AreaRating2 = 3f;
        private const float AreaRating3 = 9f;

        // 設置は敵がずっと中にいるとは限らないので、当たる回数の3分の1強ほどを目安にする
        private const float ZoneStayRatio = 0.35f;
        // 割り算の誤差で回数が1つ減らないようにする（3 / 0.3 が 9.999… になる）
        private const float CountEpsilon = 0.0001f;
        // 弾は1発が1体に当たる。貫通しない弾は通り道の半分ほどしか敵に会わない、とみなす
        private const float SolidShotAreaRatio = 0.5f;

        public static string ShapeName(SkillShape shape)
        {
            switch (shape)
            {
                case SkillShape.Melee: return "近接";
                case SkillShape.Projectile: return "弾";
                case SkillShape.Dash: return "突進";
                case SkillShape.Spread: return "拡散";
                case SkillShape.Nova: return "周囲";
                case SkillShape.Zone: return "設置";
                default: return "落下";
            }
        }

        /// <summary>形の名前に添える特徴（「3本」「貫通」「吸いこみ」など）。無ければ空</summary>
        public static string Feature(SkillDef skill)
        {
            switch (skill.Shape)
            {
                case SkillShape.Projectile:
                    return skill.Pierce ? "貫通" : string.Empty;
                case SkillShape.Spread:
                    return $"{skill.Count}本";
                case SkillShape.Zone:
                    return skill.Pull > 0f ? "吸いこみ" : string.Empty;
                case SkillShape.Drop:
                    return skill.Count > 1 ? $"{skill.Count}か所" : string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>「拡散・3本」のように、形と特徴をつなげたもの</summary>
        public static string ShapeLabel(SkillDef skill)
        {
            var feature = Feature(skill);
            return feature.Length > 0 ? $"{ShapeName(skill.Shape)}・{feature}" : ShapeName(skill.Shape);
        }

        public static int PowerRating(SkillDef skill) => Rate(HitPower(skill), PowerRating2, PowerRating3);

        /// <summary>待ち時間が短いほど高い</summary>
        public static int QuicknessRating(SkillDef skill)
        {
            return skill.Cooldown <= QuickCooldown ? 3 : skill.Cooldown <= SlowCooldown ? 2 : 1;
        }

        public static int AreaRating(SkillDef skill) => Rate(CoverArea(skill), AreaRating2, AreaRating3);

        /// <summary>1回出したときに、1体へ与えるおおよその威力</summary>
        public static float HitPower(SkillDef skill)
        {
            if (skill.Shape != SkillShape.Zone || skill.Interval <= 0f)
            {
                return skill.Power;
            }

            // 置いた瞬間と、その後の間隔ごとに当たる
            var hits = MathF.Floor(skill.Duration / skill.Interval + CountEpsilon) + 1f;
            return skill.Power * hits * ZoneStayRatio;
        }

        /// <summary>当たる床のおおよその広さ（マス²）</summary>
        public static float CoverArea(SkillDef skill)
        {
            switch (skill.Shape)
            {
                case SkillShape.Melee:
                    // 扇形の広さ ＝ 半径² × 半分の角度
                    return skill.Range * skill.Range * skill.HalfAngleDegrees * MathF.PI / 180f;
                case SkillShape.Projectile:
                    return ShotArea(skill);
                case SkillShape.Spread:
                    return ShotArea(skill) * skill.Count;
                case SkillShape.Dash:
                    return skill.Range * (Balance.PenguinRadius + skill.AreaRadius) * 2f;
                case SkillShape.Nova:
                    return Circle(skill.Range);
                case SkillShape.Zone:
                    return Circle(skill.AreaRadius);
                default:
                    return Circle(skill.AreaRadius) * skill.Count;
            }
        }

        private static float ShotArea(SkillDef skill)
        {
            var area = skill.Range * skill.ProjectileRadius * 2f;
            return skill.Pierce ? area : area * SolidShotAreaRatio;
        }

        private static float Circle(float radius) => MathF.PI * radius * radius;

        private static int Rate(float value, float rating2, float rating3)
        {
            return value >= rating3 ? 3 : value >= rating2 ? 2 : 1;
        }
    }
}
