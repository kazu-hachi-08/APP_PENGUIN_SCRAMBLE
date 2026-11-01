using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>ボスラッシュの難易度の呼び名と色（待ちの窓の札・結果の見出しで同じものを使う）</summary>
    public static class DifficultyNames
    {
        private static readonly string[] Names = { "ふつう", "むずかしい", "げきむず" };

        // ふつう 緑 / むずかしい だいだい / げきむず 赤
        private static readonly Color[] Colors =
        {
            new Color(0.30f, 0.72f, 0.38f),
            new Color(0.95f, 0.55f, 0.18f),
            new Color(0.88f, 0.22f, 0.24f),
        };

        public static string NameOf(Difficulty difficulty) => Names[Index(difficulty)];

        public static Color ColorOf(Difficulty difficulty) => Colors[Index(difficulty)];

        private static int Index(Difficulty difficulty) => Mathf.Clamp((int)difficulty, 0, Names.Length - 1);
    }
}
