using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーのシーンの入口。スタジアムはまだ作っていないので、ミニゲーム画面から来たときは「準備中」の板と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Soccer から開いたときは試しのピッチ（SoccerTestPitch）を遊ぶ（記録には書かない）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class SoccerSceneRunner : MonoBehaviour
    {
        [SerializeField] private GameObject _placeholder;
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;
        [SerializeField] private SoccerTestPitch _testPitch;

        private bool _leaving;

        /// <summary>ミニゲーム画面のスタジアム選びに、サッカーの中身（今はどれも準備中）を出せるようにする</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGuide()
        {
            MiniGameGuides.Register(MiniGameTable.SoccerId, new SoccerCourseGuide());
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.SoccerId);
            if (SoccerDevRequest.TryTakeTestPitch())
            {
                _placeholder.SetActive(false);
                _testPitch.Begin();
                return;
            }

            var game = MiniGameLaunch.Game;
            var row = game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = row != null ? $"{game.CourseWord} {row.No}  {row.Name}" : string.Empty;
        }

        /// <summary>試しのピッチにはまだメニューが無いので、Esc でそのまま戻る（試合中のメニューはフェーズ 4）</summary>
        private void Update()
        {
            if (InputManager.HasInstance && InputManager.Instance.IsPauseDown)
            {
                Leave();
            }
        }

        private void Leave()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            MiniGameLaunch.ReturnToHub();
        }
    }
}
