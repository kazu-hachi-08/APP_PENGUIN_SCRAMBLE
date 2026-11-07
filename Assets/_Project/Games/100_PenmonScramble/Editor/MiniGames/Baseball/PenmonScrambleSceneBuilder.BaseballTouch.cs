using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球のスマホの操作: 本編と同じ仮想スティック（左。投げる番に曲げる）と、本編の技1の位置の「振る」（右の大）・
    /// 技2の位置の「バント」（右の中。押している間構える）。置き場所と見た目は本編のタッチ操作と同じ部品（本編で覚えた手のまま遊べるように）。
    /// 球種は HUD の札を直接押す（BaseballSceneBuilder.Hud.cs）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        internal static readonly Color BaseballSwingColor = new Color(1f, 0.48f, 0.25f);
        internal static readonly Color BaseballBuntColor = new Color(0.22f, 0.62f, 1f);
        private const int BaseballSwingFontSize = 56;
        private const int BaseballBuntFontSize = 40;

        internal readonly struct BaseballTouchParts
        {
            public readonly VirtualJoystick Joystick;
            public readonly VirtualButton Swing;
            public readonly VirtualButton Bunt;
            public readonly TouchControlsVisibility Visibility;

            public BaseballTouchParts(VirtualJoystick joystick, VirtualButton swing, VirtualButton bunt, TouchControlsVisibility visibility)
            {
                Joystick = joystick;
                Swing = swing;
                Bunt = bunt;
                Visibility = visibility;
            }
        }

        internal static BaseballTouchParts CreateBaseballTouchControls(Transform parent)
        {
            var root = CreateStretchGroup(parent, "Touch");
            root.AddComponent<SafeAreaFitter>();
            root.AddComponent<CanvasGroup>();
            var visibility = root.AddComponent<TouchControlsVisibility>();
            var joystick = CreateJoystick(root.transform);
            var swing = CreateBaseballButton(root.transform, "Swing", "振る", Skill1Position, Skill1Size, BaseballSwingColor, BaseballSwingFontSize);
            var bunt = CreateBaseballButton(root.transform, "Bunt", "バント", Skill2Position, Skill2Size, BaseballBuntColor, BaseballBuntFontSize);
            return new BaseballTouchParts(joystick, swing, bunt, visibility);
        }

        /// <summary>厚みのある丸いボタンの面に、字を出す（押すと沈む）</summary>
        private static VirtualButton CreateBaseballButton(Transform parent, string name, string label, Vector2 position, float size, Color color, int fontSize)
        {
            var parts = CreateTouchButton(parent, name, position, size, color);
            CreateTouchImage(parts.Top, "Gloss", TouchGlossPath, Color.white, size * TouchFaceRatio);
            var text = CreateTouchLabel(parts.Top, label, fontSize);
            text.raycastTarget = false;
            DisableRaycasts(parts.Visual);
            return parts.Button;
        }

        /// <summary>遊び方のスマホの絵（BaseballSceneBuilder.HowToPlay）: 打つ番の「振る」「バント」を、試合と同じ配置で縮めて描く</summary>
        internal static void CreateBaseballPhoneBatButtons(Transform screen, float scale)
        {
            CreatePhoneControl(screen, "振る", Vector2.right, Skill1Position, Skill1Size, BaseballSwingColor, scale);
            CreatePhoneControl(screen, "バント", Vector2.right, Skill2Position, Skill2Size, BaseballBuntColor, scale);
        }

        /// <summary>遊び方のスマホの絵: 投げる番に出る、曲げるスティック（本編の移動のスティックと同じ所）</summary>
        internal static void CreateBaseballPhoneStick(Transform screen, float scale)
        {
            CreatePhoneControl(screen, "曲げる", Vector2.zero, JoystickPosition, JoystickSize, JoystickBackColor, scale);
        }

        /// <summary>試しの打席のシーン用: 仮想スティックを InputManager に、振るを Action1・バントを Action2 につなぐ</summary>
        internal static void BindBaseballInput(InputManager manager, BaseballTouchParts touch)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("_virtualJoystick").objectReferenceValue = touch.Joystick;
            so.FindProperty("_virtualAction1Button").objectReferenceValue = touch.Swing;
            so.FindProperty("_virtualAction2Button").objectReferenceValue = touch.Bunt;
            so.FindProperty("_dontDestroyOnLoad").boolValue = false;
            so.ApplyModifiedProperties();
        }
    }
}
