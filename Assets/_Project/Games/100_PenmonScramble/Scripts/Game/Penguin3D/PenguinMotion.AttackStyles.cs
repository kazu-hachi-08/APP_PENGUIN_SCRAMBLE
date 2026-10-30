using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>技の形（§4.2）ごとの体とヒレの使い方</summary>
    public sealed partial class PenguinMotion
    {
        /// <summary>
        /// 技1つの動き。Vector2 は (かがむ予備動作での値, 出したときの値)。
        /// 角度は度（Lean は正で前へ倒れる、Swing は正でヒレを後ろへ・負で前へ、Open は正でヒレを外へ）、長さは胴の背の高さ約1.2に対する値
        /// </summary>
        private readonly struct AttackStyle
        {
            public readonly Vector2 Squash;
            public readonly Vector2 Lean;
            public readonly Vector2 Twist;
            public readonly Vector2 Lift;
            public readonly Vector2 Lunge;
            public readonly Vector2 HeadLean;
            public readonly Vector2 Swing;
            public readonly Vector2 Open;

            /// <summary>右のヒレだけ使う（払う・投げる）</summary>
            public readonly bool OneFlipper;

            /// <summary>出しどころから回る角度</summary>
            public readonly float Spin;

            private AttackStyle(Vector2 squash = default, Vector2 lean = default, Vector2 twist = default, Vector2 lift = default, Vector2 lunge = default,
                Vector2 headLean = default, Vector2 swing = default, Vector2 open = default, bool oneFlipper = false, float spin = 0f)
            {
                Squash = squash;
                Lean = lean;
                Twist = twist;
                Lift = lift;
                Lunge = lunge;
                HeadLean = headLean;
                Swing = swing;
                Open = open;
                OneFlipper = oneFlipper;
                Spin = spin;
            }

            // 近接: 体をひねってためてから、右のヒレで前を払う
            private static readonly AttackStyle Melee = new AttackStyle(
                squash: new Vector2(0.13f, 0f), lean: new Vector2(-8f, 15f), twist: new Vector2(35f, -42f), lunge: new Vector2(0f, 0.15f),
                swing: new Vector2(40f, -90f), open: new Vector2(30f, 40f), oneFlipper: true);

            // 弾: 右のヒレを頭の後ろへ振りかぶって、前へ投げる
            private static readonly AttackStyle Projectile = new AttackStyle(
                lean: new Vector2(-15f, 18f), lunge: new Vector2(0f, 0.1f), headLean: new Vector2(-10f, 0f),
                swing: new Vector2(160f, -70f), oneFlipper: true);

            // 突進: 深くかがみ、ヒレを後ろへ流して前へ飛び出す（そのあとは Sliding でお腹ですべる）
            private static readonly AttackStyle Dash = new AttackStyle(
                squash: new Vector2(0.25f, 0f), lean: new Vector2(25f, 40f), lunge: new Vector2(0f, 0.3f),
                swing: new Vector2(70f, 90f));

            // 拡散: ヒレを胸の前で交差させてから、左右へ大きく開いて振りまく
            private static readonly AttackStyle Spread = new AttackStyle(
                squash: new Vector2(0.11f, -0.06f), lean: new Vector2(10f, -8f),
                swing: new Vector2(-70f, -20f), open: new Vector2(-30f, 110f));

            // 周囲: ヒレを横に広げて、その場で1回転する
            private static readonly AttackStyle Nova = new AttackStyle(
                squash: new Vector2(0.18f, 0f), open: new Vector2(60f, 85f), spin: 360f);

            // 設置: 跳び上がってヒレを上げ、足を踏みしめて前の床を指す
            private static readonly AttackStyle Zone = new AttackStyle(
                squash: new Vector2(-0.06f, 0.22f), lean: new Vector2(0f, 25f), lift: new Vector2(0.15f, 0f),
                swing: new Vector2(-60f, -50f), open: new Vector2(70f, 0f));

            // 落下: ヒレを空へ高く上げて上を見て、前へ振り下ろして呼び落とす
            private static readonly AttackStyle Drop = new AttackStyle(
                squash: new Vector2(-0.1f, 0.13f), lean: new Vector2(-10f, 15f), headLean: new Vector2(-30f, 0f),
                swing: new Vector2(0f, -60f), open: new Vector2(150f, 20f));

            public static AttackStyle Of(SkillShape shape)
            {
                switch (shape)
                {
                    case SkillShape.Projectile: return Projectile;
                    case SkillShape.Dash: return Dash;
                    case SkillShape.Spread: return Spread;
                    case SkillShape.Nova: return Nova;
                    case SkillShape.Zone: return Zone;
                    case SkillShape.Drop: return Drop;
                    default: return Melee;
                }
            }
        }
    }
}
