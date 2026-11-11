using System;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>CPU の曲げ方の型</summary>
    public enum BaseballBendPlan
    {
        /// <summary>曲げない</summary>
        Straight,
        /// <summary>外へ逃げる（右打ちの打者から遠い、一塁側）</summary>
        Away,
        /// <summary>内へ食い込む（打者の側、三塁側）</summary>
        Inside,
        /// <summary>曲げられる区間の後半だけ曲げる（打つ側が読みにくい）</summary>
        Late,
    }

    /// <summary>CPU の1球の組み立て（球種・曲げ方の型・ホームの線で通したい所）</summary>
    public readonly struct BaseballPitchPlan
    {
        public BaseballPitchPlan(BaseballPitchType type, BaseballBendPlan bend, float targetX)
        {
            Type = type;
            Bend = bend;
            TargetX = targetX;
        }

        public BaseballPitchType Type { get; }
        public BaseballBendPlan Bend { get; }

        /// <summary>ホームの線を通したい横の位置（ホームの真ん中が 0。右が正）</summary>
        public float TargetX { get; }
    }

    /// <summary>CPU のうまさ（★ごとの値は BaseballBalance.AiSkill。試合の確認で決めた）</summary>
    public readonly struct BaseballAiSkill
    {
        public BaseballAiSkill(float timingErrorSeconds, float takeBallChance, float aimError, float magicReadChance = 0f)
        {
            TimingErrorSeconds = timingErrorSeconds;
            TakeBallChance = takeBallChance;
            AimError = aimError;
            MagicReadChance = magicReadChance;
        }

        /// <summary>振る時刻のぶれ（秒。ばらつきの標準偏差）。強さの一番の差</summary>
        public float TimingErrorSeconds { get; }

        /// <summary>ゾーンの外の球を見送る確率</summary>
        public float TakeBallChance { get; }

        /// <summary>投げるときにゾーンの端を狙うぶれ（マス。標準偏差）</summary>
        public float AimError { get; }

        /// <summary>フリーズボールが凍ったのを見て、振るのを待ち直せる確率（待てなければ取ったタイミングのまま振ってしまう）</summary>
        public float MagicReadChance { get; }
    }

    /// <summary>
    /// CPU。
    /// 投げる: 球種と曲げ方の型を乱数で選び、曲げられる区間が終わるまでにねらいの所へ寄るよう曲げる入力を出す。
    /// カウントでねらいを変える（3ボールでは外す球を投げない・追い込んだら外す球を増やす）。投げる子はイニングごとに替える。
    /// 打つ: 曲げられる区間が終わって球の通る所が決まったら、見送るか振るかを決め、見えた通る所へ芯が来るよう左右へ動き、ジャストの時刻にぶれを足した時刻に振る
    /// （前へ来る速さは球種で一定なので、時刻は読める。ぶれは曲げられた球・速い球ほど大きい）。
    /// 追い込まれたらゾーンのきわの球もカットしにいく。走者がいて点差が小さいときは、ときどきバントで送る。
    /// 魔球・必殺打: ピンチ・チャンスで出しやすく、最後のイニングはさらに出しやすい（残したまま終わらないように）。
    /// 打つ側は魔球に読みを崩される（消えている間は読めない・凍った球には取ったタイミングのまま振ってしまうことがある）
    /// </summary>
    public sealed class BaseballAi
    {
        private readonly Random _random;
        private readonly BaseballAiSkill _skill;
        private bool _decided;
        private bool _willSwing;
        private float _swingLead;
        private float _swingAt;
        private bool _freezeSeen;

        /// <summary>動いていきたい軸のずれ（見えた通る所に芯が来る所。決めるまでは構えの所）</summary>
        private float _moveTarget;

        /// <summary>この球で芯を合わせ損ねる横のずれ（1球ごとに決める。歩ごとに引き直すと揺れて、平均して合ってしまうため）</summary>
        private float _moveError;
        private bool _buntPlate;
        private bool _bunting;
        private int _strikes;

        public BaseballAi(int seed, BaseballAiSkill skill)
        {
            _random = new Random(seed);
            _skill = skill;
        }

        // ---- 投げる ----

        /// <summary>
        /// 投げる子を選ぶ（打席の始めに呼ぶ）。イニングごとに順に替えて、いろいろな子が投げるようにする。
        /// ピンチ（得点圏に走者）で、その子の魔球が残っていなければ、魔球の残っている子に替える
        /// </summary>
        public static int ChoosePitcher(BaseballGame game)
        {
            var team = game.FieldingTeam;
            var size = game.LineupSize(team);
            var turn = (game.Inning - 1) % size;
            if (!IsScoringChance(game) || !game.IsMagicUsed(team, turn))
            {
                return turn;
            }

            for (var i = 1; i < size; i++)
            {
                var slot = (turn + i) % size;
                if (!game.IsMagicUsed(team, slot))
                {
                    return slot;
                }
            }

            return turn;
        }

        /// <summary>得点圏（二塁・三塁）に走者がいる（投げる側のピンチ・打つ側のチャンス）</summary>
        public static bool IsScoringChance(BaseballGame game) => game.Bases.IsOccupied(2) || game.Bases.IsOccupied(3);

        /// <summary>この球で魔球を出すか（投げる前に。今の投げる子の魔球が残っているときだけ）</summary>
        public bool WantsMagic(BaseballGame game)
        {
            if (!game.CanUseMagic)
            {
                return false;
            }

            return Roll(IsScoringChance(game) ? BaseballBalance.AiMagicPinchChance : BaseballBalance.AiMagicChance, game);
        }

        /// <summary>この球で必殺打を出すか（投げる前に。今の打つ子の必殺打が残っているときだけ。当たるまでは残る）</summary>
        public bool WantsSpecialHit(BaseballGame game)
        {
            if (!game.CanUseSpecialHit)
            {
                return false;
            }

            return Roll(IsScoringChance(game) ? BaseballBalance.AiSpecialChanceChance : BaseballBalance.AiSpecialChance, game);
        }

        /// <summary>魔球の中身（★の幅の中の乱数）を決める</summary>
        public BaseballPitchMagic RollMagic(BaseballSpecial special) => BaseballPitchMagic.Roll(special, _random);

        /// <summary>最後のイニングは出しやすくする</summary>
        private bool Roll(float chance, BaseballGame game)
        {
            var scaled = game.Inning >= BaseballRules.Innings ? chance * BaseballBalance.AiLastInningScale : chance;
            return _random.NextDouble() < Math.Min(scaled, 1f);
        }

        /// <summary>カウントを見ない1球（試しの打席・テスト）</summary>
        public BaseballPitchPlan PlanPitch(BaseballFieldDef field) => PlanPitch(field, 0, 0);

        public BaseballPitchPlan PlanPitch(BaseballFieldDef field, BaseballCount count) => PlanPitch(field, count.Balls, count.Strikes);

        /// <summary>
        /// 魔球の1球。魔球はわざと外さず、ゾーンの端をねらう。旋風ボールだけは、ゾーンの外へ逃がしてから逆へ曲げてゾーンへ戻す
        /// （打つ側には外へ逃げる球に見え、見送るとストライクになる）
        /// </summary>
        public BaseballPitchPlan PlanMagicPitch(BaseballFieldDef field, BaseballSpecial magic)
        {
            var type = (BaseballPitchType)_random.Next(3);
            var side = _random.Next(2) == 0 ? -1f : 1f;
            if (magic.Is(Element.Wind))
            {
                var outward = side < 0f ? BaseballBendPlan.Inside : BaseballBendPlan.Away;
                return new BaseballPitchPlan(type, outward, side * (field.ZoneHalfWidth + BaseballBalance.AiWindOvershoot));
            }

            var bend = PickBend();
            if (bend == BaseballBendPlan.Straight)
            {
                return new BaseballPitchPlan(type, bend, 0f);
            }

            return new BaseballPitchPlan(type, bend, side * field.ZoneHalfWidth * BaseballBalance.AiEdgeAim + Gaussian() * _skill.AimError);
        }

        private BaseballPitchPlan PlanPitch(BaseballFieldDef field, int balls, int strikes)
        {
            var type = (BaseballPitchType)_random.Next(3);
            var bend = PickBend();
            if (bend == BaseballBendPlan.Straight)
            {
                return new BaseballPitchPlan(type, bend, 0f);
            }

            var side = bend == BaseballBendPlan.Inside ? -1f
                : bend == BaseballBendPlan.Away ? 1f
                : _random.Next(2) == 0 ? -1f : 1f;
            // 3ボールはわざと外さない（狙いのぶれで外れることはある）。追い込んだら（2ストライク）外す球を増やして振らせにいく
            var mustStrike = balls >= BaseballRules.BallsForWalk - 1;
            var wasteChance = mustStrike ? 0f
                : strikes >= BaseballRules.StrikesForOut - 1 ? BaseballBalance.AiWasteChanceAhead
                : BaseballBalance.AiWasteChance;
            if (_random.NextDouble() < wasteChance)
            {
                // ストレートは曲がりが小さくゾーンの外まで届かないので、外す球はカーブかチェンジアップ
                var waste = _random.Next(2) == 0 ? BaseballPitchType.Curve : BaseballPitchType.Changeup;
                return new BaseballPitchPlan(waste, bend, side * (field.ZoneHalfWidth + BaseballBalance.AiWasteMargin));
            }

            return new BaseballPitchPlan(type, bend, side * field.ZoneHalfWidth * BaseballBalance.AiEdgeAim + Gaussian() * _skill.AimError);
        }

        private BaseballBendPlan PickBend()
        {
            var roll = _random.NextDouble();
            if (roll < BaseballBalance.AiStraightChance)
            {
                return BaseballBendPlan.Straight;
            }

            if (roll < BaseballBalance.AiStraightChance + BaseballBalance.AiLateChance)
            {
                return BaseballBendPlan.Late;
            }

            return _random.Next(2) == 0 ? BaseballBendPlan.Away : BaseballBendPlan.Inside;
        }

        /// <summary>
        /// この歩の曲げる入力（−1〜1）。曲げられる区間の終わりの横の位置がねらいに来るよう、残りの時間で要る横の加速を求める
        /// （区間の後はまっすぐ来るので、区間の終わりの位置がそのままホームの線の位置になる）
        /// </summary>
        public static float Bend(BaseballPitch pitch, BaseballPitchPlan plan, BaseballFieldDef field)
        {
            if (!pitch.CanBend || plan.Bend == BaseballBendPlan.Straight)
            {
                return 0f;
            }

            var remain = (pitch.Position.Y - pitch.BendEndY) / pitch.ForwardSpeed;
            var total = (field.Mound.Y - pitch.BendEndY) / pitch.ForwardSpeed;
            if (plan.Bend == BaseballBendPlan.Late && remain > total * BaseballBalance.AiLateFraction)
            {
                return 0f;
            }

            // 水の揺れは曲げで動かせないので、曲げで進んだ位置（揺れを除く）でねらう
            var needed = plan.TargetX - pitch.SteerX - pitch.LateralSpeed * remain;
            var accel = 2f * needed / Math.Max(remain * remain, 0.0001f);
            return Math.Clamp(accel / pitch.Spec.BendAccel, -1f, 1f);
        }

        // ---- 打つ ----

        /// <summary>次の球のために読みを消す（試しの打席・テスト。バントはしない）</summary>
        public void ResetBatter()
        {
            _decided = false;
            _willSwing = false;
            _bunting = false;
            _freezeSeen = false;
            _moveTarget = 0f;
            _moveError = Gaussian() * BaseballBalance.AiMoveError;
            _strikes = 0;
        }

        /// <summary>
        /// 試合の次の球のために読みを消し、この球でバントするかを決める（投げる前に呼ぶ）。
        /// バントは打席の始めに決めて、その打席の間は続ける（2ストライクからはバントのファウルで三振になるのでやめる）
        /// </summary>
        public void BeginPitch(BaseballGame game)
        {
            ResetBatter();
            _strikes = game.Count.Strikes;
            if (game.IsPlateStart)
            {
                _buntPlate = WantsBunt(game) && _random.NextDouble() < BaseballBalance.AiBuntChance;
            }

            _bunting = _buntPlate && _strikes < BaseballRules.StrikesForOut - 1;
        }

        /// <summary>
        /// 送りバントの場面か: 一塁か二塁に走者がいて（三塁だけならスクイズになるのでしない）、2アウト未満、
        /// 点差が小さい（勝っていても負けていても AiBuntMaxLead 点まで）
        /// </summary>
        public static bool WantsBunt(BaseballGame game)
        {
            var bases = game.Bases;
            var lead = game.Score(game.BattingTeam) - game.Score(game.FieldingTeam);
            return (bases.IsOccupied(1) || bases.IsOccupied(2))
                && game.Count.Outs < BaseballRules.OutsPerInning - 1
                && Math.Abs(lead) <= BaseballBalance.AiBuntMaxLead;
        }

        /// <summary>この歩の打つ側の入力（左右・振る・バントの構え。曲げるのは投げる側なので 0）</summary>
        public BaseballInput BatInput(BaseballBoard board)
        {
            if (_bunting)
            {
                var bunt = HoldsBunt(board);
                return new BaseballInput(0f, false, bunt, 0f, MoveInput(board));
            }

            var swing = TrySwing(board, out var swingTime);
            return new BaseballInput(0f, swing, false, swingTime, MoveInput(board));
        }

        /// <summary>
        /// 打つ子の左右の入力。動いていく先（_moveTarget）へ、この歩で着くだけの強さで動く（行き過ぎて揺れないように）
        /// </summary>
        private float MoveInput(BaseballBoard board)
        {
            if (board.Phase != BaseballBoardPhase.Pitching || !board.Bat.CanMove)
            {
                return 0f;
            }

            var pitch = board.Pitch;
            if (!_decided && pitch.IsActive && !pitch.IsHidden && !pitch.IsFrozen)
            {
                // 通る所が決まるまでは、今の横の速さのまま来たときの所へ寄っていく（後半だけ曲げる球にはつられる）
                AimMove(board, pitch.Position.X + pitch.LateralSpeed * (pitch.Position.Y - BaseballFieldDef.PlateY) / pitch.ForwardSpeed);
            }

            var gap = _moveTarget - board.Bat.Offset;
            return Math.Clamp(gap / (BaseballBalance.BatterMoveSpeed * BaseballRules.StepSeconds), -1f, 1f);
        }

        /// <summary>見えた通る所（ホームの線の横の位置）に芯が来るよう、動いていく先を決める</summary>
        private void AimMove(BaseballBoard board, float seenX)
        {
            var needed = seenX + _moveError - (board.Field.BatPivot.X + BaseballBalance.SweetSpot);
            _moveTarget = Math.Clamp(needed, -BaseballBalance.BatterMoveRange, BaseballBalance.BatterMoveRange);
        }

        /// <summary>バントの構えを続けるか。通る所が決まってゾーンの外なら、見送る賢さの割合で構えを解く（ボールにする）</summary>
        private bool HoldsBunt(BaseballBoard board)
        {
            var pitch = board.Pitch;
            if (board.Phase != BaseballBoardPhase.Pitching || !pitch.IsActive)
            {
                return false;
            }

            if (!_decided && !pitch.CanBend && !pitch.IsHidden)
            {
                _decided = true;
                var inZone = Math.Abs(pitch.Position.X) <= board.Field.ZoneHalfWidth;
                _willSwing = inZone || _random.NextDouble() >= _skill.TakeBallChance;
                AimMove(board, pitch.Position.X + Gaussian() * _skill.TimingErrorSeconds * BaseballBalance.AiReadError);
            }

            return !_decided || _willSwing;
        }

        /// <summary>
        /// この歩で振るか。振るなら swingTime に、この歩の始めから振り始めるまでの秒を入れる
        /// （人の入力と同じく歩の中の時刻まで渡すので、歩の刻みでジャストを外さない）
        /// </summary>
        public bool TrySwing(BaseballBoard board, out float swingTime)
        {
            swingTime = 0f;
            var pitch = board.Pitch;
            if (board.Phase != BaseballBoardPhase.Pitching || !pitch.IsActive || board.Bat.HasAttempted)
            {
                return false;
            }

            SeeFreeze(pitch);
            if (!_decided)
            {
                // 曲げられる区間の間は通る所がまだ決まらないので待つ。消えている・凍っている間も読めないので待つ
                if (pitch.CanBend || pitch.IsHidden || pitch.IsFrozen)
                {
                    return false;
                }

                Decide(board);
            }

            if (!_willSwing)
            {
                return false;
            }

            // 振る時刻は決めたときに取っておく（凍って止まった球には、待ち直せなければ取ったタイミングのまま振ってしまう）
            swingTime = _swingAt - pitch.Elapsed;
            if (swingTime >= BaseballRules.StepSeconds)
            {
                return false;
            }

            // 読みが遅れて振る時刻を過ぎていたら、すぐ振る（前の歩までさかのぼって振ったことにはしない）
            swingTime = Math.Max(swingTime, 0f);
            return true;
        }

        /// <summary>
        /// 振るか見送るかと、振る時刻のぶれを決める。ゾーンの中は振る（追い込まれるまではときどき見送る）。
        /// 外は見送る賢さの割合で見送るが、追い込まれていたらゾーンのきわ（AiProtectMargin まで）の球はカットしにいく
        /// </summary>
        private void Decide(BaseballBoard board)
        {
            _decided = true;
            var pitch = board.Pitch;
            var bent = Math.Abs(pitch.Position.X - board.Field.Mound.X);
            var difficulty = BaseballBalance.AiTimingDifficulty(pitch.Type) * (1f + BaseballBalance.AiBendDifficulty * bent)
                * BaseballBalance.AiMagicDifficulty(pitch.Magic.Special);
            // 旋風ボールは区間の後も横へ曲がり続ける。うまい CPU はその分を読んでホームの線の位置で決め、読めなければ今の位置で決めてしまう
            var drift = pitch.LateralSpeed != 0f && _random.NextDouble() < _skill.MagicReadChance
                ? pitch.LateralSpeed * pitch.Position.Y / pitch.ForwardSpeed
                : 0f;
            // 通る所の見え方にもぶれがある（ゾーンの外の球に手を出して空振り・きわの球を見逃して三振、が出る）
            var seenX = pitch.Position.X + drift + Gaussian() * _skill.TimingErrorSeconds * BaseballBalance.AiReadError * difficulty;
            var x = Math.Abs(seenX);
            var zone = board.Field.ZoneHalfWidth;
            var twoStrikes = _strikes >= BaseballRules.StrikesForOut - 1;
            var protect = twoStrikes && x <= zone + BaseballBalance.AiProtectMargin;
            // 追い込まれるまでは、ゾーンの中でもときどき見送る（毎打席の初球打ちにならないように。カウントが進んで三振・四球が出る）
            var swingStrike = x <= zone && (twoStrikes || _random.NextDouble() >= BaseballBalance.AiTakeStrikeChance);
            _willSwing = swingStrike || protect || (x > zone && _random.NextDouble() >= _skill.TakeBallChance);
            AimMove(board, seenX);
            _swingLead = BaseballBat.SecondsToSquare + Gaussian() * _skill.TimingErrorSeconds * difficulty;
            var toBat = (pitch.Position.Y - BaseballBalance.BallRadius - board.BatPivot.Y) / pitch.ForwardSpeed;
            _swingAt = pitch.Elapsed + toBat - _swingLead;
        }

        /// <summary>フリーズボールが凍ったのを見たら、うまさの割合で振るのを待ち直す（動き出してから読み直す）</summary>
        private void SeeFreeze(BaseballPitch pitch)
        {
            if (!pitch.IsFrozen || _freezeSeen)
            {
                return;
            }

            _freezeSeen = true;
            if (_random.NextDouble() < _skill.MagicReadChance)
            {
                _decided = false;
            }
        }

        // ---- 追い風ヒット ----

        /// <summary>
        /// 追い風ヒットの曲げる入力（CPU）。打球の進む向きにいちばん近い向きのヒットの穴（今より奥）へ向けて曲げる
        /// </summary>
        public static float Steer(BaseballBoard board)
        {
            var ball = board.Ball;
            if (board.Phase != BaseballBoardPhase.BallInPlay || !ball.CanSteer)
            {
                return 0f;
            }

            var heading = BaseballFieldDef.DegreesOf(ball.Velocity);
            var best = float.MaxValue;
            foreach (var hole in board.Field.Holes)
            {
                var center = hole.CenterAt(ball.Clock);
                if (hole.IsOut || center.Length <= ball.Position.Length)
                {
                    continue;
                }

                var diff = BaseballFieldDef.DegreesOf(center - ball.Position) - heading;
                if (Math.Abs(diff) < Math.Abs(best))
                {
                    best = diff;
                }
            }

            // 方角は右（一塁側）が正で、曲げる入力も右が正
            return best == float.MaxValue ? 0f : Math.Clamp(best / BaseballBalance.AiSteerFullDegrees, -1f, 1f);
        }

        /// <summary>標準正規分布の乱数（Box–Muller）</summary>
        private float Gaussian()
        {
            var u1 = 1.0 - _random.NextDouble();
            var u2 = _random.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }
    }
}
