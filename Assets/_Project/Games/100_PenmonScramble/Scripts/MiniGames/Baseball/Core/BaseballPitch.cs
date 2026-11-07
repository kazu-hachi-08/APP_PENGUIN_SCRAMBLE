using System;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 投げた球の進み方。マウンドからホームへ、球種の速さで盤の上をまっすぐ転がってくる。
    /// 曲げる入力（−1〜1）は横の速さを増やし（盤の下の磁石のような手ごたえ）、曲げられる区間を過ぎたら横へは動かずまっすぐ来る
    /// （打つ人が読める区間を残すため。入力の時刻だけで道のりが決まるので、オンラインでもホストとゲストが同じ道のりを描ける）
    /// </summary>
    public sealed class BaseballPitch
    {
        private readonly BaseballFieldDef _field;
        private float _bendEndY;

        public BaseballPitch(BaseballFieldDef field)
        {
            _field = field;
        }

        public BaseballPitchType Type { get; private set; }
        public BaseballPitchSpec Spec { get; private set; }

        public bool IsActive { get; private set; }

        public Vec2 Position { get; internal set; }

        /// <summary>横の速さ（曲げで増える）</summary>
        public float LateralSpeed { get; internal set; }

        /// <summary>ホームへ向かう速さ（球種で決まる。一定）</summary>
        public float ForwardSpeed { get; private set; }

        public Vec2 Velocity => new Vec2(LateralSpeed, -ForwardSpeed);

        /// <summary>まだ曲げられるか（曲げられる区間の中）</summary>
        public bool CanBend => IsActive && Position.Y > _bendEndY;

        /// <summary>曲げられる区間の終わりの線（盤の上に薄く見せる。フェーズ 2）</summary>
        public float BendEndY => _bendEndY;

        public void Start(BaseballPitchType type)
        {
            Type = type;
            Spec = BaseballBalance.Spec(type);
            var distance = _field.Mound.Y - BaseballFieldDef.PlateY;
            ForwardSpeed = distance / Spec.PlateSeconds;
            _bendEndY = _field.Mound.Y - distance * BaseballBalance.BendableFraction;
            Position = _field.Mound;
            LateralSpeed = 0f;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        /// <summary>dt だけ進める。曲げられる区間を出た瞬間に true（その歩で BendEnd を出す）</summary>
        public bool Step(float bend, float dt)
        {
            if (!IsActive)
            {
                return false;
            }

            var couldBend = CanBend;
            if (couldBend)
            {
                var limit = Spec.MaxBendSpeed;
                LateralSpeed = Math.Clamp(LateralSpeed + Math.Clamp(bend, -1f, 1f) * Spec.BendAccel * dt, -limit, limit);
            }

            var x = Math.Clamp(Position.X + LateralSpeed * dt, -BaseballBalance.PitchMaxOffset, BaseballBalance.PitchMaxOffset);
            Position = new Vec2(x, Position.Y - ForwardSpeed * dt);
            if (couldBend && !CanBend)
            {
                // 最後の区間はまっすぐ（横へは動かない）
                LateralSpeed = 0f;
                return true;
            }

            return false;
        }
    }
}
