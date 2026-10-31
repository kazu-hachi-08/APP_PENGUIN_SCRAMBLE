using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 遊び方の画面（§3.1）: 上に「操作」「ルール」のタブ。操作の絵（キーボード・コントローラー・スマホ）は
    /// PenmonScrambleSceneBuilder.HowToPlayControls.cs。タイトルとステージ中のメニューの両方から開くので、どちらよりも手前に作る
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color HowToPlayColor = new Color(0.60f, 0.45f, 0.85f);
        // ガチャの下（右隣はロゴの「乱戦！」とぶつかるため）
        private static readonly Vector2 HowToPlayButtonPosition = new Vector2(-790f, 350f);

        private const float TabY = 330f;
        private const float TabSpacing = 340f;
        private static readonly Vector2 TabSize = new Vector2(300f, 90f);
        private const float DeviceY = 225f;
        private const float DeviceSpacing = 380f;
        private static readonly Vector2 DeviceSize = new Vector2(340f, 80f);
        private static readonly Color TabIdleColor = new Color(0.30f, 0.38f, 0.55f);
        private static readonly Color TabSelectedColor = new Color(1f, 0.75f, 0.30f);

        // ルール: 左に顔、右に見出しと文。下にページの ◀ ▶
        private static readonly Vector2 RuleFacePosition = new Vector2(-580f, -30f);
        private const float RuleFaceSize = 380f;
        private static readonly Vector2 RuleHeadingPosition = new Vector2(180f, 150f);
        private static readonly Vector2 RuleHeadingSize = new Vector2(1100f, 100f);
        private const int RuleHeadingFontSize = 60;
        private static readonly Vector2 RuleBodyPosition = new Vector2(180f, -70f);
        private static readonly Vector2 RuleBodySize = new Vector2(1100f, 320f);
        private const int RuleBodyFontSize = 38;
        private const float RuleBodyLineSpacing = 1.25f;
        private const float PageNavY = -390f;
        private const float PageNavCenterX = 180f;
        private const float PageArrowOffset = 200f;
        private static readonly Vector2 PageTextSize = new Vector2(240f, 90f);

        private static HowToPlayScreen CreateHowToPlayScreen(Transform canvas)
        {
            var root = CreateScreen(canvas, "HowToPlay", "遊び方");
            var back = CreateTitleButton(root.transform, "Back", "もどる", BackPosition, BackSize, CancelColor, MenuFontSize);
            var tabs = new[]
            {
                CreateTitleButton(root.transform, "ControlsTab", "操作", new Vector2(-TabSpacing * 0.5f, TabY), TabSize, TabIdleColor, MenuFontSize),
                CreateTitleButton(root.transform, "RulesTab", "ルール", new Vector2(TabSpacing * 0.5f, TabY), TabSize, TabIdleColor, MenuFontSize),
            };
            var controls = CreateStretchGroup(root.transform, "Controls");
            var rules = CreateStretchGroup(root.transform, "Rules");
            var (devices, panels) = CreateDeviceTabs(controls.transform);

            var screen = root.AddComponent<HowToPlayScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_backButton").objectReferenceValue = back;
            SetObjectArray(so.FindProperty("_tabButtons"), tabs);
            SetObjectArray(so.FindProperty("_tabPages"), new Object[] { controls, rules });
            SetObjectArray(so.FindProperty("_deviceButtons"), devices);
            SetObjectArray(so.FindProperty("_devicePanels"), panels);
            so.FindProperty("_selectedColor").colorValue = TabSelectedColor;
            so.FindProperty("_idleColor").colorValue = TabIdleColor;
            CreateRulesPage(rules.transform, so);
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        /// <summary>キーボード・PS5 コントローラー・スマホの切り替えと、それぞれの絵（並びは HowToPlayScreen の定数と同じ）</summary>
        private static (Button[] buttons, GameObject[] panels) CreateDeviceTabs(Transform parent)
        {
            var labels = new[] { "キーボード", "PS5", "スマホ" };
            var buttons = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var position = new Vector2((i - 1) * DeviceSpacing, DeviceY);
                buttons[i] = CreateTitleButton(parent, $"Device{i}", labels[i], position, DeviceSize, TabIdleColor, SmallFontSize);
            }

            var panels = new[]
            {
                CreateKeyboardPanel(parent),
                CreateGamepadPanel(parent),
                CreateTouchPanel(parent),
            };
            return (buttons, panels);
        }

        private static void CreateRulesPage(Transform parent, SerializedObject screen)
        {
            var face = CreateCircle(parent, "Face", RuleFacePosition, RuleFaceSize, Color.white);
            face.sprite = null;
            var heading = CreateLabel(parent, "Heading", string.Empty, RuleHeadingFontSize, RuleHeadingPosition, RuleHeadingSize);
            heading.alignment = TextAnchor.MiddleLeft;
            heading.fontStyle = FontStyle.Bold;
            var body = CreateLabel(parent, "Body", string.Empty, RuleBodyFontSize, RuleBodyPosition, RuleBodySize);
            body.alignment = TextAnchor.UpperLeft;
            body.lineSpacing = RuleBodyLineSpacing;

            var prev = CreateTitleButton(parent, "PrevPage", "◀", new Vector2(PageNavCenterX - PageArrowOffset, PageNavY), StageArrowSize, ArrowColor, MenuFontSize);
            var page = CreateLabel(parent, "Page", string.Empty, MenuFontSize, new Vector2(PageNavCenterX, PageNavY), PageTextSize);
            var next = CreateTitleButton(parent, "NextPage", "▶", new Vector2(PageNavCenterX + PageArrowOffset, PageNavY), StageArrowSize, ArrowColor, MenuFontSize);

            screen.FindProperty("_ruleHeading").objectReferenceValue = heading;
            screen.FindProperty("_ruleBody").objectReferenceValue = body;
            screen.FindProperty("_ruleFace").objectReferenceValue = face;
            screen.FindProperty("_pageText").objectReferenceValue = page;
            screen.FindProperty("_prevPageButton").objectReferenceValue = prev;
            screen.FindProperty("_nextPageButton").objectReferenceValue = next;
        }

        private static GameObject CreateStretchGroup(Transform parent, string name)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            return obj;
        }

        private static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
