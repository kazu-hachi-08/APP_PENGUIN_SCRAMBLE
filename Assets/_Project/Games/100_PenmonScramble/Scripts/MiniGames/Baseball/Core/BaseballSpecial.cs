using System;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 魔球・必殺打の中身（持ち主の子の属性と★）。属性で動きが、★（1〜3）で強さが決まる。
    /// パワー（育成）は効かせない（育てた子だけが強くならないように。ミニゲームは編成の誰でも同じように遊べる）
    /// </summary>
    public readonly struct BaseballSpecial
    {
        public const int MinStars = 1;
        public const int MaxStars = 3;

        /// <summary>出していない</summary>
        public static readonly BaseballSpecial None = default;

        public BaseballSpecial(Element element, int stars)
        {
            Element = element;
            Stars = Math.Clamp(stars, MinStars, MaxStars);
            IsActive = true;
        }

        public Element Element { get; }
        public int Stars { get; }

        /// <summary>出している（None は false）</summary>
        public bool IsActive { get; }

        public bool Is(Element element) => IsActive && Element == element;

        /// <summary>種類の子の魔球・必殺打（種類の無い番号なら None）</summary>
        public static BaseballSpecial Of(int speciesNo)
        {
            var species = SpeciesTable.Get(speciesNo);
            return species != null ? new BaseballSpecial(species.Element, species.Stars) : None;
        }

        public override string ToString() => IsActive ? $"{Element}★{Stars}" : "None";
    }

    /// <summary>投げる前に決まる魔球の中身（★の幅の中の乱数もここで決めておく。投げた後の道のりは入力の時刻だけで決まるように）</summary>
    public readonly struct BaseballPitchMagic
    {
        public static readonly BaseballPitchMagic None = default;

        public BaseballPitchMagic(BaseballSpecial special, float freezeSeconds, float wobblePhase)
        {
            Special = special;
            FreezeSeconds = freezeSeconds;
            WobblePhase = wobblePhase;
        }

        public BaseballSpecial Special { get; }

        /// <summary>氷: 凍って止まる秒</summary>
        public float FreezeSeconds { get; }

        /// <summary>水: 揺れの始まりの位相（ラジアン。どちらへ揺れてホームを通るかを読めないように、投げるたびに変える）</summary>
        public float WobblePhase { get; }

        public bool IsActive => Special.IsActive;

        /// <summary>魔球の中身を決める（★の幅の中の乱数を引く）</summary>
        public static BaseballPitchMagic Roll(BaseballSpecial special, Random random)
        {
            if (!special.IsActive)
            {
                return None;
            }

            var min = BaseballBalance.FreezeMinSeconds(special.Stars);
            var max = BaseballBalance.FreezeMaxSeconds(special.Stars);
            var freeze = min + (float)random.NextDouble() * (max - min);
            var phase = (float)(random.NextDouble() * 2.0 * Math.PI);
            return new BaseballPitchMagic(special, freeze, phase);
        }
    }

    /// <summary>魔球・必殺打の名前（カットインと遊び方に出す）</summary>
    public static class BaseballSpecialNames
    {
        public static string Magic(Element element)
        {
            switch (element)
            {
                case Element.Water: return "ゆらゆらボール";
                case Element.Wind: return "旋風ボール";
                case Element.Thunder: return "消える魔球";
                default: return "フリーズボール";
            }
        }

        public static string Hit(Element element)
        {
            switch (element)
            {
                case Element.Water: return "水切りヒット";
                case Element.Wind: return "追い風ヒット";
                case Element.Thunder: return "イナズマ打ち";
                default: return "アイスロード";
            }
        }
    }
}
