using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 球と卵形を組み合わせて、おもちゃのペンギンを1体組み立てる。寸法は背の高さを約1.2とした値で、
    /// 全体の大きさは呼び出し側が根元の scale で決める。当たり判定（Collider）は付けない（ゲームの当たりは Core が持つ）。
    /// 種類ごとの飾りは PenguinModelBuilder.Ornaments*.cs
    /// </summary>
    public static partial class PenguinModelBuilder
    {
        private static readonly Color Highlight = Color.white;

        // つや（0〜1）。目はいちばん光らせて生き生きと見せる
        private const float BodyGloss = 0.85f;
        private const float BellyGloss = 0.2f;
        private const float BeakGloss = 0.6f;
        private const float EyeGloss = 1f;

        // ヒレは少し外へ開いて、正面から見ても体の横に出るようにする
        private const float FlipperSplay = 12f;
        private const float FootSplay = 14f;
        private const float TuftSpread = 28f;

        // 標準の体型の胴の太さ。ゲームの当たりの円（直径）とこの太さをそろえる
        private const float BodyWidth = 0.74f;

        // 胴の卵形（体型の倍率を掛ける前）。飾りを胴の表面に沿わせるときにも使う
        private const float TorsoCenterY = 0.44f;
        private const float TorsoHeight = 0.82f;
        private const float TorsoDepth = 0.68f;
        private const float TorsoBulge = 0.18f;

        // 白いお腹の卵形（Torso と同じく体型の倍率を掛ける前）。前へ出ている分だけ、倒れたときに胴が浮く
        private const float BellyCenterY = 0.38f;
        private const float BellyHalfHeight = 0.31f;
        private const float BellyCenterZ = 0.14f;
        private const float BellyHalfDepth = 0.23f;
        private const float BellyWidth = 0.58f;

        // 頭の球（頭の大きさの倍率を掛ける前）。頭の根元から見た中心と半径
        private const float HeadPivotY = 0.80f;
        private static readonly Vector3 HeadCenter = new Vector3(0f, 0.12f, 0f);
        private const float HeadRadius = 0.30f;

        // ひとみ（頭の大きさの倍率を掛ける前）
        private static readonly Vector3 EyePosition = new Vector3(0.10f, 0.15f, 0.315f);
        private static readonly Vector3 PupilSize = new Vector3(0.09f, 0.12f, 0.06f);
        private const float NarrowEyeHeight = 0.6f;
        // 眠そうな目は横長の線にして少し下げる（まぶたの部品で隠すと白目をむいたように見えた）
        private static readonly Vector3 SleepyEyeScale = new Vector3(1.25f, 0.28f, 1f);
        private const float SleepyEyeDrop = 0.02f;

        /// <summary>当たりの半径 radius のユニットに合う、根元の scale</summary>
        public static float ScaleForRadius(float radius) => radius * 2f / BodyWidth;

        public static PenguinModel Build(PenguinLook look, ToonMaterials materials, Transform parent)
        {
            var root = new GameObject("Penguin3D");
            root.transform.SetParent(parent, false);
            var model = root.AddComponent<PenguinModel>();
            model.SetMaterials(materials);
            var parts = new Parts(model, materials);

            var figure = Pivot("Figure", root.transform, Vector3.zero);
            var body = Pivot("Body", figure, Vector3.zero);
            var eyes = new List<Transform>();
            BuildTorso(parts, look, body);
            var head = BuildHead(parts, look, body, eyes);
            var flipperLeft = BuildFlipper(parts, look, body, -1f);
            var flipperRight = BuildFlipper(parts, look, body, 1f);
            var footLeft = BuildFoot(parts, look, figure, -1f);
            var footRight = BuildFoot(parts, look, figure, 1f);
            var shadow = BuildBlobShadow(materials, look, root.transform);

            var rig = new Rig(parts, look, figure, body, head, flipperLeft, flipperRight);
            foreach (var ornament in look.Ornaments)
            {
                BuildOrnament(rig, ornament);
            }

            model.Assign(figure, body, head, flipperLeft, flipperRight, footLeft, footRight, shadow);
            var top = HeadPivotY * look.Height + (HeadCenter.y + HeadRadius) * look.HeadSize;
            model.AssignShape(eyes, TorsoCenterY * look.Height, (BellyCenterZ + BellyHalfDepth) * look.Width, top);
            model.SetRealShadows(true);
            return model;
        }

        private static void BuildTorso(Parts parts, PenguinLook look, Transform body)
        {
            var w = look.Width;
            var h = look.Height;
            parts.Add("Torso", ToyMeshes.Egg, body, new Vector3(0f, TorsoCenterY * h, 0f), new Vector3(BodyWidth * w, TorsoHeight * h, TorsoDepth * w), look.Back, BodyGloss);
            parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, BellyCenterY * h, BellyCenterZ * w), new Vector3(BellyWidth * w, BellyHalfHeight * 2f * h, BellyHalfDepth * 2f * w), look.Belly, BellyGloss);
        }

        private static Transform BuildHead(Parts parts, PenguinLook look, Transform body, List<Transform> eyes)
        {
            var s = look.HeadSize;
            var head = Pivot("Head", body, new Vector3(0f, HeadPivotY * look.Height, 0.02f));
            parts.Add("HeadBall", ToyMeshes.Sphere, head, HeadCenter * s, Vector3.one * HeadRadius * 2f * s, look.Back, BodyGloss);
            parts.Add("Face", ToyMeshes.Sphere, head, new Vector3(0f, 0.08f, 0.16f) * s, new Vector3(0.52f, 0.42f, 0.34f) * s, look.EyeRing, BellyGloss);
            parts.Add("Beak", ToyMeshes.Sphere, head, new Vector3(0f, 0.04f, 0.34f) * s, new Vector3(0.22f, 0.10f, 0.20f) * s, look.Beak, BeakGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                BuildEye(parts, look, head, side, eyes);
            }

            if (look.Tuft)
            {
                BuildTuft(parts, look, head);
            }

            return head;
        }

        private static void BuildEye(Parts parts, PenguinLook look, Transform head, float side, List<Transform> eyes)
        {
            var s = look.HeadSize;
            var position = Vector3.Scale(EyePosition, new Vector3(side, 1f, 1f)) * s;
            var size = PupilSize;
            switch (look.Eyes)
            {
                case EyeStyle.Narrow:
                    size.y *= NarrowEyeHeight;
                    break;
                case EyeStyle.Sleepy:
                    size = Vector3.Scale(size, SleepyEyeScale);
                    position.y -= SleepyEyeDrop * s;
                    break;
            }

            eyes.Add(parts.Add("Pupil", ToyMeshes.Sphere, head, position, size * s, look.Eye, EyeGloss));
            // 線の目に光を入れると目を開けているように見えるので入れない
            if (look.Eyes != EyeStyle.Sleepy)
            {
                eyes.Add(parts.Add("EyeShine", ToyMeshes.Sphere, head, position + new Vector3(-0.012f * side, 0.03f, 0.03f) * s, Vector3.one * 0.035f * s, Highlight, 0f));
            }
        }

        /// <summary>頭のてっぺんの寝ぐせ（タイトルのキービジュアルのペンギンと同じ）</summary>
        private static void BuildTuft(Parts parts, PenguinLook look, Transform head)
        {
            var s = look.HeadSize;
            for (var i = -1; i <= 1; i++)
            {
                var pivot = Pivot("Tuft", head, new Vector3(0f, 0.40f, -0.02f) * s);
                pivot.localRotation = Quaternion.Euler(-10f, 0f, i * TuftSpread);
                parts.Add("TuftHair", ToyMeshes.Sphere, pivot, new Vector3(0f, 0.06f, 0f) * s, new Vector3(0.035f, 0.14f, 0.035f) * s, look.Back, BodyGloss);
            }
        }

        /// <summary>肩を軸に回せるよう、ヒレの形は肩の下へずらして置く。side は左が -1、右が 1</summary>
        private static Transform BuildFlipper(Parts parts, PenguinLook look, Transform body, float side)
        {
            var shoulder = Pivot(side < 0f ? "FlipperLeft" : "FlipperRight", body, new Vector3(0.34f * look.Width * side, 0.66f * look.Height, 0f));
            shoulder.localRotation = Quaternion.Euler(0f, 0f, FlipperSplay * side);
            parts.Add("Flipper", ToyMeshes.Sphere, shoulder, new Vector3(0f, -0.19f * look.Height, 0f), new Vector3(0.08f, 0.42f * look.Height, 0.24f), look.FlipperColor, BodyGloss);
            return shoulder;
        }

        /// <summary>足は胴と別に根元へ付ける。胴を揺らしても足は床から離れないようにするため</summary>
        private static Transform BuildFoot(Parts parts, PenguinLook look, Transform root, float side)
        {
            var foot = Pivot(side < 0f ? "FootLeft" : "FootRight", root, new Vector3(0.15f * look.Width * side, 0f, 0.04f));
            foot.localRotation = Quaternion.Euler(0f, FootSplay * side, 0f);
            parts.Add("Foot", ToyMeshes.Sphere, foot, new Vector3(0f, 0.04f, 0.08f), new Vector3(0.20f, 0.08f, 0.30f), look.Feet, BeakGloss);
            return foot;
        }

        private static Renderer BuildBlobShadow(ToonMaterials materials, PenguinLook look, Transform root)
        {
            var obj = new GameObject("BlobShadow");
            obj.transform.SetParent(root, false);
            // 床と同じ高さだとちらつくので少し浮かせる
            obj.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            obj.transform.localScale = new Vector3(0.95f * look.Width, 1f, 0.85f * look.Width);
            obj.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorQuad;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.BlobShadow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Transform Pivot(string objName, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(objName).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        /// <summary>形の部品を作って PenguinModel に登録する</summary>
        private readonly struct Parts
        {
            private readonly PenguinModel _model;
            private readonly ToonMaterials _materials;

            public Parts(PenguinModel model, ToonMaterials materials)
            {
                _model = model;
                _materials = materials;
            }

            public Transform Add(string objName, Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Color color, float gloss)
            {
                return Add(objName, mesh, parent, position, Quaternion.identity, scale, color, gloss);
            }

            public Transform Add(string objName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float gloss)
            {
                var obj = new GameObject(objName);
                obj.transform.SetParent(parent, false);
                obj.transform.localPosition = position;
                obj.transform.localRotation = rotation;
                obj.transform.localScale = scale;
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _materials.Get(color, gloss);
                _model.AddPart(renderer, color, gloss);
                return obj.transform;
            }
        }
    }
}
