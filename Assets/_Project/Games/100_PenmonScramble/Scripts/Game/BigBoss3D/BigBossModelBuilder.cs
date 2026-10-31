using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボス3体を球・卵形・しずく形で組み立てる。形がペンギンと違うので、ペンギンの組み立て役とは分ける。
    /// 寸法は当たりの半径を 1 とした値（全体の大きさは根元の scale = 当たりの半径）。前は +Z、足元が原点。
    /// 体ごとの組み立ては BigBossModelBuilder.Skua.cs / .Seal.cs / .Orca.cs。当たり判定（Collider）は付けない
    /// </summary>
    public static partial class BigBossModelBuilder
    {
        // つや。ペンギンと同じく、体はよく光らせておもちゃらしく、口の中や白い所は控えめにする
        private const float BodyGloss = 0.85f;
        private const float MatteGloss = 0.2f;
        private const float EyeGloss = 1f;

        private static readonly Color EyeWhite = new Color(1f, 0.97f, 0.88f);
        private static readonly Color Pupil = new Color(0.08f, 0.06f, 0.06f);
        private static readonly Color MouthInside = new Color(0.78f, 0.22f, 0.28f);
        private static readonly Color Tooth = new Color(0.98f, 0.97f, 0.92f);

        // 怒った眉の傾き（度）。天敵らしく、目尻を上げて目頭を下げる
        private const float BrowTilt = 24f;

        /// <summary>当たりの半径 radius の大ボスに合う、根元の scale</summary>
        public static float ScaleForRadius(float radius) => radius;

        public static bool Has(int bossNo) => BigBossTable.Get(bossNo) != null;

        public static BigBossModel Build(int bossNo, ToonMaterials materials, Transform parent)
        {
            var root = new GameObject("BigBoss3D");
            root.transform.SetParent(parent, false);
            var model = root.AddComponent<BigBossModel>();
            model.SetMaterials(materials);
            var kit = new Kit(model, new ToyParts(model, materials), ToyParts.Pivot("Figure", root.transform, Vector3.zero));
            switch (bossNo)
            {
                case BigBossTable.SkuaNo:
                    BuildSkua(kit);
                    break;
                case BigBossTable.LeopardSealNo:
                    BuildSeal(kit);
                    break;
                default:
                    BuildOrca(kit);
                    break;
            }

            var shadow = ToyParts.BlobShadow(materials, root.transform, kit.ShadowSize);
            model.AssignFrame(kit.Figure, shadow, kit.Top);
            model.Assign(bossNo, kit.Eyes, kit.Center);
            model.SetRealShadows(true);
            return model;
        }

        /// <summary>
        /// 目（白目・ひとみ・光）を look の向きへ向けて置き、まばたき用の軸を返す。眉はまばたきでつぶれないよう、目の軸の外に置く。
        /// side は左が -1、右が 1
        /// </summary>
        private static void Eye(Kit kit, Transform parent, Vector3 position, float diameter, float side, Vector3 look, Color? brow)
        {
            var facing = Quaternion.LookRotation(look);
            var eye = ToyParts.Pivot("Eye", parent, position);
            eye.localRotation = facing;
            var d = diameter;
            kit.Parts.Add("EyeWhite", ToyMeshes.Sphere, eye, Vector3.zero, Vector3.one * d, EyeWhite, EyeGloss);
            kit.Parts.Add("Pupil", ToyMeshes.Sphere, eye, new Vector3(0f, -0.04f * d, 0.3f * d), new Vector3(0.52f, 0.6f, 0.45f) * d, Pupil, EyeGloss);
            kit.Parts.Add("EyeShine", ToyMeshes.Sphere, eye, new Vector3(-0.1f * d * side, 0.12f * d, 0.5f * d), Vector3.one * 0.18f * d, Color.white, 0f);
            kit.Eyes.Add(eye);
            if (brow.HasValue)
            {
                var rotation = facing * Quaternion.Euler(0f, 0f, BrowTilt * side);
                kit.Parts.Add("Brow", ToyMeshes.Sphere, parent, position + facing * new Vector3(0f, 0.5f * d, 0.12f * d), rotation, new Vector3(1.25f * d, 0.3f * d, 0.35f * d), brow.Value, MatteGloss);
            }
        }

        /// <summary>しずく形の先を tip の向きへ、平たい面を normal の向きへ向ける（ヒレ・羽・歯）</summary>
        private static Quaternion Fin(Vector3 tip, Vector3 normal) => Quaternion.LookRotation(normal, tip);

        /// <summary>歯を並べる。tip は歯の先の向き（上あごは下、下あごは上）</summary>
        private static void Teeth(Kit kit, Transform parent, Vector3[] positions, Vector3 tip, float size)
        {
            foreach (var position in positions)
            {
                kit.Parts.Add("Tooth", ToyMeshes.Drop, parent, position, Quaternion.FromToRotation(Vector3.up, tip), new Vector3(0.6f, 1f, 0.6f) * size, Tooth, MatteGloss);
            }
        }

        /// <summary>組み立ての途中で持ち回る置き場所と、組み終わってから ToyModel に渡す寸法</summary>
        private sealed class Kit
        {
            public readonly BigBossModel Model;
            public readonly ToyParts Parts;
            public readonly Transform Figure;
            public readonly List<Transform> Eyes = new List<Transform>();

            /// <summary>いちばん高い所（頭の上の文字・氷の塊の高さ）</summary>
            public float Top;

            /// <summary>胴の中心の高さ</summary>
            public float Center;

            /// <summary>足元の丸い影の大きさ（横・前後）</summary>
            public Vector2 ShadowSize;

            public Kit(BigBossModel model, ToyParts parts, Transform figure)
            {
                Model = model;
                Parts = parts;
                Figure = figure;
            }

            public Transform Joint(BigBossPart part, Transform parent, Vector3 position)
            {
                var joint = ToyParts.Pivot(part.ToString(), parent, position);
                Model.SetJoint(part, joint);
                return joint;
            }
        }
    }
}
