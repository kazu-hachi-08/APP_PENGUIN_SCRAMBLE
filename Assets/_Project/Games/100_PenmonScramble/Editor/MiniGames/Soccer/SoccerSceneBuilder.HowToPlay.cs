using PenmonScramble.Game;
using UnityEngine;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーの遊び方。画面は本編の遊び方と同じ（タブ・ルールのページ・押したキーが光る絵）。サッカーのボタンは本編と同じ所にあるので、
    /// 操作の絵も本編のものを使い、言葉だけサッカーにする（キーボード・PS5・スマホ）。
    /// ルールのページは SoccerSceneRunner が起動時に SoccerHowToPlayPages へ差し替える
    /// </summary>
    public static partial class SoccerSceneBuilder
    {
        private static HowToPlayScreen CreateHowToPlay(Transform canvas)
        {
            var words = SoccerControlWords();
            return B.CreateHowToPlayScreen(canvas, parent => new[]
            {
                B.CreateKeyboardPanel(parent, words),
                B.CreateGamepadPanel(parent, words),
                B.CreateTouchPanel(parent, words),
            });
        }

        /// <summary>技1＝シュート / スライディング、技2＝パス / ダッシュ（持っているかで替わる）、必殺＝必殺シュート、交代＝動かす子の切り替え</summary>
        private static B.ControlWords SoccerControlWords()
        {
            return new B.ControlWords
            {
                Skill1 = "シュート",
                Skill2 = "パス",
                Special = "必殺シュート",
                Switch = "交代",
                PadSkill1 = "シュート / スライディング",
                PadSkill2 = "パス / ダッシュ",
                TouchSkill1 = "シュート",
                TouchSkill2 = "パス",
                TouchSpecial = "必殺",
                KeyboardTip = "J はためて離すとシュート。持っていないときは J でスライディング、K でダッシュ",
                PadTip = "× はためて離すとシュート。持っていないときは × でスライディング、○ でダッシュ",
                TouchTip = "持っていないときは、シュートがスライディングに、パスがダッシュに替わる。右上の ‖ でメニュー",
                TouchSkill1Color = B.SoccerShootColor,
                TouchSkill2Color = B.SoccerPassColor,
            };
        }
    }
}
