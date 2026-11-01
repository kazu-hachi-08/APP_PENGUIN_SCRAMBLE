using System;

namespace PenmonScramble.Core
{
    /// <summary>ボスラッシュの難易度。ストーリーはいつも Normal。通信・記録でこの値を使うので、並びを変えない</summary>
    public enum Difficulty : byte
    {
        Normal,
        Hard,
        Extreme,
    }

    /// <summary>難易度ごとに、敵の HP・攻撃・予告の長さに掛ける倍率</summary>
    public static partial class Balance
    {
        public const int DifficultyCount = 3;

        // 添字は Difficulty。予告は短くするほど難しい（よけるまでの間が減る）
        private static readonly float[] DifficultyHpScales = { 1f, 1.8f, 3f };
        private static readonly float[] DifficultyAttackScales = { 1f, 1.5f, 2.2f };
        private static readonly float[] DifficultyTelegraphScales = { 1f, 0.9f, 0.8f };

        public static float DifficultyHpScale(Difficulty difficulty) => DifficultyHpScales[IndexOf(difficulty)];

        public static float DifficultyAttackScale(Difficulty difficulty) => DifficultyAttackScales[IndexOf(difficulty)];

        /// <summary>雑魚・ステージのボスの構えと、大ボス・巨大ペンモンの赤い予告の長さに掛ける</summary>
        public static float DifficultyTelegraphScale(Difficulty difficulty) => DifficultyTelegraphScales[IndexOf(difficulty)];

        /// <summary>通信で壊れた値が届いても落ちないよう、範囲に収める</summary>
        private static int IndexOf(Difficulty difficulty) => Math.Clamp((int)difficulty, 0, DifficultyCount - 1);
    }
}
