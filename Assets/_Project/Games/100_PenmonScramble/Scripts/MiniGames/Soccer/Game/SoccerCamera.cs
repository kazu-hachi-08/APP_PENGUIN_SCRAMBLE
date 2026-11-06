using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーのカメラ: 横長のピッチを斜め上からほぼ全部見せ、ボールの方へ少しだけ寄る（どこにいても全体が分かり、ボールの側が少し大きく見える）。
    /// ゴールの瞬間はそのゴールへぐっと寄り、キックオフで全体へ戻る。
    /// PK戦では、蹴る子の後ろの少し上からゴールを見る（スティックの左右・上が、そのまま狙う左右・高さに見えるように）
    /// </summary>
    public class SoccerCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("全体")]
        [SerializeField] private float _pitch = 56f;
        [Tooltip("画面の端からの余白（画面の幅・高さに対する割合）")]
        [SerializeField] private float _margin = 0.02f;
        [Tooltip("上の余白（点と時間の HUD の下にピッチの奥の柵が隠れないように）")]
        [SerializeField] private float _topMargin = 0.13f;
        [Tooltip("全体に入れる高さ（ゴールのバーの上まで）")]
        [SerializeField] private float _fitHeight = 1.8f;

        [Header("ボールへ寄る")]
        [Tooltip("見る所を、ピッチの真ん中からボールへどれだけ寄せるか（0〜1）")]
        [SerializeField] private float _followRatio = 0.22f;
        [Tooltip("ボールがピッチの端にあるとき、どれだけ近づくか（距離の割合）")]
        [SerializeField] private float _followZoom = 0.08f;
        [SerializeField] private float _smooth = 0.45f;

        [Header("ゴール")]
        [SerializeField] private float _goalFollowRatio = 0.6f;
        [SerializeField] private float _goalZoom = 0.32f;
        [SerializeField] private float _goalSmooth = 0.3f;

        [Header("PK戦")]
        [Tooltip("ボールを置く所から、どれだけ後ろ・上に置くか")]
        [SerializeField] private float _penaltyBack = 6.5f;
        [SerializeField] private float _penaltyHeight = 3.2f;
        [Tooltip("見る所の高さ（ゴールの口の真ん中より少し下）")]
        [SerializeField] private float _penaltyLookHeight = 0.6f;

        private Vector3 _center;
        private float _distance;
        private Vector2 _halfSize = Vector2.one;
        private Vector3 _look;
        private Vector3 _lookVelocity;
        private float _zoom;
        private float _zoomVelocity;
        private Vector3? _goal;
        private bool _penalty;

        // 収まる距離を探す範囲と、探す回数（半分ずつ狭める）
        private const float FitNear = 1f;
        private const float FitFar = 200f;
        private const int FitIterations = 30;
        private const float BoardAllowance = 0.4f;

        /// <summary>ピッチ全体（柵とゴールの網まで）が収まる位置へすぐ置く</summary>
        public void ShowPitch(SoccerPitchDef pitch)
        {
            _penalty = false;
            _halfSize = new Vector2(pitch.HalfLength, pitch.HalfWidth);
            _center = Vector3.zero;
            _distance = FitDistance(Corners(pitch));
            _goal = null;
            _look = _center;
            _lookVelocity = Vector3.zero;
            _zoom = 0f;
            _zoomVelocity = 0f;
            Apply();
        }

        /// <summary>PK戦の1本: ボールの後ろの少し上から、狙うゴールを見る（ShowPitch で全体へ戻る）</summary>
        public void ShowPenalty(Vector3 spot, Vector3 goal)
        {
            _penalty = true;
            var toGoal = goal - spot;
            toGoal.y = 0f;
            var forward = toGoal.normalized;
            var position = spot - forward * _penaltyBack + Vector3.up * _penaltyHeight;
            var look = goal + Vector3.up * _penaltyLookHeight;
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position));
        }

        /// <summary>ゴールに寄る（null で戻る）</summary>
        public void FocusGoal(Vector3? goal) => _goal = goal;

        /// <summary>毎フレーム、ボールの位置を渡す</summary>
        public void Follow(Vector3 ball, float deltaTime)
        {
            if (_penalty)
            {
                return;
            }

            ball.y = 0f;
            Vector3 look;
            float zoom;
            float smooth;
            if (_goal.HasValue)
            {
                look = Vector3.Lerp(_center, _goal.Value, _goalFollowRatio);
                zoom = _goalZoom;
                smooth = _goalSmooth;
            }
            else
            {
                look = Vector3.Lerp(_center, ball, _followRatio);
                var edge = Mathf.Clamp01(Mathf.Max(Mathf.Abs(ball.x) / _halfSize.x, Mathf.Abs(ball.z) / _halfSize.y));
                zoom = _followZoom * edge;
                smooth = _smooth;
            }

            _look = Vector3.SmoothDamp(_look, look, ref _lookVelocity, smooth, Mathf.Infinity, deltaTime);
            _zoom = Mathf.SmoothDamp(_zoom, zoom, ref _zoomVelocity, smooth, Mathf.Infinity, deltaTime);
            Apply();
        }

        /// <summary>柵とゴールの網の外側を囲む箱の角（地面と、バーの上の高さ）</summary>
        private Vector3[] Corners(SoccerPitchDef pitch)
        {
            var halfX = pitch.HalfLength + pitch.GoalOf(SoccerRules.Red).Depth + BoardAllowance;
            var halfZ = pitch.HalfWidth + BoardAllowance;
            var corners = new Vector3[8];
            var index = 0;
            foreach (var y in new[] { 0f, _fitHeight })
            {
                foreach (var x in new[] { -halfX, halfX })
                {
                    foreach (var z in new[] { -halfZ, halfZ })
                    {
                        corners[index++] = new Vector3(x, y, z);
                    }
                }
            }

            return corners;
        }

        /// <summary>
        /// 斜めに見下ろすと手前ほど大きく映るので、式で近似せず、実際に置いて全部の角が画面に入る一番近い距離を半分ずつ探す
        /// </summary>
        private float FitDistance(Vector3[] corners)
        {
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            var near = FitNear;
            var far = FitFar;
            for (var i = 0; i < FitIterations; i++)
            {
                var middle = (near + far) * 0.5f;
                _camera.transform.SetPositionAndRotation(_center - rotation * Vector3.forward * middle, rotation);
                if (AllVisible(corners))
                {
                    far = middle;
                }
                else
                {
                    near = middle;
                }
            }

            return far;
        }

        private bool AllVisible(Vector3[] points)
        {
            foreach (var point in points)
            {
                var v = _camera.WorldToViewportPoint(point);
                if (v.z <= 0f || v.x < _margin || v.x > 1f - _margin || v.y < _margin || v.y > 1f - _topMargin)
                {
                    return false;
                }
            }

            return true;
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            var position = _look - rotation * Vector3.forward * (_distance * (1f - _zoom));
            _camera.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
