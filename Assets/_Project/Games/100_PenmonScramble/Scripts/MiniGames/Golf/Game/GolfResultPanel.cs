using System;
using PenmonScramble.Common.Input;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>ゴルフの結果に出すもの</summary>
    public readonly struct GolfResultInfo
    {
        public GolfResultInfo(int strokes, int par, GolfRewardBreakdown reward, bool newBest, bool firstPlay, int previousBest, bool recorded)
        {
            Strokes = strokes;
            Par = par;
            Reward = reward;
            NewBest = newBest;
            FirstPlay = firstPlay;
            PreviousBest = previousBest;
            Recorded = recorded;
        }

        public int Strokes { get; }
        public int Par { get; }
        public GolfRewardBreakdown Reward { get; }
        public bool NewBest { get; }
        public bool FirstPlay { get; }

        /// <summary>今回の前のベスト（初めてなら 0）</summary>
        public int PreviousBest { get; }

        /// <summary>記録に書いた（開発用の試しのホールでは書かない）</summary>
        public bool Recorded { get; }
    }

    /// <summary>
    /// 結果の画面の右の欄（左はスコアカード）: 合計の打数とパーとの差・ベスト更新・コインの内訳・下に「もう一度」「コースを選ぶ」「ロビーへ」の札。
    /// 札は左右で選び、決定（J / × / Enter）で押す。最初は「もう一度」を選んでおく（続けて遊びやすいように）
    /// </summary>
    public class GolfResultPanel : MonoBehaviour
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
        [Tooltip("左から もう一度・コースを選ぶ・ロビーへ")]
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
        /// オンラインでは「もう一度」を出さない（同じコースをもう一度回るときも、待ちの窓でそのまま「開始」）。
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

        public void Show(GolfResultInfo info)
        {
            gameObject.SetActive(true);
            _shownFrame = Time.frameCount;
            _navigator.Reset();
            var rank = info.Reward.Rank > 0 ? $"{info.Reward.Rank}位  " : string.Empty;
            _score.text = $"{rank}{info.Strokes} 打（{GolfScoreNames.Difference(info.Strokes, info.Par)}）";
            _best.text = BestLine(info);
            _best.color = info.NewBest ? GoodColor : Color.white;
            ShowReward(info.Reward);
            _coinTotal.text = info.Recorded ? $"+{info.Reward.Total:N0} コイン" : "試しのホールは記録しません";
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

        private static string BestLine(GolfResultInfo info)
        {
            if (!info.Recorded)
            {
                return string.Empty;
            }

            if (info.FirstPlay)
            {
                return "はじめての記録！";
            }

            return info.NewBest ? $"ベスト更新！（前は {info.PreviousBest} 打）" : $"ベスト {info.PreviousBest} 打";
        }

        /// <summary>内訳は左に名前、右にコインの2列（字の幅がそろわないので、空白で並べずに列を分ける）</summary>
        private void ShowReward(GolfRewardBreakdown reward)
        {
            var labels = "回った";
            var values = $"+{reward.BaseCoins}";
            if (reward.UnderParStrokes > 0)
            {
                labels += $"\nパーより {reward.UnderParStrokes} 打少ない";
                values += $"\n+{reward.UnderParCoins}";
            }

            if (reward.HoleInOnes > 0)
            {
                labels += $"\nホールインワン ×{reward.HoleInOnes}";
                values += $"\n+{reward.HoleInOneCoins}";
            }

            if (reward.FirstPlayBonus > 0)
            {
                labels += "\n初めて回った";
                values += $"\n+{reward.FirstPlayBonus}";
            }

            if (reward.RankBonus > 0)
            {
                labels += $"\n{reward.Rank}位";
                values += $"\n+{reward.RankBonus}";
            }

            _rewardLabels.text = labels;
            _rewardValues.text = values;
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
