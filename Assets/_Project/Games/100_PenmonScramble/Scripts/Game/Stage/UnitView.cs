using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニット1体の見た目。種類の見た目（PenguinLooks）から3Dのペンギンを組み、動き（PenguinMotion）に状態を渡す。
    /// プレイヤーは足元に席の色の輪と、頭の上に「P1」など。ボスは「BOSS」（UnitView.Parts.cs）。
    /// 位置は更新（1/30秒）ごとに受け取り、その間を補間して描く（オンラインのゲストも同じ描き方にするため）
    /// </summary>
    public partial class UnitView : MonoBehaviour
    {
        private const float SwitchPoofSeconds = 0.3f;

        // 吹っ飛びは Core の速度がスナップショットに無いので、当たった直後の動きの速さから見分ける。
        // どの種類の歩く速さ（最大 5.5 マス/秒）よりも速ければ吹っ飛ばされている
        private const float KnockbackSpeed = 6f;
        // 当たった更新と、その次の更新の動きを見る（当たった更新では、まだ飛んでいないことがあるため）
        private const int KnockbackWatchUpdates = 2;
        // 撃破の動きが終わらなかったとき（止めたままなど）でも、いつかは消す
        private const float DefeatTimeoutSeconds = 2f;

        private StageMaterials _materials;
        private PenguinModel _model;
        private PenguinMotion _motion;
        private float _radius;
        private Vector2 _from;
        private Vector2 _to;
        private UnitFlags _flags;
        private int _knockbackWatch;
        private float _defeatTimer = -1f;

        public Team Team { get; private set; }
        public int SpeciesNo { get; private set; }
        public bool IsBoss => (_flags & UnitFlags.Boss) != 0;
        private bool IsDown => (_flags & UnitFlags.Down) != 0;
        private bool IsSubmerged => (_flags & UnitFlags.Submerged) != 0;

        /// <param name="seatColor">プレイヤーの席の色。null なら輪と文字を出さない（敵）</param>
        public static UnitView Create(Transform parent, StageMaterials materials, UnitState unit, Color? seatColor, string label)
        {
            var view = new GameObject($"Unit{unit.Id}_{unit.Team}").AddComponent<UnitView>();
            view.transform.SetParent(parent, false);
            view._materials = materials;
            view.Setup(unit);
            view.CreateMarks(seatColor, label);
            return view;
        }

        private void Setup(UnitState unit)
        {
            Team = unit.Team;
            _radius = unit.Radius;
            _from = _to = unit.Position.ToVector2();
            _flags = unit.Flags;
            _revive = unit.Revive;
            BuildModel(unit);
            Render(1f);
        }

        /// <summary>種類の見た目から組み立てる。当たりの円とペンギンの胴の太さをそろえる</summary>
        private void BuildModel(UnitState unit)
        {
            if (_model != null)
            {
                Destroy(_model.gameObject);
            }

            SpeciesNo = unit.SpeciesNo;
            _model = PenguinModelBuilder.Build(PenguinLooks.Get(unit.SpeciesNo), _materials.Toon, transform);
            _model.transform.localScale = Vector3.one * PenguinModelBuilder.ScaleForRadius(_radius);
            // フェーズ0で決めたとおり、どの環境でも足元の丸い影にする
            _model.SetRealShadows(false);
            _motion = _model.gameObject.AddComponent<PenguinMotion>();
            _motion.SnapFacing(unit.Facing.ToFloor());
            ApplyMotionState(unit, Vector3.zero);
            PlaceLabel();
        }

        /// <param name="seconds">前の更新からの時間（秒）。ゲストは2回に1回しか届かないので、歩く速さを正しく出すために要る</param>
        public void PushState(UnitState unit, float seconds)
        {
            _from = _to;
            _to = unit.Position.ToVector2();
            var wasDown = IsDown;
            _flags = unit.Flags;
            _revive = unit.Revive;
            if (unit.SpeciesNo != SpeciesNo)
            {
                // 交代した（自分で替えた・倒れて次の子が出た）。組み直して、ぽんと出てきたように見せる
                BuildModel(unit);
                Poof();
                return;
            }

            if (wasDown != IsDown)
            {
                // ダウンした・起き上がった瞬間を見せる（起き上がりは先頭の子なので、種類が変わればそちらの演出で出る）
                Poof();
            }

            var velocity = (_to - _from).ToFloor() / Mathf.Max(seconds, Balance.TickSeconds);
            ApplyMotionState(unit, velocity);
            WatchKnockback(velocity, seconds);
        }

        private void ApplyMotionState(UnitState unit, Vector3 velocity)
        {
            _motion.Velocity = velocity;
            _motion.Facing = unit.Facing.ToFloor();
            _motion.Sliding = unit.Has(UnitFlags.Dashing);
            _motion.Down = unit.Has(UnitFlags.Down);
            // もぐっている大ボスは、床の丸い影だけ見せる
            _model.Figure.gameObject.SetActive(!unit.Has(UnitFlags.Submerged));
        }

        /// <summary>
        /// 当たった直後に歩くより速く動いていたら、吹っ飛ばされたとみなして転がす。
        /// 測った速さは更新の間の平均で、Core はその間も減速しているので、間の半分ぶんの減速を足して初速に戻す
        /// </summary>
        private void WatchKnockback(Vector3 velocity, float seconds)
        {
            if (_knockbackWatch <= 0)
            {
                return;
            }

            _knockbackWatch--;
            var speed = velocity.magnitude;
            if ((_flags & UnitFlags.Dashing) != 0 || speed < KnockbackSpeed)
            {
                return;
            }

            _knockbackWatch = 0;
            var initialSpeed = speed + Balance.KnockbackDeceleration * seconds * 0.5f;
            _motion.PlayKnockback(velocity / speed * initialSpeed);
        }

        private void Poof()
        {
            BurstFx.SpawnOnFloor(transform.parent, PlaceholderSprites.Ring, _materials.Decal, Color.white, _to, _radius, _radius * 2f, SwitchPoofSeconds);
        }

        /// <summary>alpha は前回の更新から次の更新までの進み具合（0〜1）</summary>
        public void Render(float alpha)
        {
            if (_defeatTimer >= 0f)
            {
                return;
            }

            transform.position = Vector2.Lerp(_from, _to, alpha).ToFloor();
            RenderMarks();
        }

        /// <summary>当たった。のけぞって、吹っ飛ばされていたら転がる</summary>
        public void Flash()
        {
            _motion.PlayHit();
            _knockbackWatch = KnockbackWatchUpdates;
        }

        public void PlayAttack(SkillShape shape) => _motion.PlayAttack(shape);

        public void PlaySpecial() => _motion.PlaySpecial();

        /// <summary>倒れたら回りながら昇って消える（§9）。Core からは消えているので、ここからは見た目だけで動く</summary>
        public void PlayDefeat()
        {
            _defeatTimer = 0f;
            _model.Figure.gameObject.SetActive(true);
            _motion.Velocity = Vector3.zero;
            _motion.Down = false;
            _motion.Sliding = false;
            _motion.PlayDefeat();
            HideMarks();
        }

        private void Update()
        {
            if (_defeatTimer < 0f)
            {
                return;
            }

            _defeatTimer += Time.deltaTime;
            if (_motion.IsDefeatFinished || _defeatTimer >= DefeatTimeoutSeconds)
            {
                Destroy(gameObject);
            }
        }
    }
}
