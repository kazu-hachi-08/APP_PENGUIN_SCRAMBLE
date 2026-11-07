using PenmonScramble.Baseball.Game;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の HUD（BaseballHud。仮）: 左上の暗い板にカウント（B 緑3つ・S 黄2つ・O 赤2つの丸）、まん中に結果の大きな文字、
    /// 点の板・投げる子の札・ゲームセットの欄（BaseballSceneBuilder.HudMatch.cs）、
    /// 投げる番の下に球種の札3枚（PC ではキーの印つき）とその上の持ち時間、PC では下に操作の説明、
    /// スマホでは仮想スティックと「振る」「バント」のボタン（PenmonScrambleSceneBuilder.BaseballTouch.cs）
    /// </summary>
    public static partial class BaseballSceneBuilder
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static readonly Vector2 CountPlatePosition = new Vector2(190f, -130f);
        private static readonly Vector2 CountPlateSize = new Vector2(320f, 220f);
        private const float CountRowTop = -60f;
        private const float CountRowStep = 68f;
        private const float CountLabelX = 70f;
        private const float CountDotX = 135f;
        private const float CountDotStep = 62f;
        private const float CountDotSize = 48f;
        private const int CountFontSize = 48;
        private static readonly Color CountOffColor = new Color(0.22f, 0.24f, 0.30f);

        private static readonly Vector2 BannerPosition = new Vector2(0f, 200f);
        private static readonly Vector2 BannerSize = new Vector2(1500f, 220f);
        private const int BannerFontSize = 140;

        private static readonly float[] CardX = { -100f, 230f, 560f };
        private const float CardY = 190f;
        private static readonly Vector2 CardSize = new Vector2(300f, 170f);
        private const int CardFontSize = 44;
        private const float KeyHintY = 82f;
        private static readonly Vector2 KeyHintSize = new Vector2(300f, 40f);
        private const int KeyHintFontSize = 28;
        private static readonly Vector2 ClockPosition = new Vector2(230f, 318f);
        private static readonly Vector2 ClockSize = new Vector2(400f, 60f);
        private const int ClockFontSize = 44;

        private static readonly Vector2 HelpPosition = new Vector2(0f, 30f);
        private static readonly Vector2 HelpSize = new Vector2(1860f, 48f);
        private const int HelpFontSize = 26;

        private static readonly string[] CardLabels =
        {
            "ストレート\n<size=24>速い・曲がりにくい</size>",
            "カーブ\n<size=24>ふつう・よく曲がる</size>",
            "チェンジアップ\n<size=24>遅い・少し曲がる</size>",
        };

        private static readonly string[] KeyHints = { "J / ×", "K / ○", "L / △" };

        private static readonly Color[] CardColors =
        {
            new Color(1f, 0.45f, 0.30f),
            new Color(0.30f, 0.58f, 1f),
            new Color(0.32f, 0.78f, 0.45f),
        };

        private readonly struct BaseballHudParts
        {
            public readonly BaseballHud Hud;
            public readonly PenmonScrambleSceneBuilder.BaseballTouchParts Touch;

            public BaseballHudParts(BaseballHud hud, PenmonScrambleSceneBuilder.BaseballTouchParts touch)
            {
                Hud = hud;
                Touch = touch;
            }
        }

        /// <summary>試しの打席・試合を始めるまで隠しておく（「準備中」の板と重ならないように）</summary>
        private static BaseballHudParts CreateHud(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("BaseballHud", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            var safe = B.CreateStretchGroup(root.transform, "Safe");
            safe.AddComponent<SafeAreaFitter>();
            var hud = root.AddComponent<BaseballHud>();
            var so = new SerializedObject(hud);

            CreateCount(safe.transform, so);
            so.FindProperty("_banner").objectReferenceValue = CreateBanner(safe.transform);
            CreatePitchCards(safe.transform, so);
            var help = B.CreateLabel(safe.transform, "Help", string.Empty, HelpFontSize, Vector2.zero, HelpSize);
            Place(help.rectTransform, BottomCenter, HelpPosition, HelpSize);
            so.FindProperty("_help").objectReferenceValue = help;

            var touch = B.CreateBaseballTouchControls(root.transform);
            so.FindProperty("_touch").objectReferenceValue = touch.Visibility;
            B.SetObjectArray(so.FindProperty("_batButtons"), new Object[] { touch.Swing.gameObject, touch.Bunt.gameObject });
            so.FindProperty("_bendStick").objectReferenceValue = touch.Joystick.gameObject;
            // ゲームセットの欄はスマホのボタンより上に重ねるので、後から作る
            CreateMatchHud(safe.transform, root.transform, so);
            so.ApplyModifiedProperties();

            root.SetActive(false);
            return new BaseballHudParts(hud, touch);
        }

        /// <summary>暗い板の上に「B」「S」「O」の字と、それぞれの丸（ボール3・ストライク2・アウト2）</summary>
        private static void CreateCount(Transform safe, SerializedObject so)
        {
            var plate = B.CreateHudImage(safe, "CountPlate", null, B.HudBackColor, TopLeft, CountPlatePosition, CountPlateSize);
            var rows = new[] { ("B", 3, "_ballLights"), ("S", 2, "_strikeLights"), ("O", 2, "_outLights") };
            for (var r = 0; r < rows.Length; r++)
            {
                var (letter, count, property) = rows[r];
                var y = CountRowTop - CountRowStep * r;
                var label = B.CreateLabel(plate.transform, letter, letter, CountFontSize, Vector2.zero, Vector2.one * CountDotSize);
                label.fontStyle = FontStyle.Bold;
                Place(label.rectTransform, TopLeft, new Vector2(CountLabelX, y), Vector2.one * CountDotSize * 1.2f);
                var lights = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    lights[i] = B.CreateHudImage(plate.transform, $"{letter}{i}", B.TouchCirclePath, CountOffColor, TopLeft,
                        new Vector2(CountDotX + CountDotStep * i, y), Vector2.one * CountDotSize);
                }

                B.SetObjectArray(so.FindProperty(property), lights);
            }
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

        /// <summary>球種の札（押すと投げる）と、札の下のキーの印・上の持ち時間</summary>
        private static void CreatePitchCards(Transform safe, SerializedObject so)
        {
            var group = B.CreateStretchGroup(safe, "PitchCards");
            var buttons = new Object[CardLabels.Length];
            var hints = new Object[CardLabels.Length];
            for (var i = 0; i < CardLabels.Length; i++)
            {
                var button = B.CreateTitleButton(group.transform, $"Pitch{i}", CardLabels[i], Vector2.zero, CardSize, CardColors[i], CardFontSize);
                Place((RectTransform)button.transform, BottomCenter, new Vector2(CardX[i], CardY), CardSize);
                buttons[i] = button;
                var hint = B.CreateLabel(group.transform, $"Key{i}", KeyHints[i], KeyHintFontSize, Vector2.zero, KeyHintSize);
                Place(hint.rectTransform, BottomCenter, new Vector2(CardX[i], KeyHintY), KeyHintSize);
                hints[i] = hint.gameObject;
            }

            var clock = B.CreateLabel(group.transform, "Clock", string.Empty, ClockFontSize, Vector2.zero, ClockSize);
            clock.fontStyle = FontStyle.Bold;
            Place(clock.rectTransform, BottomCenter, ClockPosition, ClockSize);

            so.FindProperty("_pitchCards").objectReferenceValue = group;
            B.SetObjectArray(so.FindProperty("_pitchButtons"), buttons);
            B.SetObjectArray(so.FindProperty("_keyHints"), hints);
            so.FindProperty("_clock").objectReferenceValue = clock;
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
