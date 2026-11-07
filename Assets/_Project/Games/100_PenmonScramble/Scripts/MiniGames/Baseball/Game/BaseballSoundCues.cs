using PenmonScramble.Baseball.Core;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>起きたこと（BaseballEvent）と1球の結果ごとに鳴らす効果音（音の作りは BaseballSounds）</summary>
    public static class BaseballSoundCues
    {
        // 浮いた球の着地の音を鳴らす、落ちる速さの下限（小さな跳ねでは鳴らさない）
        private const float LandMinSpeed = 1.5f;
        // フェンスに強く当たったら観客もどよめく（ホームランまであと少し）
        private const float FenceOohSpeed = 6f;

        public static void Play(BaseballEvent e, BaseballBoard board)
        {
            switch (e.Kind)
            {
                case BaseballEventKind.Pitch:
                    BaseballSounds.Play(BaseballSound.Throw);
                    break;
                case BaseballEventKind.Swing:
                    BaseballSounds.Play(BaseballSound.Swing);
                    break;
                case BaseballEventKind.Contact:
                    if (board.LastHit.HasValue && board.LastHit.Value.Bunt)
                    {
                        BaseballSounds.Play(BaseballSound.Bunt);
                    }
                    else
                    {
                        BaseballSounds.PlayBat(e.Value);
                    }

                    break;
                case BaseballEventKind.Catch:
                    BaseballSounds.Play(BaseballSound.Mitt);
                    break;
                case BaseballEventKind.Land:
                    if (e.Value >= LandMinSpeed)
                    {
                        BaseballSounds.Play(BaseballSound.Land);
                    }

                    break;
                case BaseballEventKind.FenceHit:
                    BaseballSounds.PlayFence(e.Value);
                    if (e.Value >= FenceOohSpeed)
                    {
                        BaseballSounds.Play(BaseballSound.Ooh);
                    }

                    break;
                case BaseballEventKind.LipOut:
                    BaseballSounds.Play(BaseballSound.Lip);
                    break;
                case BaseballEventKind.HoleIn:
                    BaseballSounds.Play(BaseballSound.Hole);
                    break;
                case BaseballEventKind.HomeRun:
                    BaseballSounds.Play(BaseballSound.HomeRun);
                    BaseballSounds.Play(BaseballSound.Cheer);
                    break;
                case BaseballEventKind.MagicFreeze:
                    BaseballSounds.Play(BaseballSound.Freeze);
                    break;
                case BaseballEventKind.MagicVanish:
                case BaseballEventKind.MagicAppear:
                case BaseballEventKind.MagicReverse:
                    BaseballSounds.Play(BaseballSound.Vanish);
                    break;
                case BaseballEventKind.Skip:
                    BaseballSounds.Play(BaseballSound.Skip);
                    break;
                case BaseballEventKind.GeyserLaunch:
                    BaseballSounds.Play(BaseballSound.Geyser);
                    break;
            }
        }

        /// <summary>点が入った（観客の歓声。試合の終わりのジングルはフェーズ 7）</summary>
        public static void PlayRuns() => BaseballSounds.Play(BaseballSound.Cheer);

        /// <summary>ゲームセット（勝ったら歓声とファンファーレ、それ以外はどよめき）</summary>
        public static void PlayGameOver(bool won)
        {
            if (won)
            {
                BaseballSounds.Play(BaseballSound.HomeRun);
                BaseballSounds.Play(BaseballSound.Cheer);
                return;
            }

            BaseballSounds.Play(BaseballSound.Ooh);
        }

        /// <summary>1球の結果の音（打席が終わったら、その終わり方の音を優先する）</summary>
        public static void PlayOutcome(BaseballPitchOutcome outcome, BaseballPlateResult plate)
        {
            switch (plate)
            {
                case BaseballPlateResult.Strikeout:
                    BaseballSounds.Play(BaseballSound.Strikeout);
                    return;
                case BaseballPlateResult.Walk:
                    BaseballSounds.Play(BaseballSound.Walk);
                    return;
                case BaseballPlateResult.Out:
                    BaseballSounds.Play(BaseballSound.Out);
                    return;
                case BaseballPlateResult.Hit:
                    // ホームランは越えた瞬間にファンファーレが鳴っている
                    if (outcome.Hit != BaseballHitResult.HomeRun)
                    {
                        BaseballSounds.Play(BaseballSound.Hit);
                    }

                    return;
            }

            switch (outcome.Kind)
            {
                case BaseballPitchOutcomeKind.CalledStrike:
                case BaseballPitchOutcomeKind.SwingingStrike:
                    BaseballSounds.Play(BaseballSound.Strike);
                    break;
                case BaseballPitchOutcomeKind.Ball:
                    BaseballSounds.Play(BaseballSound.Ball);
                    break;
            }
        }
    }
}
