using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフのシーンの入口。いまは中身が空で、遊ぶコースの名前と「もどる」だけ（Docs/MINIGAME_GOLF_PLAN.md フェーズ 1）。
    /// 「もどる」/ Esc で本編のシーンのミニゲーム画面へ戻る
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class GolfSceneRunner : MonoBehaviour
    {
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;

        private bool _leaving;

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.GolfId);
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
