using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// サメ: 青灰色の体・白いお腹・大きな背びれ・上が長い縦の尾びれ・えらの筋。口を開けると丸い歯が並ぶ（おもちゃらしく尖らせない）。
    /// 尾の付け根（Hips）と尾びれ（Tail）を左右に振って泳ぐ（上下に振るシャチと見分けるため）。
    /// もぐると背びれだけを床の上に残す（DiveDepth）
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        /// <summary>背中と背びれの色。本気のサメの2本目の背びれ（EffectView）も同じ色にする</summary>
        public static readonly Color SharkBack = new Color(0.40f, 0.49f, 0.60f);
        public const float SharkGloss = BodyGloss;

        private static readonly Color SharkBelly = new Color(0.94f, 0.95f, 0.96f);
        private static readonly Color SharkGill = new Color(0.24f, 0.30f, 0.38f);
        private static readonly Color SharkBrow = new Color(0.18f, 0.22f, 0.28f);

        private static readonly Vector3 SharkBodyCenter = new Vector3(0f, 0.55f, 0f);
        private static readonly Vector3 SharkFin = new Vector3(0f, 1.4f, -0.05f);
        private const float SharkFinHeight = 0.9f;
        private const float SharkFloat = 0.05f;
        // もぐったときに床の上に残す背びれの高さ（てっぺんから）
        private const float SharkFinPeek = 0.6f;

        private static void BuildShark(Kit kit)
        {
            var body = kit.Joint(BigBossPart.Body, kit.Figure, new Vector3(0f, SharkFloat, 0.25f));
            var lying = Quaternion.Euler(-90f, 0f, 0f);
            kit.Parts.Add("Torso", ToyMeshes.Egg, body, SharkBodyCenter, lying, new Vector3(1.15f, 2.0f, 1.0f), SharkBack, BodyGloss);
            kit.Parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, 0.36f, 0.1f), lying, new Vector3(0.95f, 1.6f, 0.6f), SharkBelly, MatteGloss);
            kit.Parts.Add("Snout", ToyMeshes.Sphere, body, new Vector3(0f, 0.62f, 0.92f), new Vector3(0.72f, 0.5f, 0.66f), SharkBack, BodyGloss);
            // 背びれは後ろへ倒して、平たい面を横へ向ける（横から見ても上から見ても、もぐって走るときも形が分かるように）
            kit.Parts.Add("DorsalFin", ToyMeshes.Drop, body, SharkFin, Fin(new Vector3(0f, 1f, -0.5f), Vector3.right), new Vector3(0.62f, SharkFinHeight, 0.13f), SharkBack, BodyGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                // 目は体の横に付ける（上に付けると、斜め上から見たとき奥の目が鼻先の上に飛び出して見える）
                Eye(kit, body, new Vector3(0.45f * side, 0.68f, 0.6f), 0.15f, side, new Vector3(1f * side, 0.1f, 0.35f), SharkBrow);
                BuildSharkGills(kit, body, side);
                BuildSharkFlipper(kit, body, side);
            }

            BuildSharkJaw(kit, body);
            BuildSharkTail(kit, body);
            kit.Center = SharkFloat + SharkBodyCenter.y;
            kit.Top = SharkFloat + SharkFin.y + SharkFinHeight * 0.45f;
            kit.DiveDepth = kit.Top - SharkFinPeek;
            kit.ShadowSize = new Vector2(1.8f, 3.0f);
        }

        /// <summary>目の後ろの、えらの3本の筋</summary>
        private static void BuildSharkGills(Kit kit, Transform body, float side)
        {
            for (var i = 0; i < 3; i++)
            {
                var position = new Vector3(0.55f * side, 0.55f, 0.48f - 0.13f * i);
                kit.Parts.Add("Gill", ToyMeshes.Sphere, body, position, Quaternion.Euler(0f, 0f, 8f * side), new Vector3(0.04f, 0.32f, 0.05f), SharkGill, MatteGloss);
            }
        }

        private static void BuildSharkFlipper(Kit kit, Transform body, float side)
        {
            var shoulder = kit.Joint(side < 0f ? BigBossPart.LimbLeft : BigBossPart.LimbRight, body, new Vector3(0.5f * side, 0.32f, 0.3f));
            var direction = new Vector3(side, -0.6f, -0.45f).normalized;
            AddFin(kit, shoulder, "Flipper", direction * 0.3f, direction, new Vector3(0.38f, 0.75f, 0.09f), SharkBack);
        }

        /// <summary>鼻先の下の口。上の歯は胴に付け、下あご（Jaw）を開くと上下の丸い歯が並んで見える</summary>
        private static void BuildSharkJaw(Kit kit, Transform body)
        {
            Teeth(kit, body, new[]
            {
                new Vector3(-0.26f, 0.36f, 0.78f), new Vector3(-0.14f, 0.36f, 0.88f), new Vector3(0f, 0.36f, 0.92f),
                new Vector3(0.14f, 0.36f, 0.88f), new Vector3(0.26f, 0.36f, 0.78f),
            }, Vector3.down, 0.1f);
            var jaw = kit.Joint(BigBossPart.Jaw, body, new Vector3(0f, 0.32f, 0.55f));
            kit.Parts.Add("LowerJaw", ToyMeshes.Sphere, jaw, new Vector3(0f, -0.04f, 0.2f), new Vector3(0.66f, 0.2f, 0.62f), SharkBelly, MatteGloss);
            kit.Parts.Add("Mouth", ToyMeshes.Sphere, jaw, new Vector3(0f, 0.04f, 0.2f), new Vector3(0.56f, 0.1f, 0.52f), MouthInside, MatteGloss);
            Teeth(kit, jaw, new[]
            {
                new Vector3(-0.22f, 0.08f, 0.26f), new Vector3(-0.11f, 0.08f, 0.38f), new Vector3(0.11f, 0.08f, 0.38f), new Vector3(0.22f, 0.08f, 0.26f),
            }, Vector3.up, 0.09f);
        }

        /// <summary>尾の付け根（Hips）と、上の羽が長い縦の尾びれ（Tail）</summary>
        private static void BuildSharkTail(Kit kit, Transform body)
        {
            var hips = kit.Joint(BigBossPart.Hips, body, new Vector3(0f, 0.6f, -0.85f));
            kit.Parts.Add("TailStock", ToyMeshes.Egg, hips, new Vector3(0f, 0f, -0.32f), Quaternion.Euler(-90f, 0f, 0f), new Vector3(0.6f, 1.0f, 0.6f), SharkBack, BodyGloss);
            var tail = kit.Joint(BigBossPart.Tail, hips, new Vector3(0f, 0.02f, -0.8f));
            var upper = new Vector3(0f, 0.8f, -0.6f).normalized;
            var lower = new Vector3(0f, -0.65f, -0.5f).normalized;
            kit.Parts.Add("UpperLobe", ToyMeshes.Drop, tail, upper * 0.32f, Fin(upper, Vector3.right), new Vector3(0.36f, 0.85f, 0.08f), SharkBack, BodyGloss);
            kit.Parts.Add("LowerLobe", ToyMeshes.Drop, tail, lower * 0.2f, Fin(lower, Vector3.right), new Vector3(0.3f, 0.52f, 0.08f), SharkBack, BodyGloss);
        }
    }
}
