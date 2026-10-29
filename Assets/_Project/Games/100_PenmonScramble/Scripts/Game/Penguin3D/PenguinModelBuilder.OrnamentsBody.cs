using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 体の飾りと持ち物。位置は体型の倍率（太さ W・背の高さ H）を掛けた値で書き、胴の表面の位置は胴の卵形から求める
    /// </summary>
    public static partial class PenguinModelBuilder
    {
        private static readonly Color FurWhite = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color DrumHead = new Color(0.95f, 0.90f, 0.78f);
        private static readonly Color JewelBlue = new Color(0.25f, 0.55f, 1.00f);
        private static readonly Color WoodColor = new Color(0.55f, 0.36f, 0.20f);
        private static readonly Color OrbGreen = new Color(0.45f, 0.90f, 0.50f);
        private static readonly Color RingGold = new Color(1.00f, 0.82f, 0.25f);

        // 胸の帯の高さ（背の高さに対する割合）。マゼランは2本
        private const float ChestBandHeight = 0.60f;
        private const float ChestBandLowHeight = 0.48f;
        // 首（マフラー・マントのえり・蝶ネクタイ）の高さ
        private const float NeckHeight = 0.70f;

        // 白いお腹の卵形（Torso と同じく体型の倍率を掛ける前）
        private const float BellyCenterY = 0.38f;
        private const float BellyHalfHeight = 0.31f;
        private const float BellyCenterZ = 0.14f;
        private const float BellyHalfDepth = 0.23f;

        /// <summary>お腹の前の面の z（高さ y で）</summary>
        private static float BellyFront(Rig rig, float y)
        {
            var yn = Mathf.Clamp((y - BellyCenterY * rig.H) / (BellyHalfHeight * rig.H), -1f, 1f);
            var f = Mathf.Sqrt(1f - yn * yn) * (1f - TorsoBulge * yn);
            return (BellyCenterZ + BellyHalfDepth * f) * rig.W;
        }

        /// <summary>胴を一周する輪。胴の表面に少し浮かせて巻く</summary>
        private static void BodyBand(Rig rig, string objName, float y, float lift, float thickness, Quaternion rotation, Color c, float gloss)
        {
            var radius = rig.TorsoRadius(y) + Vector2.one * lift;
            rig.Band(objName, rig.Body, new Vector3(0f, y, 0f), rotation, radius, thickness, c, gloss);
        }

        /// <summary>のどの模様（キング・コウテイの黄色）</summary>
        private static void NeckPatch(Rig rig, Color c)
        {
            // 頭のすぐ下だとあごに隠れるので、お腹の上の端にかぶせる
            var y = 0.60f * rig.H;
            rig.OnBody("NeckPatch", ToyMeshes.Sphere, new Vector3(0f, y, rig.TorsoRadius(y).y * 0.92f), Quaternion.identity, new Vector3(0.42f * rig.W, 0.20f * rig.H, 0.16f * rig.W), c, BellyGloss);
        }

        /// <summary>胸の帯。背中は黒いので、胴を一周させても前の白いところにだけ見える</summary>
        private static void ChestBand(Rig rig, Color c, float height)
        {
            BodyBand(rig, "ChestBand", height * rig.H, 0.005f, 0.05f * rig.H, Quaternion.identity, c, BellyGloss);
        }

        /// <summary>首に巻いたマフラー。前の横に結び目と、垂れた端</summary>
        private static void Scarf(Rig rig, Color c)
        {
            var y = NeckHeight * rig.H;
            var radius = rig.TorsoRadius(y) + Vector2.one * 0.02f;
            BodyBand(rig, "Scarf", y, 0.02f, 0.08f * rig.H, Quaternion.identity, c, ClothGloss);
            rig.OnBody("ScarfKnot", ToyMeshes.Sphere, new Vector3(0.10f * rig.W, y - 0.02f, radius.y * 0.95f), Quaternion.identity, Vector3.one * 0.09f, c, ClothGloss);
            rig.OnBody("ScarfEnd", ToyMeshes.Drop, new Vector3(0.13f * rig.W, y - 0.12f * rig.H, radius.y * 0.9f), Point(0.3f, -1f, 0.25f), new Vector3(0.10f, 0.20f, 0.06f), c, ClothGloss);
        }

        /// <summary>蝶ネクタイ。しずくの太いほうを外へ向けて2つ並べ、まん中に宝石</summary>
        private static void BowTie(Rig rig, Color c)
        {
            var y = 0.62f * rig.H;
            var z = rig.TorsoRadius(y).y + 0.03f;
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnBody("BowTie", ToyMeshes.Drop, new Vector3(0.08f * side, y, z), Point(-side, 0f, 0f), new Vector3(0.12f, 0.14f, 0.07f), c, ShinyGloss);
            }

            rig.OnBody("BowTieJewel", ToyMeshes.Sphere, new Vector3(0f, y, z + 0.02f), Quaternion.identity, Vector3.one * 0.06f, JewelBlue, ShinyGloss);
        }

        /// <summary>背中のマント（すそを後ろへ広げる）と、白い毛皮のえり</summary>
        private static void Cape(Rig rig, Color c)
        {
            rig.OnBody("Cape", ToyMeshes.Egg, new Vector3(0f, 0.46f * rig.H, -0.22f * rig.W), Quaternion.Euler(8f, 0f, 0f), new Vector3(0.84f * rig.W, 0.86f * rig.H, 0.34f * rig.W), c, ClothGloss);
            BodyBand(rig, "CapeCollar", NeckHeight * rig.H, 0.03f, 0.09f * rig.H, Quaternion.identity, FurWhite, ClothGloss);
        }

        private static void Backpack(Rig rig, Color c)
        {
            var y = 0.46f * rig.H;
            var back = rig.TorsoRadius(y).y;
            rig.OnBody("Backpack", ToyMeshes.Puck, new Vector3(0f, y, -back - 0.06f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.42f * rig.W, 0.16f * rig.W, 0.44f * rig.H), c, ClothGloss);
            rig.OnBody("BackpackFlap", ToyMeshes.Sphere, new Vector3(0f, y + 0.10f * rig.H, -back - 0.13f), Quaternion.identity, new Vector3(0.40f * rig.W, 0.14f * rig.H, 0.08f), Color.Lerp(c, Color.black, 0.2f), ClothGloss);
            var strapY = 0.58f * rig.H;
            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnBody("BackpackStrap", ToyMeshes.Sphere, new Vector3(0.15f * rig.W * side, strapY, rig.TorsoRadius(strapY).y * 0.95f), Quaternion.Euler(0f, 0f, -8f * side), new Vector3(0.05f * rig.W, 0.30f * rig.H, 0.04f), HatBandColor, ClothGloss);
            }
        }

        /// <summary>おなかに抱えた太鼓。前を向けた円柱と、明るい皮</summary>
        private static void Drum(Rig rig, Color c)
        {
            var y = BellyCenterY * rig.H;
            var z = BellyFront(rig, y) + 0.03f * rig.W;
            rig.OnBody("Drum", ToyMeshes.Puck, new Vector3(0f, y, z), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.34f * rig.W, 0.12f * rig.W, 0.34f * rig.W), c, AccessoryGloss);
            rig.OnBody("DrumHead", ToyMeshes.Puck, new Vector3(0f, y, z + 0.06f * rig.W), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.28f * rig.W, 0.02f * rig.W, 0.28f * rig.W), DrumHead, BellyGloss);
        }

        /// <summary>雷神の太鼓の輪。背中の後ろに立てた金の輪と、上半分に並ぶ太鼓</summary>
        private static void DrumRing(Rig rig, Color c)
        {
            var center = new Vector3(0f, 0.68f * rig.H, -0.42f * rig.W);
            var radius = 0.40f * rig.W;
            rig.Band("DrumRingFrame", rig.Body, center, Quaternion.Euler(90f, 0f, 0f), Vector2.one * radius, 0.04f, RingGold, ShinyGloss);
            for (var i = -2; i <= 2; i++)
            {
                var angle = i * 45f * Mathf.Deg2Rad;
                var position = center + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius;
                rig.OnBody("RingDrum", ToyMeshes.Puck, position, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.16f), c, AccessoryGloss);
            }
        }

        /// <summary>体のまわりを回る風の輪（2本を逆に傾ける）</summary>
        private static void WindRing(Rig rig, Color c)
        {
            rig.Band("WindRing", rig.Body, new Vector3(0f, 0.34f * rig.H, 0f), Quaternion.Euler(10f, 0f, -12f), new Vector2(0.45f, 0.42f) * rig.W, 0.04f, c, AccessoryGloss);
            rig.Band("WindRing", rig.Body, new Vector3(0f, 0.58f * rig.H, 0f), Quaternion.Euler(-8f, 0f, 10f), new Vector2(0.39f, 0.36f) * rig.W, 0.03f, c, AccessoryGloss);
        }

        /// <summary>おなかの稲妻マーク。斜めの線・短い横線・下へとがる線の3つで「く」の字を2回</summary>
        private static void Bolt(Rig rig, Color c)
        {
            BellyStroke(rig, "Bolt", ToyMeshes.Sphere, 0.03f, 0.47f, Quaternion.Euler(0f, 0f, -25f), new Vector3(0.05f, 0.14f, 0.04f), c);
            BellyStroke(rig, "Bolt", ToyMeshes.Sphere, 0.00f, 0.415f, Quaternion.Euler(0f, 0f, 70f), new Vector3(0.05f, 0.11f, 0.04f), c);
            BellyStroke(rig, "Bolt", ToyMeshes.Drop, -0.02f, 0.35f, Point(-0.45f, -1f, 0f), new Vector3(0.07f, 0.15f, 0.04f), c);
        }

        private static void BellyStroke(Rig rig, string objName, Mesh mesh, float x, float y, Quaternion rotation, Vector3 scale, Color c)
        {
            var py = y * rig.H;
            rig.OnBody(objName, mesh, new Vector3(x * rig.W, py, BellyFront(rig, py)), rotation, scale, c, ShinyGloss);
        }

        /// <summary>電気うなぎのしま。背中に下向きの山形を3段と、頭の横に線</summary>
        private static void Stripes(Rig rig, Color c)
        {
            foreach (var height in new[] { 0.30f, 0.46f, 0.62f })
            {
                var y = height * rig.H;
                var back = rig.TorsoRadius(y).y * 0.97f;
                for (var side = -1f; side <= 1f; side += 2f)
                {
                    rig.OnBody("Stripe", ToyMeshes.Sphere, new Vector3(0.08f * rig.W * side, y, -back), Quaternion.Euler(0f, 0f, -40f * side), new Vector3(0.05f * rig.W, 0.20f * rig.H, 0.05f), c, ShinyGloss);
                }
            }

            for (var side = -1f; side <= 1f; side += 2f)
            {
                rig.OnHead("Stripe", ToyMeshes.Sphere, new Vector3(0.27f * side, 0.18f, -0.05f), Quaternion.Euler(0f, 0f, 30f * side), new Vector3(0.05f, 0.16f, 0.09f), c, ShinyGloss);
            }
        }

        private static void Tail(Rig rig, Color c)
        {
            var y = 0.16f * rig.H;
            rig.OnBody("Tail", ToyMeshes.Drop, new Vector3(0f, y, -rig.TorsoRadius(y).y - 0.08f), Point(0f, 0.35f, -1f), new Vector3(0.14f, 0.32f, 0.14f), c, ShinyGloss);
        }

        /// <summary>右のヒレの先で持つうちわ（ヒレと一緒に動く）</summary>
        private static void Fan(Rig rig, Color c)
        {
            rig.Parts.Add("FanHandle", ToyMeshes.Sphere, rig.FlipperRight, new Vector3(0f, -0.38f * rig.H, 0.04f), new Vector3(0.03f, 0.12f, 0.03f), WoodColor, AccessoryGloss);
            rig.Parts.Add("Fan", ToyMeshes.Puck, rig.FlipperRight, new Vector3(0f, -0.48f * rig.H, 0.08f), Quaternion.Euler(80f, 0f, 0f), new Vector3(0.30f, 0.025f, 0.28f), c, AccessoryGloss);
        }

        /// <summary>左のヒレで持つ大きな葉っぱ（人見知りで隠れる）</summary>
        private static void Leaf(Rig rig, Color c)
        {
            var position = new Vector3(0.02f, -0.30f * rig.H, 0.16f);
            var rotation = Quaternion.Euler(-20f, 0f, 0f);
            rig.Parts.Add("Leaf", ToyMeshes.Drop, rig.FlipperLeft, position, rotation, new Vector3(0.30f, 0.50f, 0.03f), c, AccessoryGloss);
            rig.Parts.Add("LeafVein", ToyMeshes.Sphere, rig.FlipperLeft, position + rotation * new Vector3(0f, 0f, 0.012f), rotation, new Vector3(0.02f, 0.42f, 0.02f), Color.Lerp(c, Color.black, 0.3f), AccessoryGloss);
        }

        /// <summary>仙人の杖。根元に付けて、歩いても揺れないようにする。先に緑の玉</summary>
        private static void Staff(Rig rig, Color c)
        {
            var x = 0.46f * rig.W;
            rig.Parts.Add("Staff", ToyMeshes.Sphere, rig.Root, new Vector3(x, 0.50f * rig.H, 0.10f), new Vector3(0.06f, 1.0f * rig.H, 0.06f), c, AccessoryGloss);
            rig.Parts.Add("StaffOrb", ToyMeshes.Sphere, rig.Root, new Vector3(x, 1.02f * rig.H, 0.10f), Vector3.one * 0.14f, OrbGreen, ShinyGloss);
        }
    }
}
