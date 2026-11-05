using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーのスマホの操作: 本編と同じ仮想スティック（左）と、本編の技1の位置のシュートのボタン（右の大）。
    /// 置き場所と見た目は本編のタッチ操作と同じ部品（本編で覚えた手のまま遊べるように）で、中のアイコンだけサッカー用。
    /// パス・必殺シュート・切り替えのボタンは、Core にその動きを足すフェーズ 3・4 で同じ位置（本編の技2・必殺・交代）に足す
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color SoccerShootColor = new Color(1f, 0.55f, 0.2f);

        internal readonly struct SoccerTouchParts
        {
            public readonly VirtualJoystick Joystick;
            public readonly VirtualButton Shoot;
            public readonly TouchControlsVisibility Visibility;

            public SoccerTouchParts(VirtualJoystick joystick, VirtualButton shoot, TouchControlsVisibility visibility)
            {
                Joystick = joystick;
                Shoot = shoot;
                Visibility = visibility;
            }
        }

        internal static SoccerTouchParts CreateSoccerTouchControls(Transform parent)
        {
            var root = CreateStretchGroup(parent, "Touch");
            root.AddComponent<SafeAreaFitter>();
            root.AddComponent<CanvasGroup>();
            var visibility = root.AddComponent<TouchControlsVisibility>();
            var joystick = CreateJoystick(root.transform);
            var shoot = CreateSkillButton(root.transform, "Shoot", Skill1Position, Skill1Size, SoccerShootColor);
            ShowIcon(shoot, SoccerShootIconPath);
            return new SoccerTouchParts(joystick, shoot, visibility);
        }

        /// <summary>技のボタンのアイコンの場所に、決まった絵を出しておく（本編は場に出ている子の技の形を後から入れる）</summary>
        private static void ShowIcon(VirtualButton button, string spritePath)
        {
            var icon = button.transform.Find("Visual/Top/Icon").GetComponent<Image>();
            icon.sprite = UiSprite(spritePath);
            icon.enabled = true;
        }
    }
}
