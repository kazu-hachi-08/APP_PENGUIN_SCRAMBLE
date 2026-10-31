using System;
using System.Collections;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャ。後ろの3D（GachaStage）でレバーを回してカプセルを出し、割れて出てきた子の★と結果のカードを出す。
    /// 演出の間は画面のどこを押してもとばせる。初回は無料の3回を引き終わるまで「もどる」を出さない（3体そろえてから始めるため）。
    /// 確率の窓は GachaScreen.Rates.cs
    /// </summary>
    public partial class GachaScreen : MonoBehaviour
    {
        private const float StarPopSeconds = 0.25f;
        private const float StarPopStart = 0.3f;

        [SerializeField] private GachaStage _stage;
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _countText;
        [SerializeField] private Text _infoText;
        [SerializeField] private Button _pullButton;
        [SerializeField] private Text _pullLabel;
        [SerializeField] private Button _backButton;
        [Tooltip("画面全体を覆う透明なボタン。演出の間だけ出して、押すととばす")]
        [SerializeField] private Button _skipButton;
        [SerializeField] private GameObject _skipHint;
        [SerializeField] private Text _starsText;
        [SerializeField] private GachaResultCard _card;

        private readonly System.Random _random = new System.Random();
        private Coroutine _playing;
        private Coroutine _starPop;
        private OwnedPenguin _pulled;
        private bool _pulledIsNew;

        public event Action Closed;

        private static PlayerRecord Record => GameSave.Record;

        private int PulledStars => SpeciesTable.Get(_pulled.speciesNo)?.Stars ?? 1;

        private void Awake()
        {
            _pullButton.onClick.AddListener(Pull);
            _backButton.onClick.AddListener(Close);
            _skipButton.onClick.AddListener(Skip);
            AwakeRates();
        }

        // 閉じると Unity がコルーチンを止めるので、演出中のまま残らないようにする
        private void OnDisable()
        {
            _playing = null;
            _starPop = null;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            _stage.Show();
            ClearResult();
            CloseRates();
            Refresh();
        }

        private void Close()
        {
            if (_playing != null)
            {
                return;
            }

            _stage.Hide();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void Refresh()
        {
            var playing = _playing != null;
            _coinText.text = Record.Coins.ToString("N0");
            _countText.text = $"ペンモン {Record.Penguins.Count}/{Balance.MaxPenguins}";
            var free = Record.FreePullsLeft;
            _pullLabel.text = free > 0 ? $"無料で引く\n<size=34>のこり {free} 回</size>" : $"1回引く\n<size=34>{Balance.GachaCost} コイン</size>";
            var state = Record.CanPull();
            _pullButton.interactable = !playing && state == PullResult.Ok;
            _backButton.gameObject.SetActive(!playing && free == 0);
            _ratesButton.interactable = !playing;
            _skipButton.gameObject.SetActive(playing);
            _skipHint.SetActive(playing);
            _infoText.text = playing ? string.Empty : InfoText(state, free);
        }

        private static string InfoText(PullResult state, int free)
        {
            if (state == PullResult.Full)
            {
                return $"持てるのは {Balance.MaxPenguins} 体まで。ペンモン画面で売ってください";
            }

            if (free > 0)
            {
                return "はじめに3体 無料で引けます";
            }

            return state == PullResult.NotEnoughCoins ? "コインが足りません。ステージで集めよう" : string.Empty;
        }

        private void Pull()
        {
            if (_playing != null)
            {
                return;
            }

            var penguin = Record.Pull(_random);
            if (penguin == null)
            {
                Refresh();
                return;
            }

            // 演出の途中でアプリを閉じても、引いた子が消えないように先に保存する
            GameSave.Save();
            _pulled = penguin;
            _pulledIsNew = CountOfSpecies(penguin.speciesNo) == 1;
            ClearResult();
            _playing = StartCoroutine(Play());
            Refresh();
        }

        private IEnumerator Play()
        {
            yield return _stage.Play(_pulled.speciesNo, PulledStars, ShowStars);
            ShowResult();
        }

        /// <summary>演出をとばして、出てきた子が台に立った所にする</summary>
        private void Skip()
        {
            if (_playing == null)
            {
                return;
            }

            StopCoroutine(_playing);
            _stage.Finish(_pulled.speciesNo, PulledStars);
            ShowStars(PulledStars);
            ShowResult();
        }

        private void ShowResult()
        {
            _playing = null;
            _card.Show(_pulled, _pulledIsNew);
            Refresh();
        }

        private void ClearResult()
        {
            _card.Hide();
            _starsText.text = string.Empty;
        }

        private void ShowStars(int count)
        {
            _starsText.color = PenguinLabels.StarColor(PulledStars);
            _starsText.text = PenguinLabels.Stars(count);
            if (_starPop != null)
            {
                StopCoroutine(_starPop);
            }

            _starPop = StartCoroutine(PopStars());
        }

        /// <summary>★が増えるたびに、小さい所から弾んで出る</summary>
        private IEnumerator PopStars()
        {
            var target = _starsText.rectTransform;
            for (var t = 0f; t < StarPopSeconds; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * ToyEasing.Pop(t / StarPopSeconds, StarPopStart);
                yield return null;
            }

            target.localScale = Vector3.one;
            _starPop = null;
        }

        /// <summary>引いた子を入れて数える（1 なら初めての種類）</summary>
        private static int CountOfSpecies(int speciesNo)
        {
            var count = 0;
            foreach (var penguin in Record.Penguins)
            {
                if (penguin.speciesNo == speciesNo)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
