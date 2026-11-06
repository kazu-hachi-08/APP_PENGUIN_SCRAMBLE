using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>起きたこと（SoccerEvent）ごとに鳴らす効果音（音の作りは SoccerSounds）</summary>
    public static class SoccerSoundCues
    {
        // 浮いた球の着地の音を鳴らす、落ちる速さの下限（小さな跳ねでは鳴らさない）
        private const float LandMinSpeed = 1.5f;

        public static void Play(SoccerEvent e)
        {
            switch (e.Kind)
            {
                case SoccerEventKind.Kick:
                    SoccerSounds.PlayKick(e.Strength);
                    break;
                case SoccerEventKind.WallHit:
                    // 板は Team が −1、ゴールの口（バー）はそのゴールのチーム
                    if (e.Team < 0)
                    {
                        SoccerSounds.PlayHit(SoccerSound.Wall, e.Strength);
                    }
                    else
                    {
                        SoccerSounds.Play(SoccerSound.Post);
                    }

                    break;
                case SoccerEventKind.NetHit:
                    SoccerSounds.PlayHit(SoccerSound.Net, e.Strength);
                    break;
                case SoccerEventKind.Land:
                    if (e.Strength >= LandMinSpeed)
                    {
                        SoccerSounds.Play(SoccerSound.Land);
                    }

                    break;
                case SoccerEventKind.Possess:
                    SoccerSounds.Play(SoccerSound.Trap);
                    break;
                case SoccerEventKind.Goal:
                    SoccerSounds.Play(SoccerSound.Goal);
                    break;
                case SoccerEventKind.Kickoff:
                    SoccerSounds.Play(SoccerSound.Whistle);
                    break;
                case SoccerEventKind.KeeperDive:
                    SoccerSounds.Play(SoccerSound.Dive);
                    break;
                case SoccerEventKind.KeeperCatch:
                    SoccerSounds.Play(SoccerSound.Catch);
                    break;
                case SoccerEventKind.KeeperParry:
                    SoccerSounds.Play(SoccerSound.Parry);
                    break;
                case SoccerEventKind.KeeperThrow:
                    SoccerSounds.Play(SoccerSound.Throw);
                    break;
                case SoccerEventKind.Pass:
                    // パスは弱く蹴る音（速いパスほど少し強く）
                    SoccerSounds.PlayKick(e.Strength / SoccerBalance.MaxShotSpeed);
                    break;
                case SoccerEventKind.Slide:
                    SoccerSounds.Play(SoccerSound.Slide);
                    break;
                case SoccerEventKind.Tackle:
                    SoccerSounds.Play(SoccerSound.Tackle);
                    break;
                case SoccerEventKind.Dash:
                    SoccerSounds.Play(SoccerSound.Dash);
                    break;
                case SoccerEventKind.SpecialKick:
                    SoccerSounds.Play(SoccerSound.SpecialShot);
                    break;
                case SoccerEventKind.Freeze:
                    SoccerSounds.Play(SoccerSound.Freeze);
                    break;
                case SoccerEventKind.Countdown:
                    SoccerSounds.Play(SoccerSound.Countdown);
                    break;
                case SoccerEventKind.TimeUp:
                    SoccerSounds.Play(SoccerSound.TimeUp);
                    break;
                case SoccerEventKind.ShootoutStart:
                    SoccerSounds.Play(SoccerSound.Whistle);
                    break;
                case SoccerEventKind.PenaltyGoal:
                    SoccerSounds.Play(SoccerSound.Goal);
                    break;
                case SoccerEventKind.PenaltyMiss:
                    SoccerSounds.Play(SoccerSound.Miss);
                    break;
            }
        }
    }
}
