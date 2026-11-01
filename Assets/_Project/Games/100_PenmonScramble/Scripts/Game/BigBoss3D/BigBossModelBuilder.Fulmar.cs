using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// オオフルマカモメ: 大きな灰色の海鳥。オオトウゾクカモメより胴が太く重たく、翼が長い。床すれすれに浮いて滑空する。
    /// 太く大きなくちばし（先が黄色・上に鼻の管）で、ひと目でカモメと見分けられるようにする
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        /// <summary>胴の中心が浮いている高さ。カモメより低く、重たく滑空しているように見せる</summary>
        public const float FulmarHoverHeight = 0.8f;

        private static readonly Color FulmarGray = new Color(0.55f, 0.58f, 0.62f);
        private static readonly Color FulmarBelly = new Color(0.82f, 0.84f, 0.86f);
        private static readonly Color FulmarWing = new Color(0.36f, 0.38f, 0.43f);
        private static readonly Color FulmarWingTip = new Color(0.22f, 0.23f, 0.27f);
        private static readonly Color FulmarBeak = new Color(0.93f, 0.84f, 0.55f);
        private static readonly Color FulmarBeakTip = new Color(0.98f, 0.78f, 0.12f);
        private static readonly Color FulmarFeet = new Color(0.45f, 0.42f, 0.40f);
        private static readonly Color FulmarBrow = new Color(0.25f, 0.26f, 0.30f);

        private const float FulmarHeadRadius = 0.36f;
        private static readonly Vector3 FulmarHeadPivot = new Vector3(0f, 0.28f, 0.66f);
        private static readonly Vector3 FulmarHeadCenter = new Vector3(0f, 0.08f, 0.08f);

        // 翼の付け根の横の位置と、内側の翼の長さ。広げた幅はカモメ（約 1.8 倍）より広い 2.2 倍ほど
        private const float FulmarShoulderX = 0.46f;
        private const float FulmarInnerWing = 0.82f;

        private static void BuildFulmar(Kit kit)
        {
            var body = kit.Joint(BigBossPart.Body, kit.Figure, Vector3.up * FulmarHoverHeight);
            var lying = Quaternion.Euler(-90f, 0f, 0f);
            kit.Parts.Add("Torso", ToyMeshes.Egg, body, Vector3.zero, lying, new Vector3(1.12f, 1.65f, 0.98f), FulmarGray, BodyGloss);
            kit.Parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, -0.16f, 0.06f), lying, new Vector3(0.92f, 1.35f, 0.7f), FulmarBelly, MatteGloss);
            BuildFulmarHead(kit, body);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                BuildFulmarWing(kit, body, side);
                BuildFulmarFoot(kit, body, side);
            }

            BuildFulmarTail(kit, body);
            kit.Center = FulmarHoverHeight;
            kit.Top = FulmarHoverHeight + FulmarHeadPivot.y + FulmarHeadCenter.y + FulmarHeadRadius;
            kit.ShadowSize = new Vector2(2.2f, 1.8f);
        }

        /// <summary>丸い頭に、太く大きなくちばし。上に鼻の管（フルマカモメの目印）、先は黄色く下へ曲げる</summary>
        private static void BuildFulmarHead(Kit kit, Transform body)
        {
            var head = kit.Joint(BigBossPart.Head, body, FulmarHeadPivot);
            kit.Parts.Add("HeadBall", ToyMeshes.Sphere, head, FulmarHeadCenter, Vector3.one * FulmarHeadRadius * 2f, FulmarBelly, BodyGloss);
            kit.Parts.Add("Crown", ToyMeshes.Sphere, head, FulmarHeadCenter + new Vector3(0f, 0.12f, -0.08f), new Vector3(0.62f, 0.42f, 0.6f), FulmarGray, BodyGloss);
            var forward = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            kit.Parts.Add("Beak", ToyMeshes.Sphere, head, new Vector3(0f, 0.0f, 0.5f), new Vector3(0.3f, 0.24f, 0.56f), FulmarBeak, BodyGloss);
            kit.Parts.Add("NoseTube", ToyMeshes.Drop, head, new Vector3(0f, 0.13f, 0.44f), forward, new Vector3(0.14f, 0.3f, 0.12f), FulmarBeak, BodyGloss);
            kit.Parts.Add("BeakHook", ToyMeshes.Drop, head, new Vector3(0f, -0.04f, 0.78f), Quaternion.FromToRotation(Vector3.up, new Vector3(0f, -1f, 0.5f)), new Vector3(0.17f, 0.22f, 0.15f), FulmarBeakTip, BodyGloss);
            // 下あご: 口を開けて突風を吐くときに下げる
            var jaw = kit.Joint(BigBossPart.Jaw, head, new Vector3(0f, -0.08f, 0.3f));
            kit.Parts.Add("LowerBeak", ToyMeshes.Sphere, jaw, new Vector3(0f, -0.03f, 0.22f), new Vector3(0.24f, 0.12f, 0.44f), FulmarBeak, BodyGloss);
            kit.Parts.Add("Mouth", ToyMeshes.Sphere, jaw, new Vector3(0f, 0.02f, 0.16f), new Vector3(0.18f, 0.06f, 0.3f), MouthInside, MatteGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                Eye(kit, head, new Vector3(0.21f * side, 0.18f, 0.3f), 0.18f, side, new Vector3(0.6f * side, 0.05f, 1f), FulmarBrow);
            }
        }

        /// <summary>長い翼。付け根（LimbLeft/Right）から内側、先の関節（TipLeft/Right）から暗い外側と風切り羽</summary>
        private static void BuildFulmarWing(Kit kit, Transform body, float side)
        {
            var left = side < 0f;
            var shoulder = kit.Joint(left ? BigBossPart.LimbLeft : BigBossPart.LimbRight, body, new Vector3(FulmarShoulderX * side, 0.22f, 0.1f));
            kit.Parts.Add("WingInner", ToyMeshes.Sphere, shoulder, new Vector3(0.42f * side, 0f, -0.05f), new Vector3(0.92f, 0.1f, 0.58f), FulmarWing, BodyGloss);
            var tip = kit.Joint(left ? BigBossPart.TipLeft : BigBossPart.TipRight, shoulder, new Vector3(FulmarInnerWing * side, 0f, -0.05f));
            kit.Parts.Add("WingOuter", ToyMeshes.Sphere, tip, new Vector3(0.46f * side, 0f, -0.1f), new Vector3(0.98f, 0.08f, 0.44f), FulmarWingTip, BodyGloss);
            AddFin(kit, tip, "Primary", new Vector3(0.86f * side, 0f, -0.18f), new Vector3(side, 0f, -0.4f), new Vector3(0.24f, 0.46f, 0.05f), FulmarWingTip);
        }

        /// <summary>短い扇形の尾羽</summary>
        private static void BuildFulmarTail(Kit kit, Transform body)
        {
            var tail = kit.Joint(BigBossPart.Tail, body, new Vector3(0f, 0.04f, -0.76f));
            for (var i = -1; i <= 1; i++)
            {
                var direction = Quaternion.Euler(0f, i * 22f, 0f) * Vector3.back;
                AddFin(kit, tail, "TailFeather", direction * 0.18f, direction, new Vector3(0.28f, 0.42f, 0.06f), FulmarWing);
            }
        }

        /// <summary>水かきの付いた足。滑空中は後ろへ流す</summary>
        private static void BuildFulmarFoot(Kit kit, Transform body, float side)
        {
            var foot = kit.Joint(side < 0f ? BigBossPart.FootLeft : BigBossPart.FootRight, body, new Vector3(0.2f * side, -0.38f, -0.2f));
            kit.Parts.Add("Leg", ToyMeshes.Sphere, foot, new Vector3(0f, -0.08f, 0f), new Vector3(0.08f, 0.2f, 0.08f), FulmarFeet, BodyGloss);
            kit.Parts.Add("Foot", ToyMeshes.Sphere, foot, new Vector3(0f, -0.17f, 0.04f), new Vector3(0.2f, 0.05f, 0.24f), FulmarFeet, BodyGloss);
        }
    }
}
