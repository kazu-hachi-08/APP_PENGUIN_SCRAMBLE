using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ペンモン画面（一覧・編成のカード、図鑑カード、技のタブと詳しい欄）とガチャの結果の技のカードの文字は、シーンでは空で画面を開いたときに入る。
    /// そのままでは測れないので、全種類の子の文を見本として入れて測る（画面と同じ作り方の文を使う）
    /// </summary>
    public static partial class UiLayoutCheck
    {
        // パワーの見本は、4桁でいちばん幅を取る数字（上限解放しきった★3 でも4桁に収まる）
        private const int WidestPower = 8888;
        private const string EmptySlotLabel = "（空き）";

        private static void CheckPenmonScreen(Font font, List<string> issues)
        {
            foreach (var species in SpeciesTable.Species)
            {
                for (var slot = 0; slot <= SkillPractice.SpecialSlot; slot++)
                {
                    var samples = new Dictionary<Text, string>();
                    AddCardSamples(species, samples);
                    AddDetailSamples(species, samples);
                    AddSkillCardSamples(species, samples, slot);
                    CheckSamples(samples, font, issues);
                }
            }

            // 空いた編成の枠
            var empty = new Dictionary<Text, string>();
            foreach (var card in Object.FindObjectsByType<PenguinCard>(FindObjectsInactive.Include))
            {
                if (TextField(card, "_nameText") is Text name) empty[name] = EmptySlotLabel;
            }
            CheckSamples(empty, font, issues);
        }

        private static void CheckSamples(Dictionary<Text, string> samples, Font font, List<string> issues)
        {
            foreach (var pair in samples)
            {
                CheckFit(pair.Key, pair.Value, font, issues);
                CheckOverlap(pair.Key, samples, font, issues);
            }
        }

        private static void AddCardSamples(SpeciesDef species, Dictionary<Text, string> samples)
        {
            foreach (var card in Object.FindObjectsByType<PenguinCard>(FindObjectsInactive.Include))
            {
                AddSample(card, "_starsText", PenguinLabels.Stars(species.Stars), samples);
                AddSample(card, "_nameText", species.Name, samples);
                AddSample(card, "_powerText", PenguinLabels.CardPower(WidestPower), samples);
            }
        }

        private static void AddDetailSamples(SpeciesDef species, Dictionary<Text, string> samples)
        {
            foreach (var panel in Object.FindObjectsByType<PenmonDetailPanel>(FindObjectsInactive.Include))
            {
                AddSample(panel, "_titleText", PenmonDetailPanel.TitleOf(species), samples);
                AddSample(panel, "_elementText", PenguinLabels.ElementName(species.Element), samples);
                AddSample(panel, "_personalityText", SpeciesTexts.Personality(species.No), samples);
                AddSample(panel, "_statsText", PenmonDetailPanel.StatsLine(WidestPower, WidestPower, WidestPower), samples);
                AddSample(panel, "_limitText", PenguinLabels.LimitPips(Balance.MaxLimitBreak), samples);
            }
        }

        /// <summary>
        /// 技のタブ・詳しい欄（ペンモン画面）と、技のカード（ガチャの結果）。タブとカードは並びの番号が技の slot（技1・技2・必殺技）。
        /// 詳しい欄は選んだ技の文が入るので、3つの技を順に入れて測る
        /// </summary>
        private static void AddSkillCardSamples(SpeciesDef species, Dictionary<Text, string> samples, int detailSlot)
        {
            foreach (var panel in Object.FindObjectsByType<PenmonDetailPanel>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(panel);
                var tabs = so.FindProperty("_skillTabs");
                for (var slot = 0; slot < tabs.arraySize; slot++)
                {
                    if (!(tabs.GetArrayElementAtIndex(slot).objectReferenceValue is SkillTabView tab)) continue;
                    var lines = SkillTexts.For(species, slot);
                    AddSample(tab, "_headerText", lines.Header, samples);
                    AddSample(tab, "_nameText", lines.Name, samples);
                }

                if (so.FindProperty("_skillDetail").objectReferenceValue is SkillDetailView detail)
                {
                    var lines = SkillTexts.For(species, detailSlot);
                    AddSample(detail, "_shapeText", lines.Shape, samples);
                    AddSample(detail, "_descriptionText", lines.Description, samples);
                    AddSample(detail, "_ratingLabelsText", lines.RatingLabels, samples);
                    AddSample(detail, "_ratingsText", lines.Ratings, samples);
                }
            }

            foreach (var result in Object.FindObjectsByType<GachaResultCard>(FindObjectsInactive.Include))
            {
                var cards = new SerializedObject(result).FindProperty("_skillCards");
                for (var slot = 0; slot < cards.arraySize; slot++)
                {
                    if (!(cards.GetArrayElementAtIndex(slot).objectReferenceValue is SkillCardView card)) continue;
                    var lines = SkillTexts.For(species, slot);
                    AddSample(card, "_nameText", SkillCardView.NameLine(lines), samples);
                    AddSample(card, "_descriptionText", SkillCardView.DescriptionLine(lines), samples);
                    AddSample(card, "_ratingLabelsText", lines.RatingLabels, samples);
                    AddSample(card, "_ratingsText", lines.Ratings, samples);
                }
            }
        }

        private static void AddSample(Object owner, string field, string value, Dictionary<Text, string> samples)
        {
            if (TextField(owner, field) is Text text) samples[text] = value;
        }

        private static Text TextField(Object owner, string field) =>
            new SerializedObject(owner).FindProperty(field)?.objectReferenceValue as Text;

        /// <summary>全種類で同じ文の問題が出ても、1回だけ知らせる</summary>
        private static void AddOnce(List<string> issues, string issue)
        {
            if (!issues.Contains(issue)) issues.Add(issue);
        }
    }
}
