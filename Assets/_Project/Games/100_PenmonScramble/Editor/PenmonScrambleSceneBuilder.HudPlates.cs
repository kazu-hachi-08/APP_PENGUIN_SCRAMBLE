using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージの HUD のプレート:左上の自分（顔とゲージの輪・名札・HP・3体の小さな顔）、右上のエリアの進み具合と残りの敵、
    /// 上のまん中のボスの HP。斜めの板とゲージの輪の絵は HudArt、丸い顔の絵はスマホのボタンと同じ TouchArt を使う
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 HudMargin = new Vector2(40f, -28f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 HudOutlineDistance = new Vector2(2f, -2f);
        private static readonly Color PlateFrameColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color HudGoldColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Color HudRimColor = new Color(0.06f, 0.08f, 0.18f);
        private static readonly Color HudGlossColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color HudBarTrailColor = new Color(1f, 0.92f, 0.85f);
        private static readonly Color HudCooldownColor = new Color(0.02f, 0.04f, 0.12f, 0.7f);

        // 丸い顔: 面は直径のこの割合（外がゲージの輪）。顔の絵は全身（顔は絵の上寄り・少し右）なので、
        // 大きくして左下へずらし、頭を丸のまん中に収める（ずらす量は面の大きさに対して）
        private const float PortraitFaceRatio = 0.76f;
        private const float PortraitZoom = 1.5f;
        private static readonly Vector2 PortraitOffset = new Vector2(-0.08f, -0.22f);

        // ---- 左上: 自分のプレート（左上の角から測る） ----
        private static readonly Vector2 PlayerPlateSize = new Vector2(620f, 220f);
        private const float ActiveDiscDiameter = 160f;
        private static readonly Vector2 ActiveDiscPosition = new Vector2(80f, -80f);
        // 名札と HP のバーは顔の後ろから出る（左端を顔に少し重ねる）
        private static readonly Vector2 HudNamePosition = new Vector2(336f, -42f);
        private static readonly Vector2 HudNameSize = new Vector2(400f, 50f);
        private const float NameTextPaddingLeft = 40f;
        private const int NameFontSize = 30;
        private static readonly Vector2 HpBarPosition = new Vector2(360f, -96f);
        private static readonly Vector2 HpBarSize = new Vector2(440f, 42f);
        private const float HpLabelPaddingLeft = 34f;
        private const float HpTextPaddingLeft = 84f;
        private const float HpTextPaddingRight = 26f;
        private const int HpLabelFontSize = 26;
        private const int HpFontSize = 28;
        private const int HpMinFontSize = 18;
        private static readonly Color HpFillColor = new Color(0.35f, 0.9f, 0.45f);
        private const float HaloRatio = 1.6f;
        private const float SparkRatio = 1.22f;
        private static readonly Vector2 ReadyTagSize = new Vector2(132f, 38f);
        private const int ReadyTagFontSize = 24;
        private const string ReadyTagLabel = "必殺OK";
        private static readonly Color GaugeTrackColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color GaugeColor = new Color(1f, 0.6f, 0.2f);

        // 3体の小さな顔: HP のバーの下に左から並べる
        private const int MemberSlotCount = 3;
        private static readonly Vector2 FirstSlotPosition = new Vector2(212f, -164f);
        private const float SlotSpacing = 84f;
        private const float SlotDiameter = 64f;
        private const float SlotRingRatio = 1.3f;
        private static readonly Vector2 SlotHpBarSize = new Vector2(56f, 9f);
        private const float SlotHpBarGap = 9f;
        private const float SlotHpInset = 2f;
        private const int SlotCrossFontSize = 52;
        private static readonly Color SlotCrossColor = new Color(1f, 0.3f, 0.25f);

        // ---- 右上: エリアの進み具合（右上の角はメニューボタンの左） ----
        private static readonly Vector2 ProgressPlateSize = new Vector2(440f, 130f);
        private static readonly Vector2 AreaPanelPosition = new Vector2(220f, -30f);
        private static readonly Vector2 AreaPanelSize = new Vector2(440f, 58f);
        private const float AreaLabelPaddingLeft = 30f;
        private const int AreaLabelFontSize = 28;
        private const string AreaLabel = "エリア";
        private static readonly Vector2 PipRowSize = new Vector2(300f, 50f);
        private const float PipRowRight = -30f;
        private const float PipSpacing = 12f;
        private const float PipSize = 24f;
        // 1ステージのエリアの数より少し多めに作っておく
        private const int MaxAreaPips = 8;
        private static readonly Vector2 BossTagSize = new Vector2(80f, 34f);
        private const int BossTagFontSize = 22;
        private static readonly Vector2 EnemyPanelPosition = new Vector2(310f, -96f);
        private static readonly Vector2 EnemyPanelSize = new Vector2(260f, 52f);
        private const float EnemyIconSize = 64f;
        private const float EnemyIconX = 46f;
        private const float EnemyTextPaddingLeft = 84f;
        private const int EnemyFontSize = 30;

        // ---- 上のまん中: ボスの HP ----
        private static readonly Vector2 BossPlateSize = new Vector2(560f, 130f);
        private static readonly Vector2 BossSlideFrom = new Vector2(0f, 180f);
        private const float BossDiscDiameter = 116f;
        private static readonly Vector2 BossDiscPosition = new Vector2(62f, -66f);
        private static readonly Vector2 BossNamePosition = new Vector2(292f, -32f);
        private static readonly Vector2 BossNameSize = new Vector2(380f, 46f);
        private const float BossNamePaddingLeft = 36f;
        private const int BossNameFontSize = 26;
        private static readonly Vector2 BossBarPosition = new Vector2(326f, -84f);
        private static readonly Vector2 BossBarSize = new Vector2(440f, 40f);
        private static readonly Color BossNameColor = new Color(0.62f, 0.12f, 0.16f);
        private static readonly Color BossBarFillColor = new Color(0.95f, 0.28f, 0.25f);
        private static readonly Color BossRimColor = new Color(0.3f, 0.04f, 0.08f);
        private static readonly Color BossDiscColor = new Color(0.85f, 0.35f, 0.3f);

        private static void CreatePlayerPlate(Transform safe, SerializedObject hud)
        {
            var group = CreateHudGroup(safe, "PlayerPlate", TopLeft, HudMargin, PlayerPlateSize);

            // 名札と HP のバーを先に作って、顔の後ろに回す
            var namePlate = CreateFramedSlant(group, "NamePlate", Color.white, TopLeft, HudNamePosition, HudNameSize);
            var nameText = CreateHudLabel(namePlate.transform, "Text", NameFontSize, TextAnchor.MiddleLeft, NameTextPaddingLeft, 0f);
            hud.FindProperty("_namePlate").objectReferenceValue = namePlate;
            hud.FindProperty("_nameText").objectReferenceValue = nameText;

            var hpBar = CreateHudBar(group, "HpBar", TopLeft, HpBarPosition, HpBarSize, HpFillColor, colorByRatio: true);
            var hpLabel = CreateHudLabel(hpBar.transform, "Label", HpLabelFontSize, TextAnchor.MiddleLeft, HpLabelPaddingLeft, 0f);
            hpLabel.text = "HP";
            hpLabel.color = HudGoldColor;
            var hpText = CreateHudLabel(hpBar.transform, "Text", HpFontSize, TextAnchor.MiddleRight, HpTextPaddingLeft, HpTextPaddingRight);
            // ダウンの知らせは長いので、入りきらなければ字を小さくする
            SetBestFit(hpText, HpFontSize);
            hud.FindProperty("_hpBar").objectReferenceValue = hpBar;
            hud.FindProperty("_hpText").objectReferenceValue = hpText;

            var slots = new Object[MemberSlotCount];
            for (var i = 0; i < MemberSlotCount; i++)
            {
                slots[i] = CreateMemberSlot(group, i, FirstSlotPosition + new Vector2(SlotSpacing * i, 0f));
            }

            SetObjectArray(hud.FindProperty("_slots"), slots);
            CreateActiveDisc(group, hud);
        }

        /// <summary>場に出ている子の大きな顔。まわりの区切りの輪が必殺技のゲージ（HudSpecialRing）</summary>
        private static void CreateActiveDisc(RectTransform group, SerializedObject hud)
        {
            var size = Vector2.one * ActiveDiscDiameter;
            var root = CreateHudRect(group, "ActiveDisc", TopLeft, ActiveDiscPosition, size);
            var halo = CreateHudImage(root, "Halo", TouchGlowPath, HudGoldColor, Middle, Vector2.zero, size * HaloRatio);
            CreateHudImage(root, "Shadow", TouchCirclePath, ShadowColor, Middle, ShadowOffset, size);
            CreateHudImage(root, "Rim", TouchCirclePath, HudRimColor, Middle, Vector2.zero, size);
            CreateHudImage(root, "GaugeTrack", HudSegmentRingPath, GaugeTrackColor, Middle, Vector2.zero, size);
            var gauge = CreateHudImage(root, "Gauge", HudSegmentRingPath, GaugeColor, Middle, Vector2.zero, size);
            SetRadialFill(gauge);
            var spin = CreateHudImage(root, "Spin", TouchSparkRingPath, HudGoldColor, Middle, Vector2.zero, size * SparkRatio);
            var (disc, face) = AddPortraitFace(root, ActiveDiscDiameter);
            hud.FindProperty("_activeDisc").objectReferenceValue = disc;
            hud.FindProperty("_activeFace").objectReferenceValue = face;

            var tag = CreateFramedSlant(root, "ReadyTag", HudGoldColor, Middle, new Vector2(0f, -ActiveDiscDiameter * 0.5f), ReadyTagSize);
            var tagText = CreateHudLabel(tag.transform, "Text", ReadyTagFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            tagText.text = ReadyTagLabel;

            var ring = root.gameObject.AddComponent<HudSpecialRing>();
            var so = new SerializedObject(ring);
            so.FindProperty("_gauge").objectReferenceValue = gauge;
            so.FindProperty("_halo").objectReferenceValue = halo;
            so.FindProperty("_spin").objectReferenceValue = spin.rectTransform;
            so.FindProperty("_tag").objectReferenceValue = tag.rectTransform;
            so.FindProperty("_color").colorValue = GaugeColor;
            so.FindProperty("_readyColor").colorValue = HudGoldColor;
            so.ApplyModifiedProperties();
            hud.FindProperty("_special").objectReferenceValue = ring;
        }

        private static HudMemberSlot CreateMemberSlot(RectTransform group, int index, Vector2 position)
        {
            var root = CreateHudRect(group, $"Member{index + 1}", TopLeft, position, Vector2.one * SlotDiameter);
            var visual = CreateHudRect(root, "Visual", Middle, Vector2.zero, Vector2.one * SlotDiameter);
            var ringSize = Vector2.one * SlotDiameter * SlotRingRatio;
            var activeRing = CreateHudImage(visual, "ActiveRing", TouchRingPath, HudGoldColor, Middle, Vector2.zero, ringSize);
            var nextRing = CreateHudImage(visual, "NextRing", TouchRingPath, Color.white, Middle, Vector2.zero, ringSize);
            CreateHudImage(visual, "Rim", TouchCirclePath, HudRimColor, Middle, Vector2.zero, Vector2.one * SlotDiameter);
            var (disc, face) = AddPortraitFace(visual, SlotDiameter);
            var cooldown = CreateHudImage(visual, "Cooldown", TouchCirclePath, HudCooldownColor, Middle, Vector2.zero, Vector2.one * SlotDiameter * PortraitFaceRatio);
            SetRadialFill(cooldown);
            var cross = CreateHudLabel(visual, "Cross", SlotCrossFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            cross.text = "×";
            cross.color = SlotCrossColor;
            var hpFill = CreateSlotHpBar(root);

            var slot = root.gameObject.AddComponent<HudMemberSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("_visual").objectReferenceValue = visual;
            so.FindProperty("_disc").objectReferenceValue = disc;
            so.FindProperty("_face").objectReferenceValue = face;
            so.FindProperty("_cooldown").objectReferenceValue = cooldown;
            so.FindProperty("_cross").objectReferenceValue = cross.gameObject;
            so.FindProperty("_activeRing").objectReferenceValue = activeRing.gameObject;
            so.FindProperty("_nextRing").objectReferenceValue = nextRing;
            so.FindProperty("_hpFill").objectReferenceValue = hpFill;
            so.ApplyModifiedProperties();
            return slot;
        }

        /// <summary>小さな顔の下の細い HP のバー。右端（anchorMax.x）を割合に合わせて縮める</summary>
        private static RectTransform CreateSlotHpBar(RectTransform slot)
        {
            var back = CreateHudImage(slot, "Hp", null, HudBackColor, Middle, new Vector2(0f, -SlotDiameter * 0.5f - SlotHpBarGap), SlotHpBarSize);
            var fill = CreateHudImage(back.rectTransform, "Fill", null, HpFillColor, Middle, Vector2.zero, Vector2.zero);
            var rect = fill.rectTransform;
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = Vector2.one * SlotHpInset;
            rect.offsetMax = -Vector2.one * SlotHpInset;
            return rect;
        }

        private static void CreateProgressPlate(Transform safe, SerializedObject hud)
        {
            var group = CreateHudGroup(safe, "ProgressPlate", Vector2.one, new Vector2(HudMargin.x + HudMenuButtonSize + MenuButtonGap, HudMargin.y), ProgressPlateSize);

            var areaPanel = CreateFramedSlant(group, "Area", HudBackColor, TopLeft, AreaPanelPosition, AreaPanelSize);
            var areaLabel = CreateHudLabel(areaPanel.transform, "Label", AreaLabelFontSize, TextAnchor.MiddleLeft, AreaLabelPaddingLeft, 0f);
            areaLabel.text = AreaLabel;
            areaLabel.color = HudGoldColor;
            hud.FindProperty("_areaPips").objectReferenceValue = CreateAreaPips(areaPanel.rectTransform);

            var enemyPanel = CreateFramedSlant(group, "Enemies", HudBackColor, TopLeft, EnemyPanelPosition, EnemyPanelSize);
            var icon = CreateHudImage(enemyPanel.rectTransform, "Icon", null, Color.white, new Vector2(0f, 0.5f), new Vector2(EnemyIconX, 0f), Vector2.one * EnemyIconSize);
            icon.preserveAspect = true;
            var enemyText = CreateHudLabel(enemyPanel.transform, "Text", EnemyFontSize, TextAnchor.MiddleLeft, EnemyTextPaddingLeft, 0f);
            hud.FindProperty("_enemyIcon").objectReferenceValue = icon;
            hud.FindProperty("_enemyText").objectReferenceValue = enemyText;
        }

        /// <summary>つぶを右寄せで横に並べ、最後に「ボス」の札を置く（隠したつぶは詰める）</summary>
        private static HudAreaPips CreateAreaPips(RectTransform panel)
        {
            var row = CreateHudRect(panel, "Pips", new Vector2(1f, 0.5f), new Vector2(PipRowRight, 0f), PipRowSize);
            row.pivot = new Vector2(1f, 0.5f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = PipSpacing;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var pips = new Object[MaxAreaPips];
            for (var i = 0; i < MaxAreaPips; i++)
            {
                pips[i] = CreateHudImage(row, $"Pip{i + 1}", TouchCirclePath, Color.white, Middle, Vector2.zero, Vector2.one * PipSize);
            }

            var bossTag = CreateFramedSlant(row, "BossTag", BossNameColor, Middle, Vector2.zero, BossTagSize);
            CreateHudLabel(bossTag.transform, "Text", BossTagFontSize, TextAnchor.MiddleCenter, 0f, 0f).text = "ボス";

            var view = row.gameObject.AddComponent<HudAreaPips>();
            var so = new SerializedObject(view);
            SetObjectArray(so.FindProperty("_pips"), pips);
            so.FindProperty("_bossTag").objectReferenceValue = bossTag;
            so.FindProperty("_doneColor").colorValue = HudGoldColor;
            so.FindProperty("_bossColor").colorValue = BossNameColor;
            so.ApplyModifiedProperties();
            return view;
        }

        /// <summary>ボスがいる間だけ出す（StageHud が出し入れし、出すたびに上から滑り込む）</summary>
        private static void CreateBossPlate(Transform safe, SerializedObject hud)
        {
            var group = CreateHudGroup(safe, "BossPlate", new Vector2(0.5f, 1f), new Vector2(0f, HudMargin.y), BossPlateSize);
            var name = CreateFramedSlant(group, "Name", BossNameColor, TopLeft, BossNamePosition, BossNameSize);
            var nameText = CreateHudLabel(name.transform, "Text", BossNameFontSize, TextAnchor.MiddleLeft, BossNamePaddingLeft, 0f);
            SetBestFit(nameText, BossNameFontSize);
            var bar = CreateHudBar(group, "Bar", TopLeft, BossBarPosition, BossBarSize, BossBarFillColor, colorByRatio: false);

            var size = Vector2.one * BossDiscDiameter;
            var disc = CreateHudRect(group, "Disc", TopLeft, BossDiscPosition, size);
            CreateHudImage(disc, "Shadow", TouchCirclePath, ShadowColor, Middle, ShadowOffset, size);
            CreateHudImage(disc, "Rim", TouchCirclePath, BossRimColor, Middle, Vector2.zero, size);
            var (discFace, face) = AddPortraitFace(disc, BossDiscDiameter);
            discFace.color = BossDiscColor;

            SetSlideIn(group.gameObject, BossSlideFrom, 0f);
            group.gameObject.SetActive(false);
            hud.FindProperty("_bossPlate").objectReferenceValue = group.gameObject;
            hud.FindProperty("_bossBar").objectReferenceValue = bar;
            hud.FindProperty("_bossFace").objectReferenceValue = face;
            hud.FindProperty("_bossText").objectReferenceValue = nameText;
        }

        // ---- 部品 ----

        /// <summary>
        /// 斜めの板のバー（HudBar）: 暗い下地・減った分の明るい残り・中身・白い縁。
        /// 残りと中身は切り抜きに入れて、切り抜きの右端だけを縮める（斜めの形がつぶれないように）
        /// </summary>
        private static HudBar CreateHudBar(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color fillColor, bool colorByRatio)
        {
            var back = CreateSlant(parent, name, HudSlantPath, HudBackColor, anchor, position, size);
            var root = back.rectTransform;
            var trail = CreateBarClip(root, "Trail", size, HudBarTrailColor, out _);
            var fillClip = CreateBarClip(root, "Fill", size, fillColor, out var fill);
            AddSlantLayer(root, "Frame", HudSlantFramePath, PlateFrameColor, size.y);

            var bar = root.gameObject.AddComponent<HudBar>();
            var so = new SerializedObject(bar);
            so.FindProperty("_fillClip").objectReferenceValue = fillClip;
            so.FindProperty("_trailClip").objectReferenceValue = trail;
            so.FindProperty("_fill").objectReferenceValue = fill;
            so.FindProperty("_shakeTarget").objectReferenceValue = root;
            so.FindProperty("_colorByRatio").boolValue = colorByRatio;
            so.FindProperty("_color").colorValue = fillColor;
            so.ApplyModifiedProperties();
            return bar;
        }

        private static RectTransform CreateBarClip(RectTransform bar, string name, Vector2 size, Color color, out Image image)
        {
            var clip = CreateHudRect(bar, name, Vector2.zero, Vector2.zero, Vector2.zero);
            UIDialogBuilder.SetStretchAll(clip);
            clip.gameObject.AddComponent<RectMask2D>();

            // 中身はバーいっぱいの大きさのまま左に留める（切り抜きが縮んでも形は変わらない）
            image = CreateSlant(clip, "Image", HudSlantPath, color, Vector2.zero, Vector2.zero, size);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size.x, 0f);
            return clip;
        }

        /// <summary>丸い面（属性の色）を丸く切り抜いて顔の絵を入れ、上につやを重ねる。返すのは色を塗る面と顔</summary>
        private static (Image disc, Image face) AddPortraitFace(RectTransform root, float diameter)
        {
            var faceSize = Vector2.one * diameter * PortraitFaceRatio;
            var disc = CreateHudImage(root, "Disc", TouchFacePath, Color.white, Middle, Vector2.zero, faceSize);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var face = CreateHudImage(disc.rectTransform, "Face", null, Color.white, Middle, Vector2.Scale(PortraitOffset, faceSize), faceSize * PortraitZoom);
            face.preserveAspect = true;
            CreateHudImage(root, "Gloss", TouchGlossPath, HudGlossColor, Middle, Vector2.zero, faceSize);
            return (disc, face);
        }

        /// <summary>斜めの板に白い縁を重ねる。中の文字はこの板の子に置く</summary>
        private static Image CreateFramedSlant(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var plate = CreateSlant(parent, name, HudSlantPath, color, anchor, position, size);
            AddSlantLayer(plate.rectTransform, "Frame", HudSlantFramePath, PlateFrameColor, size.y);
            return plate;
        }

        private static Image CreateSlant(RectTransform parent, string name, string spritePath, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = CreateHudImage(parent, name, spritePath, color, anchor, position, size);
            image.type = Image.Type.Sliced;
            // 左右の斜めの部分を高さに合わせて縮める（どの高さでも同じ傾きに見えるように）
            image.pixelsPerUnitMultiplier = SlantSpriteHeight / size.y;
            return image;
        }

        private static void AddSlantLayer(RectTransform parent, string name, string spritePath, Color color, float height)
        {
            var layer = CreateSlant(parent, name, spritePath, color, Middle, Vector2.zero, new Vector2(0f, height));
            UIDialogBuilder.SetStretchAll(layer.rectTransform);
        }

        /// <summary>入りきらなければ字を小さくする（大ボスの長い名前・ダウンの知らせ）。小さくするには折り返しを許す必要がある</summary>
        private static void SetBestFit(Text text, int maxSize)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = HpMinFontSize;
            text.resizeTextMaxSize = maxSize;
        }

        /// <summary>画面の角（または辺のまん中）に留めるプレートの入れ物。中身は入れ物の左上から測って置く</summary>
        private static RectTransform CreateHudGroup(Transform parent, string name, Vector2 corner, Vector2 margin, Vector2 size)
        {
            var rect = CreateHudRect(parent, name, corner, Vector2.zero, size);
            rect.pivot = corner;
            // 右に留めるときは、左へ向かって離す
            rect.anchoredPosition = new Vector2(corner.x >= 1f ? -margin.x : corner.x <= 0f ? margin.x : 0f, margin.y);
            return rect;
        }

        /// <summary>anchor の点から position だけずらした所を中心に置く</summary>
        private static RectTransform CreateHudRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = UIDialogBuilder.CreateUIObject(name, parent).GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Middle;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>飾りの絵（押す邪魔をしない）。spritePath が null なら絵の無い四角（顔は実行時に入れる）</summary>
        private static Image CreateHudImage(Transform parent, string name, string spritePath, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = CreateHudRect(parent, name, anchor, position, size).gameObject.AddComponent<Image>();
            image.sprite = spritePath != null ? UiSprite(spritePath) : null;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>親いっぱいに広げた太字の白い文字（濃い紺で縁取る）。左右の余白で、板の斜めの部分や顔との重なりを避ける</summary>
        private static Text CreateHudLabel(Transform parent, string name, int fontSize, TextAnchor alignment, float paddingLeft, float paddingRight)
        {
            var rect = CreateHudRect(parent, name, Middle, Vector2.zero, Vector2.zero);
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(paddingLeft, 0f);
            rect.offsetMax = new Vector2(-paddingRight, 0f);
            var text = AddText(rect.gameObject, string.Empty, fontSize);
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            // 1行に収める（はみ出しても折り返して下の段に重ならないように）
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = HudBackColor;
            outline.effectDistance = HudOutlineDistance;
            return text;
        }
    }
}
