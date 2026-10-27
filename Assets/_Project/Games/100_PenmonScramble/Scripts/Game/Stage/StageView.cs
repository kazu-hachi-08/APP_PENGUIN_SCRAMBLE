using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// StageWorld の中身を絵にする。Core のユニット・弾と同じ ID で見た目を作り、消えたら消す
    /// </summary>
    public class StageView : MonoBehaviour
    {
        [SerializeField] private Color _playerColor = new Color(0.35f, 0.65f, 1f);
        [SerializeField] private Color _enemyColor = new Color(0.35f, 0.32f, 0.40f);
        [SerializeField] private Color _projectileColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color _areaColor = new Color(0.90f, 0.95f, 0.98f);
        [SerializeField] private Color _damageToEnemyColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _damageToPlayerColor = new Color(1f, 0.3f, 0.3f);

        private readonly Dictionary<int, UnitView> _units = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, ProjectileView> _projectiles = new Dictionary<int, ProjectileView>();
        private readonly HashSet<int> _aliveIds = new HashSet<int>();
        private readonly List<int> _removeIds = new List<int>();
        private StageWorld _world;

        public void Bind(StageWorld world)
        {
            Unbind();
            _world = world;
            _world.Hit += OnHit;
            _world.Defeated += OnDefeated;
            _world.SkillUsed += OnSkillUsed;
            CreateArea(world);
            AfterTick();
        }

        private void Unbind()
        {
            if (_world != null)
            {
                _world.Hit -= OnHit;
                _world.Defeated -= OnDefeated;
                _world.SkillUsed -= OnSkillUsed;
            }

            // 倒れる演出の途中のものやダメージ数字も、やり直すときは全部消す
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            _units.Clear();
            _projectiles.Clear();
        }

        private void CreateArea(StageWorld world)
        {
            var area = new GameObject("Area").AddComponent<SpriteRenderer>();
            area.transform.SetParent(transform, false);
            area.sprite = PlaceholderSprites.Square;
            area.color = _areaColor;
            area.sortingOrder = SortingDepth.Background;
            area.transform.position = ((world.AreaMin + world.AreaMax) * 0.5f).ToVector2();
            var size = (world.AreaMax - world.AreaMin).ToVector2();
            area.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        /// <summary>Core を1回更新したら呼ぶ。新しく出たものを作り、位置を受け取る</summary>
        public void AfterTick()
        {
            SyncUnits();
            SyncProjectiles();
        }

        public void Render(float alpha)
        {
            foreach (var view in _units.Values)
            {
                view.Render(alpha);
            }

            foreach (var view in _projectiles.Values)
            {
                view.Render(alpha);
            }
        }

        private void SyncUnits()
        {
            foreach (var unit in _world.Units)
            {
                if (!_units.TryGetValue(unit.Id, out var view))
                {
                    var color = unit.Team == Team.Player ? _playerColor : _enemyColor;
                    _units.Add(unit.Id, UnitView.Create(transform, unit, color));
                    continue;
                }

                view.PushState(unit);
            }
        }

        private void SyncProjectiles()
        {
            _aliveIds.Clear();
            foreach (var projectile in _world.Projectiles)
            {
                _aliveIds.Add(projectile.Id);
                if (_projectiles.TryGetValue(projectile.Id, out var view))
                {
                    view.PushState(projectile);
                }
                else
                {
                    _projectiles.Add(projectile.Id, ProjectileView.Create(transform, projectile, _projectileColor));
                }
            }

            // 弾には消えたイベントが無いので、Core に無くなったものを消す
            _removeIds.Clear();
            foreach (var id in _projectiles.Keys)
            {
                if (!_aliveIds.Contains(id))
                {
                    _removeIds.Add(id);
                }
            }

            foreach (var id in _removeIds)
            {
                Destroy(_projectiles[id].gameObject);
                _projectiles.Remove(id);
            }
        }

        private void OnHit(HitEvent e)
        {
            if (!_units.TryGetValue(e.TargetId, out var view))
            {
                return;
            }

            view.Flash();
            var target = _world.FindUnit(e.TargetId);
            var color = target != null && target.Team == Team.Player ? _damageToPlayerColor : _damageToEnemyColor;
            DamagePopup.Spawn(transform, e.Position.ToVector2(), e.Damage, color);
        }

        private void OnDefeated(DefeatedEvent e)
        {
            if (_units.TryGetValue(e.UnitId, out var view))
            {
                _units.Remove(e.UnitId);
                view.PlayDefeat();
            }
        }

        private void OnSkillUsed(SkillUsedEvent e)
        {
            if (_units.TryGetValue(e.UnitId, out var view))
            {
                view.Squash();
            }
        }
    }
}
