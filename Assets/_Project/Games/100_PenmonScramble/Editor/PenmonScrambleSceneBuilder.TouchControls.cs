using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>スマホ用の仮想スティック（左）と技ボタン（右）。§3 の配置</summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string KnobSpritePath = "UI/Skin/Knob.psd";
        private static readonly Vector2 JoystickPosition = new Vector2(280f, 280f);
        private const float JoystickSize = 380f;
        private const float JoystickHandleSize = 170f;
        // 枠の半分より少し小さくして、スティックを倒しきった位置が枠の中に見えるようにする
        private const float JoystickHandleRange = 130f;

        private static readonly Vector2 Skill1Position = new Vector2(-230f, 230f);
        private const float Skill1Size = 240f;
        private static readonly Vector2 Skill2Position = new Vector2(-500f, 170f);
        private const float Skill2Size = 170f;
        // §3: 必殺技は右上、交代は右下の小ボタン
        private static readonly Vector2 SpecialPosition = new Vector2(-260f, 500f);
        private const float SpecialSize = 160f;
        private static readonly Vector2 SwitchPosition = new Vector2(-70f, 70f);
        private const float SwitchSize = 110f;
        private const int SkillFontSize = 44;
        private const int SmallButtonFontSize = 32;

        private static readonly Color JoystickBackColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color JoystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color Skill1Color = new Color(1f, 0.55f, 0.35f, 0.7f);
        private static readonly Color Skill2Color = new Color(0.35f, 0.75f, 1f, 0.7f);
        // 必殺技ボタンの透明度は StageHud がゲージに合わせて変える
        private static readonly Color SpecialColor = new Color(1f, 0.85f, 0.2f, 0.35f);
        private static readonly Color SwitchColor = new Color(0.8f, 0.8f, 0.85f, 0.6f);

        private readonly struct TouchControls
        {
            public readonly VirtualJoystick Joystick;
            public readonly VirtualButton Skill1;
            public readonly VirtualButton Skill2;
            public readonly VirtualButton Special;
            public readonly VirtualButton Switch;

            public TouchControls(VirtualJoystick joystick, VirtualButton skill1, VirtualButton skill2, VirtualButton special, VirtualButton swap)
            {
                Joystick = joystick;
                Skill1 = skill1;
                Skill2 = skill2;
                Special = special;
                Switch = swap;
            }

            public Image SpecialImage => Special.GetComponent<Image>();
        }

        private static TouchControls CreateTouchControls(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("TouchControls", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<TouchControlsVisibility>();

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);
            var joystick = CreateJoystick(root.transform, knob);
            var skill1 = CreateSkillButton(root.transform, knob, "Skill1", "技1", Skill1Position, Skill1Size, Skill1Color);
            var skill2 = CreateSkillButton(root.transform, knob, "Skill2", "技2", Skill2Position, Skill2Size, Skill2Color);
            var special = CreateSkillButton(root.transform, knob, "Special", "必殺", SpecialPosition, SpecialSize, SpecialColor);
            var swap = CreateSkillButton(root.transform, knob, "Switch", "交代", SwitchPosition, SwitchSize, SwitchColor);
            swap.GetComponentInChildren<Text>().fontSize = SmallButtonFontSize;
            return new TouchControls(joystick, skill1, skill2, special, swap);
        }

        private static VirtualJoystick CreateJoystick(Transform parent, Sprite knob)
        {
            var background = CreateCircle(parent, "Joystick", knob, JoystickBackColor, JoystickSize);
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = JoystickPosition;

            var handle = CreateCircle(background.transform, "Handle", knob, JoystickHandleColor, JoystickHandleSize);
            // 指で触るのは枠の方。取っ手が触れる場所を奪わないようにする
            handle.GetComponent<Image>().raycastTarget = false;

            var joystick = background.AddComponent<VirtualJoystick>();
            var so = new SerializedObject(joystick);
            so.FindProperty("_background").objectReferenceValue = rect;
            so.FindProperty("_handle").objectReferenceValue = handle.GetComponent<RectTransform>();
            so.FindProperty("_handleRange").floatValue = JoystickHandleRange;
            so.ApplyModifiedProperties();
            return joystick;
        }

        private static VirtualButton CreateSkillButton(Transform parent, Sprite knob, string name, string label, Vector2 position, float size, Color color)
        {
            var obj = CreateCircle(parent, name, knob, color, size);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = position;

            var textObj = UIDialogBuilder.CreateUIObject("Text", obj.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, label, SkillFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return obj.AddComponent<VirtualButton>();
        }

        private static GameObject CreateCircle(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            obj.GetComponent<RectTransform>().sizeDelta = Vector2.one * size;
            var image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return obj;
        }
    }
}
