using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ボスラッシュのクリア済み・解放・報酬。クリア済みは SaveData.clearedRush に「ステージ番号 × 10 ＋ 難易度」で持つ。
    /// 解放の順番: 1つ目のステージの「ふつう」は最初から。前のステージを「ふつう」でクリアで次のステージの「ふつう」が開く。
    /// 難易度は、同じステージの1つ下をクリアで開く
    /// </summary>
    public sealed partial class PlayerRecord
    {
        public static int RushKey(int stageNo, Difficulty difficulty) => stageNo * 10 + (int)difficulty;

        public bool IsRushCleared(int stageNo, Difficulty difficulty) => Data.clearedRush.Contains(RushKey(stageNo, difficulty));

        public bool IsRushUnlocked(int stageNo, Difficulty difficulty)
        {
            if (!RushTable.IsRush(stageNo) || !IsKnown(difficulty))
            {
                return false;
            }

            if (difficulty == Difficulty.Normal)
            {
                return stageNo == RushTable.FirstNo || IsRushCleared(stageNo - 1, Difficulty.Normal);
            }

            return IsRushCleared(stageNo, difficulty - 1);
        }

        /// <summary>
        /// ボスラッシュの結果を記録し、もらったものの内訳を返す。協力では全員が自分の端末で記録する
        /// （ゲストは、ホストが選んだステージ・難易度を自分が解放していなくても、クリアすれば自分の記録に付く）
        /// </summary>
        public StageReward RecordRush(int stageNo, Difficulty difficulty, bool cleared, int roundsWon, int enemiesDefeated)
        {
            if (!RushTable.IsRush(stageNo) || !IsKnown(difficulty))
            {
                return new StageReward(0, 0, 0);
            }

            var coins = cleared ? Balance.RushClearCoins(stageNo, difficulty) : Balance.RushFailCoins(stageNo, difficulty, roundsWon, enemiesDefeated);
            var bonus = 0;
            var unlockedStage = 0;
            Difficulty? unlockedDifficulty = null;
            if (cleared && !IsRushCleared(stageNo, difficulty))
            {
                // 初クリアのときだけ、次のステージ・次の難易度を開く（判定は IsRushUnlocked）
                Data.clearedRush.Add(RushKey(stageNo, difficulty));
                bonus = Balance.RushFirstClearBonus(stageNo, difficulty);
                unlockedStage = difficulty == Difficulty.Normal && RushTable.IsRush(stageNo + 1) ? stageNo + 1 : 0;
                unlockedDifficulty = difficulty < Difficulty.Extreme ? difficulty + 1 : (Difficulty?)null;
            }

            var reward = new StageReward(coins, bonus, unlockedStage, unlockedDifficulty);
            Data.coins += reward.Total;
            return reward;
        }

        private static bool IsKnown(Difficulty difficulty) => (int)difficulty >= 0 && (int)difficulty < Balance.DifficultyCount;
    }
}
