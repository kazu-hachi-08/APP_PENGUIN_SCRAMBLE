using System;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D のペンギン1種類の見た目（色・体型・目・飾り）。見た目だけの値なので Core の SpeciesTable とは分けて持つ。
    /// 20種類の値は PenguinLooks
    /// </summary>
    public sealed class PenguinLook
    {
        public Color Back = new Color(0.17f, 0.19f, 0.27f);
        public Color Belly = new Color(0.97f, 0.97f, 0.95f);
        public Color Beak = new Color(1.00f, 0.56f, 0.14f);
        public Color Feet = new Color(1.00f, 0.50f, 0.12f);

        /// <summary>顔（目のまわり）の色。頭まで黒い種類は Back と同じにする</summary>
        public Color EyeRing = new Color(0.97f, 0.97f, 0.95f);

        /// <summary>ヒレの色。null なら背中と同じ</summary>
        public Color? Flipper;

        /// <summary>ひとみの色</summary>
        public Color Eye = new Color(0.06f, 0.06f, 0.09f);
        public EyeStyle Eyes = EyeStyle.Round;

        /// <summary>頭のてっぺんの寝ぐせ。冠羽・帽子など頭に飾りがある種類は消す</summary>
        public bool Tuft = true;

        /// <summary>胴の太さ・背の高さ・頭の大きさ（標準の体型に掛ける倍率）</summary>
        public float Width = 1f;
        public float Height = 1f;
        public float HeadSize = 1f;

        public Ornament[] Ornaments = Array.Empty<Ornament>();

        public Color FlipperColor => Flipper ?? Back;

        public static readonly PenguinLook Default = new PenguinLook();
    }

    public enum EyeStyle
    {
        Round,
        /// <summary>細い目（怒り・暑さ）</summary>
        Narrow,
        /// <summary>まぶたが半分下りた眠そうな目</summary>
        Sleepy,
    }

    /// <summary>飾りの種類。置き方は PenguinModelBuilder の Ornaments</summary>
    public enum OrnamentKind
    {
        // 顔
        EyeRings,
        EyePatches,
        BrowsAngry,
        BrowsBushy,
        BrowPlumes,
        ChinStrap,
        EarPatches,
        Beard,
        Glasses,
        SweatDrops,

        // 頭の上
        CrestSpiky,
        CrestFlowing,
        CrestErect,
        Antenna,
        Crown,
        Hat,
        Headband,
        EyeBand,
        Goggles,
        IceSpikes,
        WaterDrop,
        Fluff,
        Cloud,

        // 体
        NeckPatch,
        ChestBand,
        ChestBandLow,
        Scarf,
        BowTie,
        Cape,
        Backpack,
        Drum,
        DrumRing,
        WindRing,
        Bolt,
        Stripes,
        Tail,

        // 持ち物
        Fan,
        Leaf,
        Staff,
    }

    /// <summary>飾り1つ。色は飾りのいちばん目立つ部分の色（金具・宝石などの小さな部分の色は組み立て役が決める）</summary>
    public readonly struct Ornament
    {
        public readonly OrnamentKind Kind;
        public readonly Color Color;

        public Ornament(OrnamentKind kind, Color color)
        {
            Kind = kind;
            Color = color;
        }
    }
}
