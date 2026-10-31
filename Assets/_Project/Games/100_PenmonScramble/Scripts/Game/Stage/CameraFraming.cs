using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// カメラが床のどこを、どれだけ引いて映すかの計算（画面に依存しない純粋な計算なので、テストで確かめられるよう切り出す）。
    /// 引き（zoom）は 1 でエリア1枚分（16×9マス）が画面に収まり、2 で縦横2倍の広さが収まる
    /// </summary>
    public static class CameraFraming
    {
        /// <summary>
        /// 元気なプレイヤーをみんな映す真ん中と引き。離れていても maxZoom より引かない（ペンモンが小さくなりすぎないように）。
        /// 映す範囲は boundsMin〜boundsMax の外へ出さない（動ける範囲の外の暗い床を映しすぎないように）
        /// </summary>
        /// <param name="view">引き 1 のときに映る広さ（マス）</param>
        /// <param name="padding">プレイヤーと画面の端の間に空けておく幅（マス）</param>
        public static (Vector2 center, float zoom) FollowPlayers(IReadOnlyList<Vector2> players, Vector2 boundsMin, Vector2 boundsMax, Vector2 view, float padding, float maxZoom)
        {
            if (players.Count == 0)
            {
                return (ClampCenter((boundsMin + boundsMax) * 0.5f, boundsMin, boundsMax, view * 0.5f), 1f);
            }

            var min = players[0];
            var max = players[0];
            foreach (var player in players)
            {
                min = Vector2.Min(min, player);
                max = Vector2.Max(max, player);
            }

            var zoom = ZoomToFit((max - min) * 0.5f, view * 0.5f, padding, maxZoom);
            return (ClampCenter((min + max) * 0.5f, boundsMin, boundsMax, view * 0.5f * zoom), zoom);
        }

        /// <summary>広さ size（マス）が丸ごと画面に収まる引き。闘技場に入ったときに全体を見せるのに使う</summary>
        public static float ZoomToShow(Vector2 size, Vector2 view)
        {
            return Mathf.Max(1f, size.x / view.x, size.y / view.y);
        }

        /// <summary>真ん中から spread（縦横の半分）だけ離れた点まで、端から padding 空けて映せる引き。1〜maxZoom</summary>
        public static float ZoomToFit(Vector2 spread, Vector2 halfView, float padding, float maxZoom)
        {
            var need = Mathf.Max((spread.x + padding) / halfView.x, (spread.y + padding) / halfView.y);
            return Mathf.Clamp(need, 1f, Mathf.Max(1f, maxZoom));
        }

        /// <summary>
        /// 画面（真ん中から halfView）が min〜max の外を映さないよう、真ん中を寄せる。
        /// 画面の方が広い向きは、範囲の真ん中に置く（どちらの外も同じだけ映す）
        /// </summary>
        public static Vector2 ClampCenter(Vector2 center, Vector2 min, Vector2 max, Vector2 halfView)
        {
            return new Vector2(ClampAxis(center.x, min.x, max.x, halfView.x), ClampAxis(center.y, min.y, max.y, halfView.y));
        }

        private static float ClampAxis(float center, float min, float max, float halfView)
        {
            var low = min + halfView;
            var high = max - halfView;
            return low <= high ? Mathf.Clamp(center, low, high) : (min + max) * 0.5f;
        }
    }
}
