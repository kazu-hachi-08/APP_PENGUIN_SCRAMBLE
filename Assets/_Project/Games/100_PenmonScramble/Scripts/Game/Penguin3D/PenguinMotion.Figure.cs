using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>体全体（Figure）の動き: 向き・すべる・倒れる・必殺技で跳ぶ・吹っ飛びで転がる・撃破で飛んで消える。物理演算は使わず、決めた回転と高さで動かす</summary>
    public sealed partial class PenguinMotion
    {
        // 前へ倒れる角度。すべるときは少し頭を上げて進む向きが分かるようにし、ダウンは床にべったり
        private const float SlidePitch = 75f;
        private const float DownPitch = 90f;

        // 必殺技の流れ（割合）: ここまでかがむ → ここで跳ぶ → ここで着地
        private const float SpecialCrouchEnd = 0.3f;
        private const float SpecialJumpStart = 0.35f;
        private const float SpecialJumpEnd = 0.8f;
        // かがむ・跳ぶ・着地の切り替わりを重ねる幅（割合）。つなぎ目で止まって見えないように
        private const float SpecialBlend = 0.05f;
        private const float SpecialJumpHeight = 0.7f;
        private const float SpecialSpin = 360f;
        private const float SpecialCrouch = 0.3f;
        private const float SpecialStretch = 0.1f;
        private const float SpecialLand = 0.2f;
        private const float SpecialFlipperWindup = 60f;
        private const float SpecialFlipperRaise = 150f;
        private const float SpecialHeadLift = 20f;

        // 吹っ飛び: 飛ばしてきた側を向いたまま、後ろへ転がりながら弧を描いて跳ぶ。転がらないほど弱いときは、のけぞってすべる
        private const float KnockHop = 0.45f;
        private const float KnockWeakHop = 0.15f;
        private const float KnockTilt = 35f;

        // 足元の丸い影: 高く上がるほど小さく、倒れると体の長さに合わせて前後に伸ばす
        private const float ShadowFadeHeight = 1.5f;
        private const float ShadowMinScale = 0.4f;
        private const float ShadowLieStretch = 0.6f;

        private readonly struct FigurePose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly float Scale;

            /// <summary>床から浮いている高さ（影の大きさに使う）</summary>
            public readonly float Height;

            public FigurePose(Vector3 position, Quaternion rotation, float scale, float height)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Height = height;
            }
        }

        private FigurePose FigureOf(PenguinPose pose)
        {
            var center = Vector3.up * _rest.CenterHeight;
            var rotation = Quaternion.Euler(0f, pose.Yaw, 0f);
            var position = Vector3.zero;
            Lie(pose, center, ref position, ref rotation);
            var height = SpecialJump(pose, ref position, ref rotation);
            height += Tumble(pose, center, ref position, ref rotation);
            var scale = 1f;
            height += Vanish(pose, ref position, ref rotation, ref scale);
            return new FigurePose(position, rotation, scale, height);
        }

        /// <summary>
        /// 胴の中心を軸に前へ倒し、お腹が床に着く高さまで下ろす。斜めのときは卵形の端が中心から遠いので、
        /// 下ろす量は倒しきる近くで増やす（1 - cos）
        /// </summary>
        private void Lie(PenguinPose pose, Vector3 center, ref Vector3 position, ref Quaternion rotation)
        {
            var pitch = Mathf.Max(pose.Slide * SlidePitch, pose.Down * DownPitch);
            if (pitch <= 0f)
            {
                return;
            }

            var tilt = Quaternion.Euler(pitch, 0f, 0f);
            var drop = (_rest.CenterHeight - _rest.LieHeight) * (1f - Mathf.Cos(pitch * Mathf.Deg2Rad));
            position += rotation * (center - tilt * center) + Vector3.down * drop;
            rotation *= tilt;
        }

        private static float SpecialJump(PenguinPose pose, ref Vector3 position, ref Quaternion rotation)
        {
            if (pose.Special >= 1f)
            {
                return 0f;
            }

            var air = Mathf.InverseLerp(SpecialJumpStart, SpecialJumpEnd, pose.Special);
            var height = Mathf.Sin(air * Mathf.PI) * SpecialJumpHeight;
            position += Vector3.up * height;
            rotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 1f, air) * SpecialSpin, 0f) * rotation;
            return height;
        }

        /// <summary>飛ばされた向きへ転がる。Core と同じく速さが一定の割合で落ちるので、回る角度も最初が速く、だんだん遅くなる</summary>
        private float Tumble(PenguinPose pose, Vector3 center, ref Vector3 position, ref Quaternion rotation)
        {
            if (pose.Knock >= 1f)
            {
                return 0f;
            }

            var t = pose.Knock;
            var travelled = 1f - (1f - t) * (1f - t);
            var arc = Mathf.Sin(t * Mathf.PI);
            var rolls = pose.KnockTurns > 0f;
            var angle = rolls ? pose.KnockTurns * 360f * travelled : KnockTilt * arc;
            // 逆さになるほど持ち上げて、頭が床にめり込まないようにする（頭の上は足元より中心から遠いため）
            var upsideDown = Mathf.Max(0f, -Mathf.Cos(angle * Mathf.Deg2Rad));
            var height = arc * (rolls ? KnockHop : KnockWeakHop) + upsideDown * (_rest.TopHeight - 2f * _rest.CenterHeight);
            var axis = Vector3.Cross(Vector3.up, pose.KnockDirection);
            if (axis.sqrMagnitude <= 0f)
            {
                axis = Vector3.right;
            }

            var roll = Quaternion.AngleAxis(angle, axis.normalized);
            position = roll * (position - center) + center + Vector3.up * height;
            rotation = roll * rotation;
            return height;
        }

        private static float Vanish(PenguinPose pose, ref Vector3 position, ref Quaternion rotation, ref float scale)
        {
            return pose.Defeated ? ToyVanish.Apply(pose.Defeat, ref position, ref rotation, ref scale) : 0f;
        }

        private void ApplyShadow(PenguinPose pose, FigurePose figure)
        {
            var shadow = _model.BlobShadow.transform;
            var lie = Mathf.Max(pose.Slide, pose.Down);
            var size = Mathf.Lerp(1f, ShadowMinScale, Mathf.Clamp01(figure.Height / ShadowFadeHeight)) * figure.Scale;
            shadow.localRotation = Quaternion.Euler(0f, pose.Yaw, 0f);
            shadow.localScale = Vector3.Scale(_rest.ShadowScale, new Vector3(size, 1f, size * (1f + lie * ShadowLieStretch)));
        }
    }
}
