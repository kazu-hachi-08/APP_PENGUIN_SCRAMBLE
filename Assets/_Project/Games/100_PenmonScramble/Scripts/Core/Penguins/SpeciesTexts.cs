namespace PenmonScramble.Core
{
    /// <summary>
    /// ペンモン画面に出す文（§5.3 の個性と、技ごとの一言の説明）。
    /// 数値の表（SpeciesTable）と分けて、文だけを直しやすくしている。説明は技の数値と食い違わないように書く
    /// </summary>
    public static class SpeciesTexts
    {
        private sealed class Entry
        {
            public Entry(string personality, string skill1, string skill2)
            {
                Personality = personality;
                Skill1 = skill1;
                Skill2 = skill2;
            }

            public string Personality { get; }
            public string Skill1 { get; }
            public string Skill2 { get; }
        }

        // 添字は種類の番号-1
        private static readonly Entry[] All =
        {
            // ---- 氷 ----
            new Entry("負けずぎらいのツッパリ",
                "目の前をすばやくなぐる。押しっぱなしで連打できる",
                "おなかで前へすべり、ぶつかった敵をはね飛ばす"),
            new Entry("物知りなおじいちゃん",
                "前の広めの範囲を、ヒゲではたく",
                "つららを3本、扇のように飛ばす"),
            new Entry("世界一速いのが自慢",
                "小さな氷を、前へまっすぐ飛ばす",
                "氷の上を長くすべり、通り道の敵をはね飛ばす"),
            new Entry("自分を王様だと思っている",
                "つえを大きく振り、前の広い範囲をなぎはらう",
                "堂々と突き進み、ぶつかった敵を遠くまで吹っ飛ばす"),
            new Entry("群れのおやぶん。どっしり",
                "体ごとのしかかり、まわりの敵にまとめて当てる",
                "近くの敵の頭上に、巨大な氷柱を落とす"),

            // ---- 水 ----
            new Entry("のんびり屋",
                "水の玉を、前へまっすぐ飛ばす",
                "前に泡を置き、中の敵にじわじわ当て続ける"),
            new Entry("おしゃべり好き",
                "水しぶきを3つ、扇のように飛ばす",
                "おなかから飛びこんですべり、敵をはね飛ばす"),
            new Entry("旅好きの冒険家",
                "水をまとった蹴りで、目の前の敵を飛ばす",
                "前にうずを置き、まわりの敵を中心へ吸いこむ"),
            new Entry("暑がりで、いつも汗をかいている",
                "汗を自分のまわりに飛び散らせる",
                "近くの敵2体の足元から、熱い水が噴き出す"),
            new Entry("お金持ちのおぼっちゃま",
                "敵をつらぬく水の玉。並んだ敵にまとめて当たる",
                "大波に乗って突き進み、広い幅で敵を押し流す"),

            // ---- 風 ----
            new Entry("すぐキレるパンク",
                "短く跳びこんで蹴る。すぐにまた出せる",
                "跳び上がって着地し、まわりの敵を吹っ飛ばす"),
            new Entry("人見知りで隠れがち",
                "羽ばたいて、風を3つ扇のように飛ばす",
                "前につむじ風を置き、中の敵を外へはじき続ける"),
            new Entry("太鼓が好きなリズム係",
                "羽で太鼓のように、細かく連打する",
                "ゆっくり進む大きな衝撃波。敵をつらぬいて遠くまで届く"),
            new Entry("くるくる回るダンサー",
                "その場でくるっと回り、まわりの敵に当てる",
                "回りながら前へすべり、広い幅で敵をはね飛ばす"),
            new Entry("森に住む、なぞの仙人",
                "風の刃を5本、広い扇のように飛ばす",
                "前に巨大な竜巻を置き、中の敵を何度も吹き飛ばす"),

            // ---- 雷 ----
            new Entry("世界一小さい。静電気体質",
                "目の前の敵に、ビリッとさわる",
                "まわりに電気を放ち、近くの敵を吹っ飛ばす"),
            new Entry("発明好き",
                "電気の玉を、前へまっすぐ飛ばす",
                "前に避雷針を立て、まわりに雷を落とし続ける"),
            new Entry("冠毛がアンテナのよう",
                "速いビーム。敵をつらぬいて遠くまで届く",
                "近くの敵3体の頭上に、電波の雷を落とす"),
            new Entry("光るのが好き",
                "しっぽを大きく振り、前の広い範囲を打つ",
                "稲妻のように速く突き進み、敵をはね飛ばす"),
            new Entry("雷の神さまの子ども",
                "雷の玉を3つ、せまい扇のように飛ばす",
                "近くの敵を狙って、5か所に大きな雷を落とす"),
        };

        public static int Count => All.Length;

        /// <summary>個性。知らない種類（新しい版で増えた子）は空</summary>
        public static string Personality(int speciesNo) => Find(speciesNo)?.Personality ?? string.Empty;

        /// <summary>技の一言の説明。slot は 0 が技1</summary>
        public static string Skill(int speciesNo, int slot)
        {
            var entry = Find(speciesNo);
            if (entry == null)
            {
                return string.Empty;
            }

            return slot == 0 ? entry.Skill1 : entry.Skill2;
        }

        private static Entry Find(int speciesNo)
        {
            return speciesNo >= 1 && speciesNo <= All.Length ? All[speciesNo - 1] : null;
        }
    }
}
