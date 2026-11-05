using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 試しのピッチの仮の見た目（板と球と円柱）。ピッチの定義から地面・壁・ゴールを組み、毎フレーム球と子を写す。
    /// 本番の見た目（芝のおもちゃの地面・柵・網・Penguin3D）はフェーズ 2 で作り直す
    /// </summary>
    public class SoccerTestPitchView : MonoBehaviour
    {
        private const float GroundThickness = 0.1f;
        private const float BoardHeight = 0.5f;
        private const float BoardThickness = 0.2f;
        private const float PostRadius = 0.08f;
        private const float NetHeightScale = 0.9f;
        private const float LineWidth = 0.08f;
        private const float LineLift = 0.005f;
        private const float PlayerHeight = 1f;
        private const float NoseSize = 0.22f;
        private const float ChargeBarLength = 1f;
        private const float ChargeBarThickness = 0.12f;
        private const float ChargeBarLift = 1.4f;

        private static readonly Color GrassColor = new Color(0.42f, 0.74f, 0.36f);
        private static readonly Color LineColor = new Color(0.95f, 0.97f, 0.92f);
        private static readonly Color BoardColor = new Color(0.98f, 0.86f, 0.42f);
        private static readonly Color GoalColor = Color.white;
        private static readonly Color NetColor = new Color(0.78f, 0.82f, 0.86f);
        private static readonly Color BallColor = Color.white;
        private static readonly Color NoseColor = new Color(1f, 0.62f, 0.2f);
        private static readonly Color ChargeColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color[] TeamColors = { new Color(0.92f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.95f) };

        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;

        private readonly List<Transform> _players = new List<Transform>();
        private readonly List<Transform> _chargeBars = new List<Transform>();
        private ToonMaterials _materials;
        private Transform _ball;

        private ToonMaterials Materials => _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);

        public void Build(SoccerWorld world)
        {
            var pitch = world.Pitch;
            BuildGround(pitch);
            foreach (var wall in pitch.Walls)
            {
                BuildWall(wall, pitch);
            }

            foreach (var goal in pitch.Goals)
            {
                BuildGoalFrame(goal);
            }

            _ball = Primitive(PrimitiveType.Sphere, "Ball", BallColor);
            _ball.localScale = Vector3.one * (SoccerBalance.BallRadius * 2f);
            foreach (var player in world.Players)
            {
                BuildPlayer(player);
            }
        }

        public void Draw(SoccerWorld world, SoccerFrame previous, SoccerFrame current, float t)
        {
            _ball.localPosition = Vector3.Lerp(previous.Ball, current.Ball, t);
            for (var i = 0; i < _players.Count; i++)
            {
                var player = world.Players[i];
                _players[i].localPosition = Vector3.Lerp(previous.Players[i], current.Players[i], t);
                _players[i].localRotation = Quaternion.LookRotation(new Vector3(player.Facing.X, 0f, player.Facing.Y));
                DrawCharge(_chargeBars[i], player.Charge01);
            }
        }

        /// <summary>ためゲージは頭の上の横棒（長さがため）。本番の足元の弧はフェーズ 2</summary>
        private static void DrawCharge(Transform bar, float charge)
        {
            bar.gameObject.SetActive(charge > 0f);
            bar.localScale = new Vector3(ChargeBarLength * charge, ChargeBarThickness, ChargeBarThickness);
        }

        /// <summary>芝の板と、真ん中の線・ゴールの線（四隅の斜めの切りは壁の板で見せる）</summary>
        private void BuildGround(SoccerPitchDef pitch)
        {
            var ground = Primitive(PrimitiveType.Cube, "Ground", GrassColor);
            ground.localScale = new Vector3(pitch.HalfLength * 2f, GroundThickness, pitch.HalfWidth * 2f);
            ground.localPosition = new Vector3(0f, -GroundThickness * 0.5f, 0f);

            Line("CenterLine", Vector3.zero, new Vector3(LineWidth, pitch.HalfWidth * 2f));
            foreach (var goal in pitch.Goals)
            {
                Line("GoalLine", new Vector3(goal.LineX - goal.Outward * LineWidth * 0.5f, 0f, 0f), new Vector3(LineWidth, goal.HalfWidth * 2f));
            }
        }

        private void Line(string name, Vector3 center, Vector3 size)
        {
            var line = Primitive(PrimitiveType.Cube, name, LineColor);
            line.localScale = new Vector3(size.x, LineLift, size.y);
            line.localPosition = center + Vector3.up * LineLift;
        }

        /// <summary>板と網は線分に沿った細長い箱。ゴールの口は枠（BuildGoalFrame）で見せるので箱にしない</summary>
        private void BuildWall(SoccerWall wall, SoccerPitchDef pitch)
        {
            if (wall.Kind == SoccerWallKind.GoalMouth)
            {
                return;
            }

            var isNet = wall.Kind == SoccerWallKind.Net;
            var height = isNet ? pitch.GoalOf(wall.Team).BarHeight * NetHeightScale : BoardHeight;
            var a = ToUnity(wall.A);
            var b = ToUnity(wall.B);
            var along = b - a;
            var box = Primitive(PrimitiveType.Cube, isNet ? "Net" : "Board", isNet ? NetColor : BoardColor);
            box.localScale = new Vector3(BoardThickness, height, along.magnitude + BoardThickness);
            box.localPosition = (a + b) * 0.5f + Vector3.up * (height * 0.5f);
            box.localRotation = Quaternion.LookRotation(along);
        }

        /// <summary>ゴールの柱2本とバー</summary>
        private void BuildGoalFrame(SoccerGoal goal)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var post = Primitive(PrimitiveType.Cylinder, "Post", GoalColor);
                post.localScale = new Vector3(PostRadius * 2f, goal.BarHeight * 0.5f, PostRadius * 2f);
                post.localPosition = new Vector3(goal.LineX, goal.BarHeight * 0.5f, side * goal.HalfWidth);
            }

            var bar = Primitive(PrimitiveType.Cube, "Bar", GoalColor);
            bar.localScale = new Vector3(PostRadius * 2f, PostRadius * 2f, goal.HalfWidth * 2f);
            bar.localPosition = new Vector3(goal.LineX, goal.BarHeight, 0f);
        }

        /// <summary>チームの色の円柱と、向きを見せる鼻（くちばしの代わり）と、頭の上のためゲージ</summary>
        private void BuildPlayer(SoccerPlayer player)
        {
            var root = new GameObject($"Player{player.Index}").transform;
            root.SetParent(transform, false);
            var body = Primitive(PrimitiveType.Cylinder, "Body", TeamColors[player.Team], root);
            body.localScale = new Vector3(SoccerBalance.PlayerRadius * 2f, PlayerHeight * 0.5f, SoccerBalance.PlayerRadius * 2f);
            body.localPosition = Vector3.up * (PlayerHeight * 0.5f);
            var nose = Primitive(PrimitiveType.Cube, "Nose", NoseColor, root);
            nose.localScale = Vector3.one * NoseSize;
            nose.localPosition = new Vector3(0f, PlayerHeight * 0.75f, SoccerBalance.PlayerRadius);
            var charge = Primitive(PrimitiveType.Cube, "Charge", ChargeColor, root);
            charge.localPosition = Vector3.up * ChargeBarLift;
            charge.gameObject.SetActive(false);
            _players.Add(root);
            _chargeBars.Add(charge);
        }

        /// <summary>形だけ使う（当たりは Core で計算するので Collider は外す）</summary>
        private Transform Primitive(PrimitiveType type, string name, Color color, Transform parent = null)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = Materials.Get(color, 0f);
            obj.transform.SetParent(parent != null ? parent : transform, false);
            return obj.transform;
        }

        private static Vector3 ToUnity(Vec2 p) => new Vector3(p.X, 0f, p.Y);

        private void OnDestroy()
        {
            _materials?.DestroyAll();
        }
    }
}
