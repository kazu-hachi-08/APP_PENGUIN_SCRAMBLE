using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// シャチ: 黒い体・白いお腹とあご・目の後ろの白い模様・背中の高い背びれ。
    /// 尾の付け根（Hips）と尾びれ（Tail）を上下に振って泳ぐ。下あご（Jaw）を開くと歯が見える
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        private static readonly Color OrcaBlack = new Color(0.10f, 0.11f, 0.14f);
        private static readonly Color OrcaWhite = new Color(0.95f, 0.96f, 0.97f);
        private static readonly Color OrcaSaddle = new Color(0.55f, 0.58f, 0.63f);

        // 胴の卵形の中心と、表面に模様を沿わせるときの楕円体の半径（卵形は前が太いので、前の模様は少し外へ出す）
        private static readonly Vector3 OrcaBodyCenter = new Vector3(0f, 0.52f, 0f);
        private static readonly Vector3 OrcaBodyRadii = new Vector3(0.62f, 0.52f, 0.93f);
        private static readonly Vector3 OrcaFin = new Vector3(0f, 1.32f, -0.12f);
        private const float OrcaFinHeight = 0.95f;
        // 体を少し浮かせて置く。泳いでうなずいても、お腹が床に沈まないように
        private const float OrcaFloat = 0.04f;

        private static void BuildOrca(Kit kit)
        {
            var body = kit.Joint(BigBossPart.Body, kit.Figure, new Vector3(0f, OrcaFloat, 0.3f));
            var lying = Quaternion.Euler(-90f, 0f, 0f);
            kit.Parts.Add("Torso", ToyMeshes.Egg, body, OrcaBodyCenter, lying, new Vector3(1.2f, 1.85f, 1.0f), OrcaBlack, BodyGloss);
            kit.Parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, 0.33f, 0.14f), lying, new Vector3(0.92f, 1.45f, 0.62f), OrcaWhite, MatteGloss);
            // 背びれは少し後ろへ倒す。平たい面を横へ向けて、横から見ても上から見ても形が分かるようにする
            kit.Parts.Add("DorsalFin", ToyMeshes.Drop, body, OrcaFin, Fin(new Vector3(0f, 1f, -0.45f), Vector3.right), new Vector3(0.5f, OrcaFinHeight, 0.12f), OrcaBlack, BodyGloss);
            kit.Parts.Add("Saddle", ToyMeshes.Sphere, body, OnOrca(new Vector3(0f, 0.92f, -0.42f), 1f), SurfaceRotation(new Vector3(0f, 0.92f, -0.42f), Vector3.forward), new Vector3(0.62f, 0.06f, 0.42f), OrcaSaddle, MatteGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var patch = new Vector3(0.75f * side, 0.45f, 0.22f);
                kit.Parts.Add("EyePatch", ToyMeshes.Sphere, body, OnOrca(patch, 1.04f), SurfaceRotation(patch, new Vector3(0f, -0.35f, 1f)), new Vector3(0.24f, 0.06f, 0.44f), OrcaWhite, MatteGloss);
                Eye(kit, body, OnOrca(new Vector3(0.72f * side, 0.12f, 0.62f), 1.04f), 0.16f, side, new Vector3(0.85f * side, 0.1f, 0.55f), null);
                BuildOrcaFlipper(kit, body, side);
            }

            BuildOrcaJaw(kit, body);
            BuildOrcaTail(kit, body);
            kit.Center = OrcaFloat + OrcaBodyCenter.y;
            kit.Top = OrcaFloat + OrcaFin.y + OrcaFinHeight * 0.45f;
            kit.ShadowSize = new Vector2(1.8f, 2.9f);
        }

        private static void BuildOrcaFlipper(Kit kit, Transform body, float side)
        {
            var shoulder = kit.Joint(side < 0f ? BigBossPart.LimbLeft : BigBossPart.LimbRight, body, new Vector3(0.5f * side, 0.25f, 0.32f));
            var direction = new Vector3(side, -0.45f, -0.35f).normalized;
            kit.Parts.Add("Flipper", ToyMeshes.Drop, shoulder, direction * 0.26f, Fin(direction, Vector3.up), new Vector3(0.36f, 0.62f, 0.09f), OrcaBlack, BodyGloss);
        }

        /// <summary>白い下あごと口の中。上の歯は胴に付けて、口を開くと上下の歯が見えるようにする</summary>
        private static void BuildOrcaJaw(Kit kit, Transform body)
        {
            Teeth(kit, body, new[] { new Vector3(-0.2f, 0.34f, 0.74f), new Vector3(0.2f, 0.34f, 0.74f), new Vector3(-0.11f, 0.34f, 0.86f), new Vector3(0.11f, 0.34f, 0.86f) }, Vector3.down, 0.09f);
            var jaw = kit.Joint(BigBossPart.Jaw, body, new Vector3(0f, 0.3f, 0.6f));
            kit.Parts.Add("LowerJaw", ToyMeshes.Sphere, jaw, new Vector3(0f, -0.03f, 0.16f), new Vector3(0.62f, 0.2f, 0.6f), OrcaWhite, MatteGloss);
            kit.Parts.Add("Mouth", ToyMeshes.Sphere, jaw, new Vector3(0f, 0.05f, 0.15f), new Vector3(0.54f, 0.1f, 0.5f), MouthInside, MatteGloss);
            Teeth(kit, jaw, new[] { new Vector3(-0.18f, 0.08f, 0.24f), new Vector3(0.18f, 0.08f, 0.24f), new Vector3(-0.1f, 0.08f, 0.36f), new Vector3(0.1f, 0.08f, 0.36f) }, Vector3.up, 0.08f);
        }

        /// <summary>尾の付け根（Hips）と、左右に開いた平たい尾びれ（Tail）</summary>
        private static void BuildOrcaTail(Kit kit, Transform body)
        {
            var hips = kit.Joint(BigBossPart.Hips, body, new Vector3(0f, 0.5f, -0.78f));
            kit.Parts.Add("TailStock", ToyMeshes.Egg, hips, new Vector3(0f, -0.02f, -0.32f), Quaternion.Euler(-90f, 0f, 0f), new Vector3(0.66f, 1.0f, 0.56f), OrcaBlack, BodyGloss);
            var tail = kit.Joint(BigBossPart.Tail, hips, new Vector3(0f, -0.02f, -0.8f));
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var direction = new Vector3(0.8f * side, 0f, -0.6f).normalized;
                kit.Parts.Add("Fluke", ToyMeshes.Drop, tail, direction * 0.3f + Vector3.back * 0.05f, Fin(direction, Vector3.up), new Vector3(0.4f, 0.7f, 0.08f), OrcaBlack, BodyGloss);
            }
        }

        /// <summary>胴の楕円体の表面の点。direction は中心から見た向き、bulge は表面から少し外へ出す倍率</summary>
        private static Vector3 OnOrca(Vector3 direction, float bulge) => OrcaBodyCenter + Vector3.Scale(OrcaBodyRadii * bulge, direction.normalized);

        /// <summary>表面に寝かせる模様の向き: 平たい面を表面の外向きに、長い方を along の向きにそろえる</summary>
        private static Quaternion SurfaceRotation(Vector3 direction, Vector3 along)
        {
            var d = direction.normalized;
            var normal = new Vector3(d.x / OrcaBodyRadii.x, d.y / OrcaBodyRadii.y, d.z / OrcaBodyRadii.z).normalized;
            return Quaternion.LookRotation(along, normal);
        }
    }
}
