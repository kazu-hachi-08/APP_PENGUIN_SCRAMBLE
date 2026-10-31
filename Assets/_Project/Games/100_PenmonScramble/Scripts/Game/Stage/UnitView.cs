using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニット1体の見た目。種類の見た目（PenguinLooks）から3Dのペンギンを組み、動き（PenguinMotion）に状態を渡す。
    /// 大ボスは大ボスの組み立て役（BigBossModelBuilder）で組み、動き（BigBossMotion）に攻撃の構えも渡す（UnitView.BigBoss.cs）。
    /// プレイヤーは足元に席の色の輪と、頭の上に「P1」など。ボスは「BOSS」（UnitView.Parts.cs）。
    /// 凍る・無敵・突進・ダウン・被弾・交代の見た目は UnitView.Status.cs。
    /// 位置は更新（1/30秒）ごとに受け取り、その間を補間して描く（オンラインのゲストも同じ描き方にするため）
    /// </summary>
    public partial class UnitView : MonoBehaviour
    {
        // 吹っ飛びは Core の速度がスナップショットに無いので、当たった直後の動きの速さから見分ける。
        // どの種類の歩く速さ（最大 5.5 マス/秒）よりも速ければ吹っ飛ばされている
        private const float KnockbackSpeed = 6f;
        // 当たった更新と、その次の更新の動きを見る（当たった更新では、まだ飛んでいないことがあるため）
        private const int KnockbackWatchUpdates = 2;
        // 撃破の動きが終わらなかったとき（止めたままなど）でも、いつかは消す
        private const float DefeatTimeoutSeconds = 2f;

        private StageMaterials _materials;
        private ToyModel _model;
        private IUnitMotion _motion;
        private float _radius;
        private float _modelScale;
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
        private bool IsInMenu => (_flags & UnitFlags.InMenu) != 0;

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

        /// <summary>種類の見た目から組み立てる。当たりの円とペンギンの胴の太さ（大ボスは体の幅）をそろえる</summary>
        private void BuildModel(UnitState unit)
        {
            if (_model != null)
            {
                // Destroy は次のフレームまで残るので、先に外して、作り直した体と取り違えないようにする
                _model.transform.SetParent(null, false);
                _model.gameObject.SetActive(false);
                DestroyNow(_model.gameObject);
            }

            SpeciesNo = unit.SpeciesNo;
            if (BigBossModelBuilder.Has(SpeciesNo))
            {
                BuildBigBoss();
            }
            else
            {
                var penguin = PenguinModelBuilder.Build(PenguinLooks.Get(SpeciesNo), _materials.Toon, transform);
                _model = penguin;
                _motion = penguin.gameObject.AddComponent<PenguinMotion>();
                _bigBoss = null;
                _modelScale = PenguinModelBuilder.ScaleForRadius(_radius);
            }

            _model.transform.localScale = Vector3.one * _modelScale;
            // どの環境でも足元の丸い影にする
            _model.SetRealShadows(false);
            _motion.SnapFacing(unit.Facing.ToFloor());
            ApplyMotionState(unit, Vector3.zero);
            PlaceLabel();
            OnModelBuilt(unit);
        }

        /// <param name="seconds">前の更新からの時間（秒）。ゲストは2回に1回しか届かないので、歩く速さを正しく出すために要る</param>
        public void PushState(UnitState unit, float seconds)
        {
            _from = _to;
            _to = unit.Position.ToVector2();
            var wasDown = IsDown;
            var wasSubmerged = IsSubmerged;
            _flags = unit.Flags;
            _revive = unit.Revive;
            if (unit.SpeciesNo != SpeciesNo)
            {
                // 交代した（自分で替えた・倒れて次の子が出た）。組み直して、ぽんと出てきたように見せる
                BuildModel(unit);
                Poof();
                PopIn();
                return;
            }

            if (wasDown != IsDown)
            {
                // ダウンした・起き上がった瞬間を見せる（起き上がりは先頭の子なので、種類が変わればそちらの演出で出る）
                Poof();
            }

            if (wasSubmerged != IsSubmerged)
            {
                OnSubmergedChanged();
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
            _motion.WindingUp = unit.Has(UnitFlags.WindingUp);
            if (_bigBoss != null)
            {
                _bigBoss.Submerged = unit.Has(UnitFlags.Submerged);
            }
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
            if ((_flags & UnitFlags.Frozen) != 0)
            {
                // 凍ったまま飛ばされたら、転がらずに氷の塊ごとすべる
                return;
            }

            var speed = velocity.magnitude;
            if ((_flags & UnitFlags.Dashing) != 0 || speed < KnockbackSpeed)
            {
                return;
            }

            _knockbackWatch = 0;
            var initialSpeed = speed + Balance.KnockbackDeceleration * seconds * 0.5f;
            _motion.PlayKnockback(velocity / speed * initialSpeed);
        }

        /// <summary>alpha は前回の更新から次の更新までの進み具合（0〜1）</summary>
        public void Render(float alpha)
        {
            if (_defeatTimer >= 0f)
            {
                return;
            }

            transform.position = RenderPosition(alpha).ToFloor();
            RenderMarks();
        }

        /// <summary>当たった。一瞬白く光り、のけぞって、吹っ飛ばされていたら転がる</summary>
        public void Flash()
        {
            _flashTimer = FlashSeconds;
            _knockbackWatch = KnockbackWatchUpdates;
            // 凍っている間は氷の中で固まっているので、のけぞらない
            if ((_flags & UnitFlags.Frozen) == 0)
            {
                _motion.PlayHit();
            }
        }

        public void PlayAttack(SkillShape shape) => _motion.PlayAttack(shape);

        public void PlaySpecial() => _motion.PlaySpecial();

        /// <summary>倒れたら回りながら昇って消える。Core からは消えているので、ここからは見た目だけで動く</summary>
        public void PlayDefeat()
        {
            _defeatTimer = 0f;
            _model.Figure.gameObject.SetActive(true);
            _motion.Velocity = Vector3.zero;
            _motion.Down = false;
            _motion.Sliding = false;
            _motion.WindingUp = false;
            _motion.PlayDefeat();
            HideMarks();
            // 凍ったまま倒れたら、氷が砕けてから飛んでいく
            ApplyStatus();
        }

        /// <summary>Play していないとき（エディターのテスト）は Destroy が使えないので、すぐ消す</summary>
        private static void DestroyNow(Object obj)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void Update()
        {
            TickStatus(Time.deltaTime);
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
