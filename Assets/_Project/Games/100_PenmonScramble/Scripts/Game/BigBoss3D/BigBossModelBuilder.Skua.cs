using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// オオトウゾクカモメ: 茶色の大きな海鳥。いつも宙に浮いて羽ばたき、翼の先の白い模様（本物のトウゾクカモメの目印）と、
    /// 先の曲がった黒いくちばし・怒った眉で天敵らしくする
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        /// <summary>胴の中心が浮いている高さ。丸い影（床の当たりの位置）から体が離れて、飛んでいると分かる高さ</summary>
        public const float SkuaHoverHeight = 1.05f;

        private static readonly Color SkuaBrown = new Color(0.48f, 0.35f, 0.24f);
        private static readonly Color SkuaBelly = new Color(0.72f, 0.60f, 0.45f);
        private static readonly Color SkuaCap = new Color(0.34f, 0.24f, 0.17f);
        private static readonly Color SkuaWing = new Color(0.32f, 0.23f, 0.16f);
        private static readonly Color SkuaPatch = new Color(0.97f, 0.95f, 0.90f);
        private static readonly Color SkuaBeak = new Color(0.20f, 0.20f, 0.23f);
        private static readonly Color SkuaFeet = new Color(0.13f, 0.13f, 0.15f);
        private static readonly Color SkuaBrow = new Color(0.20f, 0.13f, 0.09f);

        // 頭の球の半径と、頭の根元から見た中心
        private const float SkuaHeadRadius = 0.35f;
        private static readonly Vector3 SkuaHeadPivot = new Vector3(0f, 0.3f, 0.62f);
        private static readonly Vector3 SkuaHeadCenter = new Vector3(0f, 0.1f, 0.08f);

        // 翼の付け根から先の関節までの長さ。広げた幅（左右の先から先まで）は当たりの円の 1.8 倍ほど
        private const float SkuaShoulderX = 0.4f;
        private const float SkuaInnerWing = 0.68f;

        private static void BuildSkua(Kit kit)
        {
            var body = kit.Joint(BigBossPart.Body, kit.Figure, Vector3.up * SkuaHoverHeight);
            // 卵形の太い方を前にして寝かせる（胸が張って、尾へ細くなる）
            var lying = Quaternion.Euler(-90f, 0f, 0f);
            kit.Parts.Add("Torso", ToyMeshes.Egg, body, Vector3.zero, lying, new Vector3(1.0f, 1.55f, 0.92f), SkuaBrown, BodyGloss);
            kit.Parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, -0.15f, 0.06f), lying, new Vector3(0.8f, 1.25f, 0.66f), SkuaBelly, MatteGloss);
            BuildSkuaHead(kit, body);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                BuildSkuaWing(kit, body, side);
                BuildSkuaFoot(kit, body, side);
            }

            BuildSkuaTail(kit, body);
            kit.Center = SkuaHoverHeight;
            kit.Top = SkuaHoverHeight + SkuaHeadPivot.y + SkuaHeadCenter.y + SkuaHeadRadius;
            kit.ShadowSize = new Vector2(1.9f, 1.7f);
        }

        private static void BuildSkuaHead(Kit kit, Transform body)
        {
            var head = kit.Joint(BigBossPart.Head, body, SkuaHeadPivot);
            kit.Parts.Add("HeadBall", ToyMeshes.Sphere, head, SkuaHeadCenter, Vector3.one * SkuaHeadRadius * 2f, SkuaBrown, BodyGloss);
            kit.Parts.Add("Cap", ToyMeshes.Sphere, head, SkuaHeadCenter + new Vector3(0f, 0.1f, -0.06f), new Vector3(0.66f, 0.5f, 0.62f), SkuaCap, BodyGloss);
            kit.Parts.Add("Beak", ToyMeshes.Drop, head, new Vector3(0f, 0.04f, 0.52f), Quaternion.FromToRotation(Vector3.up, Vector3.forward), new Vector3(0.24f, 0.46f, 0.2f), SkuaBeak, BodyGloss);
            // くちばしの先を下へ曲げる（獲物を引っかける形。カモメの怖さが出る）
            kit.Parts.Add("BeakHook", ToyMeshes.Drop, head, new Vector3(0f, -0.03f, 0.73f), Quaternion.FromToRotation(Vector3.up, new Vector3(0f, -1f, 0.4f)), new Vector3(0.11f, 0.16f, 0.11f), SkuaBeak, BodyGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                Eye(kit, head, new Vector3(0.19f * side, 0.2f, 0.33f), 0.2f, side, new Vector3(0.45f * side, 0.05f, 1f), SkuaBrow);
            }
        }

        /// <summary>付け根（LimbLeft/Right）から内側の翼、先の関節（TipLeft/Right）から外側の翼と白い模様・風切り羽</summary>
        private static void BuildSkuaWing(Kit kit, Transform body, float side)
        {
            var left = side < 0f;
            var shoulder = kit.Joint(left ? BigBossPart.LimbLeft : BigBossPart.LimbRight, body, new Vector3(SkuaShoulderX * side, 0.2f, 0.12f));
            kit.Parts.Add("WingInner", ToyMeshes.Sphere, shoulder, new Vector3(0.36f * side, 0f, -0.06f), new Vector3(0.78f, 0.09f, 0.56f), SkuaBrown, BodyGloss);
            var tip = kit.Joint(left ? BigBossPart.TipLeft : BigBossPart.TipRight, shoulder, new Vector3(SkuaInnerWing * side, 0f, -0.06f));
            kit.Parts.Add("WingOuter", ToyMeshes.Sphere, tip, new Vector3(0.38f * side, 0f, -0.1f), new Vector3(0.82f, 0.075f, 0.44f), SkuaWing, BodyGloss);
            kit.Parts.Add("WingPatch", ToyMeshes.Sphere, tip, new Vector3(0.3f * side, 0f, -0.08f), new Vector3(0.26f, 0.085f, 0.22f), SkuaPatch, MatteGloss);
            kit.Parts.Add("Primary", ToyMeshes.Drop, tip, new Vector3(0.72f * side, 0f, -0.2f), Fin(new Vector3(side, 0f, -0.5f), Vector3.up), new Vector3(0.22f, 0.42f, 0.05f), SkuaWing, BodyGloss);
        }

        /// <summary>尾羽は扇のように3枚</summary>
        private static void BuildSkuaTail(Kit kit, Transform body)
        {
            var tail = kit.Joint(BigBossPart.Tail, body, new Vector3(0f, 0.02f, -0.7f));
            for (var i = -1; i <= 1; i++)
            {
                var direction = Quaternion.Euler(0f, i * 18f, 0f) * Vector3.back;
                kit.Parts.Add("TailFeather", ToyMeshes.Drop, tail, direction * 0.22f, Fin(direction, Vector3.up), new Vector3(0.26f, 0.5f, 0.06f), SkuaWing, BodyGloss);
            }
        }

        /// <summary>ぶら下げた足。急降下では前へ突き出して、つかみかかるように見せる</summary>
        private static void BuildSkuaFoot(Kit kit, Transform body, float side)
        {
            var foot = kit.Joint(side < 0f ? BigBossPart.FootLeft : BigBossPart.FootRight, body, new Vector3(0.17f * side, -0.36f, -0.12f));
            kit.Parts.Add("Leg", ToyMeshes.Sphere, foot, new Vector3(0f, -0.1f, 0f), new Vector3(0.07f, 0.26f, 0.07f), SkuaFeet, BodyGloss);
            kit.Parts.Add("Foot", ToyMeshes.Sphere, foot, new Vector3(0f, -0.22f, 0.05f), new Vector3(0.17f, 0.05f, 0.22f), SkuaFeet, BodyGloss);
        }
    }
}
