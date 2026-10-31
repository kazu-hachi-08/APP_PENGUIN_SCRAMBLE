using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 画面の外にあるもの（大ボス・仲間・大ボスの予告）を指す、画面の端の印の置き場所の計算。
    /// 画面に依存しない純粋な計算なので、テストで確かめられるよう切り出す
    /// </summary>
    public static class EdgeMarkerPlacement
    {
        /// <summary>
        /// スクリーンの点（WorldToScreenPoint の結果）が画面の中に見えているか。
        /// 端から inset（画素）より内側だけを見えているとみなす（端で半分切れていると、見落としやすいため）
        /// </summary>
        public static bool IsOnScreen(Vector3 screenPoint, Vector2 screenSize, float inset)
        {
            return screenPoint.z > 0f
                && screenPoint.x >= inset && screenPoint.x <= screenSize.x - inset
                && screenPoint.y >= inset && screenPoint.y <= screenSize.y - inset;
        }

        /// <summary>
        /// カメラの後ろにある点は、スクリーンの上で真ん中について反対側に写るので、向きを戻す。
        /// 見下ろすカメラでは床の点が後ろに来ることはまず無いが、矢印が逆を指さないようにしておく
        /// </summary>
        public static Vector2 FacingPoint(Vector3 screenPoint, Vector2 screenSize)
        {
            var point = new Vector2(screenPoint.x, screenPoint.y);
            return screenPoint.z >= 0f ? point : screenSize - point;
        }

        /// <summary>
        /// box の真ん中から target へ向かう線が、box の縁と交わる所と、その向き（度。右が 0、反時計回り）。
        /// target が box の中なら、そのままの位置（向きは真ん中から見た向き）
        /// </summary>
        public static (Vector2 position, float angle) PlaceOnEdge(Rect box, Vector2 target)
        {
            var center = box.center;
            var direction = target - center;
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return (center, 0f);
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var half = box.size * 0.5f;
            var scaleX = Mathf.Abs(direction.x) > Mathf.Epsilon ? half.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
            var scaleY = Mathf.Abs(direction.y) > Mathf.Epsilon ? half.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
            var scale = Mathf.Min(1f, Mathf.Min(scaleX, scaleY));
            return (center + direction * scale, angle);
        }
    }
}
