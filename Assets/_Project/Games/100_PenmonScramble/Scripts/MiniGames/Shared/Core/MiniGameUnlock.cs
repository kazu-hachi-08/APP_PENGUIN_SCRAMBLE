using PenmonScramble.Core;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// コースの解放（ストーリー連動）。ストーリーでそのワールドに入れたら（前のワールドの最後のステージをクリア）、
    /// その景色のコースが開く。ワールド1のコースは最初から開いている
    /// </summary>
    public static class MiniGameUnlock
    {
        /// <param name="worldsUnlocked">ストーリーで入れるワールドの数（PlayerRecord.WorldsUnlocked。オンラインはホストの値）</param>
        public static bool IsUnlocked(MiniGameCourseDef course, int worldsUnlocked)
        {
            return course != null && course.World <= worldsUnlocked;
        }

        public static bool IsUnlocked(MiniGameCourseDef course, PlayerRecord record)
        {
            return IsUnlocked(course, record.WorldsUnlocked);
        }
    }
}
