using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using PenmonScramble.Soccer.Core;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーの HUD（仮。SoccerHud）: 上のまん中に赤・青の丸の点と、その間の時間。まん中に知らせ、PC では下に操作の説明、
    /// スマホでは仮想スティックと技1・技2・交代のボタン（PenmonScrambleSceneBuilder.SoccerTouch.cs）。
    /// 試合の流れ・必殺のゲージ・PK戦の表はフェーズ 4 で足す
    /// </summary>
    public static partial class SoccerSceneBuilder
    {
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static readonly Vector2 ScoreOffset = new Vector2(150f, -82f);
        private const float ScoreSize = 120f;
        private const float ScoreRimRatio = 1.1f;
        private const int ScoreFontSize = 72;
        private static readonly Vector2 TimePosition = new Vector2(0f, -82f);
        private static readonly Vector2 TimePlateSize = new Vector2(170f, 70f);
        private const int TimeFontSize = 44;
        private static readonly Vector2 BannerPosition = new Vector2(0f, 160f);
        private static readonly Vector2 BannerSize = new Vector2(1500f, 220f);
        private const int BannerFontSize = 150;
        private static readonly Vector2 HelpPosition = new Vector2(0f, 34f);
        private static readonly Vector2 HelpSize = new Vector2(1700f, 48f);
        private const int HelpFontSize = 26;
        private const string HelpText = "移動: WASD / 左スティック　シュート（ためて離す）/ スライディング: J / ×　パス / ダッシュ: K / ○　交代: Space / L1　もどる: Esc";

        private readonly struct SoccerHudParts
        {
            public readonly SoccerHud Hud;
            public readonly PenmonScrambleSceneBuilder.SoccerTouchParts Touch;

            public SoccerHudParts(SoccerHud hud, PenmonScrambleSceneBuilder.SoccerTouchParts touch)
            {
                Hud = hud;
                Touch = touch;
            }
        }

        /// <summary>試しのピッチを始めるまで隠しておく（ミニゲーム画面から来たときは準備中の板だけ）</summary>
        private static SoccerHudParts CreateHud(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("SoccerHud", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            var safe = B.CreateStretchGroup(root.transform, "Safe");
            safe.AddComponent<SafeAreaFitter>();
            var hud = root.AddComponent<SoccerHud>();
            var so = new SerializedObject(hud);

            so.FindProperty("_redScore").objectReferenceValue = CreateScore(safe.transform, SoccerRules.Red, -1f);
            so.FindProperty("_blueScore").objectReferenceValue = CreateScore(safe.transform, SoccerRules.Blue, 1f);
            so.FindProperty("_time").objectReferenceValue = CreateTime(safe.transform);
            so.FindProperty("_banner").objectReferenceValue = CreateBanner(safe.transform);
            var help = B.CreateLabel(safe.transform, "Help", HelpText, HelpFontSize, Vector2.zero, HelpSize);
            Place(help.rectTransform, BottomCenter, HelpPosition, HelpSize);
            so.FindProperty("_help").objectReferenceValue = help.gameObject;
            var touch = B.CreateSoccerTouchControls(root.transform);
            so.FindProperty("_touch").objectReferenceValue = touch.Visibility;
            so.FindProperty("_touchView").objectReferenceValue = touch.View;
            so.ApplyModifiedProperties();

            root.SetActive(false);
            return new SoccerHudParts(hud, touch);
        }

        /// <summary>チームの色の丸（白い縁）に、点の数字。side は真ん中から左（−1）か右（+1）か</summary>
        private static Text CreateScore(Transform safe, int team, float side)
        {
            var position = new Vector2(ScoreOffset.x * side, ScoreOffset.y);
            B.CreateHudImage(safe, "ScoreRim", B.TouchCirclePath, Color.white, TopCenter, position, Vector2.one * (ScoreSize * ScoreRimRatio));
            var disc = B.CreateHudImage(safe, "Score", B.TouchCirclePath, SoccerColors.Team(team), TopCenter, position, Vector2.one * ScoreSize);
            var text = B.CreateLabel(disc.transform, "Value", "0", ScoreFontSize, Vector2.zero, Vector2.one * ScoreSize);
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        /// <summary>点の間の、暗い板の上の時間</summary>
        private static Text CreateTime(Transform safe)
        {
            var plate = B.CreateHudImage(safe, "TimePlate", null, B.HudBackColor, TopCenter, TimePosition, TimePlateSize);
            var text = B.CreateLabel(plate.transform, "Time", "0:00", TimeFontSize, Vector2.zero, TimePlateSize);
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        private static Text CreateBanner(Transform safe)
        {
            var text = B.CreateLabel(safe, "Banner", string.Empty, BannerFontSize, BannerPosition, BannerSize);
            text.fontStyle = FontStyle.Bold;
            text.gameObject.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -8f);
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            return text;
        }

        /// <summary>anchor の点から position だけずらした所に置く（CreateLabel などはまん中に置くので、留める角を付け替える）</summary>
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
