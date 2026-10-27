using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 一瞬だけ出て、広がりながら消える丸や輪（技の範囲・突進の残像など）。見た目だけで、計算には関わらない
    /// </summary>
    public class BurstFx : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _color;
        private float _fromRadius;
        private float _toRadius;
        private float _seconds;
        private float _age;

        public static void Spawn(Transform parent, Sprite sprite, Color color, Vector2 position, float fromRadius, float toRadius, float seconds, int sortingOrder)
        {
            var fx = new GameObject("BurstFx").AddComponent<BurstFx>();
            fx.transform.SetParent(parent, false);
            fx.transform.position = position;
            fx._renderer = fx.gameObject.AddComponent<SpriteRenderer>();
            fx._renderer.sprite = sprite;
            fx._renderer.sortingOrder = sortingOrder;
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
            transform.localScale = Vector3.one * (Mathf.Lerp(_fromRadius, _toRadius, eased) * 2f);
            _renderer.color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - t));
        }
    }
}
