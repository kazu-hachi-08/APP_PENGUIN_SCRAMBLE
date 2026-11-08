using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球の見た目（ブリキの縁と木の台のおもちゃの盤）。盤の定義から台・土と芝・線・塁・フェンス・穴を組み、毎フレーム球・バット・子を写す。
    /// 台・土と芝・線・塁・マウンド・まわりの景色は BaseballView.Board.cs、フェンスは .Fence.cs、穴と札は .Holes.cs、
    /// 投げた球の通り道（曲げられる区間）とストライクのゾーンの枠は .Lane.cs、球場の仕掛け（水流・風・間欠泉）は .Gimmicks.cs。球・バット・子はそれぞれ別の部品
    /// </summary>
    public partial class BaseballView : MonoBehaviour
    {
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Texture> _textures = new List<Texture>();
        private ToonMaterials _materials;
        private Transform _root;
        private BaseballFieldDef _field;
        private BaseballBallView _ball;
        private BaseballBatView _bat;

        public ToonMaterials Materials => _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);

        /// <summary>打つ子・投げる子・守備の子（Build のあとに使える）</summary>
        public BaseballFigures Figures { get; private set; }

        /// <summary>球の見えている位置（ホームランの球はフェンスの向こうへ飛んでいく所まで。カメラが追う）</summary>
        public Vector3 BallPosition => _ball.Position;

        /// <summary>球が見えているか（投げる前は投げる子が持っているので見せない）</summary>
        public bool BallVisible => _ball.Visible;

        /// <summary>盤を組み直す（前の物は消す）。world は景色（まわりの地面の絵・フェンスの色）を決めるワールドの番号</summary>
        public void Build(BaseballFieldDef field, int world)
        {
            Clear();
            _field = field;
            _root = new GameObject("Field").transform;
            _root.SetParent(transform, false);
            var theme = StageTheme.ForWorld(world);
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.Background;
            }

            BuildBoard(field, world, theme);
            BuildFence(field, theme);
            BuildHoles(field);
            BuildGimmicks(field);
            BuildLane(field);
            _ball = new BaseballBallView(Materials, _root);
            _bat = new BaseballBatView(field, Materials, _root);
            Figures = new BaseballFigures(field, Materials, _root);
        }

        /// <summary>補間した1歩の写しを描く。clock は描く時点の盤の時計（仕掛け・動く穴の様子）</summary>
        public void Draw(BaseballBoard board, BaseballFrame frame, float deltaTime, float clock)
        {
            FollowHoles(clock, deltaTime);
            TickGimmicks(clock, deltaTime);
            Figures.FollowHoles(HoleShownAt);
            _ball.Sync(board, frame, deltaTime);
            _bat.Sync(board, frame.BatDegrees);
            Figures.Tick(board, frame, _ball.Position, deltaTime);
            TickLane(board, deltaTime);
            TickHoles(deltaTime);
        }

        /// <summary>盤の上で起きたことへの見た目の反応（ゾーンの枠が光る・穴が光る・ホームランの球が飛んでいく・子が捕る）</summary>
        public void OnEvent(BaseballEvent e, BaseballBoard board)
        {
            switch (e.Kind)
            {
                case BaseballEventKind.CrossPlate:
                    FlashZone(e.Flag);
                    break;
                case BaseballEventKind.HoleIn:
                    FlashHole(e.Index);
                    break;
                case BaseballEventKind.HomeRun:
                    _ball.FlyAway(board.Ball);
                    break;
                case BaseballEventKind.Catch:
                    _ball.ToMitt();
                    break;
            }

            Figures.OnEvent(e, board);
        }

        /// <summary>次の球の構えに戻す（球は投げる子の手の中へ、子は構えへ）</summary>
        public void ResetPitch()
        {
            _ball.ResetPitch();
            Figures.ResetForPitch();
            ResetLane();
        }

        /// <summary>Unity の基本の形の代わりに、ToyMeshes の形を置く（当たりは Core が持つので Collider は付けない）</summary>
        private Transform AddPart(string objectName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            return MiniGameMeshes.AddPart(objectName, mesh, parent, position, rotation, scale, material);
        }

        /// <summary>このビューが作ったメッシュを置く（消すときに一緒に消す）</summary>
        private MeshRenderer AddOwnedMesh(string objectName, Mesh mesh, params Material[] materials)
        {
            return MiniGameMeshes.AddOwnedMesh(_meshes, objectName, mesh, _root, materials);
        }

        private void Clear()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            foreach (var mesh in _meshes)
            {
                Destroy(mesh);
            }

            foreach (var texture in _textures)
            {
                Destroy(texture);
            }

            _meshes.Clear();
            _textures.Clear();
            _holes.Clear();
            ClearGimmicks();
            _ball = null;
            _bat = null;
            Figures = null;
        }

        private void OnDestroy()
        {
            Clear();
            _materials?.DestroyAll();
        }
    }
}
