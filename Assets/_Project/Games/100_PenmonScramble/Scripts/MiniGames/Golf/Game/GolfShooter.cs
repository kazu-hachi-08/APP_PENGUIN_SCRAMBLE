using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 打つ子: 今のペンモンの3Dに木のクラブを持たせ、球の横に立たせる（右打ち: 狙う向きが体の左。球を正面に見る）。
    /// 動きは「構える → ゲージに合わせて振りかぶる → 打つ → 球を見送る」、カップインで喜ぶ・池で頭を抱える（GolfShooter.Swing.cs）。
    /// 種類ごとの体は一度作ったら取っておく（1打ごとに子を替えても作り直さない）
    /// </summary>
    public sealed partial class GolfShooter : MonoBehaviour
    {
        // ペンギンの背の高さ約1.2 → 1m ほど。球（直径 0.36m）が大きめに見える、おもちゃの縮尺
        private const float PenguinScale = 0.82f;

        // クラブを持つ所（胴の前・胸の下。ペンギンの寸法）と、柄を前へ倒す角度（頭が足の前の床に着くように）
        private static readonly Vector3 GripPoint = new Vector3(0f, 0.5f, 0.42f);
        private const float ClubLean = 30f;

        // 構えた位置から、テレポートしたとみなす距離（ぽんと出てくる動きにする）
        private const float TeleportDistance = 1f;
        private const float PopSeconds = 0.28f;
        private const float PopFrom = 0.3f;
        private const float PopOvershoot = 0.15f;

        private readonly Dictionary<int, Actor> _actors = new Dictionary<int, Actor>();
        private ToonMaterials _materials;
        private Actor _current;
        private float _pop = 1f;

        public void Init(ToonMaterials materials) => _materials = materials;

        public bool Visible => _current != null && _current.Root.gameObject.activeSelf;

        public void Hide()
        {
            foreach (var actor in _actors.Values)
            {
                actor.Root.gameObject.SetActive(false);
            }

            _current = null;
            _state = State.Hidden;
        }

        /// <summary>この子を出す（ほかの子は隠す）。替わったときはぽんと出す</summary>
        private Actor Use(GolfMember member)
        {
            if (!_actors.TryGetValue(member.SpeciesNo, out var actor))
            {
                actor = Actor.Build(member, _materials, transform);
                _actors.Add(member.SpeciesNo, actor);
            }

            if (_current != actor)
            {
                _current?.Root.gameObject.SetActive(false);
                actor.Root.gameObject.SetActive(true);
                actor.Motion.ResetPose();
                _current = actor;
                // 新しい子は構えから始め直す
                _state = State.Hidden;
                _swing = 0f;
                StartPop();
            }

            return actor;
        }

        /// <summary>
        /// 球の横に立つ位置。クラブの頭の中心が、球の狙う向きと反対の側（球に頭の面が触れる所）に来るよう、体の位置を逆算する
        /// </summary>
        private static Vector3 StandPoint(Actor actor, Vector3 ball, Vector3 aim, GolfSurface surface)
        {
            var face = FacingFor(aim);
            var contact = ball - aim * (GolfBalance.BallRadius + GolfClub.HeadHalfWidth * PenguinScale);
            var root = contact - Quaternion.LookRotation(face) * actor.HeadOffset;
            root.y = surface.HeightAt(new Vec2(root.x, root.z));
            return root;
        }

        /// <summary>球を正面に見る向き（狙う向きの右）。右打ちなので狙う向きが体の左になる</summary>
        private static Vector3 FacingFor(Vector3 aim) => new Vector3(aim.z, 0f, -aim.x);

        private void MoveTo(Vector3 position)
        {
            var root = _current.Root;
            if (Vector3.Distance(root.localPosition, position) > TeleportDistance)
            {
                StartPop();
            }

            root.localPosition = position;
        }

        private void StartPop() => _pop = 0f;

        private void TickPop(float dt)
        {
            if (_current == null)
            {
                return;
            }

            _pop = Mathf.Min(1f, _pop + dt / PopSeconds);
            var eased = 1f - (1f - _pop) * (1f - _pop);
            var scale = Mathf.LerpUnclamped(PopFrom, 1f, eased) + Mathf.Sin(_pop * Mathf.PI) * PopOvershoot;
            _current.Root.localScale = Vector3.one * (PenguinScale * scale);
        }

        /// <summary>種類1つ分の体とクラブ</summary>
        private sealed class Actor
        {
            public Transform Root { get; private set; }
            public PenguinMotion Motion { get; private set; }
            public Transform Grip { get; private set; }
            public Transform Head { get; private set; }

            /// <summary>構えたとき（振りの角度 0）のクラブの頭の中心。根元から見た位置（体は +Z を向いている）</summary>
            public Vector3 HeadOffset { get; private set; }

            public static Actor Build(GolfMember member, ToonMaterials materials, Transform parent)
            {
                var root = new GameObject($"Shooter{member.SpeciesNo}").transform;
                root.SetParent(parent, false);
                var model = PenguinModelBuilder.Build(PenguinLooks.Get(member.SpeciesNo), materials, root);
                // 太陽の影は切っているので、足元の丸い影にする（本編のステージと同じ）
                model.SetRealShadows(false);
                var motion = model.gameObject.AddComponent<PenguinMotion>();
                var grip = new GameObject("Club").transform;
                grip.SetParent(model.Body, false);
                grip.localPosition = GripPoint;
                var head = GolfClub.Build(materials, grip, PenguinPalette.ElementColor(member.Element));
                root.localScale = Vector3.one * PenguinScale;

                var actor = new Actor { Root = root, Motion = motion, Grip = grip, Head = head };
                actor.MeasureAddress();
                root.gameObject.SetActive(false);
                return actor;
            }

            public void PoseClub(float swing)
            {
                Grip.localRotation = Quaternion.Euler(0f, 0f, swing) * Quaternion.Euler(-ClubLean, 0f, 0f);
            }

            /// <summary>
            /// 構えた姿勢を一度置いて、クラブの頭がどこに来るかを測る（体型で胴の太さ・背の高さが違うので、計算で決めずに測る）
            /// </summary>
            private void MeasureAddress()
            {
                Motion.SnapFacing(Vector3.forward);
                Motion.Holding = true;
                Motion.HoldSwing = 0f;
                // 構えへの切り替えが済むまで進める（まばたきや呼吸は測る値にほとんど効かない）
                Motion.Tick(1f);
                PoseClub(0f);
                HeadOffset = Root.InverseTransformPoint(Head.position) * PenguinScale;
                Motion.ResetPose();
            }
        }
    }
}
