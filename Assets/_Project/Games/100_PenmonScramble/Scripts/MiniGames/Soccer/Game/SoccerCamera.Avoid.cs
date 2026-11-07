using System;
using PenmonScramble.Game;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// タッチの端末で、右下のボタン（パス・シュート・必殺・交代）の下にゴールが入らないようにする。
    /// ピッチ全体を画面いっぱいに収めると右のゴールがちょうどボタンの下に来るので、見る所を少しずらし、少しだけ引いて、
    /// 両方のゴール（網とその前のキーパーの所）がボタンにかからない中で一番大きく映る置き方を探す。
    /// ボールへ寄る・ゴールへ寄るときも、ゴールがボタンにかかる手前で寄るのを止める
    /// </summary>
    public partial class SoccerCamera
    {
        [Header("スマホのボタンを避ける")]
        [Tooltip("避けるボタン（丸）。タッチの端末でだけ避ける")]
        [SerializeField] private RectTransform[] _avoidButtons;
        [Tooltip("ボタンを描いている Canvas（画面の大きさに合わせた拡大率を読む）")]
        [SerializeField] private Canvas _canvas;
        [Tooltip("ゴールラインからピッチの内へ、どこまでを隠さずに見せるか（キーパーとシュートの行き先）")]
        [SerializeField] private float _goalFront = 2.5f;
        [Tooltip("ボタンの縁からの余白（画面の高さに対する割合）")]
        [SerializeField] private float _buttonPadding = 0.015f;
        [Tooltip("見る所をピッチの真ん中からずらしてよい幅（ピッチの半分の長さ・幅に対する割合）")]
        [SerializeField] private float _maxShift = 0.35f;

        // ずらし方は前後左右それぞれこの段数ずつ試す（(2×6+1)² 通り。試合の始めに一度だけなので重くない）
        private const int ShiftSteps = 6;
        // ずらした所で収まらないときは、この割合ずつ引いて試す（最大で約 2 倍まで）
        private const float BackOffStep = 1.03f;
        private const int BackOffSteps = 25;
        private const int ClampIterations = 8;
        private const int BoxCorners = 8;

        private bool _avoiding;
        // ボタンの丸（画面のピクセル。x, y が中心、z が余白込みの半径）
        private Vector3[] _buttonCircles = Array.Empty<Vector3>();
        private Vector3[][] _goalBoxes = Array.Empty<Vector3[]>();
        private float[] _goalSides = Array.Empty<float>();
        private readonly Vector2[] _projected = new Vector2[BoxCorners];
        private readonly Vector2[] _hull = new Vector2[BoxCorners * 2];

        private static readonly Comparison<Vector2> ByXThenY = (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);

        /// <summary>ボタンを避けて収まる、一番近い置き方を探す。避けなくてよい・避けられないときは false</summary>
        private bool FitAvoidingButtons(Vector3[] corners)
        {
            _avoiding = false;
            if (!TouchControlsVisibility.IsTouchDevice || _avoidButtons == null || _avoidButtons.Length == 0 || _canvas == null)
            {
                return false;
            }

            BuildButtonCircles();
            BuildGoalBoxes(_pitchDef);
            var bestDistance = float.MaxValue;
            var bestCenter = Vector3.zero;
            for (var ix = -ShiftSteps; ix <= ShiftSteps; ix++)
            {
                for (var iz = -ShiftSteps; iz <= ShiftSteps; iz++)
                {
                    var center = new Vector3(ShiftAt(ix, _halfSize.x), 0f, ShiftAt(iz, _halfSize.y));
                    var distance = ClearDistance(center, corners, bestDistance);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCenter = center;
                    }
                }
            }

            if (bestDistance == float.MaxValue)
            {
                return false;
            }

            _center = bestCenter;
            _distance = bestDistance;
            _avoiding = true;
            return true;
        }

        private float ShiftAt(int step, float half) => (float)step / ShiftSteps * _maxShift * half;

        /// <summary>center を見て、ピッチが収まり両方のゴールがボタンにかからない一番近い距離（limit より遠くしか無ければ MaxValue）</summary>
        private float ClearDistance(Vector3 center, Vector3[] corners, float limit)
        {
            var distance = FitDistance(center, corners);
            for (var i = 0; i < BackOffSteps && distance < limit; i++, distance *= BackOffStep)
            {
                Place(center, distance);
                if (AllVisible(corners) && GoalsClear(null))
                {
                    return distance;
                }
            }

            return float.MaxValue;
        }

        /// <summary>
        /// ボタンはまだ隠れている（試合が始まる前は HUD ごと止まっている）ことがあるので、描かれた位置ではなく、
        /// 留めた角・位置・大きさとセーフエリア・Canvas の拡大率から画面の位置を出す（ボタンの親はセーフエリアいっぱい）
        /// </summary>
        private void BuildButtonCircles()
        {
            var safe = Screen.safeArea;
            var scale = _canvas.scaleFactor;
            var padding = _buttonPadding * Screen.height;
            _buttonCircles = new Vector3[_avoidButtons.Length];
            for (var i = 0; i < _avoidButtons.Length; i++)
            {
                var rect = _avoidButtons[i];
                var center = safe.min + Vector2.Scale(rect.anchorMin, safe.size) + rect.anchoredPosition * scale;
                var radius = Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y) * 0.5f * scale + padding;
                _buttonCircles[i] = new Vector3(center.x, center.y, radius);
            }
        }

        /// <summary>ゴールごとに、網の奥からピッチの内の _goalFront まで・バーの高さまでを囲む箱</summary>
        private void BuildGoalBoxes(SoccerPitchDef pitch)
        {
            _goalBoxes = new Vector3[pitch.Goals.Count][];
            _goalSides = new float[pitch.Goals.Count];
            for (var g = 0; g < pitch.Goals.Count; g++)
            {
                var goal = pitch.Goals[g];
                var back = goal.LineX + goal.Outward * goal.Depth;
                var front = goal.LineX - goal.Outward * _goalFront;
                var box = new Vector3[BoxCorners];
                var index = 0;
                foreach (var x in new[] { back, front })
                {
                    foreach (var z in new[] { -goal.HalfWidth, goal.HalfWidth })
                    {
                        foreach (var y in new[] { 0f, goal.BarHeight })
                        {
                            box[index++] = SoccerSpace.ToWorld(new Vec2(x, z), y);
                        }
                    }
                }

                _goalBoxes[g] = box;
                _goalSides[g] = Mathf.Sign(goal.LineX);
            }
        }

        /// <summary>
        /// 寄った置き方でゴールがボタンにかかるなら、収めた置き方（かからない）との間で、かからない一番寄った所まで戻す。
        /// ゴールへ寄っている間は、寄った先のゴールだけを見る（もう片方は画面の外へ出てよい）
        /// </summary>
        private void KeepGoalsClear(ref Vector3 look, ref float zoom)
        {
            var focus = _goal;
            Place(look, _distance * (1f - zoom));
            if (GoalsClear(focus))
            {
                return;
            }

            var low = 0f;
            var high = 1f;
            for (var i = 0; i < ClampIterations; i++)
            {
                var middle = (low + high) * 0.5f;
                Place(Vector3.Lerp(_center, look, middle), _distance * (1f - zoom * middle));
                if (GoalsClear(focus))
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            look = Vector3.Lerp(_center, look, low);
            zoom *= low;
        }

        /// <summary>今のカメラで、ゴールの箱がどのボタンにもかからないか（focus があれば、その側のゴールだけ見る）</summary>
        private bool GoalsClear(Vector3? focus)
        {
            for (var g = 0; g < _goalBoxes.Length; g++)
            {
                if (focus.HasValue && Mathf.Sign(focus.Value.x) != _goalSides[g])
                {
                    continue;
                }

                if (!BoxClear(_goalBoxes[g]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>箱の角を画面へ写し、それを包む多角形が、どのボタンの丸とも重ならないか</summary>
        private bool BoxClear(Vector3[] box)
        {
            for (var i = 0; i < box.Length; i++)
            {
                var p = _camera.WorldToScreenPoint(box[i]);
                if (p.z <= 0f)
                {
                    return false;
                }

                _projected[i] = p;
            }

            var count = ConvexHull(_projected, _hull);
            foreach (var circle in _buttonCircles)
            {
                if (CircleHitsPolygon(_hull, count, new Vector2(circle.x, circle.y), circle.z))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>点を包む凸多角形（反時計回り。Andrew の方法）。返すのは頂点の数</summary>
        private static int ConvexHull(Vector2[] points, Vector2[] hull)
        {
            Array.Sort(points, ByXThenY);
            var k = 0;
            for (var i = 0; i < points.Length; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0f)
                {
                    k--;
                }

                hull[k++] = points[i];
            }

            for (int i = points.Length - 2, lower = k + 1; i >= 0; i--)
            {
                while (k >= lower && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0f)
                {
                    k--;
                }

                hull[k++] = points[i];
            }

            return k - 1;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        /// <summary>丸の中心が多角形の中にあるか、どれかの辺が丸にかかれば重なっている</summary>
        private static bool CircleHitsPolygon(Vector2[] polygon, int count, Vector2 center, float radius)
        {
            var inside = count >= 3;
            var radiusSq = radius * radius;
            for (var i = 0; i < count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % count];
                if (Cross(a, b, center) < 0f)
                {
                    inside = false;
                }

                if (DistanceSqToSegment(center, a, b) < radiusSq)
                {
                    return true;
                }
            }

            return inside;
        }

        private static float DistanceSqToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            var t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
            return (a + ab * t - p).sqrMagnitude;
        }
    }
}
