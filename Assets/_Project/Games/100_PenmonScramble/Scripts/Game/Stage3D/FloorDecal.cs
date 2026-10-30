using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 床に寝かせた絵（範囲の円・輪・帯）を作る。SpriteRenderer を上向きに倒して使う。
    /// 高さは床からほんの少しだけ浮かせ、重なる物どうしは浮かせる高さを変えて、床と同じ高さでちらつかないようにする
    /// </summary>
    public static class FloorDecal
    {
        /// <summary>床に置く物の高さ（マス）。下から順に重なる</summary>
        public const float AreaHeight = 0.01f;
        public const float ReviveHeight = 0.015f;
        public const float SeatRingHeight = 0.02f;
        public const float BurstHeight = 0.03f;

        // SpriteRenderer の表（-Z 側）を上に向ける
        private static readonly Quaternion LieFlat = Quaternion.Euler(90f, 0f, 0f);

        /// <summary>size は床の上の大きさ（マス）。yaw は絵の右（+X）を床のどちらへ向けるか（度。0 で +X）</summary>
        public static SpriteRenderer Create(Transform parent, string objName, Sprite sprite, Material material, Color color, Vector3 position, Vector2 size, float yaw = 0f)
        {
            var renderer = new GameObject(objName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = SortingDepth.Ground;
            renderer.transform.position = position;
            renderer.transform.rotation = Facing(yaw);
            SetSize(renderer, size);
            return renderer;
        }

        /// <summary>絵の大きさに関係なく、床の上で size（マス）になるようにする</summary>
        public static void SetSize(SpriteRenderer renderer, Vector2 size)
        {
            var spriteSize = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        }

        /// <summary>
        /// Unity の回転は Y が最後に掛かるので、寝かせてから床の上で回す。
        /// Y で yaw 度回すと +X は (cos, 0, -sin) へ向くので、床の方向 (x, z) へ向けるには yaw = atan2(-z, x)
        /// </summary>
        public static Quaternion Facing(float yaw) => Quaternion.Euler(0f, yaw, 0f) * LieFlat;

        public static float YawOf(Vector2 floorDirection) => Mathf.Atan2(-floorDirection.y, floorDirection.x) * Mathf.Rad2Deg;
    }
}
