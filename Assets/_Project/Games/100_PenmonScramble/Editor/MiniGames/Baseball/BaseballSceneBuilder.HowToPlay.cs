using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の遊び方。画面は本編の遊び方と同じ（タブ・ルールのページ・押したキーが光る絵）で、操作の絵だけ野球のものを描く
    /// （キーボード・PS5・スマホ）。同じボタンが打つ番と投げる番で別のことをする（J＝振る / ストレート など）ので、
    /// 本編の絵を言葉だけ替える形（ControlWords）にはせず、1つのキーに2段で書く。
    /// スマホは打つ番と投げる番で出るボタンが替わるので、2台の画面の絵を並べる。
    /// ルールのページは BaseballSceneRunner が起動時に BaseballHowToPlayPages へ差し替える
    /// </summary>
    public static partial class BaseballSceneBuilder
    {
        // 打つ番・投げる番の色（キーの下の2段の字と、見出しの凡例）
        private const string BatWordColor = "#FFB27A";
        private const string PitchWordColor = "#9CC8FF";

        // キーボード: 左に WASD と矢印（本編と同じ所）、右に J K L（2段の字が隣と重ならないよう本編より広く）、その下に Space
        private static readonly Vector2 WasdCenter = new Vector2(-560f, -130f);
        private static readonly Vector2 ArrowsCenter = new Vector2(-80f, -130f);
        private static readonly Vector2 MoveLabelSize = new Vector2(700f, 60f);
        private const float ActionKeySpacing = 180f;
        private static readonly Vector2 ActionLabelSize = new Vector2(260f, 100f);
        private const float ActionLabelGap = 100f;
        private static readonly Vector2 LegendPosition = new Vector2(460f, 110f);
        private static readonly Vector2 LegendSize = new Vector2(560f, 60f);
        private static readonly Vector2 SpaceKeyPosition = new Vector2(300f, -270f);
        private static readonly Vector2 SpaceKeySize = new Vector2(400f, 100f);
        private const float SideLabelGap = 30f;
        private static readonly Vector2 SideLabelSize = new Vector2(420f, 60f);
        private const string KeyboardTip = "打つ番は J で振る・K でバント。投げる番は J K L で投げて、←→ で曲げる";

        private static readonly (string Label, Key Key, string Bat, string Pitch)[] ActionKeys =
        {
            ("J", Key.J, "振る", "ストレート"),
            ("K", Key.K, "バント", "カーブ"),
            ("L", Key.L, string.Empty, "チェンジアップ"),
        };

        // スマホ: 横に2台（左が打つ番・右が投げる番）。本編の1台の絵より少し小さい
        private static readonly Vector2 PhoneScreenSize = new Vector2(760f, 428f);
        private const float PhoneX = 440f;
        private const float PhoneY = -60f;
        private const float PhoneCaptionMargin = 34f;
        private static readonly Vector2 PhoneCaptionSize = new Vector2(400f, 50f);
        private const int PhoneCaptionFontSize = 30;
        private const int PhoneCardFontSize = 18;
        private const string TouchTip = "球種の札を押すと投げる。投げた後はスティックで曲げる。右上の ‖ でメニュー";
        private static readonly string[] PhoneCardLabels = { "ストレート", "カーブ", "チェンジ\nアップ" };

        private static HowToPlayScreen CreateHowToPlay(Transform canvas)
        {
            return B.CreateHowToPlayScreen(canvas, parent => new[]
            {
                CreateBaseballKeyboardPanel(parent),
                CreateBaseballGamepadPanel(parent),
                CreateBaseballTouchPanel(parent),
            });
        }

        private static GameObject CreateBaseballKeyboardPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Keyboard");
            var root = panel.transform;
            B.CreateKeyCross(root, WasdCenter, new[] { "W", "A", "S", "D" }, new[] { Key.W, Key.A, Key.S, Key.D });
            B.CreateKeyCross(root, ArrowsCenter, new[] { "↑", "←", "↓", "→" }, new[] { Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow });
            var moveCenter = (WasdCenter + ArrowsCenter) * 0.5f;
            B.CreateLabel(root, "Or", "または", B.ControlTipFontSize, new Vector2(moveCenter.x, WasdCenter.y), B.KeyLabelSize);
            B.CreateLabel(root, "MoveLabel", "←→ 曲げる　↑↓ 投げる子を選ぶ", B.KeyWordFontSize, moveCenter + Vector2.down * (B.KeyPitch * 0.5f + B.KeyLabelGap), MoveLabelSize);

            B.CreateLabel(root, "Legend", $"上: {Colored("打つ番", BatWordColor)}　下: {Colored("投げる番", PitchWordColor)}", B.ControlTipFontSize, LegendPosition, LegendSize);
            for (var i = 0; i < ActionKeys.Length; i++)
            {
                var (label, key, bat, pitch) = ActionKeys[i];
                var position = new Vector2(B.SkillKeysX + i * ActionKeySpacing, B.SkillKeysY);
                B.CreateKeyCap(root, label, position, Vector2.one * B.KeySize, B.KeyFontSize, new[] { key });
                // 打つ番にすることが無いキー（L）も2段目の高さをそろえるため、1段目を空けておく
                var words = $"{Colored(bat.Length > 0 ? bat : "　", BatWordColor)}\n{Colored(pitch, PitchWordColor)}";
                B.CreateLabel(root, $"{label}Label", words, B.KeyWordFontSize, position + Vector2.down * ActionLabelGap, ActionLabelSize);
            }

            B.CreateKeyCap(root, "Space", SpaceKeyPosition, SpaceKeySize, B.KeyWordFontSize, new[] { Key.Space });
            CreateSideLabel(root, "SpaceLabel", "魔球・必殺打の ON / OFF", SpaceKeyPosition, SpaceKeySize);

            B.CreateKeyCap(root, "Esc", B.EscPosition, Vector2.one * B.KeySize, B.KeyWordFontSize, new[] { Key.Escape, Key.P });
            CreateSideLabel(root, "EscLabel", "メニュー（P でも）", B.EscPosition, Vector2.one * B.KeySize);

            B.CreateControlTip(root, KeyboardTip);
            return panel;
        }

        private static string Colored(string value, string color) => $"<color={color}>{value}</color>";

        /// <summary>キーの右に、左寄せの字を置く</summary>
        private static void CreateSideLabel(Transform root, string name, string value, Vector2 keyPosition, Vector2 keySize)
        {
            var position = keyPosition + Vector2.right * (keySize.x * 0.5f + SideLabelGap + SideLabelSize.x * 0.5f);
            var label = B.CreateLabel(root, name, value, B.KeyWordFontSize, position, SideLabelSize);
            label.alignment = TextAnchor.MiddleLeft;
        }

        private static GameObject CreateBaseballGamepadPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Gamepad");
            var root = panel.transform;
            // 左の列: 曲げる・魔球と必殺打・メニュー / 右の列: 振る・バント・球種（本編と同じく、よく押すボタンを右手側に）
            B.CreatePadRow(root, 0, 0, "LStick", "L", new[] { "leftStick", "dpad" }, B.PadNeutralColor, true, "曲げる・投げる子を選ぶ");
            B.CreatePadRow(root, 0, 1, "L1", "L1", new[] { "leftShoulder" }, B.PadNeutralColor, false, "魔球・必殺打の ON / OFF");
            B.CreatePadRow(root, 0, 2, "Options", "OPTIONS", new[] { "start" }, B.PadNeutralColor, false, "メニュー");
            B.CreatePadRow(root, 1, 0, "Cross", "×", new[] { "buttonSouth" }, B.PadCrossColor, true, "振る / ストレート");
            B.CreatePadRow(root, 1, 1, "Circle", "○", new[] { "buttonEast" }, B.PadCircleColor, true, "バント / カーブ");
            B.CreatePadRow(root, 1, 2, "Triangle", "△", new[] { "buttonNorth" }, B.PadTriangleColor, true, "チェンジアップ");
            B.CreateControlTip(root, "打つ番は × で振る・○ でバント。投げる番は × ○ △ で投げて、スティックで曲げる");
            return panel;
        }

        private static GameObject CreateBaseballTouchPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Touch");
            var root = panel.transform;
            // 試合と同じ並びを、画面の絵の大きさまで縮める（基準の高さ 1080 → 絵の高さ）
            var scale = PhoneScreenSize.y / B.ReferenceResolution.y;

            var bat = CreatePhone(root, "BatPhone", -PhoneX, "打つ番");
            B.CreateBaseballPhoneBatButtons(bat, scale);
            CreatePhoneSpecialToggle(bat, "必殺打", scale);

            var pitch = CreatePhone(root, "PitchPhone", PhoneX, "投げる番");
            B.CreateBaseballPhoneStick(pitch, scale);
            CreatePhonePitchCards(pitch, scale);
            CreatePhoneSpecialToggle(pitch, "魔球", scale);

            B.CreateControlTip(root, TouchTip);
            return panel;
        }

        /// <summary>スマホの枠と画面・上のまん中の見出し・右上の ‖。中身を置く画面を返す</summary>
        private static Transform CreatePhone(Transform root, string name, float x, string caption)
        {
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(B.RoundedSpritePath);
            var position = new Vector2(x, PhoneY);
            B.CreateRoundedBox(root, $"{name}Body", position, PhoneScreenSize + Vector2.one * (B.PhoneBezel * 2f), B.PhoneBodyColor, rounded);
            var screen = B.CreateRoundedBox(root, name, position, PhoneScreenSize, B.PhoneScreenColor, rounded).transform;
            var title = B.CreateLabel(screen, "Caption", caption, PhoneCaptionFontSize, Vector2.zero, PhoneCaptionSize);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, TopCenter, Vector2.down * PhoneCaptionMargin, PhoneCaptionSize);
            B.CreatePhoneMenuIcon(screen);
            return screen;
        }

        /// <summary>投げる番の下の球種の札（試合の HUD と同じ所・同じ色）</summary>
        private static void CreatePhonePitchCards(Transform screen, float scale)
        {
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(B.RoundedSpritePath);
            var size = CardSize * scale;
            for (var i = 0; i < PhoneCardLabels.Length; i++)
            {
                var card = B.CreateRoundedBox(screen, $"Pitch{i}", Vector2.zero, size, CardColors[i], rounded);
                Place(card.rectTransform, BottomCenter, new Vector2(CardX[i], CardY) * scale, size);
                B.CreateLabel(card.transform, "Text", PhoneCardLabels[i], PhoneCardFontSize, Vector2.zero, size);
            }
        }

        /// <summary>魔球・必殺打の札（球種の札の左。打つ番にも出る）</summary>
        private static void CreatePhoneSpecialToggle(Transform screen, string label, float scale)
        {
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>(B.RoundedSpritePath);
            var size = SpecialToggleSize * scale;
            var toggle = B.CreateRoundedBox(screen, "SpecialToggle", Vector2.zero, size, SpecialToggleColor, rounded);
            Place(toggle.rectTransform, BottomCenter, SpecialTogglePosition * scale, size);
            B.CreateLabel(toggle.transform, "Text", label, PhoneCardFontSize, Vector2.zero, size);
        }
    }
}
