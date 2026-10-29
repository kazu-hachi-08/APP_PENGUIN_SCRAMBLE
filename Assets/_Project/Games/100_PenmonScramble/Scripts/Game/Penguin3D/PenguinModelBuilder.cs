using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 球と卵形を組み合わせて、おもちゃのペンギンを1体組み立てる。寸法は背の高さを約1.2とした値で、
    /// 全体の大きさは呼び出し側が根元の scale で決める。当たり判定（Collider）は付けない（ゲームの当たりは Core が持つ）
    /// </summary>
    public static class PenguinModelBuilder
    {
        private static readonly Color Pupil = new Color(0.06f, 0.06f, 0.09f);
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

        /// <summary>当たりの半径 radius のユニットに合う、根元の scale</summary>
        public static float ScaleForRadius(float radius) => radius * 2f / BodyWidth;

        public static PenguinModel Build(PenguinLook look, ToonMaterials materials, Transform parent)
        {
            var root = new GameObject("Penguin3D");
            root.transform.SetParent(parent, false);
            var model = root.AddComponent<PenguinModel>();
            var parts = new Parts(model, materials);

            var body = Pivot("Body", root.transform, Vector3.zero);
            BuildTorso(parts, look, body);
            var head = BuildHead(parts, look, body);
            var flipperLeft = BuildFlipper(parts, look, body, -1f);
            var flipperRight = BuildFlipper(parts, look, body, 1f);
            var footLeft = BuildFoot(parts, look, root.transform, -1f);
            var footRight = BuildFoot(parts, look, root.transform, 1f);
            var shadow = BuildBlobShadow(materials, look, root.transform);

            model.Assign(body, head, flipperLeft, flipperRight, footLeft, footRight, shadow);
            model.SetRealShadows(true);
            return model;
        }

        private static void BuildTorso(Parts parts, PenguinLook look, Transform body)
        {
            var w = look.Width;
            var h = look.Height;
            parts.Add("Torso", ToyMeshes.Egg, body, new Vector3(0f, 0.44f * h, 0f), new Vector3(BodyWidth * w, 0.82f * h, 0.68f * w), look.Back, BodyGloss);
            parts.Add("Belly", ToyMeshes.Egg, body, new Vector3(0f, 0.38f * h, 0.14f * w), new Vector3(0.58f * w, 0.62f * h, 0.46f * w), look.Belly, BellyGloss);
        }

        private static Transform BuildHead(Parts parts, PenguinLook look, Transform body)
        {
            var s = look.HeadSize;
            var head = Pivot("Head", body, new Vector3(0f, 0.80f * look.Height, 0.02f));
            parts.Add("HeadBall", ToyMeshes.Sphere, head, new Vector3(0f, 0.12f, 0f) * s, Vector3.one * 0.60f * s, look.Back, BodyGloss);
            parts.Add("Face", ToyMeshes.Sphere, head, new Vector3(0f, 0.08f, 0.16f) * s, new Vector3(0.52f, 0.42f, 0.34f) * s, look.EyeRing, BellyGloss);
            parts.Add("Beak", ToyMeshes.Sphere, head, new Vector3(0f, 0.04f, 0.34f) * s, new Vector3(0.22f, 0.10f, 0.20f) * s, look.Beak, BeakGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                parts.Add("Pupil", ToyMeshes.Sphere, head, new Vector3(0.10f * side, 0.15f, 0.315f) * s, new Vector3(0.09f, 0.12f, 0.06f) * s, Pupil, EyeGloss);
                parts.Add("EyeShine", ToyMeshes.Sphere, head, new Vector3(0.088f * side, 0.18f, 0.345f) * s, Vector3.one * 0.035f * s, Highlight, 0f);
            }

            BuildTuft(parts, look, head);
            return head;
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
            parts.Add("Flipper", ToyMeshes.Sphere, shoulder, new Vector3(0f, -0.19f * look.Height, 0f), new Vector3(0.08f, 0.42f * look.Height, 0.24f), look.Back, BodyGloss);
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

            public void Add(string objName, Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Color color, float gloss)
            {
                var obj = new GameObject(objName);
                obj.transform.SetParent(parent, false);
                obj.transform.localPosition = position;
                obj.transform.localScale = scale;
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _materials.Get(color, gloss);
                _model.AddPart(renderer);
            }
        }
    }
}
