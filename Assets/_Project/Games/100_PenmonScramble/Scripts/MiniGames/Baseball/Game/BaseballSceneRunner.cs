using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球のシーンの入口。ミニゲーム画面から来たときは、選んだ球場でひとりで1試合（BaseballMatch。最初は自分が先攻）。
    /// まだ作っていない球場なら「準備中」の板と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Baseball から開いたときは試しの打席・試しの試合を遊ぶ（記録には書かない）
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

        /// <summary>ミニゲーム画面の球場選びに、野球の中身（CPU の強さ・盤・報酬）を出せるようにする</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGuide()
        {
            MiniGameGuides.Register(MiniGameTable.BaseballId, new BaseballCourseGuide());
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.BaseballId);
            if (BaseballDevRequest.TryTake(out var mode))
            {
                StartMatch(mode, null, record: false);
                return;
            }

            var stadium = BaseballStadiumTable.Get(MiniGameLaunch.CourseNo);
            if (stadium != null)
            {
                StartMatch(BaseballPlayMode.MatchAway, stadium, record: true);
                return;
            }

            var game = MiniGameLaunch.Game;
            var row = game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = row != null ? $"{game.CourseWord} {row.No}  {row.Name}" : string.Empty;
        }

        private void StartMatch(BaseballPlayMode mode, BaseballStadiumDef stadium, bool record)
        {
            _placeholder.SetActive(false);
            _match.Begin(mode, stadium, record);
        }

        private void Update()
        {
            // 試合中の Esc はメニュー（StageMenu）が受ける
            if (_placeholder.activeSelf && InputManager.HasInstance && InputManager.Instance.IsPauseDown)
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
