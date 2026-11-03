using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ホール全体が収まるよう、手前の斜め上から見下ろす（試しのホールの仮のカメラ。球を追うカメラはフェーズ 3）
    /// </summary>
    public class GolfOverviewCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _pitch = 58f;

        // 画面の端ぎりぎりにならないよう、少し余白をとる
        [SerializeField] private float _margin = 1.15f;

        public void Frame(IReadOnlyList<Vec2> ground)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in ground)
            {
                min = Vector2.Min(min, new Vector2(point.X, point.Y));
                max = Vector2.Max(max, new Vector2(point.X, point.Y));
            }

            var size = max - min;
            var center = GolfSpace.ToWorld(new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f));
            var rotation = Quaternion.Euler(_pitch, 0f, 0f);
            // 奥行きは斜めに見る分だけ縮んで見える
            var depth = size.y * Mathf.Sin(_pitch * Mathf.Deg2Rad);
            var halfHeight = Mathf.Max(depth, size.x / _camera.aspect) * 0.5f * _margin;
            var distance = halfHeight / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * distance, rotation);
        }
    }
}
