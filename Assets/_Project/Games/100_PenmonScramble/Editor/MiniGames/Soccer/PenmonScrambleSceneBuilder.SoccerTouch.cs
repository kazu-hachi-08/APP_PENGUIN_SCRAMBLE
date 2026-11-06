using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーのスマホの操作: 本編と同じ仮想スティック（左）と、本編の技1の位置のシュート / スライディング（右の大）、
    /// 技2の位置のパス / ダッシュ（右の中）、必殺の位置の必殺シュート（技1の上。チームのゲージのリング）、
    /// 交代の位置の切り替え（技2の上の小。替わる子の顔を出す）。
    /// 置き場所と見た目は本編のタッチ操作と同じ部品（本編で覚えた手のまま遊べるように）で、中のアイコンだけサッカー用
    /// （持っているかで替わる。SoccerTouchView）。必殺シュートのカットインも本編と同じ帯
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color SoccerShootColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color SoccerPassColor = new Color(0.22f, 0.62f, 1f);

        internal readonly struct SoccerTouchParts
        {
            public readonly VirtualJoystick Joystick;
            public readonly VirtualButton Shoot;
            public readonly VirtualButton Pass;
            public readonly VirtualButton Special;
            public readonly VirtualButton Switch;
            public readonly TouchControlsVisibility Visibility;
            public readonly SoccerTouchView View;

            public SoccerTouchParts(VirtualJoystick joystick, VirtualButton shoot, VirtualButton pass, VirtualButton special, VirtualButton swap, TouchControlsVisibility visibility, SoccerTouchView view)
            {
                Joystick = joystick;
                Shoot = shoot;
                Pass = pass;
                Special = special;
                Switch = swap;
                Visibility = visibility;
                View = view;
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
            var pass = CreateSkillButton(root.transform, "Pass", Skill2Position, Skill2Size, SoccerPassColor);
            var special = CreateSpecialButton(root.transform);
            var swap = CreateSwitchButton(root.transform);
            ShowIcon(shoot, SoccerShootIconPath);
            ShowIcon(pass, SoccerPassIconPath);

            var view = root.AddComponent<SoccerTouchView>();
            var so = new SerializedObject(view);
            so.FindProperty("_shoot").objectReferenceValue = shoot.GetComponent<TouchSkillButton>();
            so.FindProperty("_pass").objectReferenceValue = pass.GetComponent<TouchSkillButton>();
            so.FindProperty("_switch").objectReferenceValue = swap.GetComponent<TouchSkillButton>();
            so.FindProperty("_special").objectReferenceValue = special.GetComponent<TouchSpecialButton>();
            so.FindProperty("_shootIcon").objectReferenceValue = UiSprite(SoccerShootIconPath);
            so.FindProperty("_slideIcon").objectReferenceValue = UiSprite(SoccerSlideIconPath);
            so.FindProperty("_passIcon").objectReferenceValue = UiSprite(SoccerPassIconPath);
            so.FindProperty("_dashIcon").objectReferenceValue = UiSprite(SoccerDashIconPath);
            so.ApplyModifiedProperties();
            return new SoccerTouchParts(joystick, shoot, pass, special, swap, visibility, view);
        }

        /// <summary>必殺シュートのカットイン（本編の必殺技と同じ、画面の真ん中を横切る帯）</summary>
        internal static SpecialCutIn CreateSoccerCutIn(Transform canvas) => CreateCutIn(canvas);

        /// <summary>技のボタンのアイコンの場所に、決まった絵を出しておく（本編は場に出ている子の技の形を後から入れる）</summary>
        private static void ShowIcon(VirtualButton button, string spritePath)
        {
            var icon = button.transform.Find("Visual/Top/Icon").GetComponent<Image>();
            icon.sprite = UiSprite(spritePath);
            icon.enabled = true;
        }
    }
}
