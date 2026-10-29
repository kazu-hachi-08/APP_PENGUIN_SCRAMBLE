using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 種類ごとの飾りを組み立てる。顔・頭の上の飾りは頭に付けて頭と一緒に動かし、体の飾りは胴に、持ち物はヒレに付ける
    /// </summary>
    public static partial class PenguinModelBuilder
    {
        // 飾りのつや。金具・宝石は光らせ、布はほとんど光らせない
        private const float ClothGloss = 0.15f;
        private const float AccessoryGloss = 0.6f;
        private const float ShinyGloss = 1f;

        // 輪（ToyMeshes.Torus）の管の中心の半径と管の太さ（scale 1 のとき）
        private const float TorusCenterRadius = 0.45f;
        private const float TorusTubeDiameter = 0.1f;

        private static void BuildOrnament(Rig rig, Ornament ornament)
        {
            var c = ornament.Color;
            switch (ornament.Kind)
            {
                case OrnamentKind.EyeRings: EyeRings(rig, c); break;
                case OrnamentKind.EyePatches: EyePatches(rig, c); break;
                case OrnamentKind.BrowsAngry: Brows(rig, c, angry: true); break;
                case OrnamentKind.BrowsBushy: Brows(rig, c, angry: false); break;
                case OrnamentKind.BrowPlumes: BrowPlumes(rig, c); break;
                case OrnamentKind.ChinStrap: ChinStrap(rig, c); break;
                case OrnamentKind.EarPatches: EarPatches(rig, c); break;
                case OrnamentKind.Beard: Beard(rig, c); break;
                case OrnamentKind.Glasses: Glasses(rig, c); break;
                case OrnamentKind.SweatDrops: SweatDrops(rig, c); break;

                case OrnamentKind.CrestSpiky: CrestSpiky(rig, c); break;
                case OrnamentKind.CrestFlowing: CrestFlowing(rig, c); break;
                case OrnamentKind.CrestErect: CrestErect(rig, c); break;
                case OrnamentKind.Antenna: Antenna(rig, c); break;
                case OrnamentKind.Crown: Crown(rig, c); break;
                case OrnamentKind.Hat: Hat(rig, c); break;
                case OrnamentKind.Headband: Headband(rig, c, HeadbandHeight, tails: true); break;
                case OrnamentKind.EyeBand: Headband(rig, c, EyeBandHeight, tails: false); break;
                case OrnamentKind.Goggles: Goggles(rig, c); break;
                case OrnamentKind.IceSpikes: IceSpikes(rig, c); break;
                case OrnamentKind.WaterDrop: WaterDrop(rig, c); break;
                case OrnamentKind.Fluff: Fluff(rig, c); break;
                case OrnamentKind.Cloud: Cloud(rig, c); break;

                case OrnamentKind.NeckPatch: NeckPatch(rig, c); break;
                case OrnamentKind.ChestBand: ChestBand(rig, c, ChestBandHeight); break;
                case OrnamentKind.ChestBandLow: ChestBand(rig, c, ChestBandLowHeight); break;
                case OrnamentKind.Scarf: Scarf(rig, c); break;
                case OrnamentKind.BowTie: BowTie(rig, c); break;
                case OrnamentKind.Cape: Cape(rig, c); break;
                case OrnamentKind.Backpack: Backpack(rig, c); break;
                case OrnamentKind.Drum: Drum(rig, c); break;
                case OrnamentKind.DrumRing: DrumRing(rig, c); break;
                case OrnamentKind.WindRing: WindRing(rig, c); break;
                case OrnamentKind.Bolt: Bolt(rig, c); break;
                case OrnamentKind.Stripes: Stripes(rig, c); break;
                case OrnamentKind.Tail: Tail(rig, c); break;

                case OrnamentKind.Fan: Fan(rig, c); break;
                case OrnamentKind.Leaf: Leaf(rig, c); break;
                case OrnamentKind.Staff: Staff(rig, c); break;
            }
        }

        /// <summary>+Y がこの向きを向く回転（しずくの先を向けるときに使う）</summary>
        private static Quaternion Point(float x, float y, float z) => Quaternion.FromToRotation(Vector3.up, new Vector3(x, y, z));

        /// <summary>飾りを付けるときの部品の置き場所と、体型の倍率</summary>
        private readonly struct Rig
        {
            public readonly Parts Parts;
            public readonly PenguinLook Look;
            public readonly Transform Root;
            public readonly Transform Body;
            public readonly Transform Head;
            public readonly Transform FlipperLeft;
            public readonly Transform FlipperRight;

            public Rig(Parts parts, PenguinLook look, Transform root, Transform body, Transform head, Transform flipperLeft, Transform flipperRight)
            {
                Parts = parts;
                Look = look;
                Root = root;
                Body = body;
                Head = head;
                FlipperLeft = flipperLeft;
                FlipperRight = flipperRight;
            }

            public float W => Look.Width;
            public float H => Look.Height;
            public float S => Look.HeadSize;

            /// <summary>頭に付ける。位置と大きさは頭の大きさの倍率を掛ける前の値（頭の球の中心が (0, 0.12, 0)、半径 0.3）</summary>
            public Transform OnHead(string objName, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float gloss)
            {
                return Parts.Add(objName, mesh, Head, position * S, rotation, scale * S, color, gloss);
            }

            /// <summary>胴に付ける。位置と大きさは体型を掛けたあとの値</summary>
            public Transform OnBody(string objName, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float gloss)
            {
                return Parts.Add(objName, mesh, Body, position, rotation, scale, color, gloss);
            }

            /// <summary>胴の高さ y での、胴の横（x）と前後（z）の半径。卵形の輪郭と同じ式</summary>
            public Vector2 TorsoRadius(float y)
            {
                var half = TorsoHeight * H * 0.5f;
                var yn = Mathf.Clamp((y - TorsoCenterY * H) / half, -1f, 1f);
                var f = Mathf.Sqrt(1f - yn * yn) * (1f - TorsoBulge * yn);
                return new Vector2(BodyWidth * W * 0.5f * f, TorsoDepth * W * 0.5f * f);
            }

            /// <summary>
            /// 輪を置く。radius は管の中心の半径（x, z）、thickness は管の上下の太さ。
            /// 管の横の太さは半径に比例するので、細い輪ほど細くなる（小さい飾りが太く見えないのでちょうどよい）
            /// </summary>
            public Transform Band(string objName, Transform parent, Vector3 center, Quaternion rotation, Vector2 radius, float thickness, Color color, float gloss)
            {
                var scale = new Vector3(radius.x / TorusCenterRadius, thickness / TorusTubeDiameter, radius.y / TorusCenterRadius);
                return Parts.Add(objName, ToyMeshes.Torus, parent, center, rotation, scale, color, gloss);
            }
        }
    }
}
