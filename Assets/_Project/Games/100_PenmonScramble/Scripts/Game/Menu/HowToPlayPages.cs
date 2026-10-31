using System.Collections.Generic;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 遊び方の「ルール」のページ（§3.1）。中身は §4・§6 の要約なので、ルールを変えたらここも直す
    /// </summary>
    public static class HowToPlayPages
    {
        public readonly struct Page
        {
            public readonly string Heading;
            public readonly string Body;

            /// <summary>横に出す顔（ペンギンの種類の番号）</summary>
            public readonly int SpeciesNo;

            public Page(string heading, string body, int speciesNo)
            {
                Heading = heading;
                Body = body;
                SpeciesNo = speciesNo;
            }
        }

        // 顔はそのページの話に合う子: アデリン・ジェンツ・オヤブン・キングス・マゼラ・コガタン
        public static readonly IReadOnlyList<Page> All = new[]
        {
            new Page("3体で戦う",
                "編成した3体を連れていく。場に出るのは1体だけ。\n" +
                "「交代」で次の子と入れ替わる（交代したあと2秒は交代できない）。\n" +
                "HP と技の待ち時間は1体ずつ。控えにいる間も待ち時間は進む。\n" +
                "倒れたら次の子が自動で出る。3体とも倒れたら「ダウン」。",
                1),
            new Page("技",
                "技1は待ち時間が短く、連打できる技。\n" +
                "技2は待ち時間が長いかわりに、派手な技。\n" +
                "技は向いている方へ出る。止まっているときは、一番近い敵の方を向く。\n" +
                "ボタンは押しっぱなしにすると、出せるたびに出し続ける。",
                3),
            new Page("必殺技",
                "敵にダメージを与えると、必殺技のゲージがたまる（敵5体分で満タン）。\n" +
                "満タンになるとゲージとボタンが光る。押すと属性ごとの必殺技！\n" +
                "出したあと3秒は、出した本人は無敵。\n" +
                "必殺技のダメージでは、ゲージはたまらない。",
                5),
            new Page("吹っ飛ばして連鎖",
                "強く飛ばした敵がほかの敵に当たると、その敵にもダメージを与えて飛ばす。\n" +
                "ピンボールのように連鎖させて、まとめて倒そう。\n" +
                "壁に当たると跳ね返る。強く飛ばされている間は、敵も動けない。",
                4),
            new Page("ステージの進み方",
                "エリアに入ると出入口が閉じて、敵が何回かに分けて出てくる。\n" +
                "全部倒すと「すすめ →」。右へ進んで次のエリアへ。\n" +
                "最後のエリアのボスを倒したらクリア！\n" +
                "大ボスの攻撃の前は地面が赤くなる。赤い所から逃げよう。",
                8),
            new Page("協力プレイ",
                "最大4人で遊べる。「部屋を作る」で出たコードを、仲間が「参加する」に入れる。\n" +
                "別の人の技が同じ敵に続けて当たると「合体技」が起きる。\n" +
                "ダウンした仲間のそばにいると、少しずつ復活させられる。\n" +
                "全員がダウンすると失敗。",
                16),
        };
    }
}
