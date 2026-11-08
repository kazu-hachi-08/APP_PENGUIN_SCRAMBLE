using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ゴルフの遊び方。画面は本編の遊び方と同じ（タブ・ルールのページ・押したキーが光る絵）で、
    /// 操作の絵だけゴルフのものを描く（キーボード・PS5・スマホ）。ルールのページは GolfSceneRunner が起動時に GolfHowToPlayPages へ差し替える。
    /// スマホの絵は、ゴルフの HUD の配置（打つ大ボタン・特殊のボタン・打つ子のカード）を縮めて描く
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        // キーボード: 左に向き（A D と ← →）、右に J K L、左上に Esc（本編の絵と同じ並び方）
        private static readonly Vector2 AimKeysCenter = new Vector2(-400f, -110f);
        private const float AimPairGap = 240f;
        private static readonly Vector2 AimLabelSize = new Vector2(700f, 60f);
        private const string KeyboardTip = "J でゲージを動かし、もう一度 J で止めて打つ。右ドラッグで見回す。押したキーが光ります";

        // スマホ: ドラッグで狙う案内（画面の絵のまん中）
        private const int PhoneDragFontSize = 30;
        private static readonly Vector2 PhoneDragSize = new Vector2(500f, 60f);
        private static readonly Vector2 PhoneCaptionOffset = new Vector2(0f, 60f);
        private static readonly Vector2 PhoneCaptionSize = new Vector2(240f, 40f);
        private const int PhoneCaptionFontSize = 24;
        private static readonly Color PhoneCardColor = new Color(0.95f, 0.96f, 1f);
        private const string TouchTip = "ボタンの無い所を横にドラッグで向き。「打つ」でゲージ → もう一度で打つ。2本指で見回す";

        private static HowToPlayScreen CreateHowToPlay(Transform canvas)
        {
            return B.CreateHowToPlayScreen(canvas, parent => new[]
            {
                CreateGolfKeyboardPanel(parent),
                CreateGolfGamepadPanel(parent),
                CreateGolfTouchPanel(parent),
            });
        }

        private static GameObject CreateGolfKeyboardPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Keyboard");
            var root = panel.transform;
            CreateKeyPair(root, AimKeysCenter + Vector2.left * (AimPairGap * 0.5f + B.KeyPitch * 0.5f), "A", Key.A, "D", Key.D);
            B.CreateLabel(root, "Or", "または", B.ControlTipFontSize, AimKeysCenter, B.KeyLabelSize);
            CreateKeyPair(root, AimKeysCenter + Vector2.right * (AimPairGap * 0.5f + B.KeyPitch * 0.5f), "←", Key.LeftArrow, "→", Key.RightArrow);
            B.CreateLabel(root, "AimLabel", "向き（押し続けると速く回る）", B.KeyWordFontSize, AimKeysCenter + Vector2.down * B.KeyLabelGap, AimLabelSize);

            var actions = new[] { ("J", Key.J, "打つ"), ("K", Key.K, "打つ子"), ("L", Key.L, "特殊") };
            for (var i = 0; i < actions.Length; i++)
            {
                var (label, key, action) = actions[i];
                var position = new Vector2(B.SkillKeysX + i * (B.KeyPitch + 20f), B.SkillKeysY);
                B.CreateKeyCap(root, label, position, Vector2.one * B.KeySize, B.KeyFontSize, new[] { key });
                B.CreateLabel(root, $"{label}Label", action, B.KeyWordFontSize, position + Vector2.down * B.KeyLabelGap, B.KeyLabelSize);
            }

            B.CreateKeyCap(root, "Esc", B.EscPosition, Vector2.one * B.KeySize, B.KeyWordFontSize, new[] { Key.Escape, Key.P });
            var escLabel = B.CreateLabel(root, "EscLabel", "メニュー（P でも）", B.KeyWordFontSize, B.EscPosition + new Vector2(B.KeySize * 0.5f + 200f, 0f), new Vector2(380f, 60f));
            escLabel.alignment = TextAnchor.MiddleLeft;

            B.CreateControlTip(root, KeyboardTip);
            return panel;
        }

        /// <summary>左右の2つのキーを、center を挟んで並べる</summary>
        private static void CreateKeyPair(Transform parent, Vector2 center, string left, Key leftKey, string right, Key rightKey)
        {
            var offset = Vector2.right * (B.KeyPitch * 0.5f);
            B.CreateKeyCap(parent, left, center - offset, Vector2.one * B.KeySize, B.KeyFontSize, new[] { leftKey });
            B.CreateKeyCap(parent, right, center + offset, Vector2.one * B.KeySize, B.KeyFontSize, new[] { rightKey });
        }

        private static GameObject CreateGolfGamepadPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Gamepad");
            var root = panel.transform;
            // 左の列: 向き・見回す・メニュー / 右の列: 打つ・打つ子・特殊（本編と同じく、よく押すボタンを右手側に）
            B.CreatePadRow(root, 0, 0, "LStick", "L", new[] { "leftStick", "dpad" }, B.PadNeutralColor, true, "向き（左スティック / 方向キー）");
            B.CreatePadRow(root, 0, 1, "RStick", "R", new[] { "rightStick" }, B.PadNeutralColor, true, "見回す（右スティック）");
            B.CreatePadRow(root, 0, 2, "Options", "OPTIONS", new[] { "start" }, B.PadNeutralColor, false, "メニュー");
            B.CreatePadRow(root, 1, 0, "Cross", "×", new[] { "buttonSouth" }, B.PadCrossColor, true, "打つ");
            B.CreatePadRow(root, 1, 1, "Circle", "○", new[] { "buttonEast" }, B.PadCircleColor, true, "打つ子を替える");
            B.CreatePadRow(root, 1, 2, "Triangle", "△", new[] { "buttonNorth" }, B.PadTriangleColor, true, "特殊ショット");
            B.CreateControlTip(root, "× でゲージを動かし、もう一度 × で止めて打つ。押したボタンが光ります");
            return panel;
        }

        private static GameObject CreateGolfTouchPanel(Transform parent)
        {
            var panel = B.CreateStretchGroup(parent, "Touch");
            var root = panel.transform;
            var screen = B.CreatePhoneFrame(root, "PhoneBody", "PhoneScreen", B.PhoneScreenPosition);

            // ゴルフの HUD と同じ並びを、画面の絵の大きさまで縮める（基準の高さ 1080 → 絵の高さ）
            var scale = B.PhoneScreenSize.y / B.ReferenceResolution.y;
            B.CreatePhoneControl(screen, "打つ", Vector2.right, ShootPosition, ShootSize, ShootColor, scale);
            B.CreatePhoneControl(screen, "特殊", Vector2.right, SpecialButtonPosition, SpecialButtonSize, SpecialButtonColor, scale);
            for (var i = 0; i < CardCount; i++)
            {
                var position = FirstCardPosition + Vector2.right * (CardSpacing * i);
                B.CreatePhoneControl(screen, string.Empty, Vector2.zero, position, CardDiameter, PhoneCardColor, scale);
            }

            var cardsCenter = (FirstCardPosition + Vector2.right * CardSpacing) * scale;
            var caption = B.CreateLabel(screen, "CardsCaption", "打つ子", PhoneCaptionFontSize, Vector2.zero, PhoneCaptionSize);
            Place(caption.rectTransform, BottomLeft, cardsCenter + PhoneCaptionOffset, PhoneCaptionSize);
            var drag = B.CreateLabel(screen, "Drag", "← ドラッグで向き →", PhoneDragFontSize, Vector2.zero, PhoneDragSize);
            drag.fontStyle = FontStyle.Bold;
            B.CreatePhoneMenuIcon(screen);

            B.CreateControlTip(root, TouchTip);
            return panel;
        }
    }
}
