using PenmonScramble.Common.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルの上に開く画面の共通の下地。タイトルのロビーは PenmonScrambleSceneBuilder.TitleLobby.cs、
    /// ガチャ画面は PenmonScrambleSceneBuilder.Gacha.cs、ペンモン画面は PenmonScrambleSceneBuilder.PenmonScreen.cs
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color ScreenBackColor = new Color(0.10f, 0.16f, 0.28f);
        private static readonly Color GachaColor = new Color(0.95f, 0.40f, 0.60f);
        private static readonly Color PenguinsColor = new Color(0.40f, 0.70f, 0.45f);
        private static readonly Color ArrowColor = new Color(0.30f, 0.38f, 0.55f);
        private static readonly Color CardFrameColor = new Color(0.20f, 0.26f, 0.40f);

        private const int MenuFontSize = 44;
        private const int SmallFontSize = 36;

        // コインの表示（ガチャ・ペンモン画面の右上）
        private static readonly Vector2 CoinPosition = new Vector2(680f, 480f);
        private static readonly Vector2 CoinSize = new Vector2(480f, 70f);
        // ◀ ▶（遊び方のページ送り）
        private static readonly Vector2 StageArrowSize = new Vector2(110f, 90f);

        // 画面の見出しと、遊び方の画面の「もどる」
        private static readonly Vector2 ScreenTitlePosition = new Vector2(0f, 460f);
        private static readonly Vector2 ScreenTitleSize = new Vector2(800f, 100f);
        private const int ScreenTitleFontSize = 64;
        private static readonly Vector2 BackPosition = new Vector2(-760f, 460f);
        private static readonly Vector2 BackSize = new Vector2(260f, 100f);

        /// <summary>画面全体をふさぐ下地（後ろのタイトルを押せないように）と、上の見出し</summary>
        private static GameObject CreateScreen(Transform canvas, string name, string heading)
        {
            var root = CreateFullScreenBacking(canvas, name, ScreenBackColor);
            CreateLabel(root.transform, "Heading", heading, ScreenTitleFontSize, ScreenTitlePosition, ScreenTitleSize).fontStyle = FontStyle.Bold;
            return root;
        }

        /// <summary>画面全体を覆う下地。Image で受けるので、後ろの物は押せなくなる（色が透明でも同じ）</summary>
        private static GameObject CreateFullScreenBacking(Transform parent, string name, Color color)
        {
            var obj = CreateStretchGroup(parent, name);
            obj.AddComponent<Image>().color = color;
            return obj;
        }

        /// <summary>丸い絵（ペンモンの顔・ボタンの丸）。下のボタンを押す邪魔をしない</summary>
        private static Image CreateCircle(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, Vector2.one * size);
            return AddImage(obj, AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath), color, false);
        }
    }
}
