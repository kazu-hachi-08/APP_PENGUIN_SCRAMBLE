using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// みんなでのときだけ使う HUD: 人が動かしている子の頭の上の名前（SoccerNameTags。HUD のほかの物より奥に置く）と、
    /// 結果の左に出す MVP の板（点を一番入れた人の名前と点）。名前の文字と色は試合中に SoccerNameTags・SoccerHud が入れる
    /// </summary>
    public static partial class SoccerSceneBuilder
    {
        private static readonly Vector2 NameTagSize = new Vector2(360f, 44f);
        private const int NameTagFontSize = 30;
        private const int NameTagMinFontSize = 18;

        private static readonly Vector2 MvpPosition = new Vector2(-500f, 60f);
        private static readonly Vector2 MvpSize = new Vector2(600f, 360f);
        private static readonly Vector2 MvpHeadingPosition = new Vector2(0f, 110f);
        private static readonly Vector2 MvpHeadingSize = new Vector2(560f, 100f);
        private const int MvpHeadingFontSize = 84;
        private static readonly Color MvpHeadingColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Vector2 MvpBandPosition = new Vector2(0f, -5f);
        private static readonly Vector2 MvpBandSize = new Vector2(540f, 96f);
        private static readonly Vector2 MvpNameSize = new Vector2(520f, 90f);
        private const int MvpNameFontSize = 52;
        private static readonly Vector2 MvpGoalsPosition = new Vector2(0f, -115f);
        private static readonly Vector2 MvpGoalsSize = new Vector2(540f, 70f);
        private const int MvpGoalsFontSize = 44;

        /// <summary>頭の上の名前の入れ物（画面いっぱい）と、席の数だけ写す見本の文字</summary>
        private static SoccerNameTags CreateNameTags(Transform hudRoot)
        {
            var group = B.CreateStretchGroup(hudRoot, "NameTags");
            var template = B.CreateLabel(group.transform, "NameTemplate", string.Empty, NameTagFontSize, Vector2.zero, NameTagSize);
            template.fontStyle = FontStyle.Bold;
            template.raycastTarget = false;
            // 長い名前は横に広がりすぎないよう小さくする（本編の頭の上の名前と同じ考え。縮めるかは枠の高さで決まるので折り返しを許す）
            template.horizontalOverflow = HorizontalWrapMode.Wrap;
            template.resizeTextForBestFit = true;
            template.resizeTextMinSize = NameTagMinFontSize;
            template.resizeTextMaxSize = NameTagFontSize;
            var tags = group.AddComponent<SoccerNameTags>();
            var so = new SerializedObject(tags);
            so.FindProperty("_template").objectReferenceValue = template;
            so.ApplyModifiedProperties();
            return tags;
        }

        /// <summary>「MVP」の金の見出し、席の色の帯の上に名前、その下に入れた点。結果を出すまで隠す</summary>
        private static void CreateMvp(Transform safe, SerializedObject so)
        {
            var panel = B.CreatePanel(safe, "Mvp", MvpPosition, MvpSize);
            var heading = B.CreateLabel(panel.transform, "Heading", "MVP", MvpHeadingFontSize, MvpHeadingPosition, MvpHeadingSize);
            heading.fontStyle = FontStyle.Bold;
            heading.color = MvpHeadingColor;
            var band = B.CreateHudImage(panel.transform, "Band", null, Color.white, Center, MvpBandPosition, MvpBandSize);
            var name = B.CreateLabel(band.transform, "Name", string.Empty, MvpNameFontSize, Vector2.zero, MvpNameSize);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = MvpNameFontSize / 2;
            name.resizeTextMaxSize = MvpNameFontSize;
            var goals = B.CreateLabel(panel.transform, "Goals", string.Empty, MvpGoalsFontSize, MvpGoalsPosition, MvpGoalsSize);
            goals.fontStyle = FontStyle.Bold;
            panel.SetActive(false);
            so.FindProperty("_mvp").objectReferenceValue = panel;
            so.FindProperty("_mvpName").objectReferenceValue = name;
            so.FindProperty("_mvpGoals").objectReferenceValue = goals;
            so.FindProperty("_mvpBand").objectReferenceValue = band;
        }
    }
}
