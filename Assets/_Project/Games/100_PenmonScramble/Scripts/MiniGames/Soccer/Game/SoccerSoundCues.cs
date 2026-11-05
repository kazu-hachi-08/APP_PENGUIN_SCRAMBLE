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
            }
        }
    }
}
