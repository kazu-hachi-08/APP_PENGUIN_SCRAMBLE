using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームを選ぶ画面。ロビーの左の列の「ミニゲーム」から開く。
    /// 1段目: MiniGameTable のカードが並ぶ → 押すと 2段目: そのミニゲームのコース選び（◀ ▶・鍵・ベスト）→「ひとりで」。
    /// 「もどる」/ Esc は1段ずつ戻る。ミニゲームのシーンから戻ったとき（LobbyReturn）は、遊んだコースを選んだ状態で開く。
    /// 本編（TitleMenu）はミニゲームを知らないので、開くボタンのつなぎはこちらで持つ
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る。Start は TitleFlow がロビーへ入った後に開く
    [DefaultExecutionOrder(50)]
    public class MiniGameHubScreen : MonoBehaviour
    {
        private const string HubHeading = "ミニゲーム";
        private const string NoRecordLabel = "まだ回っていません";
        private static readonly Color LockedColor = new Color(1f, 0.70f, 0.45f);
        private static readonly Color InfoColor = new Color(1f, 0.92f, 0.6f);

        [Tooltip("ロビーの左の列の「ミニゲーム」")]
        [SerializeField] private Button _openButton;
        [Tooltip("画面の中身。このコンポーネントの物は閉じている間も動かしておく（戻り先を見るため）")]
        [SerializeField] private GameObject _screen;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _headingText;
        [SerializeField] private Text _coinText;

        [Header("ミニゲームを選ぶ")]
        [SerializeField] private GameObject _gamesPage;
        [SerializeField] private MiniGameCard _cardTemplate;
        [SerializeField] private float _cardSpacing = 640f;
        [Tooltip("カードの絵。MiniGameTable.All と同じ並び")]
        [SerializeField] private Sprite[] _cardArt;

        [Header("コースを選ぶ")]
        [SerializeField] private GameObject _coursePage;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _courseNoText;
        [SerializeField] private Text _courseNameText;
        [SerializeField] private Text _courseInfoText;
        [SerializeField] private Button _prevCourseButton;
        [SerializeField] private Button _nextCourseButton;
        [SerializeField] private Button _soloButton;

        private readonly List<MiniGameCard> _cards = new List<MiniGameCard>();
        private MiniGameDef _game;
        private int _courseIndex;

        private static PlayerRecord Record => GameSave.Record;

        public bool IsOpen => _screen.activeSelf;

        private void Awake()
        {
            _openButton.onClick.AddListener(Open);
            _backButton.onClick.AddListener(Back);
            _prevCourseButton.onClick.AddListener(() => MoveCourse(-1));
            _nextCourseButton.onClick.AddListener(() => MoveCourse(1));
            _soloButton.onClick.AddListener(PlaySolo);
            _cardTemplate.gameObject.SetActive(false);
            CreateCards();
            _screen.SetActive(false);
        }

        private void Start()
        {
            if (!LobbyReturn.Take(LobbyScreen.MiniGames))
            {
                return;
            }

            Open();
            // 遊んだミニゲームのコース選びから（続けて同じコースを遊べるように）
            if (MiniGameLaunch.Game != null)
            {
                ShowCourses(MiniGameLaunch.Game, MiniGameLaunch.CourseNo);
            }
        }

        private void Update()
        {
            if (IsOpen && InputManager.HasInstance && InputManager.Instance.IsPauseDown)
            {
                Back();
            }
        }

        public void Open()
        {
            _screen.SetActive(true);
            _coinText.text = Record.Coins.ToString("N0");
            ShowGames();
        }

        /// <summary>コース選びならミニゲームのカードへ、カードなら閉じる</summary>
        private void Back()
        {
            if (_coursePage.activeSelf)
            {
                ShowGames();
                return;
            }

            _screen.SetActive(false);
        }

        private void CreateCards()
        {
            var games = MiniGameTable.All;
            for (var i = 0; i < games.Count; i++)
            {
                var card = Instantiate(_cardTemplate, _cardTemplate.transform.parent);
                card.gameObject.SetActive(true);
                // カードは横に1列、まん中にそろえて並べる
                var rect = (RectTransform)card.transform;
                rect.anchoredPosition = _cardTemplate.GetComponent<RectTransform>().anchoredPosition + Vector2.right * ((i - (games.Count - 1) * 0.5f) * _cardSpacing);
                card.Show(games[i], i < _cardArt.Length ? _cardArt[i] : null, game => ShowCourses(game, 0));
                _cards.Add(card);
            }
        }

        private void ShowGames()
        {
            _game = null;
            _headingText.text = HubHeading;
            _gamesPage.SetActive(true);
            _coursePage.SetActive(false);
        }

        /// <param name="courseNo">選んでおくコース。0 なら、開いている中で一番先のコース</param>
        private void ShowCourses(MiniGameDef game, int courseNo)
        {
            _game = game;
            _courseIndex = IndexOf(game, courseNo);
            _headingText.text = game.Name;
            _descriptionText.text = game.Description;
            _gamesPage.SetActive(false);
            _coursePage.SetActive(true);
            RefreshCourse();
        }

        private static int IndexOf(MiniGameDef game, int courseNo)
        {
            var latest = 0;
            for (var i = 0; i < game.Courses.Count; i++)
            {
                if (game.Courses[i].No == courseNo)
                {
                    return i;
                }

                if (MiniGameUnlock.IsUnlocked(game.Courses[i], Record))
                {
                    latest = i;
                }
            }

            return latest;
        }

        /// <summary>鍵の掛かったコースも見られる（どこまで進めば開くかがわかるように）。遊べないだけ</summary>
        private void MoveCourse(int step)
        {
            _courseIndex = Mathf.Clamp(_courseIndex + step, 0, _game.Courses.Count - 1);
            RefreshCourse();
        }

        private void RefreshCourse()
        {
            var course = _game.Courses[_courseIndex];
            var unlocked = MiniGameUnlock.IsUnlocked(course, Record);
            _courseNoText.text = $"コース {course.No}";
            _courseNameText.text = course.Name;
            _courseInfoText.text = unlocked ? BestLabel(course) : $"ワールド {course.World} に入ると解放";
            _courseInfoText.color = unlocked ? InfoColor : LockedColor;
            _prevCourseButton.interactable = _courseIndex > 0;
            _nextCourseButton.interactable = _courseIndex < _game.Courses.Count - 1;
            _soloButton.interactable = unlocked;
        }

        private string BestLabel(MiniGameCourseDef course)
        {
            var records = new MiniGameRecords(Record.Data);
            return records.TryGetBest(_game.Id, course.No, out var best) ? $"ベスト {best}{_game.ScoreUnit}" : NoRecordLabel;
        }

        private void PlaySolo()
        {
            var course = _game.Courses[_courseIndex];
            if (MiniGameUnlock.IsUnlocked(course, Record))
            {
                MiniGameLaunch.StartSolo(_game, course.No);
            }
        }
    }
}
