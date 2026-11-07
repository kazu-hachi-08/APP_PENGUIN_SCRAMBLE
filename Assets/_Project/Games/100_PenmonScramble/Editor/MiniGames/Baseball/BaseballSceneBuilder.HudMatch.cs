using PenmonScramble.Baseball.Game;
using PenmonScramble.Common.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 試合の HUD（BaseballHud.Match.cs・.Lineup.cs・.Mvp.cs）: 上のまん中のスコアボード（2段のイニングごとの点・合計・イニング・塁の3つのひし形）、
    /// 左右の打順のカード（左が自分・右が相手。BaseballTeamCard。みんなでの2人のチームは4人の打順なので4枚）、魔球・必殺打の札（下の球種の札の左）、
    /// みんなでの結果の MVP の板（結果の間に左へ寄せたスコアボードの下）
    /// </summary>
    public static partial class BaseballSceneBuilder
    {
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);

        private static readonly Vector2 ScorePlatePosition = new Vector2(0f, -95f);
        private static readonly Vector2 ScorePlateSize = new Vector2(860f, 170f);
        private const float ScoreNameX = -300f;
        private static readonly Vector2 ScoreNameSize = new Vector2(200f, 50f);
        private static readonly float[] ScoreCellX = { -150f, -75f, 0f, 95f };
        private static readonly Vector2 ScoreCellSize = new Vector2(80f, 50f);
        private static readonly string[] ScoreHeaders = { "1", "2", "3", "計" };
        private const float ScoreHeaderY = 55f;
        private const float ScoreAwayY = 10f;
        private const float ScoreHomeY = -42f;
        private const int ScoreHeaderFontSize = 26;
        private const int ScoreCellFontSize = 40;
        private static readonly Color ScoreHeaderColor = new Color(0.7f, 0.78f, 0.95f);
        private static readonly Vector2 InningTextPosition = new Vector2(290f, 32f);
        private static readonly Vector2 InningTextSize = new Vector2(240f, 50f);
        private const int InningFontSize = 34;
        // 塁のひし形: 二塁が上、一塁が右、三塁が左（ホームから見た形）
        private static readonly Vector2 BaseDiamondCenter = new Vector2(290f, -38f);
        private static readonly Vector2[] BaseDiamondOffsets = { new Vector2(30f, -4f), new Vector2(0f, 22f), new Vector2(-30f, -4f) };
        private const float BaseDiamondSize = 28f;
        private static readonly Color BaseDiamondOff = new Color(0.22f, 0.24f, 0.30f);

        // みんなでの2人のチームは A1・B1・A2・B2 の4人の打順（BaseballTeamPlan）
        private const int LineupSize = 4;
        private const float LineupX = 190f;
        private const float LineupHeadingY = -290f;
        private static readonly Vector2 LineupHeadingSize = new Vector2(360f, 44f);
        private const int LineupHeadingFontSize = 28;
        private const float LineupCardFirstY = -350f;
        private const float LineupCardStep = 90f;
        private static readonly Vector2 LineupCardSize = new Vector2(330f, 86f);
        private const float CardRimWidth = 4f;
        private static readonly Vector2 CardFacePosition = new Vector2(-122f, 0f);
        private const float CardFaceSize = 66f;
        private static readonly Vector2 CardNamePosition = new Vector2(-20f, 0f);
        private static readonly Vector2 CardNameSize = new Vector2(140f, 60f);
        private const int CardNameFontSize = 26;
        private static readonly Vector2 CardMagicPosition = new Vector2(112f, 18f);
        private static readonly Vector2 CardHitPosition = new Vector2(112f, -20f);
        private static readonly Vector2 CardBadgeSize = new Vector2(92f, 32f);
        private const int CardBadgeFontSize = 20;
        // 持ち主の札（P1〜P4）は、カードの左上の角から少しはみ出す（顔に重ならない高さ）
        private static readonly Vector2 CardOwnerPosition = new Vector2(-122f, 40f);
        private static readonly Vector2 CardOwnerSize = new Vector2(150f, 28f);
        private const int CardOwnerFontSize = 20;

        private static readonly Vector2 MvpPosition = new Vector2(-430f, -235f);
        private static readonly Vector2 MvpSize = new Vector2(600f, 300f);
        private static readonly Vector2 MvpHeadingPosition = new Vector2(0f, 92f);
        private static readonly Vector2 MvpHeadingSize = new Vector2(560f, 90f);
        private const int MvpHeadingFontSize = 76;
        private static readonly Color MvpHeadingColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Vector2 MvpBandPosition = new Vector2(0f, -4f);
        private static readonly Vector2 MvpBandSize = new Vector2(540f, 88f);
        private static readonly Vector2 MvpNameSize = new Vector2(520f, 82f);
        private const int MvpNameFontSize = 50;
        private static readonly Vector2 MvpRbiPosition = new Vector2(0f, -100f);
        private static readonly Vector2 MvpRbiSize = new Vector2(540f, 64f);
        private const int MvpRbiFontSize = 40;

        private static readonly Vector2 SpecialTogglePosition = new Vector2(-230f, 190f);
        private static readonly Vector2 SpecialToggleSize = new Vector2(270f, 170f);
        private const int SpecialToggleFontSize = 40;
        private static readonly Color SpecialToggleColor = new Color(0.30f, 0.33f, 0.42f);

        private static void CreateMatchHud(Transform safe, SerializedObject so)
        {
            CreateScoreboard(safe, so);
            CreateLineups(safe, so);
            CreateSpecialToggle(safe, so);
            CreateMvp(safe, so);
        }

        /// <summary>スコアボード: 上に回の見出し、その下に先攻・後攻の2段（名前・1〜3回の点・合計）、右にイニングと塁のひし形</summary>
        private static void CreateScoreboard(Transform safe, SerializedObject so)
        {
            var plate = B.CreateHudImage(safe, "Scoreboard", null, B.HudBackColor, TopCenter, ScorePlatePosition, ScorePlateSize);
            for (var i = 0; i < ScoreHeaders.Length; i++)
            {
                var header = B.CreateLabel(plate.transform, $"Header{i}", ScoreHeaders[i], ScoreHeaderFontSize, new Vector2(ScoreCellX[i], ScoreHeaderY), ScoreCellSize);
                header.color = ScoreHeaderColor;
            }

            so.FindProperty("_awayName").objectReferenceValue = CreateScoreName(plate.transform, "AwayName", ScoreAwayY);
            so.FindProperty("_homeName").objectReferenceValue = CreateScoreName(plate.transform, "HomeName", ScoreHomeY);
            B.SetObjectArray(so.FindProperty("_awayCells"), CreateScoreCells(plate.transform, "Away", ScoreAwayY));
            B.SetObjectArray(so.FindProperty("_homeCells"), CreateScoreCells(plate.transform, "Home", ScoreHomeY));

            var inning = B.CreateLabel(plate.transform, "Inning", string.Empty, InningFontSize, InningTextPosition, InningTextSize);
            var lights = new Object[BaseDiamondOffsets.Length];
            for (var i = 0; i < lights.Length; i++)
            {
                var diamond = B.CreateHudImage(plate.transform, $"Base{i + 1}", null, BaseDiamondOff, Center,
                    BaseDiamondCenter + BaseDiamondOffsets[i], Vector2.one * BaseDiamondSize);
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                lights[i] = diamond;
            }

            so.FindProperty("_scoreboard").objectReferenceValue = plate.rectTransform;
            so.FindProperty("_inningText").objectReferenceValue = inning;
            B.SetObjectArray(so.FindProperty("_baseLights"), lights);
        }

        private static Text CreateScoreName(Transform plate, string name, float y)
        {
            var text = B.CreateLabel(plate, name, string.Empty, ScoreCellFontSize, new Vector2(ScoreNameX, y), ScoreNameSize);
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = true;
            return text;
        }

        private static Object[] CreateScoreCells(Transform plate, string row, float y)
        {
            var cells = new Object[ScoreCellX.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = B.CreateLabel(plate, $"{row}{i}", string.Empty, ScoreCellFontSize, new Vector2(ScoreCellX[i], y), ScoreCellSize);
                cell.fontStyle = FontStyle.Bold;
                cells[i] = cell;
            }

            return cells;
        }

        /// <summary>打順のカード: 左（カウントの板の下）に自分のチーム、右に相手のチーム。見出しは試合の中で入れる</summary>
        private static void CreateLineups(Transform safe, SerializedObject so)
        {
            var group = B.CreateStretchGroup(safe, "Lineups");
            so.FindProperty("_lineups").objectReferenceValue = group;
            so.FindProperty("_ownHeading").objectReferenceValue = CreateLineupHeading(group.transform, "OwnHeading", TopLeft, LineupX);
            so.FindProperty("_cpuHeading").objectReferenceValue = CreateLineupHeading(group.transform, "CpuHeading", TopRight, -LineupX);
            var own = new Object[LineupSize];
            var cpu = new Object[LineupSize];
            for (var i = 0; i < LineupSize; i++)
            {
                var y = LineupCardFirstY - LineupCardStep * i;
                own[i] = CreateTeamCard(group.transform, $"Own{i}", TopLeft, new Vector2(LineupX, y), true);
                cpu[i] = CreateTeamCard(group.transform, $"Cpu{i}", TopRight, new Vector2(-LineupX, y), false);
            }

            B.SetObjectArray(so.FindProperty("_ownCards"), own);
            B.SetObjectArray(so.FindProperty("_cpuCards"), cpu);
        }

        private static Text CreateLineupHeading(Transform parent, string name, Vector2 anchor, float x)
        {
            var heading = B.CreateLabel(parent, name, string.Empty, LineupHeadingFontSize, Vector2.zero, LineupHeadingSize);
            heading.fontStyle = FontStyle.Bold;
            Place(heading.rectTransform, anchor, new Vector2(x, LineupHeadingY), LineupHeadingSize);
            return heading;
        }

        /// <summary>カード1枚: 縁（今の子だけ光る）の内側に属性の色の板、左に顔・まん中に名前・右に「魔球」「必殺打」の札。自分のカードは押せる</summary>
        private static BaseballTeamCard CreateTeamCard(Transform parent, string name, Vector2 anchor, Vector2 position, bool selectable)
        {
            var rim = RoundedImage(parent, name, Color.clear, anchor, position, LineupCardSize);
            rim.raycastTarget = selectable;
            var back = RoundedImage(rim.transform, "Back", Color.gray, Center, Vector2.zero, LineupCardSize - Vector2.one * (CardRimWidth * 2f));
            back.raycastTarget = false;
            var face = B.CreateHudImage(back.transform, "Face", null, Color.white, Center, CardFacePosition, Vector2.one * CardFaceSize);
            face.raycastTarget = false;
            var label = B.CreateLabel(back.transform, "Name", string.Empty, CardNameFontSize, CardNamePosition, CardNameSize);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontStyle = FontStyle.Bold;
            var magic = CreateCardBadge(back.transform, "Magic", "魔球", CardMagicPosition, out var magicText);
            var hit = CreateCardBadge(back.transform, "Hit", "必殺打", CardHitPosition, out var hitText);
            // 持ち主の札は暗くした板の上でも席の色のまま見えるよう、縁の子にする
            var owner = CreateCardBadge(rim.transform, "Owner", string.Empty, CardOwnerPosition, out var ownerText);
            owner.rectTransform.sizeDelta = CardOwnerSize;
            ownerText.rectTransform.sizeDelta = CardOwnerSize;
            ownerText.fontSize = CardOwnerFontSize;
            ownerText.color = Color.black;
            owner.gameObject.SetActive(false);

            var card = rim.gameObject.AddComponent<BaseballTeamCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_back").objectReferenceValue = back;
            so.FindProperty("_rim").objectReferenceValue = rim;
            so.FindProperty("_face").objectReferenceValue = face;
            so.FindProperty("_name").objectReferenceValue = label;
            so.FindProperty("_magicBadge").objectReferenceValue = magic;
            so.FindProperty("_magicText").objectReferenceValue = magicText;
            so.FindProperty("_hitBadge").objectReferenceValue = hit;
            so.FindProperty("_hitText").objectReferenceValue = hitText;
            so.FindProperty("_ownerBadge").objectReferenceValue = owner;
            so.FindProperty("_ownerText").objectReferenceValue = ownerText;
            if (selectable)
            {
                var button = rim.gameObject.AddComponent<Button>();
                button.targetGraphic = rim;
                // 色の変化は BaseballTeamCard が付ける（押せない間に暗くすると、属性の色が読みにくくなる）
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                so.FindProperty("_button").objectReferenceValue = button;
            }

            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>みんなでの結果の MVP: 「MVP」の金の見出し、席の色の帯の上に名前、その下に打点。結果を出すまで隠す（サッカーと同じ形）</summary>
        private static void CreateMvp(Transform safe, SerializedObject so)
        {
            var panel = B.CreatePanel(safe, "Mvp", MvpPosition, MvpSize);
            var heading = B.CreateLabel(panel.transform, "Heading", "MVP", MvpHeadingFontSize, MvpHeadingPosition, MvpHeadingSize);
            heading.fontStyle = FontStyle.Bold;
            heading.color = MvpHeadingColor;
            var band = B.CreateHudImage(panel.transform, "Band", null, Color.white, Center, MvpBandPosition, MvpBandSize);
            var name = B.CreateLabel(band.transform, "Name", string.Empty, MvpNameFontSize, Vector2.zero, MvpNameSize);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = MvpNameFontSize / 2;
            name.resizeTextMaxSize = MvpNameFontSize;
            var rbi = B.CreateLabel(panel.transform, "Rbi", string.Empty, MvpRbiFontSize, MvpRbiPosition, MvpRbiSize);
            rbi.fontStyle = FontStyle.Bold;
            panel.SetActive(false);
            so.FindProperty("_mvp").objectReferenceValue = panel;
            so.FindProperty("_mvpName").objectReferenceValue = name;
            so.FindProperty("_mvpRbi").objectReferenceValue = rbi;
            so.FindProperty("_mvpBand").objectReferenceValue = band;
        }

        /// <summary>角の丸い板（Unity の組み込みの UISprite を9スライスで）</summary>
        private static Image RoundedImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = B.CreateHudImage(parent, name, null, color, anchor, position, size);
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(B.RoundedSpritePath);
            image.type = Image.Type.Sliced;
            return image;
        }

        private static Image CreateCardBadge(Transform parent, string name, string label, Vector2 position, out Text text)
        {
            var badge = RoundedImage(parent, name, Color.gray, Center, position, CardBadgeSize);
            badge.raycastTarget = false;
            text = B.CreateLabel(badge.transform, "Label", label, CardBadgeFontSize, Vector2.zero, CardBadgeSize);
            text.fontStyle = FontStyle.Bold;
            Object.DestroyImmediate(text.GetComponent<Outline>());
            return badge;
        }

        /// <summary>魔球・必殺打の札（下の球種の札の左。押すと ON / OFF。字は試合の中で入れる）と、その下のキーの印</summary>
        private static void CreateSpecialToggle(Transform safe, SerializedObject so)
        {
            var button = B.CreateTitleButton(safe, "SpecialToggle", string.Empty, Vector2.zero, SpecialToggleSize, SpecialToggleColor, SpecialToggleFontSize);
            Place((RectTransform)button.transform, BottomCenter, SpecialTogglePosition, SpecialToggleSize);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = button.GetComponentInChildren<Text>();
            label.supportRichText = true;
            var hint = B.CreateLabel(safe, "SpecialKey", "Space / L1", KeyHintFontSize, Vector2.zero, KeyHintSize);
            Place(hint.rectTransform, BottomCenter, new Vector2(SpecialTogglePosition.x, KeyHintY), KeyHintSize);

            so.FindProperty("_specialButton").objectReferenceValue = button;
            so.FindProperty("_specialImage").objectReferenceValue = button.targetGraphic;
            so.FindProperty("_specialLabel").objectReferenceValue = label;
            so.FindProperty("_specialKeyHint").objectReferenceValue = hint.gameObject;
        }
    }
}
