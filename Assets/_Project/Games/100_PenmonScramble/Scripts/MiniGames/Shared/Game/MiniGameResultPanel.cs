using System;
using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// 結果の欄に出す中身（ミニゲームごとに作る）。見出しの点・ベストの一言・コインの内訳（名前とコインの行）・合計の一言
    /// </summary>
    public sealed class MiniGameResultView
    {
        private readonly List<(string Label, int Coins)> _rewards = new List<(string, int)>();

        /// <summary>大きく出す結果（「27 打（−3）」「勝ち！ 3 − 1」など）</summary>
        public string Score { get; set; } = string.Empty;

        /// <summary>ベストの一言（「ベスト更新！」など）。記録しないときは空</summary>
        public string Best { get; set; } = string.Empty;

        /// <summary>ベストの一言を目立つ色にする（ベスト更新・初めての記録）</summary>
        public bool BestHighlighted { get; set; }

        /// <summary>合計の一言（「+120 コイン」・記録しないときの断り）</summary>
        public string CoinTotal { get; set; } = string.Empty;

        public IReadOnlyList<(string Label, int Coins)> Rewards => _rewards;

        /// <summary>内訳の1行を足す（0 コインの行も出したいときだけ足す）</summary>
        public MiniGameResultView AddReward(string label, int coins)
        {
            _rewards.Add((label, coins));
            return this;
        }
    }

    /// <summary>
    /// ミニゲームの結果の欄: 結果の点・ベストの一言・コインの内訳・下に「もう一度」「コース（スタジアム）を選ぶ」「ロビーへ」の札。
    /// 札は左右で選び、決定（J / × / Enter）で押す。最初は出している札の一番左を選んでおく（一人で遊んだときは「もう一度」。続けて遊びやすいように）。
    /// 中身（MiniGameResultView）はミニゲームごとに作って渡す
    /// </summary>
    public class MiniGameResultPanel : MonoBehaviour
    {
        public const int RetryCard = 0;
        public const int CoursesCard = 1;
        public const int LobbyCard = 2;

        private static readonly Color GoodColor = new Color(1f, 0.85f, 0.3f);

        [SerializeField] private Text _score;
        [SerializeField] private Text _best;
        [SerializeField] private Text _rewardLabels;
        [SerializeField] private Text _rewardValues;
        [SerializeField] private Text _coinTotal;
        [Tooltip("左から もう一度・コース（スタジアム）を選ぶ・ロビーへ")]
        [SerializeField] private StageMenuCard[] _cards;
        [Tooltip("オンラインのゲストに出す一言（コースを選ぶのはホスト）")]
        [SerializeField] private Text _note;
        [SerializeField] private float _cardStagger = 0.06f;

        private readonly MenuNavigator _navigator = new MenuNavigator();
        private readonly bool[] _available = { true, true, true };
        private int _selected;
        private int _shownFrame;

        /// <summary>札が押された（RetryCard など）</summary>
        public event Action<int> Chosen;

        private void Awake()
        {
            foreach (var card in _cards)
            {
                card.Hovered += c => Select(Array.IndexOf(_cards, c));
                card.Pressed += c => Press(Array.IndexOf(_cards, c));
            }
        }

        /// <summary>
        /// オンラインでは「もう一度」を出さない（同じコースをもう一度遊ぶときも、待ちの窓でそのまま「開始」）。
        /// 「コースを選ぶ」（全員で待ちの窓へ戻る）はホストだけ。ゲストは「ロビーへ」（部屋から抜ける）だけで、ホストを待つ
        /// </summary>
        public void SetOnline(bool online, bool host)
        {
            _available[RetryCard] = !online;
            _available[CoursesCard] = !online || host;
            _available[LobbyCard] = true;
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].gameObject.SetActive(_available[i]);
            }

            _note.gameObject.SetActive(online && !host);
        }

        public void Show(MiniGameResultView view)
        {
            gameObject.SetActive(true);
            _shownFrame = Time.frameCount;
            _navigator.Reset();
            _score.text = view.Score;
            _best.text = view.Best;
            _best.color = view.BestHighlighted ? GoodColor : Color.white;
            ShowRewards(view.Rewards);
            _coinTotal.text = view.CoinTotal;
            Select(FirstAvailable());
            for (var i = 0; i < _cards.Length; i++)
            {
                if (!_available[i])
                {
                    continue;
                }

                _cards[i].SnapSelection();
                _cards[i].PlayIn(0.3f + i * _cardStagger);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            _navigator.Update();
            if (Time.frameCount == _shownFrame)
            {
                return;
            }

            if (_navigator.Step.x != 0)
            {
                Select(NextAvailable(_selected, _navigator.Step.x));
                GameSounds.Play(GameSound.Click);
            }
            else if (_navigator.Submit || (InputManager.HasInstance && InputManager.Instance.IsActionDown(0)))
            {
                Press(_selected);
            }
        }

        /// <summary>内訳は左に名前、右にコインの2列（字の幅がそろわないので、空白で並べずに列を分ける）</summary>
        private void ShowRewards(IReadOnlyList<(string Label, int Coins)> rewards)
        {
            var labels = new List<string>();
            var values = new List<string>();
            foreach (var (label, coins) in rewards)
            {
                labels.Add(label);
                values.Add($"+{coins}");
            }

            _rewardLabels.text = string.Join("\n", labels);
            _rewardValues.text = string.Join("\n", values);
        }

        private int FirstAvailable() => NextAvailable(-1, 1);

        /// <summary>出している札の中で、step の向きに次の札（端なら今のまま）</summary>
        private int NextAvailable(int from, int step)
        {
            for (var i = from + step; i >= 0 && i < _cards.Length; i += step)
            {
                if (_available[i])
                {
                    return i;
                }
            }

            return from < 0 ? LobbyCard : from;
        }

        private void Select(int index)
        {
            if (index < 0)
            {
                return;
            }

            _selected = index;
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetSelected(i == index);
            }
        }

        private void Press(int index)
        {
            if (index < 0 || !gameObject.activeSelf || !_available[index])
            {
                return;
            }

            GameSounds.Play(GameSound.Click);
            Chosen?.Invoke(index);
        }
    }
}
