using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球のシーンの入口。ミニゲーム画面から来たときは、選んだ球場でひとりで1試合（BaseballMatch。最初は自分が先攻）。
    /// 部屋の待ちの窓から来たときは、引き継いだ部屋でみんなで1試合（BaseballOnlinePlay）。部屋が切れていたらロビーへ。
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
        [SerializeField] private BaseballOnlinePlay _online;
        [SerializeField] private OnlineSession _session;
        [SerializeField] private HowToPlayScreen _howToPlay;

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
            // 遊び方の画面は本編と同じ物なので、ルールのページだけ野球のものに差し替える
            if (_howToPlay != null)
            {
                _howToPlay.UsePages(BaseballHowToPlayPages.All);
            }

            // 「準備中」の板・みんなでの「待っています」の間も、本編の曲のままにしない
            _match.PlayMusic();
            if (BaseballDevRequest.TryTake(out var mode))
            {
                StartMatch(mode, null, record: false);
                return;
            }

            if (MiniGameLaunch.IsOnline)
            {
                StartOnline();
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

        /// <summary>移っている間にホストが落ちて部屋を引き継げなかったら、ロビーへ戻る</summary>
        private void StartOnline()
        {
            if (MiniGameLaunch.IsRoomLost(_session, nameof(BaseballSceneRunner)))
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
