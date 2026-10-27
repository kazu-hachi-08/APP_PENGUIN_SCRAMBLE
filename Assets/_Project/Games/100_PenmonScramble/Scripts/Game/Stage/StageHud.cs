using System;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中の画面表示: 自分の HP・残りの敵・結果
    /// </summary>
    public class StageHud : MonoBehaviour
    {
        [SerializeField] private RectTransform _hpFill;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _enemiesText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        private string _playerLabel = string.Empty;

        public event Action RetryRequested;
        public event Action TitleRequested;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            _titleButton.onClick.AddListener(() => TitleRequested?.Invoke());
        }

        /// <summary>バーを自分のペンギンと同じ色にして、協力のときにどれが自分か分かるようにする</summary>
        public void SetPlayer(string label, Color color)
        {
            _hpFillImage.color = color;
            _playerLabel = label;
        }

        public void SetPlayerHp(int hp, int maxHp)
        {
            var ratio = maxHp > 0 ? (float)hp / maxHp : 0f;
            _hpFill.anchorMax = new Vector2(ratio, 1f);
            _hpText.text = $"{_playerLabel}  HP {hp} / {maxHp}";
        }

        public void SetEnemiesLeft(int count)
        {
            _enemiesText.text = $"のこり {count} 体";
        }

        /// <param name="canRetry">「もう一度」を押せるのはホストだけ（ゲストはホストが始め直すのを待つ）</param>
        public void ShowResult(bool cleared, bool canRetry)
        {
            var title = cleared ? "クリア！" : "やられた…";
            _resultText.text = canRetry ? title : $"{title}\n<size=48>ホストが「もう一度」を押すと始まります</size>";
            _retryButton.gameObject.SetActive(canRetry);
            _resultPanel.SetActive(true);
        }

        public void HideResult()
        {
            _resultPanel.SetActive(false);
        }
    }
}
