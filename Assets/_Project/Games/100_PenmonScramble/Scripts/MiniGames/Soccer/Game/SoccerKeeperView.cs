using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// おもちゃのキーパーの動き（SoccerKeeper の状態を写す）: 構えては横へちょこちょこ跳ね、背中のねじが回る。
    /// 飛びつくと台ごと横へ倒れて両手を伸ばし、起き上がる。取ったら胸の前で抱え、投げるときは下から振る。
    /// 点を取られるとうなだれる
    /// </summary>
    public sealed class SoccerKeeperView
    {
        // ねじの回る速さ（度/秒）。動くほど速く回す
        private const float KeySpin = 120f;
        private const float KeySpinPerSpeed = 160f;
        // 横に動くときの跳ね（ゼンマイのおもちゃらしく小刻みに）と、動く向きへの傾き
        private const float HopRate = 7f;
        private const float HopHeight = 0.06f;
        private const float MoveRoll = 8f;
        private const float IdleBob = 0.015f;
        private const float IdleBobRate = 2f;

        // 飛びつき: 倒れる角度・跳ぶ高さ・倒れきるまでの秒・起き上がりにかける秒・気づくまでかがむ深さ
        private const float DiveAngle = 82f;
        private const float DiveHop = 0.35f;
        private const float DiveSeconds = 0.3f;
        private const float GetUpSeconds = 0.3f;
        private const float CrouchSquash = 0.12f;

        // 腕の構え（度）: 横へ開く・前へ上げる
        private static readonly Vector2 ReadyArms = new Vector2(35f, 25f);
        private static readonly Vector2 DiveArms = new Vector2(165f, 10f);
        private static readonly Vector2 HoldArms = new Vector2(18f, 80f);
        private static readonly Vector2 SlumpArms = new Vector2(6f, -5f);
        private const float ArmBlendRate = 12f;
        private const float SlumpHeadDown = 28f;

        // 投げる（下から振る）・取った瞬間に弾む
        private const float ThrowSeconds = 0.35f;
        private const float ThrowFrom = -50f;
        private const float ThrowTo = 110f;
        private const float CatchPopSeconds = 0.25f;
        private const float CatchPopScale = 0.12f;

        private const float ShadowSize = 1.1f;
        private const float ShadowLift = 0.02f;

        private readonly SoccerKeeperModel _model;
        private readonly Transform _shadow;
        private Vector2 _arms = ReadyArms;
        private float _time;
        private float _keyAngle;
        private float _throw = 1f;
        private float _catchPop = 1f;
        private bool _slumping;

        public SoccerKeeperView(SoccerKeeper keeper, ToonMaterials materials, Transform parent)
        {
            Team = keeper.Team;
            _model = SoccerKeeperModel.Build(SoccerColors.Team(keeper.Team), SoccerColors.TeamDark(keeper.Team), materials, parent);
            _model.Root.localRotation = Quaternion.LookRotation(SoccerSpace.Direction(keeper.Facing));
            _shadow = new GameObject("KeeperShadow").transform;
            _shadow.SetParent(parent, false);
            var blob = new GameObject("Blob");
            blob.transform.SetParent(_shadow, false);
            blob.transform.localScale = new Vector3(ShadowSize, 1f, ShadowSize);
            blob.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorQuad;
            var renderer = blob.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.BlobShadow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public int Team { get; }

        /// <summary>position は補間した足元の位置、holding はボールを持っているか</summary>
        public void Sync(SoccerKeeper keeper, Vector3 position, bool holding, float deltaTime)
        {
            _time += deltaTime;
            _throw = Mathf.Min(1f, _throw + deltaTime / ThrowSeconds);
            _catchPop = Mathf.Min(1f, _catchPop + deltaTime / CatchPopSeconds);
            _model.Root.localPosition = position;
            _shadow.localPosition = new Vector3(position.x, ShadowLift, position.z);

            var (tilt, lift, squash) = Body(keeper);
            var side = new Vector3(0f, 0f, keeper.DiveSide);
            var axis = Vector3.Cross(Vector3.up, side);
            var rootRotation = _model.Root.localRotation;
            var worldTilt = Quaternion.AngleAxis(tilt, axis.sqrMagnitude > 0f ? axis : Vector3.right);
            _model.Figure.localRotation = Quaternion.Inverse(rootRotation) * worldTilt * rootRotation * Quaternion.Euler(0f, 0f, Roll(keeper));
            _model.Figure.localPosition = Vector3.up * lift;
            var pop = Mathf.Sin(_catchPop * Mathf.PI) * CatchPopScale;
            _model.Figure.localScale = new Vector3(1f + squash * 0.5f + pop, 1f - squash + pop, 1f + squash * 0.5f + pop);

            PoseArms(keeper, holding, deltaTime);
            _model.Head.localRotation = Quaternion.Euler(_slumping ? SlumpHeadDown : 0f, 0f, 0f);
            _keyAngle += (KeySpin + Mathf.Abs(keeper.Speed) * KeySpinPerSpeed) * deltaTime;
            _model.Key.localRotation = Quaternion.Euler(0f, 0f, _keyAngle);
        }

        /// <summary>投げた: 腕を後ろから前へ振る</summary>
        public void PlayThrow() => _throw = 0f;

        /// <summary>取った: 体がぽんと弾む</summary>
        public void PlayCatch() => _catchPop = 0f;

        /// <summary>点を取られてうなだれる（キックオフで戻す）</summary>
        public void SetSlump(bool slumping) => _slumping = slumping;

        /// <summary>状態ごとの体の傾き（度）・浮く高さ・つぶれ</summary>
        private (float tilt, float lift, float squash) Body(SoccerKeeper keeper)
        {
            var seconds = keeper.StateSeconds;
            switch (keeper.State)
            {
                case SoccerKeeperState.Dive:
                    if (seconds < SoccerBalance.KeeperReactSeconds)
                    {
                        return (0f, 0f, CrouchSquash * Mathf.Clamp01(seconds / SoccerBalance.KeeperReactSeconds));
                    }

                    var p = Mathf.Clamp01((seconds - SoccerBalance.KeeperReactSeconds) / DiveSeconds);
                    return (DiveAngle * Mathf.SmoothStep(0f, 1f, p), Mathf.Sin(p * Mathf.PI) * DiveHop, 0f);
                case SoccerKeeperState.Down:
                    var getUp = Mathf.InverseLerp(SoccerBalance.KeeperDownSeconds - GetUpSeconds, SoccerBalance.KeeperDownSeconds, seconds);
                    return (DiveAngle * (1f - Mathf.SmoothStep(0f, 1f, getUp)), 0f, 0f);
                default:
                    var moving = Mathf.Clamp01(Mathf.Abs(keeper.Speed) / SoccerBalance.KeeperGuardSpeed);
                    var hop = Mathf.Abs(Mathf.Sin(_time * HopRate * Mathf.PI)) * HopHeight * moving;
                    var bob = (Mathf.Sin(_time * IdleBobRate * Mathf.PI * 2f) * 0.5f + 0.5f) * IdleBob;
                    return (0f, hop + bob, 0f);
            }
        }

        /// <summary>構えて横へ動くとき、動く向きへ少し傾く（前を向いているので、横は体の左右）</summary>
        private float Roll(SoccerKeeper keeper)
        {
            if (keeper.State != SoccerKeeperState.Guard)
            {
                return 0f;
            }

            // 体の右（ローカル +X）がピッチの Y のどちらか: 前が +X 向きなら右は −Y
            var rightIsPositiveY = keeper.Facing.X < 0f;
            var towardRight = rightIsPositiveY ? keeper.Speed : -keeper.Speed;
            return -Mathf.Clamp(towardRight / SoccerBalance.KeeperGuardSpeed, -1f, 1f) * MoveRoll;
        }

        private void PoseArms(SoccerKeeper keeper, bool holding, float deltaTime)
        {
            var target = keeper.State == SoccerKeeperState.Dive || keeper.State == SoccerKeeperState.Down ? DiveArms
                : holding ? HoldArms
                : _slumping ? SlumpArms
                : ReadyArms;
            _arms = Vector2.Lerp(_arms, target, 1f - Mathf.Exp(-ArmBlendRate * deltaTime));
            var forward = _arms.y;
            if (_throw < 1f)
            {
                forward = Mathf.Lerp(ThrowFrom, ThrowTo, Mathf.SmoothStep(0f, 1f, _throw));
            }

            // 前へ上げるのは X の負の回転、横へ開くのは左腕が Z の負・右腕が Z の正の回転
            _model.ArmLeft.localRotation = Quaternion.Euler(-forward, 0f, -_arms.x);
            _model.ArmRight.localRotation = Quaternion.Euler(-forward, 0f, _arms.x);
        }
    }
}
