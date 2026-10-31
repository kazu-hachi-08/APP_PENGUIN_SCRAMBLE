using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// スマホ用の仮想スティック（左）と技ボタン（右）。
    /// ボタンは押すと沈む厚みのある丸で、中に技の形・待ちの扇・必殺技のゲージのリング・次に出る子の顔を出す（TouchControlsView）。絵は TouchArt
    /// </summary>
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
        // 必殺技は技1の上、交代は技2の上の小ボタン（どれも親指を少し動かせば届く範囲）
        private static readonly Vector2 SpecialPosition = new Vector2(-330f, 570f);
        private const float SpecialSize = 170f;
        private static readonly Vector2 SwitchPosition = new Vector2(-610f, 420f);
        private const float SwitchSize = 130f;
        private const int SkillFontSize = 52;

        // ボタンの中の割合（直径に対して）。面の外が白い縁になる
        private const float TouchFaceRatio = 0.88f;
        private const float TouchIconRatio = 0.6f;
        private const float SwitchFaceRatio = 0.7f;
        // 押すと沈む厚み（直径に対して）。大きいボタンほど深く沈む
        private const float TouchDepthRatio = 0.05f;
        private static readonly Vector2 TouchShadowExtra = new Vector2(0f, -6f);
        private const float SpecialHaloRatio = 1.75f;
        private const float SpecialSparkRatio = 1.32f;
        private const float SwitchLabelY = -0.4f;
        private const int SwitchLabelFontSize = 28;
        private static readonly Vector2 TouchIconOutlineDistance = new Vector2(3f, -3f);
        // 厚みとアイコンの縁取りは面の色を濃くする（TouchSkillButton が属性の色に変えるときも同じ割合）
        private const float TouchBaseDarken = 0.45f;
        private const float TouchIconOutlineDarken = 0.55f;
        private const string SwitchLabel = "交代";
        private const string SpecialLabel = "必殺";

        private static readonly Vector2 ShadowOffset = new Vector2(0f, -10f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color BorderColor = Color.white;
        private static readonly Color ButtonLabelOutlineColor = new Color(0f, 0f, 0f, 0.5f);
        private static readonly Vector2 ButtonLabelOutlineDistance = new Vector2(2f, -2f);
        private static readonly Color CooldownColor = new Color(0.02f, 0.04f, 0.12f, 0.6f);
        private static readonly Color SpecialRimColor = new Color(0.10f, 0.12f, 0.22f, 0.9f);
        private static readonly Color SpecialGaugeColor = new Color(1f, 0.93f, 0.45f);
        private static readonly Color SpecialSparkColor = new Color(1f, 0.95f, 0.6f, 0.9f);

        private static readonly Color JoystickBackColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color JoystickHandleColor = new Color(0.92f, 0.95f, 1f);
        private static readonly Color JoystickHandleRimColor = new Color(0.55f, 0.75f, 1f);
        // 技のボタンの色は、場に出ている子の属性の色に変わる（TouchControlsView）。これは遊び方の絵と、最初の1フレームの色
        private static readonly Color Skill1Color = new Color(1f, 0.48f, 0.25f);
        private static readonly Color Skill2Color = new Color(0.22f, 0.62f, 1f);
        private static readonly Color SpecialColor = new Color(1f, 0.78f, 0.1f);
        private static readonly Color SwitchColor = new Color(0.42f, 0.48f, 0.6f);

        private readonly struct TouchControls
        {
            public readonly VirtualJoystick Joystick;
            public readonly VirtualButton Skill1;
            public readonly VirtualButton Skill2;
            public readonly VirtualButton Special;
            public readonly VirtualButton Switch;
            public readonly TouchControlsView View;

            public TouchControls(VirtualJoystick joystick, VirtualButton skill1, VirtualButton skill2, VirtualButton special, VirtualButton swap, TouchControlsView view)
            {
                Joystick = joystick;
                Skill1 = skill1;
                Skill2 = skill2;
                Special = special;
                Switch = swap;
                View = view;
            }
        }

        /// <summary>ボタン1つの部品。種類ごとに中身を足してから、見た目のコンポーネントにつなぐ</summary>
        private readonly struct TouchButtonParts
        {
            public readonly VirtualButton Button;
            public readonly Transform Visual;
            public readonly Transform Top;
            public readonly Image Rim;
            public readonly Image Face;
            public readonly Image Base;

            public TouchButtonParts(VirtualButton button, Transform visual, Transform top, Image rim, Image face, Image baseImage)
            {
                Button = button;
                Visual = visual;
                Top = top;
                Rim = rim;
                Face = face;
                Base = baseImage;
            }
        }

        private static TouchControls CreateTouchControls(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("TouchControls", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<SafeAreaFitter>();
            root.AddComponent<CanvasGroup>();
            var visibility = root.AddComponent<TouchControlsVisibility>();

            var joystick = CreateJoystick(root.transform);
            var skill1 = CreateSkillButton(root.transform, "Skill1", Skill1Position, Skill1Size, Skill1Color);
            var skill2 = CreateSkillButton(root.transform, "Skill2", Skill2Position, Skill2Size, Skill2Color);
            var special = CreateSpecialButton(root.transform);
            var swap = CreateSwitchButton(root.transform);

            var view = root.AddComponent<TouchControlsView>();
            var so = new SerializedObject(view);
            so.FindProperty("_visibility").objectReferenceValue = visibility;
            so.FindProperty("_skill1").objectReferenceValue = skill1.GetComponent<TouchSkillButton>();
            so.FindProperty("_skill2").objectReferenceValue = skill2.GetComponent<TouchSkillButton>();
            so.FindProperty("_special").objectReferenceValue = special.GetComponent<TouchSpecialButton>();
            so.FindProperty("_switch").objectReferenceValue = swap.GetComponent<TouchSkillButton>();
            so.ApplyModifiedProperties();
            return new TouchControls(joystick, skill1, skill2, special, swap, view);
        }

        /// <summary>台（輪と矢印）の上を、つやのある白い玉が動く</summary>
        private static VirtualJoystick CreateJoystick(Transform parent)
        {
            var background = CreateTouchImage(parent, "Joystick", TouchStickBasePath, Color.white, JoystickSize);
            var rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = JoystickPosition;

            // 指で触るのは台の方。玉が触れる場所を奪わないよう、玉の絵はどれも押せなくする
            var handle = UIDialogBuilder.CreateUIObject("Handle", rect);
            handle.GetComponent<RectTransform>().sizeDelta = Vector2.one * JoystickHandleSize;
            CreateTouchImage(handle.transform, "Shadow", TouchCirclePath, ShadowColor, JoystickHandleSize).rectTransform.anchoredPosition = ShadowOffset;
            CreateTouchImage(handle.transform, "Rim", TouchCirclePath, JoystickHandleRimColor, JoystickHandleSize);
            CreateTouchImage(handle.transform, "Face", TouchFacePath, JoystickHandleColor, JoystickHandleSize * TouchFaceRatio);
            CreateTouchImage(handle.transform, "Gloss", TouchGlossPath, Color.white, JoystickHandleSize * TouchFaceRatio);
            foreach (var image in handle.GetComponentsInChildren<Image>())
            {
                image.raycastTarget = false;
            }

            var joystick = background.gameObject.AddComponent<VirtualJoystick>();
            var so = new SerializedObject(joystick);
            so.FindProperty("_background").objectReferenceValue = rect;
            so.FindProperty("_handle").objectReferenceValue = handle.GetComponent<RectTransform>();
            so.FindProperty("_handleRange").floatValue = JoystickHandleRange;
            so.ApplyModifiedProperties();
            return joystick;
        }

        private static VirtualButton CreateSkillButton(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var parts = CreateTouchButton(parent, name, position, size, color);
            var icon = CreateTouchImage(parts.Top, "Icon", null, Color.white, size * TouchIconRatio);
            icon.preserveAspect = true;
            icon.enabled = false;
            var outline = icon.gameObject.AddComponent<Outline>();
            outline.effectDistance = TouchIconOutlineDistance;
            outline.effectColor = Color.Lerp(color, Color.black, TouchIconOutlineDarken);
            BindSkillButton(parts, size, icon, outline);
            return parts.Button;
        }

        /// <summary>交代: 次に出る子の顔と、下に小さく「交代」</summary>
        private static VirtualButton CreateSwitchButton(Transform parent)
        {
            var parts = CreateTouchButton(parent, "Switch", SwitchPosition, SwitchSize, SwitchColor);
            var face = CreateTouchImage(parts.Top, "Icon", null, Color.white, SwitchSize * SwitchFaceRatio);
            face.preserveAspect = true;
            face.enabled = false;
            BindSkillButton(parts, SwitchSize, face, null);
            var label = CreateTouchLabel(parts.Top, SwitchLabel, SwitchLabelFontSize);
            label.rectTransform.anchoredPosition = new Vector2(0f, SwitchSize * SwitchLabelY);
            return parts.Button;
        }

        /// <summary>待ちの扇と、明けたときの光を重ねて TouchSkillButton につなぐ（扇と光はアイコンより手前）</summary>
        private static void BindSkillButton(TouchButtonParts parts, float size, Image icon, Outline outline)
        {
            var cooldown = CreateTouchImage(parts.Top, "Cooldown", TouchCirclePath, CooldownColor, size * TouchFaceRatio);
            SetRadialFill(cooldown);
            cooldown.enabled = false;
            CreateTouchImage(parts.Top, "Gloss", TouchGlossPath, Color.white, size * TouchFaceRatio);
            var flash = CreateTouchImage(parts.Top, "Flash", TouchCirclePath, Color.clear, size);
            flash.enabled = false;
            DisableRaycasts(parts.Visual);

            var view = parts.Button.gameObject.AddComponent<TouchSkillButton>();
            var so = new SerializedObject(view);
            so.FindProperty("_face").objectReferenceValue = parts.Face;
            so.FindProperty("_base").objectReferenceValue = parts.Base;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_iconOutline").objectReferenceValue = outline;
            so.FindProperty("_cooldown").objectReferenceValue = cooldown;
            so.FindProperty("_flash").objectReferenceValue = flash;
            so.ApplyModifiedProperties();
        }

        /// <summary>必殺技: 縁は暗くして、そこにゲージの金のリングがたまる。満タンで後ろの光と外の輪が出る（TouchSpecialButton）</summary>
        private static VirtualButton CreateSpecialButton(Transform parent)
        {
            var parts = CreateTouchButton(parent, "Special", SpecialPosition, SpecialSize, SpecialColor);
            parts.Rim.color = SpecialRimColor;
            var gauge = CreateTouchImage(parts.Top, "Gauge", TouchRingPath, SpecialGaugeColor, SpecialSize);
            SetRadialFill(gauge);
            gauge.fillAmount = 0f;
            var label = CreateTouchLabel(parts.Top, SpecialLabel, SkillFontSize);
            CreateTouchImage(parts.Top, "Gloss", TouchGlossPath, Color.white, SpecialSize * TouchFaceRatio);

            // 光と輪はボタンの後ろ（Visual の最初）に置く
            var halo = CreateTouchImage(parts.Visual, "Halo", TouchGlowPath, SpecialColor, SpecialSize * SpecialHaloRatio);
            halo.transform.SetAsFirstSibling();
            halo.gameObject.SetActive(false);
            var spark = CreateTouchImage(parts.Visual, "Spark", TouchSparkRingPath, SpecialSparkColor, SpecialSize * SpecialSparkRatio);
            spark.transform.SetSiblingIndex(1);
            spark.gameObject.SetActive(false);
            DisableRaycasts(parts.Visual);

            var view = parts.Button.gameObject.AddComponent<TouchSpecialButton>();
            var so = new SerializedObject(view);
            so.FindProperty("_face").objectReferenceValue = parts.Face;
            so.FindProperty("_base").objectReferenceValue = parts.Base;
            so.FindProperty("_gauge").objectReferenceValue = gauge;
            so.FindProperty("_halo").objectReferenceValue = halo;
            so.FindProperty("_spin").objectReferenceValue = spark.rectTransform;
            so.FindProperty("_label").objectReferenceValue = label.rectTransform;
            so.FindProperty("_readyColor").colorValue = SpecialColor;
            so.ApplyModifiedProperties();
            return parts.Button;
        }

        /// <summary>
        /// 厚みのある丸いボタンの土台: 影・下の厚み（Base）・押すと沈む面（Top: 白い縁と色の面）。
        /// 触れる範囲は親（透明な丸）が持ち、見た目は子の Visual にまとめる
        /// </summary>
        private static TouchButtonParts CreateTouchButton(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var hit = CreateTouchImage(parent, name, TouchCirclePath, Color.clear, size);
            var rect = hit.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = position;

            var depth = size * TouchDepthRatio;
            var visual = UIDialogBuilder.CreateUIObject("Visual", rect);
            UIDialogBuilder.SetStretchAll(visual.GetComponent<RectTransform>());
            var shadow = CreateTouchImage(visual.transform, "Shadow", TouchCirclePath, ShadowColor, size);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -depth) + TouchShadowExtra;
            var baseImage = CreateTouchImage(visual.transform, "Base", TouchCirclePath, Color.Lerp(color, Color.black, TouchBaseDarken), size);
            baseImage.rectTransform.anchoredPosition = new Vector2(0f, -depth);

            var top = UIDialogBuilder.CreateUIObject("Top", visual.transform);
            top.GetComponent<RectTransform>().sizeDelta = Vector2.one * size;
            var rim = CreateTouchImage(top.transform, "Rim", TouchCirclePath, BorderColor, size);
            var face = CreateTouchImage(top.transform, "Face", TouchFacePath, color, size * TouchFaceRatio);

            var button = hit.gameObject.AddComponent<VirtualButton>();
            var press = hit.gameObject.AddComponent<TouchButtonPress>();
            var so = new SerializedObject(press);
            so.FindProperty("_visual").objectReferenceValue = visual.GetComponent<RectTransform>();
            so.FindProperty("_top").objectReferenceValue = top.GetComponent<RectTransform>();
            so.FindProperty("_pressDepth").floatValue = depth;
            so.ApplyModifiedProperties();
            return new TouchButtonParts(button, visual.transform, top.transform, rim, face, baseImage);
        }

        private static Image CreateTouchImage(Transform parent, string name, string spritePath, Color color, float size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            obj.GetComponent<RectTransform>().sizeDelta = Vector2.one * size;
            var image = obj.AddComponent<Image>();
            image.sprite = spritePath != null ? UiSprite(spritePath) : null;
            image.color = color;
            return image;
        }

        private static Text CreateTouchLabel(Transform parent, string label, int fontSize)
        {
            var obj = UIDialogBuilder.CreateUIObject("Text", parent);
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(fontSize * 4f, fontSize * 1.5f);
            var text = AddText(obj, label, fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = LabelOutlineColor;
            outline.effectDistance = ButtonLabelOutlineDistance;
            return text;
        }

        /// <summary>上から時計回りに減っていく扇（待ちの残り・ゲージ）</summary>
        private static void SetRadialFill(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
        }

        /// <summary>見た目の部品は押す邪魔をしない（触れる範囲はボタンの親が持つ）</summary>
        private static void DisableRaycasts(Transform visual)
        {
            foreach (var image in visual.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
            }

            foreach (var text in visual.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
            }
        }
    }
}
