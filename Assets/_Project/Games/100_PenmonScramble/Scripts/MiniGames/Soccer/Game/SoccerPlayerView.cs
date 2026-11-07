using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// フィールドの子1体: 本編の3Dのペンモン（PenguinMotion で歩く・ためる・蹴る・すべる・転ぶ・喜ぶ・悔しがる）と、
    /// 頭のチームの色のはちまき、足元の輪（動かしている子はチームの色を明るくした太い輪でゆっくり脈打つ、AI の子は細く沈んだチームの色の輪）と
    /// ためゲージの弧、この端末の人が動かしている子だけ頭の上の黄色い印（SoccerControlMarker）。
    /// CPU のチームの子は本編の雑魚と同じく暗い色にする（はちまきは暗くしない）。氷の必殺シュートで凍っている間は、水色に染めて止める
    /// </summary>
    public sealed class SoccerPlayerView
    {
        private const float RingGloss = 0.3f;
        // 輪の大きさ（直径）・厚み。AI の子の輪は味方の場所が分かる程度に細く、動かしている子の輪は太く大きくして差をはっきりさせる
        // （6体全員に同じくらいの輪があると、どれが自分か見分けにくかったため）
        private const float RingSize = 0.9f;
        private const float RingThickness = 0.4f;
        private const float ControlledRingSize = 1.25f;
        private const float ControlledRingThickness = 1.5f;
        // 動かしている子の輪はチームの色を白へ寄せる（席の色だと、赤のチームに青い輪が付いて相手に見えたため）
        private const float ControlledRingLighten = 0.4f;
        // AI の子の輪は灰色へ寄せて沈める（動かしている子の明るい輪を引き立てる）
        private const float AiRingMute = 0.35f;
        private static readonly Color AiRingMuteColor = new Color(0.45f, 0.45f, 0.45f);
        // 動かしている子の輪のゆっくりした脈（大きさの揺れ幅と速さ）
        private const float PulseAmount = 0.06f;
        private const float PulseRate = 4f;
        // 切り替わったときの輪のポン
        private const float RingPopScale = 1.8f;
        private const float RingPopSeconds = 0.3f;
        private const float RingLift = 0.02f;
        private const float FrozenTintAmount = 0.6f;
        private static readonly Color FrozenColor = new Color(0.65f, 0.9f, 1f);

        private readonly Transform _root;
        private readonly PenguinMotion _motion;
        private readonly Transform _ring;
        private readonly MeshRenderer _ringRenderer;
        private readonly SoccerChargeArc _charge;
        private readonly SoccerControlMarker _marker;
        private readonly ToonMaterials _materials;
        private readonly int _team;
        private readonly PenguinModel _model;
        private readonly PenguinTint _baseTint;
        private bool _controlled;
        private bool _frozen;
        private float _pulseTime;
        private float _ringPop;

        public SoccerPlayerView(SoccerPlayer player, int speciesNo, bool wild, ToonMaterials materials, Transform parent)
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
            _model = model;
            // 太陽の影は切っているので、足元の丸い影にする（本編のステージと同じ）
            model.SetRealShadows(false);
            _baseTint = wild ? PenguinTint.Dark(UnitView.EnemyDarkness) : PenguinTint.None;
            model.SetTint(_baseTint);
            PenguinModelBuilder.AddTeamHeadband(model, look, materials, SoccerColors.Team(_team));

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
            _marker = new SoccerControlMarker(materials, _root);
            SetControlled(false, false, false);
        }

        /// <summary>
        /// 人が動かしている子はチームの色を明るくした太い輪、そうでなければ細く沈んだ輪。
        /// local はこの端末の人が動かしている子（頭の上の印を出す）、pop は今その子へ切り替わった（輪と印をポンと弾ませる）
        /// </summary>
        public void SetControlled(bool controlled, bool local, bool pop)
        {
            _controlled = controlled;
            var team = SoccerColors.Team(_team);
            _ringRenderer.sharedMaterial = _materials.Get(controlled ? Color.Lerp(team, Color.white, ControlledRingLighten) : Color.Lerp(team, AiRingMuteColor, AiRingMute), RingGloss);
            _ringPop = controlled && pop ? RingPopSeconds : 0f;
            _marker.SetVisible(local, pop);
            ApplyRingScale();
        }

        private void TickRing(float deltaTime)
        {
            _marker.Tick(deltaTime);
            if (!_controlled)
            {
                return;
            }

            _pulseTime += deltaTime;
            _ringPop = Mathf.Max(0f, _ringPop - deltaTime);
            ApplyRingScale();
        }

        private void ApplyRingScale()
        {
            var size = RingSize;
            var thickness = RingThickness;
            if (_controlled)
            {
                var pulse = 1f + Mathf.Sin(_pulseTime * PulseRate) * PulseAmount;
                size = ControlledRingSize * pulse * SoccerPop.Scale(_ringPop, RingPopSeconds, RingPopScale);
                thickness = ControlledRingThickness;
            }

            _ring.localScale = new Vector3(size, thickness * 0.1f, size);
        }

        /// <summary>position は補間した足元の位置</summary>
        public void Sync(SoccerPlayer player, Vector3 position, float deltaTime)
        {
            _root.localPosition = position;
            SetFrozen(player.IsFrozen);
            _motion.Velocity = SoccerSpace.Direction(player.Velocity);
            _motion.Facing = SoccerSpace.Direction(player.Facing);
            var charge = player.IsCharging ? player.Charge01 : 0f;
            _motion.KickCharge = charge;
            _motion.Sliding = player.State == SoccerPlayerState.Slide;
            // 奪われて転ぶ・外して起き上がるところは、本編のダウンの姿（腹ばい）。凍っている間は立ったまま止まる
            _motion.Down = player.IsDown && !player.IsFrozen;
            _charge.Show(_controlled ? charge : 0f, SoccerSpace.Direction(player.Facing), deltaTime);
            TickRing(deltaTime);
        }

        public int Team => _team;

        /// <summary>足元の位置（世界の座標）</summary>
        public Vector3 Position => _root.position;

        private void SetFrozen(bool frozen)
        {
            if (frozen == _frozen)
            {
                return;
            }

            _frozen = frozen;
            _model.SetTint(frozen ? _baseTint.Toward(FrozenColor, FrozenTintAmount) : _baseTint);
        }

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
