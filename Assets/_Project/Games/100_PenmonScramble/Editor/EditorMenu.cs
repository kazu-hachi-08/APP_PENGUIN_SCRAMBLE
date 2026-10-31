namespace PenmonScramble.Editor
{
    /// <summary>
    /// Tools/PenmonScramble メニューの置き場所と並び順。
    /// 目的（作る・配る・確かめる・撮る）ごとにまとめ、Unity は並び順が 11 以上離れると区切り線を入れるので、
    /// グループの間はそれ以上あける。並びを変えるときはここだけ直す
    /// </summary>
    public static class EditorMenu
    {
        private const string Root = "Tools/PenmonScramble/";

        public const string Rebuild = Root + "Rebuild";
        public const int RebuildPriority = 0;

        public const string Release = Root + "Release/";
        public const int ReleasePriority = 20;

        public const string Check = Root + "Check/";
        public const int CheckPriority = 40;

        public const string Benchmark = Root + "Benchmark/";
        public const int BenchmarkPriority = 41;

        public const string Capture = Root + "Capture/";
        // 試作の場を開く項目と、そこで撮る項目の間に区切り線を入れる
        public const int CaptureOpenPriority = 60;
        public const int CapturePriority = 71;
    }
}
