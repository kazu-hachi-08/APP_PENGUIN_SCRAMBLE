using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 20種類のペンギンの見た目の定義表（番号は §5.3 の No）。題材のペンギンの模様と個性（§5.3）を飾りで見せ、
    /// 属性（§5.2）は飾りの色で分かるようにする: 氷は水色、水は青、風は緑と黄色の冠羽、雷は黄色
    /// </summary>
    public static class PenguinLooks
    {
        private static readonly Color White = Hex(0xF7F7F2);
        private static readonly Color Dark = Hex(0x1F2330);
        private static readonly Color Orange = Hex(0xFF8F24);
        private static readonly Color Gold = Hex(0xFFD23F);
        private static readonly Color Yellow = Hex(0xFFD633);
        private static readonly Color PinkFeet = Hex(0xE89AA0);

        private static readonly Color Ice = PenguinPalette.ElementColor(Element.Ice);
        private static readonly Color Water = PenguinPalette.ElementColor(Element.Water);
        private static readonly Color Wind = PenguinPalette.ElementColor(Element.Wind);
        private static readonly Color Thunder = PenguinPalette.ElementColor(Element.Thunder);

        private static readonly PenguinLook[] All =
        {
            // ---- 氷 ----
            // 1 アデリン: 頭まで黒く、白いアイリング。負けずぎらいのツッパリなので怒り眉と細い目
            new PenguinLook
            {
                Back = Hex(0x1F2433), EyeRing = Hex(0x1F2433), Beak = Hex(0xE0573A), Eyes = EyeStyle.Narrow,
                Ornaments = O((OrnamentKind.EyeRings, White), (OrnamentKind.BrowsAngry, White), (OrnamentKind.Scarf, Ice)),
            },
            // 2 ヒゲジイ: あごのひも模様・丸メガネ・白い眉のおじいちゃん。少し小さく丸い
            new PenguinLook
            {
                Back = Hex(0x3A4152), Beak = Hex(0x3B3B45), Feet = PinkFeet, Tuft = false, Width = 1.05f, Height = 0.94f,
                Ornaments = O((OrnamentKind.ChinStrap, Dark), (OrnamentKind.Glasses, Hex(0xC9A640)), (OrnamentKind.BrowsBushy, Hex(0xB8BEC8))),
            },
            // 3 ジェンツ: 目の上の白い模様・まっ赤なくちばし・速さ自慢のはちまき。細身で背が高い
            new PenguinLook
            {
                Back = Hex(0x24324A), EyeRing = Hex(0x24324A), Beak = Hex(0xFF5A1A), Feet = Hex(0xFF6A1A), Tuft = false, Width = 0.9f, Height = 1.06f,
                Ornaments = O((OrnamentKind.EyePatches, White), (OrnamentKind.Headband, Ice)),
            },
            // 4 キングス: 金色の耳の模様と胸・王冠・赤いマント。背が高い
            new PenguinLook
            {
                Back = Hex(0x2E3440), EyeRing = Hex(0x2E3440), Beak = Hex(0xF27A3A), Feet = Hex(0x4A4A55), Tuft = false, Width = 0.95f, Height = 1.15f, HeadSize = 0.95f,
                Ornaments = O((OrnamentKind.EarPatches, Hex(0xFFA23A)), (OrnamentKind.NeckPatch, Hex(0xFFC24A)), (OrnamentKind.Crown, Gold), (OrnamentKind.Cape, Hex(0xD8343A))),
            },
            // 5 オヤブン（★3）: コウテイペンギン。いちばん大きくどっしり。氷のトゲの冠とマフラー
            new PenguinLook
            {
                Back = Hex(0x1E2638), EyeRing = Hex(0x1E2638), Beak = Hex(0xF29A4A), Feet = Hex(0x4A4A55), Tuft = false, Width = 1.25f, Height = 1.15f, HeadSize = 1.05f,
                Ornaments = O((OrnamentKind.EarPatches, Hex(0xFFE07A)), (OrnamentKind.NeckPatch, Hex(0xFFE38A)), (OrnamentKind.IceSpikes, Hex(0xAEEBFF)), (OrnamentKind.Scarf, Ice)),
            },

            // ---- 水 ----
            // 6 フンボ: 胸の黒い帯・眠そうな目・頭に水のしずく。のんびりで少し太め
            new PenguinLook
            {
                Back = Hex(0x2A3550), Feet = Hex(0x3A3A44), Eyes = EyeStyle.Sleepy, Tuft = false, Width = 1.08f,
                Ornaments = O((OrnamentKind.ChestBand, Dark), (OrnamentKind.WaterDrop, Water)),
            },
            // 7 ケープン: 胸の帯・目の上のピンク・青いスカーフのおしゃべり
            new PenguinLook
            {
                Back = Hex(0x2A3346), Feet = Hex(0x3A3A44),
                Ornaments = O((OrnamentKind.EyePatches, Hex(0xF49AB0)), (OrnamentKind.ChestBand, Dark), (OrnamentKind.Scarf, Water)),
            },
            // 8 マゼラ: 胸の帯が2本・探検帽・青いリュックの冒険家
            new PenguinLook
            {
                Back = Hex(0x2D3243), Feet = Hex(0x3A3A44), Tuft = false,
                Ornaments = O((OrnamentKind.ChestBand, Dark), (OrnamentKind.ChestBandLow, Dark), (OrnamentKind.Hat, Hex(0xC9A86A)), (OrnamentKind.Backpack, Water)),
            },
            // 9 ガラパ: 小さい・暑くて目を細め、汗をかいてうちわを持つ
            new PenguinLook
            {
                Back = Hex(0x2B3A55), Feet = Hex(0x3A3A44), Eyes = EyeStyle.Narrow, Width = 0.86f, Height = 0.86f, HeadSize = 1.05f,
                Ornaments = O((OrnamentKind.ChestBand, Dark), (OrnamentKind.SweatDrops, Hex(0xBFE8FF)), (OrnamentKind.Fan, Water)),
            },
            // 10 ロワイヤル（★3）: 白い顔・金の眉の冠羽・金の蝶ネクタイのおぼっちゃま
            new PenguinLook
            {
                Back = Hex(0x3B4660), Beak = Hex(0xF2A07A), Feet = PinkFeet, Tuft = false, Width = 1.05f, Height = 1.05f,
                Ornaments = O((OrnamentKind.BrowPlumes, Hex(0xFFC93A)), (OrnamentKind.BowTie, Gold), (OrnamentKind.WaterDrop, Water)),
            },

            // ---- 風 ----
            // 11 イワトビ: 黄色いとげとげの冠羽・赤い目・怒り眉のパンク。小柄
            new PenguinLook
            {
                Back = Hex(0x2A2E38), EyeRing = Hex(0x2A2E38), Beak = Hex(0xE0472E), Feet = PinkFeet, Eye = Hex(0xD8303A), Eyes = EyeStyle.Narrow, Tuft = false, Width = 0.92f,
                Ornaments = O((OrnamentKind.CrestSpiky, Yellow), (OrnamentKind.BrowsAngry, Yellow), (OrnamentKind.Scarf, Wind)),
            },
            // 12 フィヨル: 黄色い眉の冠羽・葉っぱに隠れる人見知り
            new PenguinLook
            {
                Back = Hex(0x2E3A3A), EyeRing = Hex(0x2E3A3A), Beak = Hex(0xE0703A), Feet = PinkFeet, Tuft = false,
                Ornaments = O((OrnamentKind.BrowPlumes, Yellow), (OrnamentKind.Leaf, Wind)),
            },
            // 13 スネア: 黄色い眉の冠羽・おなかに緑の太鼓
            new PenguinLook
            {
                Back = Hex(0x2C3640), EyeRing = Hex(0x2C3640), Beak = Hex(0xE0703A), Feet = PinkFeet, Tuft = false,
                Ornaments = O((OrnamentKind.BrowPlumes, Hex(0xFFE14D)), (OrnamentKind.Drum, Wind)),
            },
            // 14 マカロ: 額から流れるオレンジの冠羽・腰のまわりの風の輪のダンサー
            new PenguinLook
            {
                Back = Hex(0x2A2F3A), EyeRing = Hex(0x2A2F3A), Beak = Hex(0xE0603A), Feet = PinkFeet, Tuft = false, Width = 0.95f,
                Ornaments = O((OrnamentKind.CrestFlowing, Hex(0xFFA530)), (OrnamentKind.WindRing, Wind)),
            },
            // 15 キガシラ（★3）: 頭をめぐる淡い黄色の帯・金の目・白いひげと杖の仙人
            new PenguinLook
            {
                Back = Hex(0x4A5560), EyeRing = Hex(0xF2EAC0), Beak = Hex(0xE89A6A), Feet = PinkFeet, Eye = Hex(0xC8901A), Tuft = false,
                Ornaments = O((OrnamentKind.EyeBand, Hex(0xF5E27A)), (OrnamentKind.BrowsBushy, White), (OrnamentKind.Beard, Hex(0xE6E8EE)), (OrnamentKind.Staff, Hex(0x8A5A33))),
            },

            // ---- 雷 ----
            // 16 コガタン: 世界一小さい青いペンギン。静電気で毛が逆立ち、おなかに稲妻
            new PenguinLook
            {
                Back = Hex(0x4A7BC4), EyeRing = Hex(0x4A7BC4), Belly = Hex(0xF0F4FF), Beak = Hex(0x3B3B45), Feet = PinkFeet, Tuft = false,
                Width = 0.8f, Height = 0.8f, HeadSize = 1.12f,
                Ornaments = O((OrnamentKind.Fluff, Hex(0x4A7BC4)), (OrnamentKind.Bolt, Thunder)),
            },
            // 17 ハネジロ: 白いヒレ・頭にゴーグルの発明家
            new PenguinLook
            {
                Back = Hex(0x5A6E9A), Flipper = White, Beak = Hex(0x3B3B45), Feet = PinkFeet,
                Ornaments = O((OrnamentKind.Goggles, Thunder), (OrnamentKind.Bolt, Thunder)),
            },
            // 18 シュレタ: 立った黄色い冠羽・アンテナのように光る玉
            new PenguinLook
            {
                Back = Hex(0x30343F), EyeRing = Hex(0x30343F), Beak = Hex(0xE0703A), Feet = PinkFeet, Tuft = false,
                Ornaments = O((OrnamentKind.CrestErect, Yellow), (OrnamentKind.Antenna, Thunder)),
            },
            // 19 エレペン: 電気うなぎのような黄色いしま模様・黄色いヒレ・しっぽ
            new PenguinLook
            {
                Back = Hex(0x1F3A4A), EyeRing = Hex(0x1F3A4A), Flipper = Thunder, Belly = Hex(0xFFF6C8), Beak = Thunder, Feet = Thunder, Eye = Hex(0x1A6A8A),
                Ornaments = O((OrnamentKind.EyeRings, Thunder), (OrnamentKind.Stripes, Thunder), (OrnamentKind.Tail, Thunder)),
            },
            // 20 ライジン（★3）: 頭の上に雷雲・背中に太鼓の輪・金の目の雷の神さまの子ども
            new PenguinLook
            {
                Back = Hex(0x3A3550), EyeRing = Hex(0x3A3550), Beak = Hex(0xFFB02E), Feet = Hex(0xFFB02E), Eye = Hex(0xE0A010), Tuft = false, Width = 1.1f, Height = 1.1f,
                Ornaments = O((OrnamentKind.EyeRings, Gold), (OrnamentKind.Cloud, Hex(0x8A90A0)), (OrnamentKind.DrumRing, Hex(0xD84A3A))),
            },
        };

        public static int Count => All.Length;

        /// <summary>番号（1〜20）から引く。ペンギン以外の番号（大ボスなど）や範囲外は標準の見た目</summary>
        public static PenguinLook Get(int speciesNo)
        {
            return Has(speciesNo) ? All[speciesNo - 1] : PenguinLook.Default;
        }

        public static bool Has(int speciesNo) => speciesNo >= 1 && speciesNo <= All.Length;

        private static Ornament[] O(params (OrnamentKind kind, Color color)[] ornaments)
        {
            var result = new Ornament[ornaments.Length];
            for (var i = 0; i < ornaments.Length; i++)
            {
                result[i] = new Ornament(ornaments[i].kind, ornaments[i].color);
            }

            return result;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }
}
