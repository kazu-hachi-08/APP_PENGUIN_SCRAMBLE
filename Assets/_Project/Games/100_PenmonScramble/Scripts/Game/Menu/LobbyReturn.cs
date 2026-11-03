using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>本編のシーンを読み直したときに、ロビーの上に開いておく画面</summary>
    public enum LobbyScreen
    {
        None,

        /// <summary>ミニゲームを選ぶ画面（ミニゲームのシーンから戻ったとき）</summary>
        MiniGames,
    }

    /// <summary>
    /// シーンをまたいで残す小さな「戻り先」。ミニゲームは別のシーンなので、本編のシーンへ戻るときに
    /// どの画面から始めるかをここで渡す（ステージから戻ったときはロビーから、の TitleFlow の仕組みを広げたもの）。
    /// 頼まれていれば TitleFlow は「タップしてスタート」を出さずにロビーへ入り、頼まれた画面が自分で開く
    /// </summary>
    public static class LobbyReturn
    {
        private static LobbyScreen _pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // エディタで「ドメインを読み直さない」設定でも、再生のたびに何も頼まれていない所から始める
            _pending = LobbyScreen.None;
        }

        /// <summary>本編のシーンへ戻る前に呼ぶ</summary>
        public static void Request(LobbyScreen screen) => _pending = screen;

        /// <summary>何か頼まれているか（TitleFlow がスタート画面を飛ばす判定に使う。取り出さない）</summary>
        public static bool HasPending => _pending != LobbyScreen.None;

        /// <summary>screen が頼まれていれば取り出して true（1回だけ）</summary>
        public static bool Take(LobbyScreen screen)
        {
            if (_pending != screen)
            {
                return false;
            }

            _pending = LobbyScreen.None;
            return true;
        }
    }
}
