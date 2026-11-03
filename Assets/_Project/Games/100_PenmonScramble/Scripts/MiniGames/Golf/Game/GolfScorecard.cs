using System;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// スコアカード: 見出しの行（ホール 1・2・3・合計）、パーの行、全員の行（席の色の帯・名前・ホールごとの打数・合計とパーとの差）。
    /// ホールの間に出して「つぎのホールへ」を待つ。結果の画面では左に寄せて、つぎの札は出さない。
    /// パーより少ない打数は金、多い打数は水色。まだ回っていないホールは「−」。組み立ては GolfSceneBuilder.Scorecard.cs
    /// </summary>
    public class GolfScorecard : MonoBehaviour
    {
        // _cells の並び: 1行に「名前・ホール × HolesPerCourse・合計」
        public const int Columns = GolfRules.HolesPerCourse + 2;
        public const int HeaderRow = 0;
        public const int ParRow = 1;
        public const int FirstPlayerRow = 2;
        public const int Rows = FirstPlayerRow + GolfRules.MaxPlayers;

        private const string NotPlayed = "−";
        private static readonly Color UnderColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color OverColor = new Color(0.65f, 0.85f, 1f);
        private static readonly Color EvenColor = Color.white;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private Text _title;
        [Tooltip("行ごとに 名前・ホール1〜3・合計（Rows × Columns）")]
        [SerializeField] private Text[] _cells;
        [Tooltip("全員の行の帯（席の色）")]
        [SerializeField] private Image[] _playerPlates;
        [SerializeField] private StageMenuCard _nextCard;
        [SerializeField] private Vector2 _centerPosition = new Vector2(0f, 60f);
        [SerializeField] private Vector2 _dockedPosition = new Vector2(-420f, 40f);

        private readonly MenuNavigator _navigator = new MenuNavigator();
        private Action _onNext;
        private int _shownFrame;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _nextCard.Pressed += _ => Next();
        }

        /// <param name="onNext">「つぎのホールへ」を押したとき。null ならつぎの札を出さない（結果の画面で使うとき）</param>
        public void Show(GolfRound round, string title, string nextLabel, Action onNext)
        {
            gameObject.SetActive(true);
            _shownFrame = Time.frameCount;
            _navigator.Reset();
            _title.text = title;
            _onNext = onNext;
            _panel.anchoredPosition = onNext != null ? _centerPosition : _dockedPosition;
            _nextCard.gameObject.SetActive(onNext != null);
            if (onNext != null)
            {
                _nextCard.SetLabel(nextLabel);
                _nextCard.SetSelected(true);
                _nextCard.SnapSelection();
                _nextCard.PlayIn(0.15f);
            }

            Fill(round);
        }

        public void Hide()
        {
            _onNext = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _navigator.Update();
            // 開いたフレームの押下は、前の画面の決定なので数えない
            if (_onNext == null || Time.frameCount == _shownFrame)
            {
                return;
            }

            var shoot = InputManager.HasInstance && InputManager.Instance.IsActionDown(0);
            if (_navigator.Submit || shoot)
            {
                Next();
            }
        }

        private void Next()
        {
            var next = _onNext;
            if (next == null)
            {
                return;
            }

            GameSounds.Play(GameSound.Click);
            Hide();
            next();
        }

        private void Fill(GolfRound round)
        {
            var holes = round.HoleCount;
            SetCell(HeaderRow, 0, "ホール", EvenColor);
            SetCell(ParRow, 0, "パー", EvenColor);
            for (var hole = 0; hole < GolfRules.HolesPerCourse; hole++)
            {
                var shown = hole < holes;
                SetCell(HeaderRow, hole + 1, shown ? (hole + 1).ToString() : string.Empty, EvenColor);
                SetCell(ParRow, hole + 1, shown ? round.Course.Holes[hole].Par.ToString() : string.Empty, EvenColor);
            }

            SetCell(HeaderRow, Columns - 1, "合計", EvenColor);
            SetCell(ParRow, Columns - 1, round.Course.TotalPar.ToString(), EvenColor);
            for (var seat = 0; seat < GolfRules.MaxPlayers; seat++)
            {
                FillPlayer(round, seat);
            }
        }

        private void FillPlayer(GolfRound round, int seat)
        {
            var row = FirstPlayerRow + seat;
            var playing = seat < round.PlayerCount;
            _playerPlates[seat].gameObject.SetActive(playing);
            for (var column = 0; column < Columns; column++)
            {
                _cells[row * Columns + column].gameObject.SetActive(playing);
            }

            if (!playing)
            {
                return;
            }

            _playerPlates[seat].color = GolfSeats.ColorOf(seat) * new Color(1f, 1f, 1f, 0.35f);
            SetCell(row, 0, GolfSeats.Label(seat), EvenColor);
            for (var hole = 0; hole < GolfRules.HolesPerCourse; hole++)
            {
                if (hole >= round.HoleCount)
                {
                    SetCell(row, hole + 1, string.Empty, EvenColor);
                    continue;
                }

                var par = round.Course.Holes[hole].Par;
                var recorded = round.IsRecorded(hole);
                var strokes = round.Strokes(hole, seat);
                SetCell(row, hole + 1, recorded ? strokes.ToString() : NotPlayed, recorded ? ColorFor(strokes, par) : EvenColor);
            }

            var total = round.Total(seat);
            var parSoFar = round.ParThroughRecorded();
            SetCell(row, Columns - 1, $"{total}（{GolfScoreNames.Difference(total, parSoFar)}）", ColorFor(total, parSoFar));
        }

        private static Color ColorFor(int strokes, int par) => strokes < par ? UnderColor : strokes > par ? OverColor : EvenColor;

        private void SetCell(int row, int column, string text, Color color)
        {
            var cell = _cells[row * Columns + column];
            cell.text = text;
            cell.color = color;
        }
    }
}
