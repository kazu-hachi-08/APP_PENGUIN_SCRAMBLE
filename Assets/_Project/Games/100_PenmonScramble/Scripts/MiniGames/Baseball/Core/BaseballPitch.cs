using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 投げた球の進み方。マウンドからホームへ、球種の速さで盤の上をまっすぐ転がってくる。
    /// 曲げる入力（−1〜1）は横の速さを増やし（盤の下の磁石のような手ごたえ）、曲げられる区間を過ぎたら横へは動かずまっすぐ来る
    /// （打つ人が読める区間を残すため。入力の時刻だけで道のりが決まるので、オンラインでもホストとゲストが同じ道のりを描ける）。
    /// 魔球（BaseballPitchMagic）はここで動きを変える: 氷は途中で凍って止まり、水は左右に揺れ、風は曲げが大きく効いて最後に逆へ曲がり、雷は途中で消える
    /// </summary>
    public sealed class BaseballPitch
    {
        private readonly BaseballFieldDef _field;
        private float _bendEndY;
        private float _distance;
        private float _freezeLeft;
        private bool _froze;
        private Vec2 _position;

        public BaseballPitch(BaseballFieldDef field)
        {
            _field = field;
        }

        public BaseballPitchType Type { get; private set; }
        public BaseballPitchSpec Spec { get; private set; }

        /// <summary>魔球（出していなければ None）</summary>
        public BaseballPitchMagic Magic { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>球の位置（水の揺れを含む）。テストで直に置くときは、曲げで進んだ横の位置も同じ所に置く</summary>
        public Vec2 Position
        {
            get => _position;
            internal set
            {
                _position = value;
                SteerX = value.X;
            }
        }

        /// <summary>曲げで進んだ横の位置（水の揺れを除く。CPU が曲げてねらう所と比べるのはこちら）</summary>
        public float SteerX { get; private set; }

        /// <summary>横の速さ（曲げで増える）</summary>
        public float LateralSpeed { get; internal set; }

        /// <summary>ホームへ向かう速さ（球種で決まる。一定。凍っている間は止まる）</summary>
        public float ForwardSpeed { get; private set; }

        public Vec2 Velocity => IsFrozen ? Vec2.Zero : new Vec2(LateralSpeed, -ForwardSpeed);

        /// <summary>投げてからの秒（凍っている間も進む）</summary>
        public float Elapsed { get; private set; }

        /// <summary>道のりの進んだ割合（マウンド 0・ホームの線 1）</summary>
        public float Progress => ProgressAt(_position.Y);

        /// <summary>氷の魔球で凍って止まっている</summary>
        public bool IsFrozen => _freezeLeft > 0f;

        /// <summary>雷の魔球で消えている（見た目だけ。当たりは消えている間も変わらない）</summary>
        public bool IsHidden { get; private set; }

        /// <summary>まだ曲げられるか（曲げられる区間の中）</summary>
        public bool CanBend => IsActive && _position.Y > _bendEndY;

        /// <summary>曲げられる区間の終わりの線（盤の上に薄く見せる）</summary>
        public float BendEndY => _bendEndY;

        public void Start(BaseballPitchType type) => Start(type, BaseballPitchMagic.None);

        public void Start(BaseballPitchType type, BaseballPitchMagic magic)
        {
            Type = type;
            Spec = BaseballBalance.Spec(type);
            Magic = magic;
            _distance = _field.Mound.Y - BaseballFieldDef.PlateY;
            ForwardSpeed = _distance / Spec.PlateSeconds;
            _bendEndY = _field.Mound.Y - _distance * BaseballBalance.BendableFraction;
            Position = _field.Mound;
            LateralSpeed = 0f;
            Elapsed = 0f;
            _freezeLeft = 0f;
            _froze = false;
            IsHidden = false;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
            _freezeLeft = 0f;
            IsHidden = false;
        }

        /// <summary>dt だけ進める。曲げられる区間を出た瞬間に true</summary>
        public bool Step(float bend, float dt) => Step(bend, dt, null);

        /// <summary>dt だけ進める。曲げられる区間を出た瞬間に true（その歩で BendEnd を出す）。魔球の起きたことも events に足す</summary>
        public bool Step(float bend, float dt, List<BaseballEvent> events)
        {
            if (!IsActive)
            {
                return false;
            }

            Elapsed += dt;
            if (IsFrozen)
            {
                StepFrozen(dt, events);
                return false;
            }

            var couldBend = CanBend;
            if (couldBend)
            {
                var scale = Magic.Special.Is(Element.Wind) ? BaseballBalance.WindBendScale : 1f;
                var limit = Spec.MaxBendSpeed * scale;
                LateralSpeed = Math.Clamp(LateralSpeed + Math.Clamp(bend, -1f, 1f) * Spec.BendAccel * scale * dt, -limit, limit);
            }

            SteerX = Math.Clamp(SteerX + LateralSpeed * dt, -BaseballBalance.PitchMaxOffset, BaseballBalance.PitchMaxOffset);
            var y = _position.Y - ForwardSpeed * dt;
            _position = new Vec2(Math.Clamp(SteerX + Wobble(ProgressAt(y)), -BaseballBalance.PitchMaxOffset, BaseballBalance.PitchMaxOffset), y);
            var ended = couldBend && !CanBend;
            if (ended)
            {
                EndBend(events);
            }

            StepMagic(events);
            return ended;
        }

        private void StepFrozen(float dt, List<BaseballEvent> events)
        {
            _freezeLeft -= dt;
            if (_freezeLeft > 0f)
            {
                return;
            }

            _freezeLeft = 0f;
            events?.Add(new BaseballEvent(BaseballEventKind.MagicThaw, _position));
        }

        /// <summary>曲げられる区間の終わり: ふつうはまっすぐ（横へは動かない）。旋風ボールは、それまでと逆へ曲がり出す</summary>
        private void EndBend(List<BaseballEvent> events)
        {
            events?.Add(new BaseballEvent(BaseballEventKind.BendEnd, _position));
            if (!Magic.Special.Is(Element.Wind))
            {
                LateralSpeed = 0f;
                return;
            }

            // 曲げていた向きの逆へ。曲げていなければ、ずれている側から真ん中を横切る向きへ（まっすぐなら外へ）
            var basis = LateralSpeed != 0f ? LateralSpeed : SteerX != 0f ? SteerX : -1f;
            LateralSpeed = -Math.Sign(basis) * BaseballBalance.WindReverseSpeed(Magic.Special.Stars);
            events?.Add(new BaseballEvent(BaseballEventKind.MagicReverse, _position));
        }

        /// <summary>氷は決まった所で凍り、雷は決まった区間だけ消える</summary>
        private void StepMagic(List<BaseballEvent> events)
        {
            var special = Magic.Special;
            if (special.Is(Element.Ice) && !_froze && Progress >= BaseballBalance.FreezeAt)
            {
                _froze = true;
                _freezeLeft = Magic.FreezeSeconds;
                events?.Add(new BaseballEvent(BaseballEventKind.MagicFreeze, _position, Magic.FreezeSeconds));
            }

            if (!special.Is(Element.Thunder))
            {
                return;
            }

            var progress = Progress;
            var hidden = progress >= BaseballBalance.VanishAt && progress < BaseballBalance.AppearAt(special.Stars);
            if (hidden == IsHidden)
            {
                return;
            }

            IsHidden = hidden;
            events?.Add(new BaseballEvent(hidden ? BaseballEventKind.MagicVanish : BaseballEventKind.MagicAppear, _position));
        }

        /// <summary>水の揺れ（マウンドでは 0 から始まり、道のりに沿って波打つ。位相で、どちら側でホームを通るかが投げるたびに変わる）</summary>
        private float ProgressAt(float y) => Math.Max(0f, (_field.Mound.Y - y) / _distance);

        private float Wobble(float progress)
        {
            if (!Magic.Special.Is(Element.Water))
            {
                return 0f;
            }

            var amplitude = BaseballBalance.WobbleAmplitude(Magic.Special.Stars);
            var phase = Magic.WobblePhase;
            var angle = 2f * MathF.PI * BaseballBalance.WobbleCycles * progress + phase;
            return amplitude * (MathF.Sin(angle) - MathF.Sin(phase));
        }
    }
}
