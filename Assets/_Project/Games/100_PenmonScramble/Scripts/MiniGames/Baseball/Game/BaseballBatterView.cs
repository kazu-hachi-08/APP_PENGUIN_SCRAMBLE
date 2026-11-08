using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 打つ子: バットの軸の左（三塁側）に、ホームの方を向いて立つ（右打ち）。両ヒレで軸の金具を握り、
    /// バットの角度に合わせて体をひねる（構えではバットが捕手の側＝右後ろ、振るとヒレと体が前へ回る）。
    /// 軸が打席の中を左右へ動いたら、握ったまま横歩きでついて行く。
    /// 当たったら一塁へ走り出し、ホームランで喜ぶ・アウトと三振で悔しがる・四球は一塁へ歩く。
    /// 種類ごとの体は一度作ったら取っておく（打者が替わっても作り直さない）
    /// </summary>
    public sealed class BaseballBatterView
    {
        // 軸からの立つ位置（左へ・後ろへ）。ヒレの先が軸の金具に届く所
        private const float StandLeft = 0.42f;
        private const float StandBack = 0.05f;
        // バットの角度を体のひねり（向き）とヒレの振りに分ける割合。体は一部だけ回し、残りはヒレが受け持つ
        private const float TwistShare = 0.35f;
        private const float SwingShare = 0.8f;
        private const float MaxTwist = 45f;
        private const float RunSpeed = 3.2f;
        private const float WalkSpeed = 1.4f;
        // 一塁の手前で止まる（塁の上に重ならないように）
        private const float StopShort = 0.35f;

        private readonly Dictionary<(int, bool), BaseballPenguin> _bodies = new Dictionary<(int, bool), BaseballPenguin>();
        private readonly BaseballFieldDef _field;
        private readonly ToonMaterials _materials;
        private readonly Transform _parent;
        private BaseballPenguin _current;
        private Vector3 _runTarget;
        private float _runSpeed;
        private bool _running;
        private bool _inStance;
        private float _batOffset;

        public BaseballBatterView(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            _field = field;
            _materials = materials;
            _parent = parent;
        }

        private Vector3 StandPoint => BaseballSpace.ToWorld(_field.BatPivot) + new Vector3(_batOffset - StandLeft, 0f, -StandBack);

        /// <summary>この子を打席に立たせる（ほかの子は隠す）。替わったときはぽんと出す</summary>
        public void Use(int species, bool wild)
        {
            // 同じ種類が両方のチームにいることがあるので、CPU の暗い色の体とは別に持つ
            var body = BaseballPenguin.GetOrBuild(_bodies, "Batter", species, wild, _materials, _parent);

            var changed = _current != body;
            _current?.SetActive(false);
            _current = body;
            body.SetActive(true);
            EnterStance(changed);
        }

        /// <summary>打席の構えに戻す（走っていた・喜んでいた子は、打席にぽんと戻る）</summary>
        public void EnterStance(bool pop)
        {
            if (_current == null)
            {
                return;
            }

            var moved = Vector3.Distance(_current.Root.localPosition, StandPoint) > 0.1f;
            _current.Motion.ResetPose();
            _current.PlaceAt(StandPoint, Vector3.right, pop || moved);
            _current.Motion.Holding = true;
            _running = false;
            _inStance = true;
        }

        public void Tick(float batDegrees, float batOffset, float deltaTime)
        {
            _batOffset = batOffset;
            if (_current == null)
            {
                return;
            }

            if (_inStance)
            {
                FollowPivot(deltaTime);
                PoseWithBat(batDegrees);
            }

            _current.Tick(deltaTime);
            if (_running)
            {
                TickRun(deltaTime);
            }
        }

        /// <summary>軸について横へ動く（動いた速さを渡して、足踏みさせる）</summary>
        private void FollowPivot(float deltaTime)
        {
            var root = _current.Root;
            var to = StandPoint;
            _current.Motion.Velocity = deltaTime > 0f ? (to - root.localPosition) / deltaTime : Vector3.zero;
            root.localPosition = to;
        }

        /// <summary>バットの角度（正が投手の方）から、体の向きとヒレの振り（正が右へ振り上げる）を決める</summary>
        private void PoseWithBat(float batDegrees)
        {
            var twist = Mathf.Clamp(batDegrees * TwistShare, -MaxTwist, MaxTwist) * Mathf.Deg2Rad;
            _current.Motion.Facing = new Vector3(Mathf.Cos(twist), 0f, Mathf.Sin(twist));
            _current.Motion.HoldSwing = -batDegrees * SwingShare;
        }

        /// <summary>今いる所（まだ誰も立っていなければ打席）</summary>
        public Vector3 Position => _current != null ? _current.Root.localPosition : StandPoint;

        /// <summary>隠す（塁に出て走者の子と入れ替わった。次の打者が Use で出てくる）</summary>
        public void Hide()
        {
            if (_current == null)
            {
                return;
            }

            _current.SetActive(false);
            _current.Motion.Velocity = Vector3.zero;
            _running = false;
            _inStance = false;
        }

        /// <summary>打った: バットを置いて一塁へ走り出す</summary>
        public void RunToFirst() => StartRun(RunSpeed);

        /// <summary>四球: 一塁へ歩く</summary>
        public void WalkToFirst() => StartRun(WalkSpeed);

        private void StartRun(float speed)
        {
            if (_current == null)
            {
                return;
            }

            _inStance = false;
            _current.Motion.Holding = false;
            _current.Motion.HoldSwing = 0f;
            var first = BaseballSpace.ToWorld(_field.Base(1));
            var from = _current.Root.localPosition;
            _runTarget = first - (first - from).normalized * StopShort;
            _runSpeed = speed;
            _running = true;
        }

        private void TickRun(float deltaTime)
        {
            var root = _current.Root;
            var to = _runTarget - root.localPosition;
            var step = _runSpeed * deltaTime;
            if (to.magnitude <= step)
            {
                root.localPosition = _runTarget;
                _current.Motion.Velocity = Vector3.zero;
                _running = false;
                return;
            }

            var direction = to.normalized;
            root.localPosition += direction * step;
            _current.Motion.Velocity = direction * _runSpeed;
            _current.Motion.Facing = direction;
        }

        /// <summary>喜ぶ（ホームラン・ヒット）・悔しがる（アウト・三振）。構えのままなら構えを解く</summary>
        public void React(bool cheer, bool despair)
        {
            if (_current == null)
            {
                return;
            }

            if (_inStance)
            {
                _inStance = false;
                _current.Motion.Holding = false;
                _current.Motion.HoldSwing = 0f;
            }

            _current.React(cheer, despair);
        }
    }
}
