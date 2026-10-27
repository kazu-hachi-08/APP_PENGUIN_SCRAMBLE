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
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _enemiesText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _retryButton;

        public event Action RetryRequested;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
        }

        public void SetPlayerHp(int hp, int maxHp)
        {
            var ratio = maxHp > 0 ? (float)hp / maxHp : 0f;
            _hpFill.anchorMax = new Vector2(ratio, 1f);
            _hpText.text = $"HP {hp} / {maxHp}";
        }

        public void SetEnemiesLeft(int count)
        {
            _enemiesText.text = $"のこり {count} 体";
        }

        public void ShowResult(bool cleared)
        {
            _resultText.text = cleared ? "クリア！" : "やられた…";
            _resultPanel.SetActive(true);
        }

        public void HideResult()
        {
            _resultPanel.SetActive(false);
        }
    }
}
