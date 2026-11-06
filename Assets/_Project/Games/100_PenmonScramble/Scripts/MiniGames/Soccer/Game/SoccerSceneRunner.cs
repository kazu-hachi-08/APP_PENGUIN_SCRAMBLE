using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーのシーンの入口。ミニゲーム画面から来たときは、選んだスタジアムでひとりで1試合（SoccerMatchPlay）。
    /// まだ作っていないスタジアムなら「準備中」の板と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Soccer から開いたときは、スタジアム1の試合（試しのピッチ）か PK戦の確認を遊ぶ（記録には書かない）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class SoccerSceneRunner : MonoBehaviour
    {
        // 開発用の遊び方で使うスタジアム（海岸）
        private const int DevStadium = 1;

        [SerializeField] private GameObject _placeholder;
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;
        [SerializeField] private SoccerMatchPlay _play;

        private bool _leaving;

        /// <summary>ミニゲーム画面のスタジアム選びに、サッカーの中身（CPU の強さ・仕掛け・報酬）を出せるようにする</summary>
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
            var dev = SoccerDevRequest.Take();
            if (dev != SoccerDevMode.None)
            {
                StartMatch(SoccerStadiumTable.Get(DevStadium), record: false, shootoutOnly: dev == SoccerDevMode.Shootout);
                return;
            }

            var stadium = SoccerStadiumTable.Get(MiniGameLaunch.CourseNo);
            if (stadium != null)
            {
                StartMatch(stadium, record: true, shootoutOnly: false);
                return;
            }

            var game = MiniGameLaunch.Game;
            var row = game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = row != null ? $"{game.CourseWord} {row.No}  {row.Name}" : string.Empty;
        }

        private void StartMatch(SoccerStadiumDef stadium, bool record, bool shootoutOnly)
        {
            _placeholder.SetActive(false);
            _play.Begin(stadium, record, shootoutOnly);
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
