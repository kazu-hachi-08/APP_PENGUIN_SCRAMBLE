using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スナップショットを絵にする。ユニット・弾・範囲と同じ ID で見た目を作り、消えたら消す。
    /// ホストもゲストも同じスナップショットから描くので、どちらの画面も同じに見える
    /// </summary>
    public partial class StageView : MonoBehaviour
    {
        // 席ごとの色（P1〜P4）。協力のときに自分のペンギンを見分けるため
        [SerializeField] private Color[] _seatColors =
        {
            new Color(0.35f, 0.65f, 1f),
            new Color(1f, 0.45f, 0.45f),
            new Color(0.40f, 0.85f, 0.45f),
            new Color(1f, 0.80f, 0.25f),
        };
        [SerializeField] private Color _areaColor = new Color(0.90f, 0.95f, 0.98f);
        [SerializeField] private Color _damageToEnemyColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _damageToPlayerColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private SpecialCutIn _cutIn;

        private readonly Dictionary<int, UnitView> _units = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, ProjectileView> _projectiles = new Dictionary<int, ProjectileView>();
        private readonly Dictionary<int, EffectView> _effects = new Dictionary<int, EffectView>();
        private readonly HashSet<int> _aliveIds = new HashSet<int>();
        private readonly List<int> _removeIds = new List<int>();
        private int _round;

        public Color SeatColor(int seat) => _seatColors[seat % _seatColors.Length];

        /// <summary>新しいスナップショットを受け取る。出来事の演出を出し、新しく出たものを作り、位置を受け取る</summary>
        public void Apply(StageSnapshot snapshot)
        {
            if (snapshot.Round != _round)
            {
                Rebuild(snapshot);
            }

            // 倒れたユニットはスナップショットにもういないので、消す前に当たった演出を出す
            foreach (var hit in snapshot.Hits)
            {
                OnHit(hit);
            }

            foreach (var defeat in snapshot.Defeats)
            {
                OnDefeated(defeat);
            }

            SyncUnits(snapshot);
            SyncProjectiles(snapshot);
            SyncEffects(snapshot);
            PlayEvents(snapshot);
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

        /// <summary>「もう一度」で始め直したとき。倒れる演出の途中のものやダメージ数字も全部消す</summary>
        private void Rebuild(StageSnapshot snapshot)
        {
            _round = snapshot.Round;
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            _units.Clear();
            _projectiles.Clear();
            _effects.Clear();
            CreateArea(snapshot);
        }

        private void CreateArea(StageSnapshot snapshot)
        {
            var area = new GameObject("Area").AddComponent<SpriteRenderer>();
            area.transform.SetParent(transform, false);
            area.sprite = PlaceholderSprites.Square;
            area.color = _areaColor;
            area.sortingOrder = SortingDepth.Background;
            area.transform.position = ((snapshot.AreaMin + snapshot.AreaMax) * 0.5f).ToVector2();
            var size = (snapshot.AreaMax - snapshot.AreaMin).ToVector2();
            area.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void SyncUnits(StageSnapshot snapshot)
        {
            _aliveIds.Clear();
            foreach (var unit in snapshot.Units)
            {
                _aliveIds.Add(unit.Id);
                if (_units.TryGetValue(unit.Id, out var view))
                {
                    view.PushState(unit);
                    continue;
                }

                _units.Add(unit.Id, CreateUnitView(unit));
            }

            // 倒されずに消えた（ゲストが抜けた）ものは、演出なしで消す
            RemoveMissing(_units, view => Destroy(view.gameObject));
        }

        private UnitView CreateUnitView(UnitState unit)
        {
            if (unit.Team == Team.Enemy)
            {
                return UnitView.Create(transform, unit, null, null);
            }

            return UnitView.Create(transform, unit, SeatColor(unit.Seat), SeatLabel(unit.Seat));
        }

        public static string SeatLabel(int seat) => $"P{seat + 1}";

        private void SyncProjectiles(StageSnapshot snapshot)
        {
            _aliveIds.Clear();
            foreach (var projectile in snapshot.Projectiles)
            {
                _aliveIds.Add(projectile.Id);
                if (_projectiles.TryGetValue(projectile.Id, out var view))
                {
                    view.PushState(projectile);
                }
                else
                {
                    _projectiles.Add(projectile.Id, ProjectileView.Create(transform, projectile, projectile.Team == Team.Enemy));
                }
            }

            // 弾には消えたイベントが無いので、スナップショットに無くなったものを消す
            RemoveMissing(_projectiles, view => Destroy(view.gameObject));
        }

        /// <summary>範囲は置いた場所から動かないので、出たら作って、無くなったら消すだけ（落ちた演出は BurstEvent で出る）</summary>
        private void SyncEffects(StageSnapshot snapshot)
        {
            _aliveIds.Clear();
            foreach (var effect in snapshot.Effects)
            {
                _aliveIds.Add(effect.Id);
                if (!_effects.ContainsKey(effect.Id))
                {
                    _effects.Add(effect.Id, EffectView.Create(transform, effect));
                }
            }

            RemoveMissing(_effects, view => Destroy(view.gameObject));
        }

        /// <summary>_aliveIds に無いものを消す</summary>
        private void RemoveMissing<T>(Dictionary<int, T> views, System.Action<T> destroy)
        {
            _removeIds.Clear();
            foreach (var id in views.Keys)
            {
                if (!_aliveIds.Contains(id))
                {
                    _removeIds.Add(id);
                }
            }

            foreach (var id in _removeIds)
            {
                destroy(views[id]);
                views.Remove(id);
            }
        }

        private void OnHit(HitEvent e)
        {
            if (!_units.TryGetValue(e.TargetId, out var view))
            {
                return;
            }

            view.Flash();
            var color = view.Team == Team.Player ? _damageToPlayerColor : _damageToEnemyColor;
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
    }
}
