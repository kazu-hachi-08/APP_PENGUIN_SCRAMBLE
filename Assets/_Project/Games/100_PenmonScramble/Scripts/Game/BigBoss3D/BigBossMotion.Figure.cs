using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>大ボスの体全体（Figure）の動き: 向き・浮く・前後に倒れる・被弾でのけぞる・もぐる／飛び出す・撃破で飛んで消える</summary>
    public sealed partial class BigBossMotion
    {
        // 被弾: 大きい体なので、ペンギンより小さくのけぞる。のけぞる軸は大ボスごとに違う（体の端が床へ沈むことがある）ので、少し浮かせる
        private const float HitPitch = 6f;
        private const float HitHop = 0.15f;
        private const float HitPush = 0.08f;
        private const float HitShake = 4f;

        // もぐる: 小さく跳ねてから頭から床へ沈む。頭のてっぺんより少し深くまで沈めて、斜めの体がはみ出さないようにする
        private const float DiveMargin = 0.3f;
        private const float DiveHop = 0.3f;
        private const float DivePitch = 35f;

        // 飛び出す: 床の下から頭を上にして飛び出し、弧を描いて着地してつぶれる
        private const float EmergeAirShare = 0.7f;
        private const float EmergePeak = 1.3f;
        private const float EmergePitch = -50f;
        private const float EmergeNod = 25f;
        private const float EmergeLandSquash = 0.2f;

        // 足元の丸い影: 高く上がるほど小さく
        private const float ShadowFadeHeight = 1.5f;
        private const float ShadowMinScale = 0.4f;

        /// <summary>体全体の動かし方。大ボスごとの動き（Pose*）が足していき、最後に ApplyFigure で置く</summary>
        private struct FigureOffset
        {
            public float Lift;
            public float Forward;

            /// <summary>前へ倒す角度（度）。負で頭を上げる。Pivot を軸に回す</summary>
            public float Pitch;
            public float Roll;
            public Vector3 Pivot;

            /// <summary>つぶす（正）・伸ばす（負）</summary>
            public float Squash;

            public static FigureOffset Rest(float centerHeight) => new FigureOffset { Pivot = Vector3.up * centerHeight };
        }

        private void ApplyFigure(BigBossPose pose, FigureOffset f)
        {
            HitRecoil(pose, ref f);
            f.Squash += EmergeLandSquash * Bell(pose.Emerge, EmergeAirShare - 0.05f, EmergeAirShare + 0.08f, 1f);
            var yaw = Quaternion.Euler(0f, pose.Yaw, 0f);
            var tilt = Quaternion.Euler(f.Pitch, 0f, f.Roll);
            var position = yaw * (new Vector3(0f, f.Lift, f.Forward) + f.Pivot - tilt * f.Pivot);
            var rotation = yaw * tilt;
            var height = f.Lift + Dive(pose, ref position, ref rotation);
            var scale = 1f;
            if (pose.Defeated)
            {
                height += ToyVanish.Apply(pose.Defeat, ref position, ref rotation, ref scale);
            }

            _model.Figure.localPosition = position;
            _model.Figure.localRotation = rotation;
            _model.Figure.localScale = new Vector3(1f + f.Squash * 0.5f, 1f - f.Squash, 1f + f.Squash * 0.5f) * scale;
            ApplyShadow(pose, height, scale);
        }

        private static void HitRecoil(BigBossPose pose, ref FigureOffset f)
        {
            if (pose.Hit >= 1f)
            {
                return;
            }

            var t = pose.Hit;
            var k = Bell(t, 0f, 0.15f, 1f);
            f.Pitch -= HitPitch * k;
            f.Lift += HitHop * k;
            f.Forward -= HitPush * k;
            f.Roll += HitShake * Mathf.Sin(t * 50f) * (1f - t);
        }

        /// <summary>もぐる・飛び出す。床から上の高さを返す（影の大きさに使う）</summary>
        private float Dive(BigBossPose pose, ref Vector3 position, ref Quaternion rotation)
        {
            var depth = _model.TopHeight + DiveMargin;
            if (pose.Submerge > 0f)
            {
                var s = pose.Submerge;
                var hop = DiveHop * Bell(s, 0f, 0.25f, 0.6f);
                position += Vector3.up * (hop - depth * s * s);
                rotation *= Quaternion.Euler(DivePitch * Mathf.SmoothStep(0f, 1f, s), 0f, 0f);
                return hop;
            }

            if (pose.Emerge >= EmergeAirShare)
            {
                return 0f;
            }

            var air = pose.Emerge / EmergeAirShare;
            var height = Mathf.Lerp(-depth, 0f, air) + Mathf.Sin(air * Mathf.PI) * EmergePeak;
            position += Vector3.up * height;
            rotation *= Quaternion.Euler(Mathf.Lerp(EmergePitch, 0f, air) + EmergeNod * Bell(air, 0.5f, 0.8f, 1f), 0f, 0f);
            return Mathf.Max(0f, height);
        }

        private void ApplyShadow(BigBossPose pose, float height, float scale)
        {
            var shadow = _model.BlobShadow.transform;
            var size = Mathf.Lerp(1f, ShadowMinScale, Mathf.Clamp01(height / ShadowFadeHeight)) * scale;
            shadow.localRotation = Quaternion.Euler(0f, pose.Yaw, 0f);
            shadow.localScale = Vector3.Scale(_shadowScale, new Vector3(size, 1f, size));
        }
    }
}
