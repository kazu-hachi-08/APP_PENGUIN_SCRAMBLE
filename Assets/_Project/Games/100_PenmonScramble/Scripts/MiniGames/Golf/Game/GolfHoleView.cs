using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ホールの見た目（おもちゃのミニゴルフの台）。ホールの定義表から地面・壁・カップを組み、毎フレーム球を写す。
    /// 地面は GolfHoleView.Ground.cs、壁は .Walls.cs、カップと旗とティーは .Cup.cs、
    /// 動く物・押し流す帯・間欠泉・ワープの泡は .Gimmicks.cs、特殊ショット（氷の道・水切り・雷の加速）の見た目は .Specials.cs。球・ガイド線・打つ子は別の部品
    /// </summary>
    public partial class GolfHoleView : MonoBehaviour
    {
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<GolfBallView> _balls = new List<GolfBallView>();
        private ToonMaterials _materials;
        private Transform _course;
        private GolfGuideView _guide;
        private GolfShooter _shooter;
        private GolfScenery _scenery = new GolfScenery();
        private GolfHoleDef _hole;

        public GolfSurface Surface { get; private set; }

        /// <summary>打つ子（ペンモンとクラブ）。ホールを組み直しても同じものを使う</summary>
        public GolfShooter Shooter => _shooter;

        public ToonMaterials Materials => _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);

        /// <summary>ホールを組み直す（前のホールの物は消す）。world は景色（地面の絵と色）を決めるワールドの番号</summary>
        public void Build(GolfHoleDef hole, int playerCount, int world)
        {
            Clear();
            _course = new GameObject("Course").transform;
            _course.SetParent(transform, false);
            Surface = new GolfSurface(hole);
            var theme = StageTheme.ForWorld(world);
            _scenery = GolfScenery.For(world);
            _hole = hole;
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.Background;
            }

            BuildGround(hole, world, theme);
            BuildWalls(hole, theme);
            BuildCup(hole, theme);
            BuildTee(hole);
            BuildGimmicks(hole);
            for (var i = 0; i < playerCount; i++)
            {
                _balls.Add(new GolfBallView(Materials, _course, GolfSeats.ColorOf(i)));
            }

            _guide ??= new GolfGuideView(Materials, transform);
            if (_shooter == null)
            {
                _shooter = new GameObject("Shooter").AddComponent<GolfShooter>();
                _shooter.transform.SetParent(transform, false);
                _shooter.Init(Materials);
            }

            _shooter.Hide();
        }

        /// <summary>球の位置・高さを写す（坂の上では見た目の地面に乗せる）</summary>
        public void SyncBalls(IReadOnlyList<GolfBall> balls, float deltaTime)
        {
            for (var i = 0; i < _balls.Count && i < balls.Count; i++)
            {
                _balls[i].Sync(balls[i], Surface, deltaTime);
            }
        }

        /// <summary>ガイド線の点線を出す。null で隠す。special は特殊ショットを ON にしている（点の色を替える）</summary>
        public void ShowGuide(GolfGuideLine? line, float deltaTime, bool special = false) => _guide.Show(line, Surface, deltaTime, special);

        /// <summary>球の中心の3Dの位置（見た目の地面の上）</summary>
        public Vector3 BallPoint(GolfBall ball) => GolfSpace.ToWorld(ball.Position, Surface.HeightAt(ball.Position) + GolfBalance.BallRadius + ball.Height);

        /// <summary>地面の上の位置（見た目の地面の高さ）</summary>
        public Vector3 GroundPoint(Vec2 point) => GolfSpace.ToWorld(point, Surface.HeightAt(point));

        private void Update()
        {
            var dt = Time.deltaTime;
            WaveFlag(dt);
            TickWallPops(dt);
            TickSplashes(dt);
            TickSpecials(dt);
        }

        /// <summary>Unity の基本の形の代わりに、ToyMeshes の形を置く（当たり判定は Core が持つので Collider は付けない）</summary>
        private Transform AddPart(string objectName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            return MiniGameMeshes.AddPart(objectName, mesh, parent, position, rotation, scale, material);
        }

        /// <summary>このビューが作ったメッシュを置く（消すときに一緒に消す）</summary>
        private MeshRenderer AddOwnedMesh(string objectName, Mesh mesh, params Material[] materials)
        {
            return MiniGameMeshes.AddOwnedMesh(_meshes, objectName, mesh, _course, materials);
        }

        private void Clear()
        {
            if (_course != null)
            {
                Destroy(_course.gameObject);
            }

            foreach (var mesh in _meshes)
            {
                Destroy(mesh);
            }

            _meshes.Clear();
            _balls.Clear();
            _wallPops.Clear();
            _splashes.Clear();
            ClearSpecials();
            ClearGimmicks();
            _flag = null;
        }

        private void OnDestroy()
        {
            Clear();
            _materials?.DestroyAll();
        }
    }
}
