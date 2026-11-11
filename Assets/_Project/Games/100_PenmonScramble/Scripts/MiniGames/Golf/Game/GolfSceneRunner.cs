using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフのシーンの入口。ミニゲーム画面から来たときは、選んだコースをひとりで回る（GolfRoundPlay）。
    /// 部屋の待ちの窓から来たときは、引き継いだ部屋でみんなで回る（GolfOnlinePlay）。部屋が切れていたらロビーへ。
    /// まだ作っていないコースなら「準備中」の板と「もどる」だけ。
    /// エディタの Tools/PenmonScramble/Golf から開いたときは試しの1ホールを遊ぶ（記録には書かない）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public class GolfSceneRunner : MonoBehaviour
    {
        [SerializeField] private GameObject _placeholder;
        [SerializeField] private Text _courseText;
        [SerializeField] private Button _backButton;
        [SerializeField] private GolfRoundPlay _play;
        [SerializeField] private GolfOnlinePlay _online;
        [SerializeField] private OnlineSession _session;
        [Tooltip("メニューから開く遊び方（本編と同じ画面）。ルールのページをゴルフのものに差し替える")]
        [SerializeField] private HowToPlayScreen _howToPlay;

        private bool _leaving;

        /// <summary>ミニゲーム画面のコース選びに、ゴルフのコースの中身（ホールとパー・報酬）を出せるようにする</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGuide()
        {
            MiniGameGuides.Register(MiniGameTable.GolfId, new GolfCourseGuide());
        }

        private void Awake()
        {
            _backButton.onClick.AddListener(Leave);
            if (_howToPlay != null)
            {
                _howToPlay.UsePages(GolfHowToPlayPages.All);
            }
        }

        private void Start()
        {
            MiniGameLaunch.EnsureSelected(MiniGameTable.GolfId);
            _play.PlayMusic();
            if (GolfDevRequest.TryTakeTestHole(out var players))
            {
                StartRound(GolfCourseTable.TestCourse(), players, record: false);
                return;
            }

            if (MiniGameLaunch.IsOnline)
            {
                StartOnline();
                return;
            }

            var course = GolfCourseTable.Get(MiniGameLaunch.CourseNo);
            if (course != null)
            {
                StartRound(course, 1, record: true);
                return;
            }

            var row = MiniGameLaunch.Game?.Course(MiniGameLaunch.CourseNo);
            _courseText.text = row != null ? $"コース {row.No}  {row.Name}" : string.Empty;
        }

        private void StartRound(GolfCourseDef course, int players, bool record)
        {
            _placeholder.SetActive(false);
            _play.BeginSolo(course, players, record);
        }

        /// <summary>移っている間にホストが落ちて部屋を引き継げなかったら、ロビーへ戻る</summary>
        private void StartOnline()
        {
            if (MiniGameLaunch.IsRoomLost(_session, nameof(GolfSceneRunner)))
            {
                _leaving = true;
                MiniGameLaunch.ReturnToLobby();
                return;
            }

            _placeholder.SetActive(false);
            _online.Begin(MiniGameLaunch.CourseNo);
        }

        private void Update()
        {
            // 遊んでいる間の Esc はメニュー（StageMenu）が受ける
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
