using System;
using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Baseball.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.Golf.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using PenmonScramble.Soccer.Game;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ミニゲーム画面のコースのカードと、待ちの窓のミニゲームの板の文字は、シーンでは空でコースを選んだときに入る。
    /// 強さと仕掛けの一言が枠の幅を越えて折り返し、上下の行に重なったことがあるので、
    /// 全ミニゲーム × 全コース（待ちの窓は ひとり・協力 1〜4人・対戦）の文を見本として入れて測る
    /// </summary>
    public static partial class UiLayoutCheck
    {
        // ベストの見本は、桁がいちばん多くなりそうな数（ゴルフ「99打（+90）」、サッカー・野球「+99」）
        private const int WidestBest = 99;

        private static void CheckMiniGames(Font font, List<string> issues)
        {
            CheckHubCards(font, issues);
            CheckTeamRoomPanels(font, issues);
            CheckGolfRoomPanels(font, issues);
        }

        /// <summary>ミニゲーム画面のコースのカード（まだ遊んでいない＝報酬が2行のときで測る）</summary>
        private static void CheckHubCards(Font font, List<string> issues)
        {
            foreach (var hub in Object.FindObjectsByType<MiniGameHubScreen>(FindObjectsInactive.Include))
            {
                foreach (var game in MiniGameTable.All)
                {
                    var guide = GuideOf(game.Id);
                    if (guide == null) continue;
                    foreach (var course in game.Courses)
                    {
                        var samples = new Dictionary<Text, string>();
                        AddSample(hub, "_descriptionText", game.Description, samples);
                        AddSample(hub, "_courseNoText", $"{game.CourseWord} {course.No}", samples);
                        AddSample(hub, "_courseNameText", course.Name, samples);
                        AddSample(hub, "_courseInfoText", $"ベスト {guide.FormatBest(course.No, WidestBest)}", samples);
                        AddSample(hub, "_courseLayoutText", guide.Layout(course.No), samples);
                        AddSample(hub, "_rewardText", guide.Reward(course.No, firstPlay: true), samples);
                        CheckSamples(samples, font, issues);
                    }
                }
            }
        }

        /// <summary>待ちの窓のサッカー・野球の板（強さは遊び方と人数で変わるので、全部の組み合わせで測る）</summary>
        private static void CheckTeamRoomPanels(Font font, List<string> issues)
        {
            foreach (var panel in Object.FindObjectsByType<MiniGameTeamRoomPanel>(FindObjectsInactive.Include))
            {
                var (gameId, onlineLayout) = TeamRoomGuideOf(panel);
                if (onlineLayout == null) continue;
                var game = MiniGameTable.Get(gameId);
                var guide = GuideOf(gameId);
                foreach (var course in game.Courses)
                {
                    foreach (var layout in OnlineLayouts(course.No, onlineLayout))
                    {
                        var samples = new Dictionary<Text, string>();
                        AddSample(panel, "_stadiumNoText", $"{game.CourseWord} {course.No}", samples);
                        AddSample(panel, "_stadiumNameText", course.Name, samples);
                        AddSample(panel, "_layoutText", layout, samples);
                        AddSample(panel, "_rewardText", guide.Reward(course.No, firstPlay: true), samples);
                        CheckSamples(samples, font, issues);
                    }
                }
            }
        }

        /// <summary>待ちの窓のゴルフの板</summary>
        private static void CheckGolfRoomPanels(Font font, List<string> issues)
        {
            var guide = new GolfCourseGuide();
            var game = MiniGameTable.Get(MiniGameTable.GolfId);
            foreach (var panel in Object.FindObjectsByType<GolfRoomPanel>(FindObjectsInactive.Include))
            {
                foreach (var course in game.Courses)
                {
                    var samples = new Dictionary<Text, string>();
                    AddSample(panel, "_courseNoText", $"{game.CourseWord} {course.No}", samples);
                    AddSample(panel, "_courseNameText", course.Name, samples);
                    AddSample(panel, "_layoutText", guide.Layout(course.No), samples);
                    AddSample(panel, "_rewardText", guide.Reward(course.No, firstPlay: true), samples);
                    CheckSamples(samples, font, issues);
                }
            }
        }

        private static IEnumerable<string> OnlineLayouts(int courseNo, Func<int, MiniGameOnlineMode, int, string> onlineLayout)
        {
            yield return onlineLayout(courseNo, MiniGameOnlineMode.Versus, MiniGameRoomChoice.MaxSeats);
            for (var humans = 1; humans <= MiniGameRoomChoice.MaxSeats; humans++)
            {
                yield return onlineLayout(courseNo, MiniGameOnlineMode.Coop, humans);
            }
        }

        // 実行時は各ミニゲームのシーンが起動時に MiniGameGuides へ登録する。エディタでは登録が無いので、ここで作る
        private static IMiniGameCourseGuide GuideOf(string gameId)
        {
            switch (gameId)
            {
                case MiniGameTable.GolfId: return new GolfCourseGuide();
                case MiniGameTable.SoccerId: return new SoccerCourseGuide();
                case MiniGameTable.BaseballId: return new BaseballCourseGuide();
                default: return null;
            }
        }

        private static (string gameId, Func<int, MiniGameOnlineMode, int, string> onlineLayout) TeamRoomGuideOf(MiniGameTeamRoomPanel panel)
        {
            switch (panel)
            {
                case SoccerRoomPanel _: return (MiniGameTable.SoccerId, new SoccerCourseGuide().OnlineLayout);
                case BaseballRoomPanel _: return (MiniGameTable.BaseballId, new BaseballCourseGuide().OnlineLayout);
                default: return (null, null);
            }
        }
    }
}
