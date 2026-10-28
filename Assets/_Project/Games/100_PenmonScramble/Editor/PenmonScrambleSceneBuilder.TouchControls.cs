using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
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
        // 位置はセーフエリア（ノッチ・角の丸み）の内側から測る。角に寄せすぎると機種によって押せないので、少し内側に置く
        private static readonly Vector2 JoystickPosition = new Vector2(330f, 300f);
        private const float JoystickSize = 380f;
        private const float JoystickHandleSize = 170f;
        // 枠の半分より少し小さくして、スティックを倒しきった位置が枠の中に見えるようにする
        private const float JoystickHandleRange = 130f;

        private static readonly Vector2 Skill1Position = new Vector2(-330f, 300f);
        private const float Skill1Size = 240f;
        private static readonly Vector2 Skill2Position = new Vector2(-600f, 200f);
        private const float Skill2Size = 180f;
        // §3: 必殺技は技1の上、交代は技2の上の小ボタン（どれも親指を少し動かせば届く範囲）
        private static readonly Vector2 SpecialPosition = new Vector2(-330f, 570f);
        private const float SpecialSize = 170f;
        private static readonly Vector2 SwitchPosition = new Vector2(-610f, 420f);
        private const float SwitchSize = 130f;
        private const int SkillFontSize = 52;
        private const int SmallButtonFontSize = 36;

        // 縁取り・影の太さ。ボタンの大きさに対する割合
        private const float BorderRatio = 0.06f;
        private static readonly Vector2 ShadowOffset = new Vector2(0f, -10f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color BorderColor = Color.white;
        private static readonly Color ButtonLabelOutlineColor = new Color(0f, 0f, 0f, 0.5f);
        private static readonly Vector2 ButtonLabelOutlineDistance = new Vector2(2f, -2f);

        private static readonly Color JoystickBackColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color JoystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
        // 地面の上でも埋もれないよう、ほぼ不透明にする
        private static readonly Color Skill1Color = new Color(1f, 0.48f, 0.25f, 0.92f);
        private static readonly Color Skill2Color = new Color(0.22f, 0.62f, 1f, 0.92f);
        // 必殺技ボタンの透明度は StageHud がゲージに合わせて変える
        private static readonly Color SpecialColor = new Color(1f, 0.78f, 0.1f, 0.35f);
        private static readonly Color SwitchColor = new Color(0.42f, 0.48f, 0.6f, 0.92f);

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

            // ゲージで光らせるのは色の面（Face）。縁取りは常に見せて、押せる場所だと分かるようにする
            public Image SpecialImage => Special.transform.Find("Visual/Face").GetComponent<Image>();
        }

        private static TouchControls CreateTouchControls(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("TouchControls", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<SafeAreaFitter>();
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

        /// <summary>
        /// 影・白い縁取り・色の面・文字を重ねたボタン。触れる範囲は親（透明）が持ち、見た目は子の Visual にまとめて押したときに縮める
        /// </summary>
        private static VirtualButton CreateSkillButton(Transform parent, Sprite knob, string name, string label, Vector2 position, float size, Color color)
        {
            var obj = CreateCircle(parent, name, knob, Color.clear, size);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = position;

            var visual = UIDialogBuilder.CreateUIObject("Visual", obj.transform);
            UIDialogBuilder.SetStretchAll(visual.GetComponent<RectTransform>());
            var shadow = CreateCircle(visual.transform, "Shadow", knob, ShadowColor, size);
            shadow.GetComponent<RectTransform>().anchoredPosition = ShadowOffset;
            CreateCircle(visual.transform, "Border", knob, BorderColor, size);
            CreateCircle(visual.transform, "Face", knob, color, size * (1f - BorderRatio * 2f));
            foreach (var image in visual.GetComponentsInChildren<Image>())
            {
                image.raycastTarget = false;
            }

            var textObj = UIDialogBuilder.CreateUIObject("Text", visual.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, label, SkillFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = ButtonLabelOutlineColor;
            outline.effectDistance = ButtonLabelOutlineDistance;

            var button = obj.AddComponent<VirtualButton>();
            var press = obj.AddComponent<TouchButtonPress>();
            var so = new SerializedObject(press);
            so.FindProperty("_visual").objectReferenceValue = visual.GetComponent<RectTransform>();
            so.ApplyModifiedProperties();
            return button;
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
