namespace PenmonScramble.Game
{
    /// <summary>
    /// 半透明の演出どうしの描く順番。ペンギン・床の前後は3Dの奥行きで決まるので、ここで決めるのは
    /// 床に寝かせた物（範囲・輪）より、はじける演出・光・文字を後から重ねることだけ
    /// </summary>
    public static class SortingDepth
    {
        /// <summary>床に置く範囲（設置・落下の予告・席の輪）。ほかの演出より先に描く</summary>
        public const int Ground = -30000;
        /// <summary>文字（いちばん後に描いて、いつも見えるように）。はじける演出・光はこれより少し前に描く</summary>
        public const int Popup = 30000;
    }
}
