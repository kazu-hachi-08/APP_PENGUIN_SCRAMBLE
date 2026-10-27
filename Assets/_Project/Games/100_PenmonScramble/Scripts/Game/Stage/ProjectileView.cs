using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>技の弾の仮の見た目（属性の色の丸）。UnitView と同じく、更新の間を補間して描く</summary>
    public class ProjectileView : MonoBehaviour
    {
        // 敵の弾は少し暗くして、自分たちの弾と見分けられるようにする
        private const float EnemyDarkness = 0.4f;

        private SpriteRenderer _renderer;
        private Vector2 _from;
        private Vector2 _to;

        public static ProjectileView Create(Transform parent, ProjectileState projectile, bool isEnemy)
        {
            var view = new GameObject($"Projectile{projectile.Id}").AddComponent<ProjectileView>();
            view.transform.SetParent(parent, false);
            view.transform.localScale = Vector3.one * (projectile.Radius * 2f);
            view._renderer = view.gameObject.AddComponent<SpriteRenderer>();
            view._renderer.sprite = PlaceholderSprites.Circle;
            var color = PenguinPalette.ElementColor(projectile.Element);
            view._renderer.color = isEnemy ? Color.Lerp(color, Color.black, EnemyDarkness) : color;
            view._from = view._to = projectile.Position.ToVector2();
            view.Render(1f);
            return view;
        }

        public void PushState(ProjectileState projectile)
        {
            _from = _to;
            _to = projectile.Position.ToVector2();
        }

        public void Render(float alpha)
        {
            var position = Vector2.Lerp(_from, _to, alpha);
            transform.position = position;
            // 弾は同じ高さのペンギンより手前に見せる
            _renderer.sortingOrder = SortingDepth.ForY(position.y) + 3;
        }
    }
}
