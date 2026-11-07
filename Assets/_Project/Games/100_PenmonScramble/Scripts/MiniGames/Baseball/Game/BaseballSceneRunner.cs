using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球のシーンの入口。球場はまだ作っていないので、ミニゲーム画面から来たときは「準備中」の板と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Baseball から開いたときは試しの打席・試しの試合（BaseballMatch）を遊ぶ（記録には書かない）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class BaseballSceneRunner : MonoBehaviour
    {
        [SerializeField] private GameObject _placeholder;
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;
        [SerializeField] private BaseballMatch _match;

        private bool _leaving;

        /// <summary>ミニゲーム画面の球場選びに、野球の中身（今はどれも準備中）を出せるようにする</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGuide()
        {
            MiniGameGuides.Register(MiniGameTable.BaseballId, new BaseballCourseGuide());
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
            _match.LeaveRequested += Leave;
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.BaseballId);
            if (BaseballDevRequest.TryTake(out var mode))
            {
                _placeholder.SetActive(false);
                _match.Begin(mode);
                return;
            }

            var game = MiniGameLaunch.Game;
            var row = game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = row != null ? $"{game.CourseWord} {row.No}  {row.Name}" : string.Empty;
        }

        /// <summary>試しの打席・試合にはまだメニューが無いので、Esc でそのまま戻る（試合中のメニューはフェーズ 4）</summary>
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
