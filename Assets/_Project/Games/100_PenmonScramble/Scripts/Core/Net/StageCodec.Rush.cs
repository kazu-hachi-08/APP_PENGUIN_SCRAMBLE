namespace PenmonScramble.Core
{
    /// <summary>ボスラッシュの部屋の待ちの窓で、ホストが選んだステージと難易度（ゲストに見せるだけ。遊ぶステージはスナップショットで届く）</summary>
    public readonly struct RushChoice
    {
        public RushChoice(int stageNo, Difficulty difficulty)
        {
            StageNo = stageNo;
            Difficulty = difficulty;
        }

        public int StageNo { get; }
        public Difficulty Difficulty { get; }
    }

    public static partial class StageCodec
    {
        public static byte[] EncodeRushChoice(RushChoice choice)
        {
            return Encode(writer =>
            {
                writer.Write((byte)choice.StageNo);
                writer.Write((byte)choice.Difficulty);
            });
        }

        /// <summary>版が違う・壊れている・知らないステージや難易度のときは false</summary>
        public static bool TryDecodeRushChoice(byte[] data, out RushChoice choice)
        {
            if (!TryRead(data, reader => new RushChoice(reader.ReadByte(), (Difficulty)reader.ReadByte()), out choice))
            {
                return false;
            }

            return RushTable.IsRush(choice.StageNo) && (int)choice.Difficulty < Balance.DifficultyCount;
        }
    }
}
