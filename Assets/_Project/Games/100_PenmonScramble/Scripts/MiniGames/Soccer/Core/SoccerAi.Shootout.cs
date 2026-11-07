using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// PK戦の AI（人と同じ SoccerInput を出す）。蹴る: 狙う所（左右・真ん中・高さ）と強さを乱数で決め、少し構えてからためて、狙った強さで離す
    /// （強いほど狙った強さのぶれが小さく、赤い所に入れにくい）。守る: 飛ぶ向きを乱数で決め、蹴る子がため始めたのを見て、
    /// 強さに応じた割合で狙いを読む
    /// </summary>
    public sealed partial class SoccerAi
    {
        private const float PenaltyMinPower = 0.35f;

        // 考えている1本（SoccerShootout.TotalKicks）。替わったら決め直す
        private int _penaltyKick = -1;
        private int _penaltySide;
        private bool _penaltyHigh;
        private float _penaltyPower;
        private float _penaltyWait;
        private int _diveSide;
        private bool _diveRead;

        /// <summary>人の子でない蹴る子・守る側の子の入力を書く（ほかの子は何も押さない）</summary>
        public void ThinkShootout(SoccerShootout shootout, SoccerInput[] inputs, Func<int, bool> isHuman)
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                if (!isHuman(i))
                {
                    inputs[i] = SoccerInput.None;
                }
            }

            if (shootout == null || shootout.Phase == SoccerShootoutPhase.Finished)
            {
                return;
            }

            if (shootout.TotalKicks != _penaltyKick)
            {
                PlanPenalty(shootout);
            }

            var kicker = shootout.Kicker;
            if (kicker >= 0 && kicker < inputs.Length && !isHuman(kicker))
            {
                inputs[kicker] = KickInput(shootout);
            }

            var keeper = shootout.KeeperInputIndex(shootout.KeeperTeam);
            if (keeper >= 0 && keeper < inputs.Length && !isHuman(keeper))
            {
                inputs[keeper] = DiveInput(shootout);
            }
        }

        private void PlanPenalty(SoccerShootout shootout)
        {
            _penaltyKick = shootout.TotalKicks;
            var kicking = _levels[shootout.KickingTeam];
            _penaltySide = Roll(SoccerBalance.AiPenaltyCenterChance) ? 0 : (Roll(0.5f) ? -1 : 1);
            _penaltyHigh = Roll(SoccerBalance.AiPenaltyHighChance);
            _penaltyPower = Math.Clamp(SoccerBalance.AiPenaltyPower + Triangle() * kicking.PenaltyPowerSpread, PenaltyMinPower, 1f);
            _penaltyWait = SoccerBalance.AiPenaltyMinWait + (float)_random.NextDouble() * (SoccerBalance.AiPenaltyMaxWait - SoccerBalance.AiPenaltyMinWait);
            _diveSide = _random.Next(3) - 1;
            _diveRead = false;
        }

        /// <summary>狙う向きへスティックを倒したまま、構えの間は離しておき、ため始めたら狙った強さまで押し続ける</summary>
        private SoccerInput KickInput(SoccerShootout shootout)
        {
            var move = new Vec2(_penaltySide, _penaltyHigh ? 1f : 0f);
            var aiming = shootout.Phase == SoccerShootoutPhase.Aim && shootout.PhaseSeconds >= _penaltyWait;
            var hold = aiming && shootout.Charge01 < _penaltyPower;
            return new SoccerInput(move, hold);
        }

        /// <summary>蹴る子がため始めたら、一度だけ狙いを読むか決める</summary>
        private SoccerInput DiveInput(SoccerShootout shootout)
        {
            if (shootout.IsCharging && !_diveRead)
            {
                _diveRead = true;
                if (Roll(_levels[shootout.KeeperTeam].PenaltyReadChance))
                {
                    _diveSide = shootout.AimSide;
                }
            }

            return new SoccerInput(new Vec2(_diveSide, 0f), false);
        }
    }
}
