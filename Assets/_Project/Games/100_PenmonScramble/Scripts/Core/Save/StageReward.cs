namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの結果でもらったものの内訳。結果の画面で「コイン」「初クリア ボーナス」「ステージ ○ 解放！」を
    /// 別々の札で順に見せるため、合計だけでなく内訳を返す
    /// </summary>
    public readonly struct StageReward
    {
        public StageReward(int coins, int firstClearBonus, int unlockedStageNo)
        {
            Coins = coins;
            FirstClearBonus = firstClearBonus;
            UnlockedStageNo = unlockedStageNo;
        }

        /// <summary>クリア・失敗で決まるコイン（初クリアのボーナスは含まない）</summary>
        public int Coins { get; }

        /// <summary>初めてクリアしたときだけ付くコイン。それ以外は 0</summary>
        public int FirstClearBonus { get; }

        /// <summary>今回のクリアで新しく遊べるようになったステージの番号。無ければ 0（最後のステージ・2回目以降のクリア・失敗）</summary>
        public int UnlockedStageNo { get; }

        public int Total => Coins + FirstClearBonus;
    }
}
