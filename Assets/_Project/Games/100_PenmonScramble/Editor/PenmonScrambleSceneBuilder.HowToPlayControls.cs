using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 遊び方の「操作」の絵。キーボードはキーの並びどおりに置き、押したキーが光る（HowToPlayKey）。
    /// スマホはステージ中の仮想スティック・ボタンの配置（TouchControls の定数）を縮めて描く
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string RoundedSpritePath = "UI/Skin/UISprite.psd";
        private static readonly Color KeyCapColor = new Color(0.95f, 0.96f, 1f);
        private static readonly Color KeyCapTextColor = new Color(0.15f, 0.20f, 0.30f);
        private static readonly Vector2 KeyCapShadowOffset = new Vector2(0f, -8f);
        private const float KeySize = 110f;
        private const float KeyPitch = 120f;
        private const int KeyFontSize = 48;
        private const int KeyWordFontSize = 34;
        private const int ControlTipFontSize = 32;
        private static readonly Vector2 ControlTipPosition = new Vector2(0f, -400f);
        private static readonly Vector2 ControlTipSize = new Vector2(1700f, 70f);
        private static readonly Vector2 KeyLabelSize = new Vector2(260f, 60f);

        // キーボード: 左に移動（WASD と矢印）、右に技（J K L と Space）、左上に Esc
        private static readonly Vector2 WasdCenter = new Vector2(-560f, -130f);
        private static readonly Vector2 ArrowsCenter = new Vector2(-80f, -130f);
        private static readonly Vector2 EscPosition = new Vector2(-680f, 110f);
        private const float SkillKeysY = -10f;
        private const float SkillKeysX = 280f;
        private static readonly Vector2 SpacePosition = new Vector2(420f, -180f);
        private static readonly Vector2 SpaceSize = new Vector2(400f, 100f);
        private const float KeyLabelGap = 85f;

        // PS5 コントローラー: 左右2列に「ボタンの絵 + 何をするか」を並べる。記号の色は PS の記号の色に寄せる
        private static readonly float[] PadColumnX = { -640f, 120f };
        private static readonly float[] PadRowY = { 110f, -30f, -170f };
        private const float PadGlyphSize = 110f;
        private static readonly Vector2 PadWideGlyphSize = new Vector2(200f, 90f);
        private const int PadWideFontSize = 28;
        // 絵の右端から文字の左端まで
        private const float PadLabelGap = 30f;
        private static readonly Vector2 PadLabelSize = new Vector2(520f, 80f);
        private static readonly Color PadCrossColor = new Color(0.40f, 0.60f, 0.95f);
        private static readonly Color PadCircleColor = new Color(0.90f, 0.35f, 0.35f);
        private static readonly Color PadTriangleColor = new Color(0.30f, 0.75f, 0.60f);
        private static readonly Color PadNeutralColor = new Color(0.55f, 0.60f, 0.70f);

        // スマホ: 横長の画面の絵に、ステージ中の配置を縮めて置く
        private static readonly Vector2 PhoneScreenPosition = new Vector2(0f, -70f);
        private static readonly Vector2 PhoneScreenSize = new Vector2(830f, 467f);
        private const float PhoneBezel = 24f;
        private static readonly Color PhoneBodyColor = new Color(0.05f, 0.07f, 0.12f);
        private static readonly Color PhoneScreenColor = new Color(0.45f, 0.62f, 0.75f);
        private const int PhoneButtonFontSize = 28;
        private const float PhoneMenuIconSize = 50f;
        private const float PhoneMenuIconMargin = 18f;

        private static GameObject CreateKeyboardPanel(Transform parent)
        {
            var panel = CreateStretchGroup(parent, "Keyboard");
            var root = panel.transform;
            CreateKeyCross(root, WasdCenter, new[] { "W", "A", "S", "D" }, new[] { Key.W, Key.A, Key.S, Key.D });
            CreateKeyCross(root, ArrowsCenter, new[] { "↑", "←", "↓", "→" }, new[] { Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow });
            var moveCenter = (WasdCenter + ArrowsCenter) * 0.5f;
            CreateLabel(root, "Or", "または", ControlTipFontSize, new Vector2(moveCenter.x, WasdCenter.y), KeyLabelSize);
            CreateLabel(root, "MoveLabel", "移動", KeyWordFontSize, moveCenter + Vector2.down * (KeyPitch * 0.5f + KeyLabelGap), KeyLabelSize);

            var skills = new[] { ("J", Key.J, "技1"), ("K", Key.K, "技2"), ("L", Key.L, "必殺技") };
            for (var i = 0; i < skills.Length; i++)
            {
                var (label, key, action) = skills[i];
                var position = new Vector2(SkillKeysX + i * (KeyPitch + 20f), SkillKeysY);
                CreateKeyCap(root, label, position, Vector2.one * KeySize, KeyFontSize, new[] { key });
                CreateLabel(root, $"{label}Label", action, KeyWordFontSize, position + Vector2.down * KeyLabelGap, KeyLabelSize);
            }

            CreateKeyCap(root, "Space", SpacePosition, SpaceSize, KeyWordFontSize, new[] { Key.Space });
            CreateLabel(root, "SpaceLabel", "交代", KeyWordFontSize, SpacePosition + Vector2.down * KeyLabelGap, KeyLabelSize);

            CreateKeyCap(root, "Esc", EscPosition, Vector2.one * KeySize, KeyWordFontSize, new[] { Key.Escape, Key.P });
            var escLabel = CreateLabel(root, "EscLabel", "メニュー（P でも）", KeyWordFontSize, EscPosition + new Vector2(KeySize * 0.5f + 200f, 0f), new Vector2(380f, 60f));
            escLabel.alignment = TextAnchor.MiddleLeft;

            CreateControlTip(root, "技は向いている方へ出る。キーは押しっぱなしで出し続ける。押したキーが光ります");
            return panel;
        }

        /// <summary>上・左・下・右のキーを、キーボードと同じ逆T字に並べる</summary>
        private static void CreateKeyCross(Transform parent, Vector2 center, string[] labels, Key[] keys)
        {
            var offsets = new[] { Vector2.up, Vector2.left, Vector2.zero, Vector2.right };
            for (var i = 0; i < offsets.Length; i++)
            {
                var position = center + offsets[i] * KeyPitch;
                CreateKeyCap(parent, labels[i], position, Vector2.one * KeySize, KeyFontSize, new[] { keys[i] });
            }
        }

        private static GameObject CreateGamepadPanel(Transform parent)
        {
            var panel = CreateStretchGroup(parent, "Gamepad");
            var root = panel.transform;
            // 左の列: 移動・交代・メニュー / 右の列: 技（よく押すボタンを右手側にまとめる）
            // 記号は全角の × ○ △（同梱の日本語フォントに字がある）。光る判定は場所（buttonSouth など）なので、ほかのコントローラーでも同じ場所が光る
            CreatePadRow(root, 0, 0, "LStick", "L", new[] { "leftStick", "dpad" }, PadNeutralColor, true, "移動（左スティック / 方向キー）");
            CreatePadRow(root, 0, 1, "L1", "L1", new[] { "leftShoulder" }, PadNeutralColor, false, "交代");
            CreatePadRow(root, 0, 2, "Options", "OPTIONS", new[] { "start" }, PadNeutralColor, false, "メニュー");
            CreatePadRow(root, 1, 0, "Cross", "×", new[] { "buttonSouth" }, PadCrossColor, true, "技1");
            CreatePadRow(root, 1, 1, "Circle", "○", new[] { "buttonEast" }, PadCircleColor, true, "技2");
            CreatePadRow(root, 1, 2, "Triangle", "△", new[] { "buttonNorth" }, PadTriangleColor, true, "必殺技");
            CreateControlTip(root, "PS5 コントローラーのボタン。押したボタンが光ります");
            return panel;
        }

        private static void CreatePadRow(Transform parent, int column, int row, string name, string glyph, string[] controls, Color color, bool round, string action)
        {
            var position = new Vector2(PadColumnX[column], PadRowY[row]);
            var size = round ? Vector2.one * PadGlyphSize : PadWideGlyphSize;
            var fontSize = round ? KeyWordFontSize : PadWideFontSize;
            var key = CreateKeyCap(parent, glyph, position, size, fontSize, new Key[0], controls, round, name);
            key.GetComponentInChildren<Text>().color = Color.white;
            key.transform.Find("Face").GetComponent<Image>().color = color;
            // 文字は絵の右端から測る。L1・OPTIONS は丸より横に長いので、丸の幅で測ると絵に重なる
            var labelPosition = position + Vector2.right * (size.x * 0.5f + PadLabelGap + PadLabelSize.x * 0.5f);
            var label = CreateLabel(parent, $"{name}Label", action, KeyWordFontSize, labelPosition, PadLabelSize);
            label.alignment = TextAnchor.MiddleLeft;
        }

        private static GameObject CreateTouchPanel(Transform parent)
        {
            var panel = CreateStretchGroup(parent, "Touch");
            var root = panel.transform;
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
            CreateRoundedBox(root, "PhoneBody", PhoneScreenPosition, PhoneScreenSize + Vector2.one * (PhoneBezel * 2f), PhoneBodyColor, rounded);
            var screen = CreateRoundedBox(root, "PhoneScreen", PhoneScreenPosition, PhoneScreenSize, PhoneScreenColor, rounded).transform;

            // ステージ中と同じ並びを、画面の絵の大きさまで縮める（基準の高さ 1080 → 絵の高さ）
            var scale = PhoneScreenSize.y / ReferenceResolution.y;
            CreatePhoneControl(screen, "移動", Vector2.zero, JoystickPosition, JoystickSize, JoystickBackColor, scale);
            CreatePhoneControl(screen, "技1", Vector2.right, Skill1Position, Skill1Size, Skill1Color, scale);
            CreatePhoneControl(screen, "技2", Vector2.right, Skill2Position, Skill2Size, Skill2Color, scale);
            CreatePhoneControl(screen, "必殺", Vector2.right, SpecialPosition, SpecialSize, SpecialColor, scale);
            CreatePhoneControl(screen, "交代", Vector2.right, SwitchPosition, SwitchSize, SwitchColor, scale);
            CreatePhoneMenuIcon(screen);

            CreateControlTip(root, "左のスティックで移動。技ボタンは押しっぱなしで出し続ける。右上の ‖ でメニュー");
            return panel;
        }

        /// <summary>ステージ中のボタン1つを、画面の絵の中の同じ角から同じ割合の位置に置く</summary>
        private static void CreatePhoneControl(Transform screen, string label, Vector2 corner, Vector2 position, float size, Color color, float scale)
        {
            var obj = CreateTouchImage(screen, label, TouchFacePath, color, size * scale).gameObject;
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = corner;
            rect.anchoredPosition = position * scale;
            obj.GetComponent<Image>().raycastTarget = false;

            var textObj = UIDialogBuilder.CreateUIObject("Text", obj.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, label, PhoneButtonFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            textObj.AddComponent<Outline>().effectColor = ButtonLabelOutlineColor;
        }

        /// <summary>ステージ中の右上のメニューボタンと同じ「‖」</summary>
        private static void CreatePhoneMenuIcon(Transform screen)
        {
            var icon = CreateMenuIcon(screen, "Menu", PhoneMenuIconSize);
            var rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = -Vector2.one * PhoneMenuIconMargin;
        }

        /// <summary>
        /// キー1つ: 影・丸い角の面・文字。round はコントローラーの丸いボタン。押すと光る（HowToPlayKey）。
        /// name は記号（× など）をオブジェクト名に入れないためのもので、省くと label を使う
        /// </summary>
        private static HowToPlayKey CreateKeyCap(Transform parent, string label, Vector2 position, Vector2 size, int fontSize, Key[] keys, string[] padControls = null, bool round = false, string name = null)
        {
            var obj = UIDialogBuilder.CreateUIObject($"Key_{name ?? label}", parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(round ? KnobSpritePath : RoundedSpritePath);
            var shadow = CreateRoundedBox(obj.transform, "Shadow", KeyCapShadowOffset, size, ShadowColor, sprite);
            var face = CreateRoundedBox(obj.transform, "Face", Vector2.zero, size, KeyCapColor, sprite);
            shadow.raycastTarget = false;
            face.raycastTarget = false;

            var textObj = UIDialogBuilder.CreateUIObject("Text", obj.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, label, fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = KeyCapTextColor;
            text.raycastTarget = false;

            var key = obj.AddComponent<HowToPlayKey>();
            var so = new SerializedObject(key);
            so.FindProperty("_face").objectReferenceValue = face;
            var keysProperty = so.FindProperty("_keys");
            keysProperty.arraySize = keys.Length;
            for (var i = 0; i < keys.Length; i++)
            {
                keysProperty.GetArrayElementAtIndex(i).intValue = (int)keys[i];
            }

            var pads = padControls ?? new string[0];
            var padsProperty = so.FindProperty("_padControls");
            padsProperty.arraySize = pads.Length;
            for (var i = 0; i < pads.Length; i++)
            {
                padsProperty.GetArrayElementAtIndex(i).stringValue = pads[i];
            }

            so.ApplyModifiedProperties();
            return key;
        }

        private static Image CreateRoundedBox(Transform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateControlTip(Transform parent, string value)
        {
            CreateLabel(parent, "Tip", value, ControlTipFontSize, ControlTipPosition, ControlTipSize);
        }
    }
}
