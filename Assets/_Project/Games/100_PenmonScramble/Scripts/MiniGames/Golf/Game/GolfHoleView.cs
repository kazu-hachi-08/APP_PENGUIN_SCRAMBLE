using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ホールの仮の見た目（板と球と点線）。ホールの定義表から組み、毎フレーム球の位置を写す。
    /// 本番のおもちゃの見た目はフェーズ 3 で作り直す（Docs/MINIGAME_GOLF_PLAN.md）
    /// </summary>
    public partial class GolfHoleView : MonoBehaviour
    {
        // 点線の点の間（m）と、置いておく点の数（ガイド線の最大の長さ ÷ 間 より多く）
        private const float GuideDotSpacing = 0.35f;
        private const int GuideDotCount = 40;
        private static readonly Vector3 GuideDotScale = new Vector3(0.12f, 0.01f, 0.12f);
        private const float GuideDotHeight = 0.04f;

        // カップの中の球は少し沈めて、入ったことが見て分かるようにする
        private const float InCupSink = 0.12f;

        // 席の色（オンラインで誰の球か見分ける）。本番は白い球に席の色の帯（フェーズ 3）
        private static readonly Color[] SeatColors =
        {
            new Color(1f, 0.38f, 0.36f),
            new Color(0.36f, 0.62f, 1f),
            new Color(0.42f, 0.86f, 0.42f),
            new Color(1f, 0.84f, 0.3f),
        };

        // トゥーンの色の名前（ToonMaterials と同じ。点ごとに色を変えても素材を増やさない）
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color GuideColor = Color.white;
        private static readonly Color GuideBounceColor = new Color(1f, 0.9f, 0.45f);

        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;
        [SerializeField] private GolfOverviewCamera _camera;

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Transform> _balls = new List<Transform>();
        private readonly List<Renderer> _guideDots = new List<Renderer>();
        private ToonMaterials _materials;
        private Transform _course;
        private MaterialPropertyBlock _dotBlock;

        /// <summary>ホールを組み直す（前のホールの物は消す）</summary>
        public void Build(GolfHoleDef hole, int playerCount)
        {
            _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);
            _dotBlock ??= new MaterialPropertyBlock();
            Clear();
            _course = new GameObject("Course").transform;
            _course.SetParent(transform, false);
            BuildCourse(hole);
            BuildBalls(playerCount);
            BuildGuideDots();
            _camera.Frame(hole.Ground);
        }

        /// <summary>球の位置・高さを写す。コースから外れた球は隠す</summary>
        public void Sync(IReadOnlyList<GolfBall> balls)
        {
            for (var i = 0; i < _balls.Count && i < balls.Count; i++)
            {
                var ball = balls[i];
                _balls[i].gameObject.SetActive(ball.Active);
                var height = GolfBalance.BallRadius + ball.Height - (ball.InCup ? InCupSink : 0f);
                _balls[i].localPosition = GolfSpace.ToWorld(ball.Position, height);
            }
        }

        /// <summary>ガイド線の点線を出す。null で隠す</summary>
        public void ShowGuide(GolfGuideLine? line)
        {
            var used = 0;
            if (line.HasValue)
            {
                var guide = line.Value;
                used = PlaceDots(guide.Start, guide.End, 0, GuideColor);
                if (guide.Stop == GolfGuideStop.Wall)
                {
                    used = PlaceDots(guide.End, guide.BounceEnd, used, GuideBounceColor);
                }
            }

            for (var i = used; i < _guideDots.Count; i++)
            {
                _guideDots[i].gameObject.SetActive(false);
            }
        }

        /// <summary>from から to へ点を並べ、使い終わった点の番号を返す</summary>
        private int PlaceDots(Vec2 from, Vec2 to, int first, Color color)
        {
            var length = Vec2.Distance(from, to);
            var index = first;
            _dotBlock.SetColor(BaseColorId, color);
            for (var s = GuideDotSpacing; s <= length && index < _guideDots.Count; s += GuideDotSpacing)
            {
                var dot = _guideDots[index++];
                dot.gameObject.SetActive(true);
                dot.transform.localPosition = GolfSpace.ToWorld(from + (to - from) * (s / length), GuideDotHeight);
                dot.SetPropertyBlock(_dotBlock);
            }

            return index;
        }

        private void BuildBalls(int playerCount)
        {
            for (var i = 0; i < playerCount; i++)
            {
                var ball = CreatePrimitive(PrimitiveType.Sphere, $"Ball{i + 1}", _materials.Get(SeatColors[i % SeatColors.Length], 1f));
                ball.localScale = Vector3.one * (GolfBalance.BallRadius * 2f);
                _balls.Add(ball);
            }
        }

        private void BuildGuideDots()
        {
            var material = _materials.Floor(Texture2D.whiteTexture, Color.white);
            for (var i = 0; i < GuideDotCount; i++)
            {
                var dot = CreatePrimitive(PrimitiveType.Cylinder, "GuideDot", material);
                dot.localScale = GuideDotScale;
                dot.gameObject.SetActive(false);
                _guideDots.Add(dot.GetComponent<Renderer>());
            }
        }

        /// <summary>Unity の基本形を置く。当たり判定は Core で計算するので、Collider は外す</summary>
        private Transform CreatePrimitive(PrimitiveType type, string objectName, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = objectName;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.transform.SetParent(_course, false);
            return obj.transform;
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
            _guideDots.Clear();
        }

        private void OnDestroy()
        {
            Clear();
            _materials?.DestroyAll();
        }
    }
}
