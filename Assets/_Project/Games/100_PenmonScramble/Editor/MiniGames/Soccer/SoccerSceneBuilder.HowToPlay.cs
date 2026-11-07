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

        /// <summary>技1（J / ×）＝パス / ダッシュ、技2（K / ○）＝シュート / スライディング（持っているかで替わる）、必殺＝必殺シュート、交代＝動かす子の切り替え</summary>
        private static B.ControlWords SoccerControlWords()
        {
            return new B.ControlWords
            {
                Skill1 = "パス",
                Skill2 = "シュート",
                Special = "必殺シュート",
                Switch = "交代",
                PadSkill1 = "パス / ダッシュ",
                PadSkill2 = "シュート / スライディング",
                TouchSkill1 = "パス",
                TouchSkill2 = "シュート",
                TouchSpecial = "必殺",
                KeyboardTip = "K はためて離すとシュート。持っていないときは K でスライディング、J でダッシュ",
                PadTip = "○ はためて離すとシュート。持っていないときは ○ でスライディング、× でダッシュ",
                TouchTip = "持っていないときは、シュートがスライディングに、パスがダッシュに替わる。右上の ‖ でメニュー",
                TouchSkill1Color = B.SoccerPassColor,
                TouchSkill2Color = B.SoccerShootColor,
            };
        }
    }
}
