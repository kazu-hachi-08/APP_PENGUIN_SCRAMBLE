using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 一瞬だけ出て、広がりながら消える丸や輪（技の範囲・出てきたときの輪など）。見た目だけで、計算には関わらない。
    /// ふつうは床に寝かせて置き、光（合体技など）はカメラの方へ向けて宙に浮かせる
    /// </summary>
    public class BurstFx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _color;
        private float _fromRadius;
        private float _toRadius;
        private float _seconds;
        private float _age;

        /// <summary>床に寝かせて広げる。position は床の上の位置（高さは FloorDecal.BurstHeight）</summary>
        public static void SpawnOnFloor(Transform parent, Sprite sprite, Material material, Color color, Vector2 position, float fromRadius, float toRadius, float seconds)
        {
            var renderer = FloorDecal.Create(parent, "BurstFx", sprite, material, color, position.ToFloor(FloorDecal.BurstHeight), Vector2.one);
            renderer.sortingOrder = SortingDepth.Popup - 3;
            Begin(renderer, color, fromRadius, toRadius, seconds);
        }

        /// <summary>宙に浮かせて、いつもカメラの方を向けて広げる（光）</summary>
        public static void SpawnFacingCamera(Transform parent, Sprite sprite, Material material, Color color, Vector3 position, float fromRadius, float toRadius, float seconds)
        {
            var renderer = new GameObject("BurstFx").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.transform.position = position;
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingDepth.Popup - 2;
            renderer.gameObject.AddComponent<FaceCamera>();
            Begin(renderer, color, fromRadius, toRadius, seconds);
        }

        private static void Begin(SpriteRenderer renderer, Color color, float fromRadius, float toRadius, float seconds)
        {
            var fx = renderer.gameObject.AddComponent<BurstFx>();
            fx._renderer = renderer;
            fx._color = color;
            fx._fromRadius = fromRadius;
            fx._toRadius = toRadius;
            fx._seconds = Mathf.Max(0.01f, seconds);
            fx.Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = _age / _seconds;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Apply(t);
        }

        private void Apply(float t)
        {
            // 最初に速く広がって、ゆっくり止まる方が「はじけた」感じに見える
            var eased = 1f - (1f - t) * (1f - t);
            var diameter = Mathf.Lerp(_fromRadius, _toRadius, eased) * 2f;
            FloorDecal.SetSize(_renderer, Vector2.one * diameter);
            _renderer.color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - t));
        }
    }
}
