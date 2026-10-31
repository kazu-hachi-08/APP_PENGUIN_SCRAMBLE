using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ガチャ画面（§7）と、タイトルの上に開く画面の共通の下地。タイトルのロビーは PenmonScrambleSceneBuilder.TitleLobby.cs、ペンモン画面は PenmonScrambleSceneBuilder.PenmonScreen.cs
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color ScreenBackColor = new Color(0.10f, 0.16f, 0.28f);
        private static readonly Color GachaColor = new Color(0.95f, 0.40f, 0.60f);
        private static readonly Color PenguinsColor = new Color(0.40f, 0.70f, 0.45f);
        private static readonly Color ArrowColor = new Color(0.30f, 0.38f, 0.55f);
        private static readonly Color CardFrameColor = new Color(0.20f, 0.26f, 0.40f);
        private static readonly Color GlowColor = new Color(1f, 1f, 1f, 0.35f);

        private const int MenuFontSize = 44;
        private const int SmallFontSize = 36;

        // コインの表示（ガチャ・ペンモン画面の右上）
        private static readonly Vector2 CoinPosition = new Vector2(680f, 480f);
        private static readonly Vector2 CoinSize = new Vector2(480f, 70f);
        // ◀ ▶（遊び方のページ送り）
        private static readonly Vector2 StageArrowSize = new Vector2(110f, 90f);

        // ガチャ画面
        private static readonly Vector2 ScreenTitlePosition = new Vector2(0f, 460f);
        private static readonly Vector2 ScreenTitleSize = new Vector2(800f, 100f);
        private const int ScreenTitleFontSize = 64;
        private static readonly Vector2 CapsulePosition = new Vector2(-200f, 80f);
        private const float CapsuleSize = 320f;
        private const float GlowSize = 560f;
        private static readonly Vector2 StarsPosition = new Vector2(-200f, -170f);
        private static readonly Vector2 StarsSize = new Vector2(600f, 120f);
        private const int StarsFontSize = 100;
        private static readonly Vector2 NamePosition = new Vector2(-200f, -300f);
        private static readonly Vector2 NameSize = new Vector2(1100f, 140f);
        private static readonly Vector2 InfoPosition = new Vector2(0f, -440f);
        private static readonly Vector2 InfoSize = new Vector2(1400f, 70f);
        private static readonly Vector2 PullPosition = new Vector2(600f, 40f);
        private static readonly Vector2 PullSize = new Vector2(460f, 200f);
        private static readonly Vector2 BackPosition = new Vector2(-760f, 460f);
        private static readonly Vector2 BackSize = new Vector2(260f, 100f);

        /// <summary>画面全体をふさぐ下地（後ろのタイトルを押せないように）と、上の見出し</summary>
        private static GameObject CreateScreen(Transform canvas, string name, string heading)
        {
            var root = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = ScreenBackColor;
            CreateLabel(root.transform, "Heading", heading, ScreenTitleFontSize, ScreenTitlePosition, ScreenTitleSize).fontStyle = FontStyle.Bold;
            return root;
        }

        private static GachaScreen CreateGachaScreen(Transform canvas)
        {
            var root = CreateScreen(canvas, "Gacha", "ガチャ");
            var coin = CreateLabel(root.transform, "Coins", string.Empty, MenuFontSize, CoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;
            var glow = CreateCircle(root.transform, "Glow", CapsulePosition, GlowSize, GlowColor);
            var capsule = CreateCircle(root.transform, "Capsule", CapsulePosition, CapsuleSize, Color.white);
            var penguin = CreateCircle(root.transform, "Penguin", CapsulePosition, CapsuleSize, Color.white);
            var stars = CreateLabel(root.transform, "Stars", string.Empty, StarsFontSize, StarsPosition, StarsSize);
            var name = CreateLabel(root.transform, "Name", string.Empty, MenuFontSize, NamePosition, NameSize);
            var info = CreateLabel(root.transform, "Info", string.Empty, SmallFontSize, InfoPosition, InfoSize);
            var pull = CreateTitleButton(root.transform, "Pull", string.Empty, PullPosition, PullSize, GachaColor, CardFontSize);
            var back = CreateTitleButton(root.transform, "Back", "もどる", BackPosition, BackSize, CancelColor, MenuFontSize);

            var screen = root.AddComponent<GachaScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_infoText").objectReferenceValue = info;
            so.FindProperty("_pullButton").objectReferenceValue = pull;
            so.FindProperty("_pullLabel").objectReferenceValue = pull.GetComponentInChildren<Text>();
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_capsule").objectReferenceValue = capsule.rectTransform;
            so.FindProperty("_capsuleImage").objectReferenceValue = capsule;
            so.FindProperty("_glow").objectReferenceValue = glow;
            so.FindProperty("_starsText").objectReferenceValue = stars;
            so.FindProperty("_penguin").objectReferenceValue = penguin.rectTransform;
            so.FindProperty("_penguinImage").objectReferenceValue = penguin;
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        /// <summary>丸い絵（ペンモンの顔・カプセル・光）。下のボタンを押す邪魔をしない</summary>
        private static Image CreateCircle(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, Vector2.one * size);
            var image = obj.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
