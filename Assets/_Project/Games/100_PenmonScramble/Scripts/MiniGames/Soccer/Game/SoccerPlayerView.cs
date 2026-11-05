using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// フィールドの子1体: 本編の3Dのペンモン（PenguinMotion で歩く・ためる・蹴る・喜ぶ・悔しがる）と、
    /// 足元の輪（動かしている子は席の色の太い輪、AI の子はチームの色の細い輪）とためゲージの弧
    /// </summary>
    public sealed class SoccerPlayerView
    {
        private const float RingGloss = 0.3f;
        // 輪の大きさ（直径）・厚み。動かしている子の輪は太く大きくして目立たせる
        private const float RingSize = 1.0f;
        private const float RingThickness = 0.5f;
        private const float ControlledRingSize = 1.15f;
        private const float ControlledRingThickness = 1.1f;
        private const float RingLift = 0.02f;

        private readonly Transform _root;
        private readonly PenguinMotion _motion;
        private readonly Transform _ring;
        private readonly MeshRenderer _ringRenderer;
        private readonly SoccerChargeArc _charge;
        private readonly ToonMaterials _materials;
        private readonly int _team;
        private bool _controlled;

        public SoccerPlayerView(SoccerPlayer player, int speciesNo, ToonMaterials materials, Transform parent)
        {
            _materials = materials;
            _team = player.Team;
            _root = new GameObject($"Player{player.Index}").transform;
            _root.SetParent(parent, false);

            // 体だけ大きさを当たりの円に合わせる（輪と弧はマスの長さのまま置く）
            var body = new GameObject("Body").transform;
            body.SetParent(_root, false);
            body.localScale = Vector3.one * PenguinModelBuilder.ScaleForRadius(SoccerBalance.PlayerRadius);
            var look = PenguinLooks.Has(speciesNo) ? PenguinLooks.Get(speciesNo) : PenguinLooks.Get(1);
            var model = PenguinModelBuilder.Build(look, materials, body);
            // 太陽の影は切っているので、足元の丸い影にする（本編のステージと同じ）
            model.SetRealShadows(false);
            _motion = model.gameObject.AddComponent<PenguinMotion>();
            _motion.SnapFacing(SoccerSpace.Direction(player.Facing));

            var ring = new GameObject("Ring");
            ring.transform.SetParent(_root, false);
            ring.transform.localPosition = Vector3.up * RingLift;
            ring.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.Torus;
            _ringRenderer = ring.AddComponent<MeshRenderer>();
            _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring = ring.transform;
            _charge = new SoccerChargeArc(materials, _root);
            SetControlled(false, Color.white);
        }

        /// <summary>動かしている子は席の色の太い輪、そうでなければチームの色を暗くした細い輪</summary>
        public void SetControlled(bool controlled, Color seatColor)
        {
            _controlled = controlled;
            var size = controlled ? ControlledRingSize : RingSize;
            var thickness = controlled ? ControlledRingThickness : RingThickness;
            _ring.localScale = new Vector3(size, thickness * 0.1f, size);
            _ringRenderer.sharedMaterial = _materials.Get(controlled ? seatColor : SoccerColors.TeamDark(_team), RingGloss);
        }

        /// <summary>position は補間した足元の位置</summary>
        public void Sync(SoccerPlayer player, Vector3 position, float deltaTime)
        {
            _root.localPosition = position;
            _motion.Velocity = SoccerSpace.Direction(player.Velocity);
            _motion.Facing = SoccerSpace.Direction(player.Facing);
            var charge = player.IsCharging ? player.Charge01 : 0f;
            _motion.KickCharge = charge;
            _charge.Show(_controlled ? charge : 0f, SoccerSpace.Direction(player.Facing), deltaTime);
        }

        public int Team => _team;

        public void Kick(float strength) => _motion.PlayKick(strength);

        /// <summary>点を取って喜ぶ・取られて悔しがる（どちらも false で立った姿勢へ戻る）</summary>
        public void React(bool cheer, bool despair)
        {
            _motion.Cheering = cheer;
            _motion.Despairing = despair;
        }

        public void Destroy() => _charge.Destroy();
    }
}
