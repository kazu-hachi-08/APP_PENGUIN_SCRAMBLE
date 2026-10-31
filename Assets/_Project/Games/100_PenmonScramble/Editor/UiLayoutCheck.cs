using System.Collections.Generic;
using System.Text;
using PenmonScramble.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 開いているシーンの文字（Text）が、枠からはみ出していないか・途中で折り返していないか・となりの部品に重なっていないかを調べる。
    /// SceneBuilder は位置と大きさを数値で決めるので、文字数やフォントが変わってもエディタ上では気づきにくい。
    /// 実行時は GameFontApplier が日本語フォントへ差し替えて字の幅・行の高さが変わるので、そのフォントで測る
    /// </summary>
    public static class UiLayoutCheck
    {
        // 縁取り（Outline）や丸めのぶんの、はみ出しとみなさない余裕（画面の単位）
        private const float Tolerance = 4f;
        // 文字のまわりの重なりは、この大きさを超えたら知らせる（縁取りが触れる程度は見逃す）
        private const float OverlapTolerance = 6f;

        [MenuItem(EditorMenu.Check + "UI Text Layout", priority = EditorMenu.CheckPriority)]
        private static void Run()
        {
            var font = GameFontApplier.Font;
            var issues = new List<string>();
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (string.IsNullOrEmpty(text.text) || EditorUtility.IsPersistent(text)) continue;
                CheckFit(text, font, issues);
                CheckOverlap(text, font, issues);
            }

            if (issues.Count == 0)
            {
                Debug.Log("[UiLayoutCheck] 文字の配置に問題は見つかりませんでした");
                return;
            }

            var report = new StringBuilder($"[UiLayoutCheck] 文字の配置の問題: {issues.Count} 件\n");
            foreach (var issue in issues) report.AppendLine(issue);
            Debug.LogWarning(report.ToString());
        }

        /// <summary>枠に入りきるか。Best Fit の文字はいちばん小さくしたときで測る（それでも入らなければ切れて消える）</summary>
        private static void CheckFit(Text text, Font font, List<string> issues)
        {
            var size = text.rectTransform.rect.size;
            var settings = Settings(text, font, size);

            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var oneLine = Generate(text.text, settings);
            settings.horizontalOverflow = text.horizontalOverflow;
            var wrapped = Generate(text.text, settings);

            if (text.horizontalOverflow == HorizontalWrapMode.Overflow && oneLine.width > size.x + Tolerance)
            {
                issues.Add($"{PathOf(text)}: 横にはみ出す（文字 {oneLine.width:0} > 枠 {size.x:0}）「{Short(text.text)}」");
            }
            else if (text.horizontalOverflow == HorizontalWrapMode.Wrap && wrapped.lines > oneLine.lines)
            {
                issues.Add($"{PathOf(text)}: 幅が足りず途中で折り返す（{oneLine.lines} 行 → {wrapped.lines} 行）「{Short(text.text)}」");
            }

            if (wrapped.height > size.y + Tolerance)
            {
                var result = text.verticalOverflow == VerticalWrapMode.Truncate && text.resizeTextForBestFit ? "切れて消える" : "はみ出す";
                issues.Add($"{PathOf(text)}: 縦に{result}（文字 {wrapped.height:0} > 枠 {size.y:0}）「{Short(text.text)}」");
            }
        }

        /// <summary>
        /// 実際に字が出る範囲が、同じ親の下のほかの部品に重なっていないか。
        /// 文字を丸ごと含む部品は背景の板とみなし、知らせない
        /// </summary>
        private static void CheckOverlap(Text text, Font font, List<string> issues)
        {
            var parent = text.transform.parent;
            if (parent == null) return;
            var glyphs = GlyphRect(text, font);
            if (glyphs.width <= 0f) return;

            foreach (Transform sibling in parent)
            {
                if (sibling == text.transform || !sibling.gameObject.activeSelf) continue;
                if (sibling.GetComponentInChildren<Graphic>(true) == null) continue;

                var other = sibling.GetComponent<Text>() is Text otherText
                    ? GlyphRect(otherText, font)
                    : RectIn(sibling as RectTransform, parent);
                if (other.width <= 0f || Contains(other, glyphs)) continue;

                var overlap = Intersect(glyphs, other);
                if (overlap.width > OverlapTolerance && overlap.height > OverlapTolerance)
                {
                    issues.Add($"{PathOf(text)}: 「{sibling.name}」に重なる（{overlap.width:0} x {overlap.height:0}）「{Short(text.text)}」");
                }
            }
        }

        /// <summary>字が出る範囲（親の座標で）。Best Fit の文字はいちばん小さくしたときで測る（それでも重なるなら必ず重なる）</summary>
        private static Rect GlyphRect(Text text, Font font)
        {
            var rect = text.rectTransform;
            var settings = Settings(text, font, rect.rect.size);
            var generator = new TextGenerator();
            generator.Populate(text.text, settings);
            if (generator.vertexCount == 0) return Rect.zero;

            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            foreach (var vertex in generator.verts)
            {
                min = Vector2.Min(min, vertex.position);
                max = Vector2.Max(max, vertex.position);
            }

            // 頂点は Text の基準点からの位置。親の座標へ移す
            var offset = rect.localPosition;
            return Rect.MinMaxRect(min.x + offset.x, min.y + offset.y, max.x + offset.x, max.y + offset.y);
        }

        private static TextGenerationSettings Settings(Text text, Font font, Vector2 size)
        {
            var settings = text.GetGenerationSettings(size);
            settings.font = font;
            settings.scaleFactor = 1f;
            // 実行時に GameFontApplier が Best Fit でない文字を縦に溢れさせるので、それに合わせる
            if (!text.resizeTextForBestFit) settings.verticalOverflow = VerticalWrapMode.Overflow;
            if (text.resizeTextForBestFit)
            {
                settings.resizeTextForBestFit = false;
                settings.fontSize = text.resizeTextMinSize;
            }
            return settings;
        }

        private static (float width, float height, int lines) Generate(string value, TextGenerationSettings settings)
        {
            var generator = new TextGenerator();
            generator.Populate(value, settings);
            var lines = generator.lineCount;
            var width = generator.GetPreferredWidth(value, settings);
            var height = generator.GetPreferredHeight(value, settings);
            return (width, height, lines);
        }

        private static Rect RectIn(RectTransform rect, Transform parent)
        {
            if (rect == null) return Rect.zero;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = parent.InverseTransformPoint(corners[0]);
            var max = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool Contains(Rect outer, Rect inner) =>
            outer.xMin <= inner.xMin && outer.yMin <= inner.yMin && outer.xMax >= inner.xMax && outer.yMax >= inner.yMax;

        private static Rect Intersect(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        private static string PathOf(Component component)
        {
            var path = component.name;
            for (var parent = component.transform.parent; parent != null; parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }
            return path;
        }

        private static string Short(string value)
        {
            var oneLine = value.Replace("\n", " / ");
            return oneLine.Length > 24 ? oneLine.Substring(0, 24) + "…" : oneLine;
        }
    }
}
