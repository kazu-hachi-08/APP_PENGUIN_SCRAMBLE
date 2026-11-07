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
    /// サッカーの HUD（SoccerHud）: 上のまん中に赤・青の丸の点と、その間の残り時間、点の下にチームの必殺ゲージ。
    /// まん中に知らせ、PC では下に操作の説明と必殺の一言、スマホでは仮想スティックと技1・技2・必殺・交代のボタン
    /// （PenmonScrambleSceneBuilder.SoccerTouch.cs）。右上にメニューの「‖」。PK戦の表とためゲージは SoccerSceneBuilder.HudShootout.cs、
    /// みんなでの頭の上の名前と MVP の板は SoccerSceneBuilder.HudOnline.cs
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
        private static readonly Vector2 GaugeOffset = new Vector2(150f, -168f);
        private static readonly Vector2 GaugeSize = new Vector2(190f, 20f);
        private const float GaugeBackPadding = 6f;
        private static readonly Vector2 BannerPosition = new Vector2(0f, 160f);
        private static readonly Vector2 BannerSize = new Vector2(1500f, 220f);
        private const int BannerFontSize = 150;
        private static readonly Vector2 HelpPosition = new Vector2(0f, 34f);
        private static readonly Vector2 HelpSize = new Vector2(1800f, 48f);
        private const int HelpFontSize = 24;
        private static readonly Vector2 SpecialHintPosition = new Vector2(0f, 96f);
        private static readonly Vector2 SpecialHintSize = new Vector2(900f, 60f);
        private const int SpecialHintFontSize = 40;
        private static readonly Color SpecialHintColor = new Color(1f, 0.85f, 0.25f);
        private const string HelpText = "移動 WASD / 左スティック　パス / ダッシュ J / ×　シュート（ためて離す）/ スライディング K / ○　必殺 L / △　交代 Space / L1　メニュー Esc";
        private const string SpecialHintText = "L / △ で必殺シュート！";

        private readonly struct SoccerHudParts
        {
            public readonly SoccerHud Hud;
            public readonly PenmonScrambleSceneBuilder.SoccerTouchParts Touch;
            public readonly Button Menu;
            public readonly SoccerNameTags NameTags;

            public SoccerHudParts(SoccerHud hud, PenmonScrambleSceneBuilder.SoccerTouchParts touch, Button menu, SoccerNameTags nameTags)
            {
                Hud = hud;
                Touch = touch;
                Menu = menu;
                NameTags = nameTags;
            }
        }

        /// <summary>試合を始めるまで隠しておく（ミニゲーム画面から来て、まだ作っていないスタジアムのときは準備中の板だけ）</summary>
        private static SoccerHudParts CreateHud(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("SoccerHud", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            // 頭の上の名前は、点・知らせ・ボタンより奥に出す
            var nameTags = CreateNameTags(root.transform);
            var safe = B.CreateStretchGroup(root.transform, "Safe");
            safe.AddComponent<SafeAreaFitter>();
            var hud = root.AddComponent<SoccerHud>();
            var so = new SerializedObject(hud);

            so.FindProperty("_redScore").objectReferenceValue = CreateScore(safe.transform, SoccerRules.Red, -1f);
            so.FindProperty("_blueScore").objectReferenceValue = CreateScore(safe.transform, SoccerRules.Blue, 1f);
            so.FindProperty("_time").objectReferenceValue = CreateTime(safe.transform);
            CreateGauges(safe.transform, so);
            CreateShootoutBoard(safe.transform, so);
            CreatePenaltyPanel(safe.transform, so);
            CreateMvp(safe.transform, so);
            so.FindProperty("_banner").objectReferenceValue = CreateBanner(safe.transform);
            var help = B.CreateLabel(safe.transform, "Help", HelpText, HelpFontSize, Vector2.zero, HelpSize);
            Place(help.rectTransform, BottomCenter, HelpPosition, HelpSize);
            so.FindProperty("_help").objectReferenceValue = help.gameObject;
            var hint = B.CreateLabel(safe.transform, "SpecialHint", SpecialHintText, SpecialHintFontSize, Vector2.zero, SpecialHintSize);
            Place(hint.rectTransform, BottomCenter, SpecialHintPosition, SpecialHintSize);
            hint.fontStyle = FontStyle.Bold;
            hint.color = SpecialHintColor;
            so.FindProperty("_specialHint").objectReferenceValue = hint;
            var touch = B.CreateSoccerTouchControls(root.transform);
            so.FindProperty("_touch").objectReferenceValue = touch.Visibility;
            so.FindProperty("_touchView").objectReferenceValue = touch.View;
            // 右上の「‖」は本編の HUD と同じ部品（メニューは StageMenu が出し入れする）
            var menu = B.CreateMenuButton(root.transform);
            so.ApplyModifiedProperties();

            root.SetActive(false);
            return new SoccerHudParts(hud, touch, menu, nameTags);
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
            var text = B.CreateLabel(plate.transform, "Time", "3:00", TimeFontSize, Vector2.zero, TimePlateSize);
            text.fontStyle = FontStyle.Bold;
            return text;
        }

        /// <summary>点の丸の下に、暗い板の上のチームの色の棒（たまった分だけ左から伸びる）。PK戦の間は表と入れ替えて隠す</summary>
        private static void CreateGauges(Transform safe, SerializedObject so)
        {
            var group = B.CreateStretchGroup(safe, "Gauges");
            var gauges = new Object[SoccerRules.TeamCount];
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                var side = team == SoccerRules.Red ? -1f : 1f;
                var position = new Vector2(GaugeOffset.x * side, GaugeOffset.y);
                B.CreateHudImage(group.transform, "GaugeBack", null, B.HudBackColor, TopCenter, position, GaugeSize + Vector2.one * GaugeBackPadding);
                var fill = B.CreateHudImage(group.transform, "Gauge", null, SoccerColors.Team(team), TopCenter, position, GaugeSize);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = 0f;
                gauges[team] = fill;
            }

            B.SetObjectArray(so.FindProperty("_gauges"), gauges);
            so.FindProperty("_gaugeGroup").objectReferenceValue = group;
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
