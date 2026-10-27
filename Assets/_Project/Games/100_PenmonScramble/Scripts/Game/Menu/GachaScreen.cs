using System;
using System.Collections;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャ（§7）。引くと、カプセルが揺れて ★の色に光り、★が1つずつ出てから中の子が出てくる。
    /// 初回は無料の3回を引き終わるまで「もどる」を出さない（3体そろえてから始めるため）
    /// </summary>
    public class GachaScreen : MonoBehaviour
    {
        private const float ShakeSeconds = 0.8f;
        private const float ShakeAngle = 15f;
        private const float ShakesPerSecond = 6f;
        private const float StarIntervalSeconds = 0.3f;
        private const float PopSeconds = 0.25f;
        private const float PopOvershoot = 1.25f;

        [SerializeField] private Text _coinText;
        [SerializeField] private Text _infoText;
        [SerializeField] private Button _pullButton;
        [SerializeField] private Text _pullLabel;
        [SerializeField] private Button _backButton;
        [SerializeField] private RectTransform _capsule;
        [SerializeField] private Image _capsuleImage;
        [SerializeField] private Image _glow;
        [SerializeField] private Text _starsText;
        [SerializeField] private RectTransform _penguin;
        [SerializeField] private Image _penguinImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Color _capsuleColor = Color.white;

        private readonly System.Random _random = new System.Random();
        private bool _playing;

        public event Action Closed;

        private static PlayerRecord Record => GameSave.Record;

        private void Awake()
        {
            _pullButton.onClick.AddListener(Pull);
            _backButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ResetStage();
            Refresh();
        }

        private void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void Refresh()
        {
            _coinText.text = $"コイン {Record.Coins}";
            var free = Record.FreePullsLeft;
            _pullLabel.text = free > 0 ? $"無料で引く\n<size=36>のこり {free} 回</size>" : $"1回引く\n<size=36>{Balance.GachaCost} コイン</size>";
            var state = Record.CanPull();
            _pullButton.interactable = !_playing && state == PullResult.Ok;
            _backButton.gameObject.SetActive(!_playing && free == 0);
            if (!_playing)
            {
                _infoText.text = InfoText(state, free);
            }
        }

        private static string InfoText(PullResult state, int free)
        {
            if (state == PullResult.Full)
            {
                return $"持てるのは {Balance.MaxPenguins} 体まで。ペンギン一覧で売ってください";
            }

            if (free > 0)
            {
                return "はじめに3体 無料で引けます";
            }

            return state == PullResult.NotEnoughCoins ? "コインが足りません。ステージで集めよう" : string.Empty;
        }

        private void Pull()
        {
            if (_playing)
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
            ResetStage();
            StartCoroutine(Play(penguin));
        }

        private IEnumerator Play(OwnedPenguin penguin)
        {
            _playing = true;
            _infoText.text = string.Empty;
            Refresh();
            var species = SpeciesTable.Get(penguin.speciesNo);
            var starColor = PenguinLabels.StarColor(species.Stars);

            yield return Shake();
            _capsuleImage.color = starColor;
            _glow.color = starColor;
            _glow.gameObject.SetActive(true);
            yield return ShowStars(species.Stars, starColor);

            _capsule.gameObject.SetActive(false);
            _penguin.gameObject.SetActive(true);
            _penguinImage.color = PenguinPalette.SpeciesColor(species.No);
            _nameText.text = $"{PenguinLabels.Title(species)}\nパワー {penguin.power}";
            yield return Pop(_penguin);

            _playing = false;
            Refresh();
        }

        private IEnumerator Shake()
        {
            for (var t = 0f; t < ShakeSeconds; t += Time.deltaTime)
            {
                var angle = Mathf.Sin(t * ShakesPerSecond * Mathf.PI * 2f) * ShakeAngle * (t / ShakeSeconds);
                _capsule.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            _capsule.localRotation = Quaternion.identity;
        }

        private IEnumerator ShowStars(int stars, Color color)
        {
            _starsText.color = color;
            for (var i = 1; i <= stars; i++)
            {
                _starsText.text = PenguinLabels.Stars(i);
                yield return new WaitForSeconds(StarIntervalSeconds);
            }
        }

        /// <summary>小さい所から少し大きくなりすぎて、元の大きさに戻る</summary>
        private static IEnumerator Pop(RectTransform target)
        {
            for (var t = 0f; t < PopSeconds; t += Time.deltaTime)
            {
                var ratio = t / PopSeconds;
                var scale = ratio < 0.7f ? Mathf.Lerp(0f, PopOvershoot, ratio / 0.7f) : Mathf.Lerp(PopOvershoot, 1f, (ratio - 0.7f) / 0.3f);
                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private void ResetStage()
        {
            StopAllCoroutines();
            _playing = false;
            _capsule.gameObject.SetActive(true);
            _capsule.localRotation = Quaternion.identity;
            _capsuleImage.color = _capsuleColor;
            _glow.gameObject.SetActive(false);
            _starsText.text = string.Empty;
            _penguin.gameObject.SetActive(false);
            _nameText.text = string.Empty;
        }
    }
}
