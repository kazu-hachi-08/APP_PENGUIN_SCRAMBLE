using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 顔と頭の上の飾り。位置・大きさは頭の大きさの倍率を掛ける前の値で書く（頭の球の中心が (0, 0.12, 0)、半径 0.3。前は +Z）
    /// </summary>
    public static partial class PenguinModelBuilder
    {
        private static readonly Color StrapColor = new Color(0.22f, 0.20f, 0.22f);
        private static readonly Color HatBandColor = new Color(0.40f, 0.26f, 0.16f);
        private static readonly Color JewelRed = new Color(0.90f, 0.15f, 0.25f);
        private static readonly Color BoltColor = PenguinPalette.ElementColor(Element.Thunder);

        // はちまきは額（目の上の模様より上）、キガシラの帯は目の高さ（頭の球の中心からの高さ）
        private const float HeadbandHeight = 0.16f;
        private const float EyeBandHeight = 0.03f;
        // あごのひもの輪の傾きと、頭の球の中心から輪の面までの距離
        private const float ChinStrapTilt = 35f;
        private const float ChinStrapDrop = 0.18f;

        /// <summary>頭の球の表面の点（dir の向き。lift だけ外へ）</summary>
        private static Vector3 HeadSurface(Vector3 dir, float lift = 0f) => HeadCenter + dir.normalized * (HeadRadius + lift);

        /// <summary>頭の表面に沿わせる回転。+Z が toward のほう（表面に沿う向きに直す）、+Y が外を向く</summary>
        private static Quaternion AlongHead(Vector3 dir, Vector3 toward)
        {
            var normal = dir.normalized;
            var tangent = toward - normal * Vector3.Dot(toward, normal);
            return Quaternion.LookRotation(tangent, normal);
        }

        /// <summary>しずくを頭の表面から外へ生やす。根元が表面に埋まるよう、長さの4割だけ外へずらす</summary>
        private static void HeadSpike(Rig rig, string objName, Vector3 rootDir, Vector3 pointDir, Vector3 scale, Color color, float gloss)
        {
            var position = HeadSurface(rootDir) + pointDir.normalized * scale.y * 0.4f;
            rig.OnHead(objName, ToyMeshes.Drop, position, Quaternion.FromToRotation(Vector3.up, pointDir), scale, color, gloss);
        }

        /// <summary>頭に付ける小さな土台。中の部品は頭の大きさの倍率を掛ける前の値で置ける</summary>
        private static Transform HeadPivot(Rig rig, string objName, Vector3 position, Quaternion rotation)
        {
            var pivot = Pivot(objName, rig.Head, position * rig.S);
            pivot.localRotation = rotation;
            pivot.localScale = Vector3.one * rig.S;
            return pivot;
        }

        private static void EyeRings(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("EyeRing", ToyMeshes.Sphere, new Vector3(0.10f * side, 0.15f, 0.305f), Quaternion.identity, new Vector3(0.14f, 0.17f, 0.04f), c, BellyGloss);
            }
        }

        /// <summary>目の上の模様（ジェンツーの白・ケープのピンク）。外側を少し上げる</summary>
        private static void EyePatches(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("EyePatch", ToyMeshes.Sphere, new Vector3(0.11f * side, 0.27f, 0.24f), Quaternion.Euler(0f, 18f * side, 15f * side), new Vector3(0.15f, 0.08f, 0.12f), c, BellyGloss);
            }
        }

        /// <summary>怒り眉は内側を下げ、ふさふさ眉は外側を下げる。どちらも外側を顔の丸みに沿って後ろへ回す</summary>
        private static void Brows(Rig rig, Color c, bool angry)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                if (angry)
                {
                    rig.OnHead("Brow", ToyMeshes.Sphere, new Vector3(0.10f * side, 0.255f, 0.27f), Quaternion.Euler(0f, 18f * side, 22f * side), new Vector3(0.13f, 0.035f, 0.05f), c, BellyGloss);
                }
                else
                {
                    rig.OnHead("Brow", ToyMeshes.Sphere, new Vector3(0.10f * side, 0.265f, 0.26f), Quaternion.Euler(0f, 18f * side, -14f * side), new Vector3(0.16f, 0.06f, 0.07f), c, BellyGloss);
                }
            }
        }

        /// <summary>眉から頭の横へ流れる冠羽（フィヨルドランド・スネアーズ・ロイヤル）。後ろの端で羽がはねる</summary>
        private static void BrowPlumes(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var stripeDir = new Vector3(0.55f * side, 0.5f, 0.65f);
                rig.OnHead("BrowStripe", ToyMeshes.Sphere, HeadSurface(stripeDir, -0.01f), AlongHead(stripeDir, new Vector3(0.35f * side, -0.05f, -1f)), new Vector3(0.07f, 0.06f, 0.30f), c, AccessoryGloss);
                HeadSpike(rig, "Plume", new Vector3(0.8f * side, 0.3f, -0.4f), new Vector3(0.6f * side, 0.5f, -1f), new Vector3(0.11f, 0.26f, 0.11f), c, AccessoryGloss);
                HeadSpike(rig, "Plume", new Vector3(0.8f * side, 0.2f, -0.5f), new Vector3(0.8f * side, -0.1f, -0.9f), new Vector3(0.10f, 0.22f, 0.10f), c, AccessoryGloss);
            }
        }

        /// <summary>
        /// あごのひも模様。前を下げた面で頭の球を切った小さな輪にする。中心を通る大きな輪だと後ろが頭のてっぺんに出て、ヘルメットのふちに見えるため。
        /// 前はあごの下、後ろは頭の中心の高さの後ろを通る
        /// </summary>
        private static void ChinStrap(Rig rig, Color c)
        {
            var rotation = Quaternion.Euler(ChinStrapTilt, 0f, 0f);
            var center = HeadCenter - rotation * Vector3.up * ChinStrapDrop;
            var radius = Mathf.Sqrt(HeadRadius * HeadRadius - ChinStrapDrop * ChinStrapDrop) + 0.005f;
            rig.Band("ChinStrap", rig.Head, center * rig.S, rotation, Vector2.one * radius * rig.S, 0.03f * rig.S, c, BodyGloss);
        }

        private static void EarPatches(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("EarPatch", ToyMeshes.Sphere, new Vector3(0.25f * side, 0.04f, 0.12f), Quaternion.Euler(-15f, 30f * side, -15f * side), new Vector3(0.12f, 0.24f, 0.18f), c, BellyGloss);
            }
        }

        /// <summary>くちばしの下から垂れる長いひげ（仙人）。白いお腹に埋もれないよう前へ突き出す</summary>
        private static void Beard(Rig rig, Color c)
        {
            rig.OnHead("Beard", ToyMeshes.Drop, new Vector3(0f, -0.12f, 0.34f), Point(0f, -1f, 0.6f), new Vector3(0.20f, 0.36f, 0.14f), c, BellyGloss);
        }

        /// <summary>丸メガネ。目の前に輪を2つと、つなぎ</summary>
        private static void Glasses(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.Band("Glasses", rig.Head, new Vector3(0.10f * side, 0.15f, 0.36f) * rig.S, Quaternion.Euler(90f, 0f, 0f), Vector2.one * 0.085f * rig.S, 0.025f * rig.S, c, ShinyGloss);
            }

            rig.OnHead("GlassesBridge", ToyMeshes.Sphere, new Vector3(0f, 0.17f, 0.37f), Quaternion.identity, new Vector3(0.06f, 0.02f, 0.02f), c, ShinyGloss);
        }

        private static void SweatDrops(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("Sweat", ToyMeshes.Drop, new Vector3(0.28f * side, 0.27f, 0.08f), Point(0.3f * side, 1f, 0f), new Vector3(0.07f, 0.10f, 0.07f), c, ShinyGloss);
            }
        }

        /// <summary>イワトビのパンクなとげとげ。頭の上から後ろへ扇のように広げる</summary>
        private static void CrestSpiky(Rig rig, Color c)
        {
            for (var i = -2; i <= 2; i++)
            {
                HeadSpike(rig, "Crest", new Vector3(i * 0.35f, 1f, -0.35f), new Vector3(i * 0.6f, 1f, -0.6f), new Vector3(0.08f, 0.30f, 0.08f), c, AccessoryGloss);
            }
        }

        /// <summary>マカロニの冠羽。額から後ろへ流れて、両側に垂れる</summary>
        private static void CrestFlowing(Rig rig, Color c)
        {
            HeadSpike(rig, "Crest", new Vector3(0f, 0.9f, 0.4f), new Vector3(0f, 0.4f, -1f), new Vector3(0.10f, 0.30f, 0.10f), c, AccessoryGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                HeadSpike(rig, "Crest", new Vector3(0.25f * side, 0.8f, 0.55f), new Vector3(side, -0.25f, -0.8f), new Vector3(0.11f, 0.36f, 0.11f), c, AccessoryGloss);
                HeadSpike(rig, "Crest", new Vector3(0.4f * side, 0.7f, 0.2f), new Vector3(0.6f * side, 0f, -1f), new Vector3(0.09f, 0.28f, 0.09f), c, AccessoryGloss);
            }
        }

        /// <summary>シュレーターの立った冠羽。目の上から上へまっすぐ</summary>
        private static void CrestErect(Rig rig, Color c)
        {
            for (var side = -1f; side <= 1f; side += 2f)
            {
                HeadSpike(rig, "Crest", new Vector3(0.35f * side, 0.85f, 0.4f), new Vector3(0.15f * side, 1f, -0.15f), new Vector3(0.10f, 0.26f, 0.10f), c, AccessoryGloss);
            }
        }

        private static void Antenna(Rig rig, Color c)
        {
            rig.OnHead("AntennaStick", ToyMeshes.Sphere, new Vector3(0f, 0.52f, -0.03f), Quaternion.identity, new Vector3(0.035f, 0.26f, 0.035f), StrapColor, AccessoryGloss);
            rig.OnHead("AntennaBall", ToyMeshes.Sphere, new Vector3(0f, 0.66f, -0.03f), Quaternion.identity, Vector3.one * 0.11f, c, ShinyGloss);
        }

        /// <summary>王冠。頭の上の輪に5本のとがりと、前に赤い宝石</summary>
        private static void Crown(Rig rig, Color c)
        {
            var crown = HeadPivot(rig, "Crown", new Vector3(0f, 0.39f, -0.01f), Quaternion.Euler(-10f, 0f, 0f));
            rig.Parts.Add("CrownBand", ToyMeshes.Puck, crown, Vector3.zero, new Vector3(0.30f, 0.08f, 0.30f), c, ShinyGloss);
            for (var i = 0; i < 5; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                rig.Parts.Add("CrownPoint", ToyMeshes.Drop, crown, new Vector3(Mathf.Sin(angle) * 0.12f, 0.08f, Mathf.Cos(angle) * 0.12f), new Vector3(0.07f, 0.12f, 0.07f), c, ShinyGloss);
            }

            rig.Parts.Add("CrownJewel", ToyMeshes.Sphere, crown, new Vector3(0f, 0.01f, 0.15f), Vector3.one * 0.06f, JewelRed, ShinyGloss);
        }

        /// <summary>探検帽。つば・帽子の帯・てっぺん</summary>
        private static void Hat(Rig rig, Color c)
        {
            var hat = HeadPivot(rig, "Hat", new Vector3(0f, 0.36f, -0.01f), Quaternion.Euler(-12f, 0f, 0f));
            rig.Parts.Add("HatBrim", ToyMeshes.Puck, hat, Vector3.zero, new Vector3(0.66f, 0.04f, 0.66f), c, ClothGloss);
            rig.Parts.Add("HatBand", ToyMeshes.Puck, hat, new Vector3(0f, 0.05f, 0f), new Vector3(0.38f, 0.06f, 0.38f), HatBandColor, ClothGloss);
            rig.Parts.Add("HatTop", ToyMeshes.Puck, hat, new Vector3(0f, 0.10f, 0f), new Vector3(0.36f, 0.16f, 0.36f), c, ClothGloss);
        }

        /// <summary>頭を一周する帯。height は頭の球の中心からの高さ。はちまきは後ろで結んだ端を垂らす</summary>
        private static void Headband(Rig rig, Color c, float height, bool tails)
        {
            var radius = Mathf.Sqrt(HeadRadius * HeadRadius - height * height) + 0.012f;
            var tilt = tails ? -14f : 0f;
            rig.Band("Headband", rig.Head, (HeadCenter + Vector3.up * height) * rig.S, Quaternion.Euler(tilt, 0f, 0f), Vector2.one * radius * rig.S, 0.06f * rig.S, c, ClothGloss);
            if (!tails)
            {
                return;
            }

            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("HeadbandTail", ToyMeshes.Drop, new Vector3(0.05f * side, HeadCenter.y + height, -radius - 0.06f), Point(0.4f * side, -0.5f, -1f), new Vector3(0.08f, 0.22f, 0.05f), c, ClothGloss);
            }
        }

        /// <summary>おでこに上げたゴーグル。帯とレンズ2つ</summary>
        private static void Goggles(Rig rig, Color c)
        {
            rig.Band("GogglesStrap", rig.Head, (HeadCenter + Vector3.up * 0.17f) * rig.S, Quaternion.Euler(-15f, 0f, 0f), Vector2.one * 0.26f * rig.S, 0.05f * rig.S, StrapColor, ClothGloss);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var dir = new Vector3(0.35f * side, 0.75f, 0.55f);
                var rotation = Quaternion.FromToRotation(Vector3.up, dir);
                rig.OnHead("GogglesFrame", ToyMeshes.Puck, HeadSurface(dir, 0.01f), rotation, new Vector3(0.15f, 0.06f, 0.15f), StrapColor, AccessoryGloss);
                rig.OnHead("GogglesLens", ToyMeshes.Puck, HeadSurface(dir, 0.035f), rotation, new Vector3(0.12f, 0.03f, 0.12f), c, ShinyGloss);
            }
        }

        /// <summary>氷のトゲの冠（★3 のオヤブン）。まん中ほど長い</summary>
        private static void IceSpikes(Rig rig, Color c)
        {
            for (var i = -2; i <= 2; i++)
            {
                var size = 1f - 0.2f * Mathf.Abs(i);
                HeadSpike(rig, "IceSpike", new Vector3(i * 0.45f, 1f, -0.1f), new Vector3(i * 0.5f, 1f, 0f), new Vector3(0.11f, 0.32f, 0.11f) * size, c, ShinyGloss);
            }
        }

        private static void WaterDrop(Rig rig, Color c)
        {
            rig.OnHead("WaterDrop", ToyMeshes.Drop, new Vector3(0f, 0.52f, 0f), Quaternion.identity, new Vector3(0.16f, 0.22f, 0.16f), c, ShinyGloss);
        }

        /// <summary>静電気で逆立った毛</summary>
        private static void Fluff(Rig rig, Color c)
        {
            var dirs = new[]
            {
                new Vector3(0f, 1f, -0.1f), new Vector3(0.5f, 0.85f, -0.2f), new Vector3(-0.5f, 0.85f, -0.2f),
                new Vector3(0.8f, 0.4f, -0.3f), new Vector3(-0.8f, 0.4f, -0.3f), new Vector3(0f, 0.7f, -0.7f),
            };
            foreach (var dir in dirs)
            {
                HeadSpike(rig, "Fluff", dir, dir, new Vector3(0.10f, 0.18f, 0.10f), c, BodyGloss);
            }
        }

        /// <summary>頭の上に浮かぶ雷雲と、そこから落ちる小さな稲妻</summary>
        private static void Cloud(Rig rig, Color c)
        {
            var cloud = HeadPivot(rig, "Cloud", new Vector3(0f, 0.72f, -0.05f), Quaternion.identity);
            var puffs = new[]
            {
                new Vector4(0f, 0f, 0f, 0.30f), new Vector4(0.17f, -0.04f, 0f, 0.22f), new Vector4(-0.17f, -0.04f, 0f, 0.22f),
                new Vector4(0.08f, 0.07f, -0.08f, 0.22f), new Vector4(-0.07f, 0.06f, 0.09f, 0.20f),
            };
            foreach (var puff in puffs)
            {
                rig.Parts.Add("CloudPuff", ToyMeshes.Sphere, cloud, puff, Vector3.one * puff.w, c, ClothGloss);
            }

            rig.Parts.Add("CloudBolt", ToyMeshes.Drop, cloud, new Vector3(0.06f, -0.17f, 0.04f), Point(0.3f, -1f, 0f), new Vector3(0.06f, 0.14f, 0.06f), BoltColor, ShinyGloss);
            rig.Parts.Add("CloudBolt", ToyMeshes.Drop, cloud, new Vector3(-0.03f, -0.27f, 0.04f), Point(-0.3f, -1f, 0f), new Vector3(0.05f, 0.10f, 0.05f), BoltColor, ShinyGloss);
        }
    }
}
