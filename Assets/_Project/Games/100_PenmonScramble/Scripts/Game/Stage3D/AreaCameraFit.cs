using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 斜め上から見下ろすカメラを、床の上の長方形（エリア）が画面にちょうど収まる位置に置く。
    /// 奥の端はペンギンの頭の高さまで入るようにする（奥に立ったペンギンの頭が切れないように）
    /// </summary>
    public static class AreaCameraFit
    {
        private const float MinDistance = 1f;
        private const float MaxDistance = 200f;
        private const int SearchSteps = 40;
        private const int CenteringSteps = 6;
        private const float OrthographicDistance = 50f;

        /// <summary>透視カメラ: 見下ろす角度（pitch）と画角を決めたまま、収まるいちばん近い距離を探す</summary>
        public static void FitPerspective(Camera camera, Vector3 center, Vector2 size, float pitch, float fieldOfView, float headroom, float margin)
        {
            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;
            var (position, rotation) = PerspectivePose(camera.aspect, center, size, pitch, fieldOfView, headroom, margin);
            camera.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>FitPerspective で置く位置と向き。ステージのカメラは、エリアへ動く土台からの相対で置くのでこちらを使う</summary>
        public static (Vector3 position, Quaternion rotation) PerspectivePose(float aspect, Vector3 center, Vector2 size, float pitch, float fieldOfView, float headroom, float margin)
        {
            var rotation = Quaternion.Euler(pitch, 0f, 0f);
            var tanY = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * (1f - margin);
            var tanX = tanY * aspect;
            float near = MinDistance, far = MaxDistance;
            for (var i = 0; i < SearchSteps; i++)
            {
                var mid = (near + far) * 0.5f;
                if (Fits(Centered(center, rotation, mid, size, headroom), rotation, center, size, headroom, tanX, tanY)) far = mid;
                else near = mid;
            }

            return (Centered(center, rotation, far, size, headroom), rotation);
        }

        /// <summary>
        /// 透視だと手前が大きく写るので、中心を向けただけではエリアが画面の上へ寄る。
        /// 上下の端が画面の中心から同じ角度になるまで、カメラを画面の上下方向へずらす
        /// </summary>
        private static Vector3 Centered(Vector3 center, Quaternion rotation, float distance, Vector2 size, float headroom)
        {
            var position = Position(center, rotation, distance);
            var inverse = Quaternion.Inverse(rotation);
            for (var step = 0; step < CenteringSteps; step++)
            {
                float min = float.MaxValue, max = float.MinValue;
                for (var corner = 0; corner < 8; corner++)
                {
                    var local = inverse * (Corner(center, size, headroom, corner) - position);
                    var angle = Mathf.Atan2(local.y, local.z);
                    min = Mathf.Min(min, angle);
                    max = Mathf.Max(max, angle);
                }

                position += rotation * Vector3.up * (Mathf.Tan((min + max) * 0.5f) * distance);
            }

            return position;
        }

        private static Vector3 Corner(Vector3 center, Vector2 size, float headroom, int corner) => center + new Vector3(
            (corner & 1) == 0 ? -size.x * 0.5f : size.x * 0.5f,
            (corner & 4) == 0 ? 0f : headroom,
            (corner & 2) == 0 ? -size.y * 0.5f : size.y * 0.5f);

        /// <summary>平行投影カメラ: 横幅と、奥行き（斜めに見た分だけ縮む）の両方が収まる大きさにする</summary>
        public static void FitOrthographic(Camera camera, Vector3 center, Vector2 size, float pitch, float headroom, float margin)
        {
            camera.orthographic = true;
            var rotation = Quaternion.Euler(pitch, 0f, 0f);
            var radians = pitch * Mathf.Deg2Rad;
            var halfHeight = (size.y * Mathf.Sin(radians) + headroom * Mathf.Cos(radians)) * 0.5f;
            var halfWidth = size.x * 0.5f / camera.aspect;
            camera.orthographicSize = Mathf.Max(halfHeight, halfWidth) / (1f - margin);
            camera.transform.SetPositionAndRotation(Position(center, rotation, OrthographicDistance), rotation);
        }

        private static Vector3 Position(Vector3 center, Quaternion rotation, float distance) => center - rotation * Vector3.forward * distance;

        private static bool Fits(Vector3 position, Quaternion rotation, Vector3 center, Vector2 size, float headroom, float tanX, float tanY)
        {
            var inverse = Quaternion.Inverse(rotation);
            for (var corner = 0; corner < 8; corner++)
            {
                var local = inverse * (Corner(center, size, headroom, corner) - position);
                if (local.z <= 0f || Mathf.Abs(local.x) > local.z * tanX || Mathf.Abs(local.y) > local.z * tanY)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
