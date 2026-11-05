using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// おもちゃのキーパーの体を組み立てる: 丸い台の上に、チームの色のたまご形の胴（白い帯）、肌色の頭に帽子とつば、
    /// 大きな手袋の腕、背中のゼンマイのねじ。前は +Z、足元（台の底）が原点。動かすのは SoccerKeeperView
    /// </summary>
    public sealed class SoccerKeeperModel
    {
        private const float Gloss = 0.5f;

        private static readonly Color BaseColor = new Color(0.25f, 0.27f, 0.33f);
        private static readonly Color SkinColor = new Color(1f, 0.85f, 0.70f);
        private static readonly Color EyeColor = new Color(0.1f, 0.1f, 0.14f);
        private static readonly Color GloveColor = new Color(0.78f, 0.95f, 0.30f);
        private static readonly Color KeyColor = new Color(0.95f, 0.78f, 0.30f);

        // 寸法（m）。背の高さ約 1.1（ペンモンより少し低い、おもちゃの人形）
        private static readonly Vector3 StandScale = new Vector3(0.72f, 0.12f, 0.72f);
        private static readonly Vector3 TorsoScale = new Vector3(0.62f, 0.74f, 0.54f);
        private const float TorsoY = 0.48f;
        private static readonly Vector3 BandScale = new Vector3(0.62f, 0.9f, 0.55f);
        private const float BandY = 0.42f;
        private const float HeadY = 0.86f;
        private const float HeadSize = 0.38f;
        private static readonly Vector3 EyeOffset = new Vector3(0.075f, 0.13f, 0.17f);
        private const float EyeSize = 0.06f;
        private static readonly Vector3 CapScale = new Vector3(0.4f, 0.32f, 0.4f);
        private const float CapY = 0.15f;
        private static readonly Vector3 BrimScale = new Vector3(0.3f, 0.04f, 0.22f);
        private static readonly Vector3 BrimOffset = new Vector3(0f, 0.15f, 0.18f);
        private static readonly Vector3 ShoulderOffset = new Vector3(0.32f, 0.68f, 0f);
        private static readonly Vector3 ArmScale = new Vector3(0.12f, 0.34f, 0.12f);
        private const float GloveSize = 0.22f;
        private static readonly Vector3 KeyOffset = new Vector3(0f, 0.55f, -0.3f);
        private static readonly Vector3 KeyShaftScale = new Vector3(0.05f, 0.14f, 0.05f);
        private static readonly Vector3 KeyWingScale = new Vector3(0.16f, 0.2f, 0.05f);
        private const float KeyWingSpread = 0.1f;

        public Transform Root { get; private set; }

        /// <summary>傾ける・跳ねる体全体（台の底を軸に回す）</summary>
        public Transform Figure { get; private set; }

        public Transform Head { get; private set; }
        public Transform ArmLeft { get; private set; }
        public Transform ArmRight { get; private set; }

        /// <summary>背中のねじ（前後の軸で回る）</summary>
        public Transform Key { get; private set; }

        public static SoccerKeeperModel Build(Color team, Color teamDark, ToonMaterials materials, Transform parent)
        {
            var model = new SoccerKeeperModel();
            model.Root = new GameObject("Keeper").transform;
            model.Root.SetParent(parent, false);
            model.Figure = Empty("Figure", model.Root, Vector3.zero);
            var figure = model.Figure;

            Part("Stand", ToyMeshes.Puck, figure, Vector3.up * (StandScale.y * 0.5f), StandScale, materials.Get(BaseColor, Gloss));
            Part("Torso", ToyMeshes.Egg, figure, Vector3.up * TorsoY, TorsoScale, materials.Get(team, Gloss));
            Part("Band", ToyMeshes.Torus, figure, Vector3.up * BandY, Vector3.Scale(BandScale, new Vector3(1f, 0.1f, 1f)), materials.Get(Color.white, Gloss));

            model.Head = Empty("Head", figure, Vector3.up * HeadY);
            Part("Face", ToyMeshes.Sphere, model.Head, Vector3.up * (HeadSize * 0.4f), Vector3.one * HeadSize, materials.Get(SkinColor, Gloss));
            foreach (var side in new[] { -1f, 1f })
            {
                Part("Eye", ToyMeshes.Sphere, model.Head, new Vector3(EyeOffset.x * side, EyeOffset.y, EyeOffset.z), Vector3.one * EyeSize, materials.Get(EyeColor, 1f));
            }

            Part("Cap", ToyMeshes.Hemisphere, model.Head, Vector3.up * CapY, CapScale, materials.Get(teamDark, Gloss));
            Part("Brim", ToyMeshes.Puck, model.Head, BrimOffset, BrimScale, materials.Get(Color.white, Gloss));

            model.ArmLeft = BuildArm(figure, -1f, team, materials);
            model.ArmRight = BuildArm(figure, 1f, team, materials);
            model.Key = BuildKey(figure, materials);
            return model;
        }

        /// <summary>肩を軸に下がる腕と、先の大きな手袋</summary>
        private static Transform BuildArm(Transform figure, float side, Color team, ToonMaterials materials)
        {
            var shoulder = Empty(side < 0f ? "ArmLeft" : "ArmRight", figure, new Vector3(ShoulderOffset.x * side, ShoulderOffset.y, ShoulderOffset.z));
            Part("Arm", ToyMeshes.Puck, shoulder, Vector3.down * (ArmScale.y * 0.5f), ArmScale, materials.Get(team, Gloss));
            Part("Glove", ToyMeshes.Sphere, shoulder, Vector3.down * ArmScale.y, Vector3.one * GloveSize, materials.Get(GloveColor, Gloss));
            return shoulder;
        }

        /// <summary>背中から後ろへ出た軸と、ちょうちょ形の2枚の羽（ゼンマイのねじ）</summary>
        private static Transform BuildKey(Transform figure, ToonMaterials materials)
        {
            var key = Empty("Key", figure, KeyOffset);
            var material = materials.Get(KeyColor, 0.8f);
            Part("Shaft", ToyMeshes.Puck, key, Vector3.back * (KeyShaftScale.y * 0.5f), KeyShaftScale, material).localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (var side in new[] { -1f, 1f })
            {
                Part("Wing", ToyMeshes.Sphere, key, new Vector3(KeyWingSpread * side, 0f, -KeyShaftScale.y), KeyWingScale, material);
            }

            return key;
        }

        private static Transform Empty(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        private static Transform Part(string name, Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return obj.transform;
        }
    }
}
