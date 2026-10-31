using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ヒョウアザラシ: 灰色の長い体にヒョウ柄の斑点。大きな頭と、開くと歯の並んだ口。
    /// 胴は前（Body）と後ろ（Hips）・後ろヒレ（Tail）の3つに分けて、くねらせて這えるようにする
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        private static readonly Color SealBack = new Color(0.47f, 0.51f, 0.58f);
        private static readonly Color SealBelly = new Color(0.82f, 0.84f, 0.87f);
        private static readonly Color SealSpot = new Color(0.24f, 0.26f, 0.32f);
        private static readonly Color SealFlipper = new Color(0.36f, 0.39f, 0.46f);

        // 胴（前の半分）の楕円体: 中心と半径
        private static readonly Vector3 SealChestCenter = new Vector3(0f, 0.47f, 0f);
        private static readonly Vector3 SealChestRadii = new Vector3(0.625f, 0.46f, 0.675f);
        private static readonly Vector3 SealHipsCenter = new Vector3(0f, 0f, -0.25f);
        private static readonly Vector3 SealHipsRadii = new Vector3(0.49f, 0.35f, 0.5f);

        private static readonly Vector3 SealHeadPivot = new Vector3(0f, 0.68f, 0.42f);
        // 体を少し浮かせて置く。這って胸を揺らしても、お腹が床に沈まないように
        private const float SealFloat = 0.03f;
        private static readonly Vector3 SealHeadCenter = new Vector3(0f, 0.04f, 0.3f);
        private const float SealHeadHalfHeight = 0.3f;

        // ヒョウ柄の斑点（楕円体の中心から見た向きと大きさ）。左右でずらして、模様に見えるようにする
        private static readonly Vector4[] SealChestSpots =
        {
            new Vector4(0.45f, 0.75f, 0.2f, 0.2f),
            new Vector4(-0.4f, 0.8f, -0.3f, 0.17f),
            new Vector4(0.05f, 0.9f, -0.45f, 0.14f),
            new Vector4(-0.75f, 0.45f, 0.35f, 0.13f),
            new Vector4(0.8f, 0.4f, -0.35f, 0.15f),
        };

        private static readonly Vector4[] SealHipsSpots =
        {
            new Vector4(-0.3f, 0.85f, 0.1f, 0.16f),
            new Vector4(0.45f, 0.75f, -0.4f, 0.13f),
            new Vector4(-0.7f, 0.5f, -0.5f, 0.11f),
        };

        private static void BuildSeal(Kit kit)
        {
            var body = kit.Joint(BigBossPart.Body, kit.Figure, new Vector3(0f, SealFloat, 0.15f));
            kit.Parts.Add("Chest", ToyMeshes.Sphere, body, SealChestCenter, SealChestRadii * 2f, SealBack, BodyGloss);
            kit.Parts.Add("ChestBelly", ToyMeshes.Sphere, body, new Vector3(0f, 0.3f, 0.08f), new Vector3(1.02f, 0.55f, 1.2f), SealBelly, MatteGloss);
            Spots(kit, body, SealChestCenter, SealChestRadii, SealChestSpots);

            var hips = kit.Joint(BigBossPart.Hips, body, new Vector3(0f, 0.36f, -0.45f));
            kit.Parts.Add("Hips", ToyMeshes.Sphere, hips, SealHipsCenter, SealHipsRadii * 2f, SealBack, BodyGloss);
            kit.Parts.Add("HipsBelly", ToyMeshes.Sphere, hips, new Vector3(0f, -0.12f, -0.2f), new Vector3(0.8f, 0.4f, 0.85f), SealBelly, MatteGloss);
            Spots(kit, hips, SealHipsCenter, SealHipsRadii, SealHipsSpots);

            var tail = kit.Joint(BigBossPart.Tail, hips, new Vector3(0f, -0.14f, -0.7f));
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var direction = new Vector3(0.35f * side, 0f, -1f).normalized;
                AddFin(kit, tail, "HindFlipper", new Vector3(0.1f * side, 0f, 0f) + direction * 0.24f, direction, new Vector3(0.34f, 0.5f, 0.08f), SealFlipper);
                BuildSealFlipper(kit, body, side);
            }

            BuildSealHead(kit, body);
            kit.Center = SealFloat + SealChestCenter.y;
            kit.Top = SealFloat + SealHeadPivot.y + SealHeadCenter.y + SealHeadHalfHeight;
            kit.ShadowSize = new Vector2(1.7f, 2.8f);
        }

        /// <summary>前ヒレは後ろへ寝かせておき、這うときに前後へこぐ</summary>
        private static void BuildSealFlipper(Kit kit, Transform body, float side)
        {
            var shoulder = kit.Joint(side < 0f ? BigBossPart.LimbLeft : BigBossPart.LimbRight, body, new Vector3(0.55f * side, 0.2f, 0.15f));
            var direction = new Vector3(0.6f * side, -0.25f, -0.75f).normalized;
            AddFin(kit, shoulder, "Flipper", direction * 0.26f, direction, new Vector3(0.32f, 0.62f, 0.08f), SealFlipper);
        }

        /// <summary>大きな頭と鼻先。下あご（Jaw）には赤い口の中と下の歯を付け、開くと見える</summary>
        private static void BuildSealHead(Kit kit, Transform body)
        {
            var head = kit.Joint(BigBossPart.Head, body, SealHeadPivot);
            kit.Parts.Add("HeadBall", ToyMeshes.Sphere, head, SealHeadCenter, new Vector3(0.78f, SealHeadHalfHeight * 2f, 0.9f), SealBack, BodyGloss);
            kit.Parts.Add("Snout", ToyMeshes.Sphere, head, new Vector3(0f, -0.04f, 0.62f), new Vector3(0.56f, 0.34f, 0.55f), SealBack, BodyGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                kit.Parts.Add("Nostril", ToyMeshes.Sphere, head, new Vector3(0.08f * side, 0.1f, 0.87f), new Vector3(0.07f, 0.04f, 0.05f), SealSpot, MatteGloss);
                Eye(kit, head, new Vector3(0.25f * side, 0.2f, 0.46f), 0.17f, side, new Vector3(0.8f * side, 0.2f, 0.6f), SealSpot);
            }

            Teeth(kit, head, new[] { new Vector3(-0.17f, -0.2f, 0.62f), new Vector3(0.17f, -0.2f, 0.62f), new Vector3(-0.1f, -0.2f, 0.78f), new Vector3(0.1f, -0.2f, 0.78f) }, Vector3.down, 0.09f);

            var jaw = kit.Joint(BigBossPart.Jaw, head, new Vector3(0f, -0.14f, 0.28f));
            kit.Parts.Add("LowerJaw", ToyMeshes.Sphere, jaw, new Vector3(0f, -0.05f, 0.26f), new Vector3(0.52f, 0.18f, 0.62f), SealBelly, MatteGloss);
            kit.Parts.Add("Mouth", ToyMeshes.Sphere, jaw, new Vector3(0f, 0.02f, 0.25f), new Vector3(0.44f, 0.1f, 0.52f), MouthInside, MatteGloss);
            Teeth(kit, jaw, new[] { new Vector3(-0.15f, 0.06f, 0.36f), new Vector3(0.15f, 0.06f, 0.36f), new Vector3(-0.09f, 0.06f, 0.5f), new Vector3(0.09f, 0.06f, 0.5f) }, Vector3.up, 0.08f);
        }

        /// <summary>楕円体の表面に沿わせて、平たい斑点を置く。spot の xyz は中心から見た向き、w は大きさ</summary>
        private static void Spots(Kit kit, Transform parent, Vector3 center, Vector3 radii, Vector4[] spots)
        {
            foreach (var spot in spots)
            {
                var direction = new Vector3(spot.x, spot.y, spot.z).normalized;
                var normal = new Vector3(direction.x / radii.x, direction.y / radii.y, direction.z / radii.z).normalized;
                var position = center + Vector3.Scale(radii, direction);
                kit.Parts.Add("Spot", ToyMeshes.Sphere, parent, position, Quaternion.FromToRotation(Vector3.up, normal), new Vector3(spot.w, spot.w * 0.3f, spot.w * 0.8f), SealSpot, BodyGloss);
            }
        }
    }
}
