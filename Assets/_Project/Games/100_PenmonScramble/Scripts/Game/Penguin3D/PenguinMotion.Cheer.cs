using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// クリアの結果で喜ぶ動き: かがんで跳ね、ヒレを上げて振り、着地でつぶれるのを繰り返す。3回に1回は跳んだまま1回転する。
    /// 何体も並ぶので、始まりを少しずつずらして同時に跳ばないようにする
    /// </summary>
    public sealed partial class PenguinMotion
    {
        private const float CheerBlendRate = 6f;
        private const float CheerCycleSeconds = 0.75f;
        // 喜び始めをずらす幅（秒）。並んだ子がばらばらに跳ねて、にぎやかに見える
        private const float CheerStartJitter = 0.3f;
        // 1回の跳ねの流れ（割合）: ここまでかがむ → ここで跳ぶ → ここで着地
        private const float CheerCrouchEnd = 0.18f;
        private const float CheerJumpStart = 0.2f;
        private const float CheerJumpEnd = 0.72f;
        private const float CheerHopHeight = 0.45f;
        private const int CheerSpinEvery = 3;
        private const float CheerCrouch = 0.22f;
        private const float CheerStretch = 0.1f;
        private const float CheerLand = 0.18f;
        // ヒレは高く上げたまま、速く振る
        private const float CheerFlipperRaise = 120f;
        private const float CheerFlipperWave = 25f;
        private const float CheerWaveRate = 4f;
        private const float CheerHeadLift = 15f;

        private bool _cheering;

        /// <summary>結果の画面で喜ぶ。止めると立った姿勢へなめらかに戻る</summary>
        public bool Cheering
        {
            get => _cheering;
            set
            {
                if (value && !_cheering)
                {
                    _pose.CheerTime = -Random.Range(0f, CheerStartJitter);
                }

                _cheering = value;
            }
        }

        private void TickCheer(float dt)
        {
            _pose.Cheer = Mathf.MoveTowards(_pose.Cheer, Cheering && !Down ? 1f : 0f, CheerBlendRate * dt);
            if (Cheering)
            {
                _pose.CheerTime += dt;
            }
        }

        /// <summary>何回目の跳ねの、どこまで進んだか（割合）。待っている間は 0 回目の始まり</summary>
        private static (int hop, float t) CheerPhase(PenguinPose pose)
        {
            var time = Mathf.Max(0f, pose.CheerTime) / CheerCycleSeconds;
            var hop = Mathf.FloorToInt(time);
            return (hop, time - hop);
        }

        /// <summary>体全体を跳ねさせる。CheerSpinEvery 回に1回は空中で1回転する</summary>
        private static float CheerHop(PenguinPose pose, ref Vector3 position, ref Quaternion rotation)
        {
            if (pose.Cheer <= 0f)
            {
                return 0f;
            }

            var (hop, t) = CheerPhase(pose);
            var air = Mathf.InverseLerp(CheerJumpStart, CheerJumpEnd, t);
            var height = Mathf.Sin(air * Mathf.PI) * CheerHopHeight * pose.Cheer;
            position += Vector3.up * height;
            if (hop % CheerSpinEvery == CheerSpinEvery - 1)
            {
                rotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 1f, air) * 360f, 0f) * rotation;
            }

            return height;
        }

        /// <summary>かがむ・伸びる・つぶれるのは胴、ヒレは上げたまま振り、跳んでいる間は顔を上げる</summary>
        private static void CheerParts(PenguinPose pose, ref PartPose p)
        {
            var w = pose.Cheer;
            if (w <= 0f)
            {
                return;
            }

            var (_, t) = CheerPhase(pose);
            var crouch = Bell(t, 0f, CheerCrouchEnd, CheerJumpStart + 0.05f);
            var air = Bell(t, CheerJumpStart, (CheerJumpStart + CheerJumpEnd) * 0.5f, CheerJumpEnd);
            var land = Bell(t, CheerJumpEnd - 0.05f, CheerJumpEnd + 0.08f, 1f);
            p.Squash((crouch * CheerCrouch - air * CheerStretch + land * CheerLand) * w);
            var wave = Mathf.Sin(pose.CheerTime * CheerWaveRate * Mathf.PI * 2f) * CheerFlipperWave;
            p.FlipperLeft.Euler.z -= (CheerFlipperRaise + wave) * w;
            p.FlipperRight.Euler.z += (CheerFlipperRaise - wave) * w;
            p.Head.Euler += new Vector3(-air * CheerHeadLift * w, 0f, 0f);
        }
    }
}
