using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ミニゲームを選ぶ画面（MiniGameHubScreen）: 1段目はミニゲームのカードの列、2段目はコース選び（◀ ▶・ベスト・鍵）と「ひとりで」。
    /// ロビーの左の列の「ミニゲーム」から開く（ボタンは CreateTitleMenu が作ってつなぐ）。
    /// ミニゲームのシーンは各ミニゲームの SceneBuilder（GolfSceneBuilder など）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string MiniGameIconPath = UiArtFolder + "IconMiniGame.png";
        private static readonly Color MiniGamesColor = new Color(0.25f, 0.70f, 0.60f);

        private static readonly Vector2 HubHeadingPosition = new Vector2(0f, 466f);
        private static readonly Vector2 HubHeadingSize = new Vector2(700f, 100f);

        // 1段目: カード（絵・名前・説明）。並べる間隔は MiniGameHubScreen が持つ
        private static readonly Vector2 HubCardPosition = new Vector2(0f, -40f);
        private static readonly Vector2 HubCardSize = new Vector2(560f, 700f);
        private const float HubCardSpacing = 640f;
        private static readonly Vector2 HubCardArtPosition = new Vector2(0f, 110f);
        private const float HubCardArtSize = 360f;
        private static readonly Vector2 HubCardNamePosition = new Vector2(0f, -140f);
        private static readonly Vector2 HubCardNameSize = new Vector2(500f, 90f);
        private const int HubCardNameFontSize = 56;
        private static readonly Vector2 HubCardDescPosition = new Vector2(0f, -250f);
        private static readonly Vector2 HubCardDescSize = new Vector2(480f, 110f);
        private const int HubCardDescFontSize = 32;

        // 2段目: コースのカードと「ひとりで」
        private static readonly Vector2 HubDescPosition = new Vector2(0f, 330f);
        private static readonly Vector2 HubDescSize = new Vector2(1400f, 70f);
        private const int HubDescFontSize = 40;
        private static readonly Vector2 CourseCardPosition = new Vector2(0f, 20f);
        private static readonly Vector2 CourseCardSize = new Vector2(1000f, 420f);
        private const float CourseArrowX = 410f;
        private static readonly Vector2 CourseArrowSize = new Vector2(110f, 220f);
        // 枠の高さは字の行の高さ（字の大きさの 1.45 倍）が収まるように
        private static readonly Vector2 CourseNoPosition = new Vector2(0f, 140f);
        private static readonly Vector2 CourseNoSize = new Vector2(680f, 70f);
        private static readonly Vector2 CourseNamePosition = new Vector2(0f, 30f);
        private static readonly Vector2 CourseNameSize = new Vector2(680f, 120f);
        private static readonly Vector2 CourseInfoPosition = new Vector2(0f, -95f);
        private static readonly Vector2 CourseInfoSize = new Vector2(680f, 70f);
        private const int CourseNoFontSize = 40;
        private const int CourseNameFontSize = 80;
        private const int CourseInfoFontSize = 42;
        private static readonly Vector2 HubSoloPosition = new Vector2(0f, -340f);
        private static readonly Vector2 HubSoloSize = new Vector2(420f, 160f);
        private const string HubSoloLabel = "ひとりで";

        // ページを切り替えるたびに、下から少し持ち上がって出る
        private static readonly Vector2 HubPageSlideFrom = new Vector2(0f, -120f);

        /// <summary>
        /// 画面を作る。部品の入った「Screen」は閉じた状態で、MiniGameHubScreen を付けた外側は開いたままにする
        /// （ミニゲームのシーンから戻ったときに、自分で開くため）
        /// </summary>
        private static MiniGameHubScreen CreateMiniGameHub(Transform canvas)
        {
            var root = CreateStretchGroup(canvas, "MiniGames");
            var screen = CreateFullScreenBacking(root.transform, "Screen", ScreenBackColor);
            var back = CreateTitleButton(screen.transform, "Back", "もどる", PenmonBackPosition, PenmonBackSize, CancelColor, MenuFontSize);
            var heading = CreateLabel(screen.transform, "Heading", string.Empty, PenmonHeadingFontSize, HubHeadingPosition, HubHeadingSize);
            heading.fontStyle = FontStyle.Bold;
            var coin = CreateCoinPill(screen.transform);

            var games = CreateStretchGroup(screen.transform, "Games");
            SetSlideIn(games, HubPageSlideFrom, 0f);
            var template = CreateMiniGameCard(games.transform);

            var courses = CreateStretchGroup(screen.transform, "Courses");
            SetSlideIn(courses, HubPageSlideFrom, 0f);
            var description = CreateLabel(courses.transform, "Description", string.Empty, HubDescFontSize, HubDescPosition, HubDescSize);
            var card = CreatePanel(courses.transform, "CourseCard", CourseCardPosition, CourseCardSize).transform;
            var no = CreateLabel(card, "No", string.Empty, CourseNoFontSize, CourseNoPosition, CourseNoSize);
            no.color = StageNoColor;
            var name = CreateLabel(card, "Name", string.Empty, CourseNameFontSize, CourseNamePosition, CourseNameSize);
            name.fontStyle = FontStyle.Bold;
            var info = CreateLabel(card, "Info", string.Empty, CourseInfoFontSize, CourseInfoPosition, CourseInfoSize);
            info.fontStyle = FontStyle.Bold;
            var prev = CreateTitleButton(card, "PrevCourse", "◀", new Vector2(-CourseArrowX, 0f), CourseArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextCourse", "▶", new Vector2(CourseArrowX, 0f), CourseArrowSize, ArrowColor, MenuFontSize);
            var solo = CreateTitleButton(courses.transform, "Solo", HubSoloLabel, HubSoloPosition, HubSoloSize, SoloColor, SoloButtonFontSize);

            var hub = root.AddComponent<MiniGameHubScreen>();
            var so = new SerializedObject(hub);
            so.FindProperty("_screen").objectReferenceValue = screen;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_headingText").objectReferenceValue = heading;
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_gamesPage").objectReferenceValue = games;
            so.FindProperty("_cardTemplate").objectReferenceValue = template;
            so.FindProperty("_cardSpacing").floatValue = HubCardSpacing;
            SetObjectArray(so.FindProperty("_cardArt"), MiniGameCardArt());
            so.FindProperty("_coursePage").objectReferenceValue = courses;
            so.FindProperty("_descriptionText").objectReferenceValue = description;
            so.FindProperty("_courseNoText").objectReferenceValue = no;
            so.FindProperty("_courseNameText").objectReferenceValue = name;
            so.FindProperty("_courseInfoText").objectReferenceValue = info;
            so.FindProperty("_prevCourseButton").objectReferenceValue = prev;
            so.FindProperty("_nextCourseButton").objectReferenceValue = next;
            so.FindProperty("_soloButton").objectReferenceValue = solo;
            so.ApplyModifiedProperties();
            screen.SetActive(false);
            return hub;
        }

        /// <summary>ロビーの左の列の「ミニゲーム」を、画面の開くボタンにつなぐ</summary>
        private static void WireMiniGameHub(MiniGameHubScreen hub, Button openButton)
        {
            var so = new SerializedObject(hub);
            so.FindProperty("_openButton").objectReferenceValue = openButton;
            so.ApplyModifiedProperties();
        }

        /// <summary>カードの型（実行時に MiniGameTable の数だけ複製する）。押せる板に、絵・名前・説明</summary>
        private static MiniGameCard CreateMiniGameCard(Transform parent)
        {
            var button = CreateTitleButton(parent, "CardTemplate", string.Empty, HubCardPosition, HubCardSize, MiniGamesColor, MenuFontSize);
            var artObj = UIDialogBuilder.CreateUIObject("Art", button.transform);
            PlaceCenter(artObj.GetComponent<RectTransform>(), HubCardArtPosition, Vector2.one * HubCardArtSize);
            var art = artObj.AddComponent<Image>();
            art.preserveAspect = true;
            art.raycastTarget = false;
            var name = CreateLabel(button.transform, "Name", string.Empty, HubCardNameFontSize, HubCardNamePosition, HubCardNameSize);
            name.fontStyle = FontStyle.Bold;
            var description = CreateLabel(button.transform, "Description", string.Empty, HubCardDescFontSize, HubCardDescPosition, HubCardDescSize);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;

            var card = button.gameObject.AddComponent<MiniGameCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_art").objectReferenceValue = art;
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.FindProperty("_descriptionText").objectReferenceValue = description;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>
        /// カードの絵（MiniGameTable.All と同じ並び）。ゴルフはコースができるまで旗の絵（フェーズ 3 以降でコースを撮った絵にする）
        /// </summary>
        private static Object[] MiniGameCardArt()
        {
            var games = MiniGameTable.All;
            var art = new Object[games.Count];
            for (var i = 0; i < games.Count; i++)
            {
                art[i] = games[i].Id == MiniGameTable.GolfId ? UiSprite(MiniGameIconPath) : null;
            }

            return art;
        }

        // ---- 絵: 芝の上の旗とカップと球（ロビーの「ミニゲーム」のアイコンとゴルフのカード） ----

        private static readonly Color IconGrassColor = new Color(0.47f, 0.82f, 0.36f);
        private static readonly Color IconHoleColor = new Color(0.13f, 0.28f, 0.14f);
        private static readonly Color IconPoleColor = new Color(0.96f, 0.96f, 0.92f);
        private static readonly Color IconFlagColor = new Color(1f, 0.36f, 0.34f);
        private static readonly Color IconBallColor = Color.white;
        private static readonly Color IconBallRimColor = new Color(0.62f, 0.66f, 0.72f);

        private static void CreateMiniGameArt()
        {
            WriteUiSprite(MiniGameIconPath, DrawIcon(MiniGamePixel), 0);
        }

        /// <summary>奥から重ねる: 芝の丸 → カップ → 旗の棒 → 旗 → 球</summary>
        private static Color MiniGamePixel(Vector2 p, float pixel)
        {
            var color = Color.clear;
            color = Over(color, IconGrassColor, IconCoverage(EllipseDistance(p - new Vector2(0f, -0.66f), new Vector2(0.9f, 0.26f)), pixel));
            color = Over(color, IconHoleColor, IconCoverage(EllipseDistance(p - new Vector2(-0.18f, -0.64f), new Vector2(0.22f, 0.07f)), pixel));
            color = Over(color, IconPoleColor, IconCoverage(BoxDistance(p - new Vector2(-0.18f, 0.1f), new Vector2(0.04f, 0.74f)), pixel));
            var flag = TriangleDistance(p, new Vector2(-0.14f, 0.84f), new Vector2(-0.14f, 0.3f), new Vector2(0.66f, 0.57f));
            color = Over(color, IconFlagColor, IconCoverage(flag, pixel));
            var ball = (p - new Vector2(0.46f, -0.5f)).magnitude - 0.15f;
            color = Over(color, IconBallRimColor, IconCoverage(ball, pixel));
            color = Over(color, IconBallColor, IconCoverage(ball + 0.03f, pixel));
            return color;
        }

        /// <summary>だ円の縁からのおおよその距離（内側が負）。アイコンの縁のぼかしに使うだけなので近い値で足りる</summary>
        private static float EllipseDistance(Vector2 p, Vector2 radius)
        {
            return (new Vector2(p.x / radius.x, p.y / radius.y).magnitude - 1f) * Mathf.Min(radius.x, radius.y);
        }

        private static float BoxDistance(Vector2 p, Vector2 half)
        {
            var d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half;
            return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        /// <summary>三角形の縁からの距離（内側が負。iq さんの sdTriangle）</summary>
        private static float TriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            var pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            var pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            var pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            var s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            var d0 = new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x));
            var d1 = new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x));
            var d2 = new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x));
            var d = Vector2.Min(Vector2.Min(d0, d1), d2);
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }
    }
}
