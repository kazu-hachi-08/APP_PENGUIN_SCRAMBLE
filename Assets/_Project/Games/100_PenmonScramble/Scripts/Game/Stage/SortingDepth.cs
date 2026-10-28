namespace PenmonScramble.Game
{
    /// <summary>
    /// 斜め見下ろしなので、奥（画面の上）にいるものほど後ろに描く（§9）
    /// </summary>
    public static class SortingDepth
    {
        // 0.01マスの差でも前後が入れ替わるようにする
        private const float Scale = 100f;

        // 1体の中で重ねる部品（体・向きの三角など）の数。前後の別のユニットと混ざらないよう間を空ける
        private const int LayersPerObject = 4;

        // sortingOrder は 16bit（-32768〜32767）。範囲外の値は丸められて前後が逆転する（地面がペンギンより手前になる）ので、内側に収める
        /// <summary>エリアの外（画面の端）に敷く、暗くした地面</summary>
        public const int Outside = -30000;
        public const int Background = Outside + 1;
        /// <summary>地面に置く範囲（設置・落下の予告）。どのペンギンよりも後ろ</summary>
        public const int Ground = Background + 1;
        public const int Popup = 30000;

        public static int ForY(float y) => -UnityEngine.Mathf.RoundToInt(y * Scale) * LayersPerObject;
    }
}
