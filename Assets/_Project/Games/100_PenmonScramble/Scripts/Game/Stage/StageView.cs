using System.Collections.Generic;
using PenmonScramble.Common.Profile;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スナップショットを3Dで描く。Core の床の上の (x, y) を3Dの (x, 0, y) に置く（CoreConversions.ToFloor）。
    /// ユニット・弾・範囲と同じ ID で見た目を作り、消えたら消す。
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
        [SerializeField] private Color _damageToEnemyColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _damageToPlayerColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private SpecialCutIn _cutIn;
        // シェーダーはシーンの参照で持つ（Shader.Find だとビルドに入らないことがあるため）
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;
        [SerializeField] private Shader _fxShader;
        // ペンモン画面の技の実演（SkillDemoView）で使う。床は1枚だけ敷き、カメラの追従・揺れ・カットイン・音・席の印を出さない
        [SerializeField] private bool _preview;

        // ボスが倒れたときの揺れ（マス・秒）
        private const float BossDefeatShake = 0.3f;
        private const float BossDefeatShakeSeconds = 0.5f;
        private const float SpawnPoofSeconds = 0.3f;
        // 倒したときに飛び散るコインの数（星はその半分）と、散らばる広さ
        private const int DefeatCoins = 3;
        private const int BossDefeatCoins = 10;
        private const float BossDefeatSpread = 1.8f;

        private readonly Dictionary<int, UnitView> _units = new Dictionary<int, UnitView>();
        private readonly Dictionary<int, ProjectileView> _projectiles = new Dictionary<int, ProjectileView>();
        private readonly Dictionary<int, EffectView> _effects = new Dictionary<int, EffectView>();
        private readonly Dictionary<int, WarningView> _warnings = new Dictionary<int, WarningView>();
        private readonly HashSet<int> _aliveIds = new HashSet<int>();
        private readonly List<int> _removeIds = new List<int>();
        private StageMaterials _materials;
        private int _round;
        private int _lastTick = -1;

        // 前のスナップショットからの時間（秒）。ゲストは2回に1回しか届かないので、ユニットの歩く速さを出すのに要る
        private float _sinceLastSnapshot = Balance.TickSeconds;

        private void Awake()
        {
            _materials = new StageMaterials(_toonShader, _blobShadowShader, _fxShader);
        }

        private void OnDestroy()
        {
            _materials?.DestroyAll();
        }

        public Color SeatColor(int seat) => _seatColors[seat % _seatColors.Length];

        /// <summary>新しいスナップショットを受け取る。出来事の演出を出し、新しく出たものを作り、位置を受け取る</summary>
        public void Apply(StageSnapshot snapshot)
        {
            var isNewRound = snapshot.Round != _round;
            if (isNewRound)
            {
                Rebuild(snapshot);
            }

            var ticks = isNewRound || _lastTick < 0 ? 1 : Mathf.Max(1, snapshot.Tick - _lastTick);
            _sinceLastSnapshot = ticks * Balance.TickSeconds;
            _lastTick = snapshot.Tick;

            // 倒れたユニットはスナップショットにもういないので、消す前に当たった演出を出す
            foreach (var hit in snapshot.Hits)
            {
                OnHit(hit);
            }

            foreach (var defeat in snapshot.Defeats)
            {
                OnDefeated(defeat);
            }

            SyncUnits(snapshot, showSpawn: !isNewRound);
            SyncProjectiles(snapshot);
            SyncEffects(snapshot);
            SyncWarnings(snapshot);
            PushBossWarnings(snapshot);
            PlayEvents(snapshot);
            UpdateGates(snapshot);
            if (!_preview)
            {
                FollowCamera(snapshot, snap: isNewRound);
            }
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
            _warnings.Clear();
            CreateGround(snapshot);
        }

        /// <param name="showSpawn">挑戦の途中で出てきた敵（次の波）は、ぽんと出てきたように見せる</param>
        private void SyncUnits(StageSnapshot snapshot, bool showSpawn)
        {
            _aliveIds.Clear();
            foreach (var unit in snapshot.Units)
            {
                _aliveIds.Add(unit.Id);
                if (_units.TryGetValue(unit.Id, out var view))
                {
                    view.PushState(unit, _sinceLastSnapshot);
                    continue;
                }

                _units.Add(unit.Id, CreateUnitView(unit));
                if (showSpawn)
                {
                    BurstFx.SpawnOnFloor(transform, DecalSprites.Ring, _materials.Decal, Color.white, unit.Position.ToVector2(), unit.Radius, unit.Radius * 2.5f, SpawnPoofSeconds);
                }
            }

            // 倒されずに消えた（ゲストが抜けた）ものは、演出なしで消す
            RemoveMissing(_units, view => Destroy(view.gameObject));
        }

        private UnitView CreateUnitView(UnitState unit)
        {
            if (unit.Team == Team.Enemy || _preview)
            {
                return UnitView.Create(transform, _materials, unit, null, null);
            }

            return UnitView.Create(transform, _materials, unit, SeatColor(unit.Seat), SeatLabel(unit.Seat));
        }

        /// <summary>頭の上・HUD に出す名前。名前の無い席は「P1」など（SeatNames）</summary>
        public static string SeatLabel(int seat) => SeatNames.Get(seat);

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
                    _projectiles.Add(projectile.Id, ProjectileView.Create(transform, _materials, projectile, projectile.Team == Team.Enemy));
                }
            }

            // 弾には消えたイベントが無いので、スナップショットに無くなったものを消す
            RemoveMissing(_projectiles, view => Destroy(view.gameObject));
        }

        /// <summary>範囲は出たら作って、無くなったら消す（落ちた演出は BurstEvent で出る）。動くのは雷雲だけ</summary>
        private void SyncEffects(StageSnapshot snapshot)
        {
            _aliveIds.Clear();
            foreach (var effect in snapshot.Effects)
            {
                _aliveIds.Add(effect.Id);
                if (_effects.TryGetValue(effect.Id, out var view))
                {
                    view.PushState(effect);
                }
                else
                {
                    _effects.Add(effect.Id, EffectView.Create(transform, _materials, effect));
                }
            }

            RemoveMissing(_effects, view => Destroy(view.gameObject));
        }

        /// <summary>大ボスの攻撃の予告。新しく出たら警報を鳴らす（同時にいくつ出ても1回）</summary>
        private void SyncWarnings(StageSnapshot snapshot)
        {
            _aliveIds.Clear();
            var appeared = false;
            foreach (var warning in snapshot.Warnings)
            {
                _aliveIds.Add(warning.Id);
                if (_warnings.TryGetValue(warning.Id, out var view))
                {
                    view.PushState(warning);
                }
                else
                {
                    _warnings.Add(warning.Id, WarningView.Create(transform, _materials, warning));
                    appeared = true;
                }
            }

            RemoveMissing(_warnings, view => Destroy(view.gameObject));
            if (appeared && !_preview)
            {
                GameSounds.Play(GameSound.Warning);
            }
        }

        /// <summary>大ボスに赤い範囲を渡して、攻撃の構え・攻撃が出た瞬間の動きをさせる（範囲は大ボスの分しか出ない）</summary>
        private void PushBossWarnings(StageSnapshot snapshot)
        {
            foreach (var view in _units.Values)
            {
                if (view.IsBigBoss)
                {
                    view.PushWarnings(snapshot.Warnings);
                }
            }
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
            DamagePopup.Spawn(transform, _materials, e.Position.ToVector2(), e.Damage, color);
        }

        private void OnDefeated(DefeatedEvent e)
        {
            if (!_units.TryGetValue(e.UnitId, out var view))
            {
                return;
            }

            _units.Remove(e.UnitId);
            view.PlayDefeat();
            if (e.Team == Team.Enemy)
            {
                // 倒したらコインと星が飛び散る（§9）。ボスは多く、広く
                DefeatSparkle.Spawn(transform, _materials, e.Position.ToVector2(), view.IsBoss ? BossDefeatCoins : DefeatCoins, view.IsBoss ? BossDefeatSpread : 1f);
            }

            if (view.IsBoss && !_preview)
            {
                _cameraShake.Shake(BossDefeatShake, BossDefeatShakeSeconds);
                GameSounds.Play(GameSound.BossDefeat);
            }
        }
    }
}
