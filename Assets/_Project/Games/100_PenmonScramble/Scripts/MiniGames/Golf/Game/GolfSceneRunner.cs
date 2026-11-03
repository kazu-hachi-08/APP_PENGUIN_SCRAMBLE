using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフのシーンの入口。ミニゲーム画面から来たときは、まだコースが無いので名前と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Golf から開いたときは試しの1ホールを遊ぶ（Docs/MINIGAME_GOLF_PLAN.md フェーズ 2）。
    /// 「もどる」/ Esc で本編のシーンのミニゲーム画面へ戻る
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class GolfSceneRunner : MonoBehaviour
    {
        [SerializeField] private GameObject _placeholder;
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;
        [SerializeField] private GolfTestHolePlay _testHole;

        private bool _leaving;

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.GolfId);
            if (GolfDevRequest.TryTakeTestHole(out var players))
            {
                _placeholder.SetActive(false);
                _testHole.Begin(players);
                return;
            }

            var course = MiniGameLaunch.Game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = course != null ? $"コース {course.No}  {course.Name}（{GolfRules.HolesPerCourse}ホール）" : string.Empty;
        }

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
