using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// エリアの進み具合（右上の文字・画面の真ん中の知らせ）とボスの HP（§6.1）
    /// </summary>
    public partial class StageHud
    {
        private const float BannerSeconds = 1.5f;
        // 消える前のこの時間で薄くする
        private const float BannerFadeSeconds = 0.4f;

        [SerializeField] private Text _progressText;
        [SerializeField] private Text _bannerText;
        [SerializeField] private GameObject _bossBar;
        [SerializeField] private RectTransform _bossFill;
        [SerializeField] private Text _bossText;

        private float _bannerTimer;
        private int _announcedRound = -1;
        private int _announcedArea = -1;
        private bool _announcedGate;

        public void SetProgress(StageSnapshot snapshot)
        {
            var isBossArea = IsBossArea(snapshot);
            var area = isBossArea ? "ボスエリア" : $"エリア {snapshot.AreaIndex + 1} / {snapshot.AreaCount}";
            _progressText.text = snapshot.IsGateOpen ? "すすめ →" : $"{area}　のこり {snapshot.CountAlive(Team.Enemy)} 体";
            Announce(snapshot, isBossArea);
            SetBoss(snapshot);
        }

        private static bool IsBossArea(StageSnapshot snapshot) => snapshot.AreaCount > 1 && snapshot.AreaIndex == snapshot.AreaCount - 1;

        /// <summary>エリアに入ったとき・出入口が開いたときに、画面の真ん中に大きく出す</summary>
        private void Announce(StageSnapshot snapshot, bool isBossArea)
        {
            if (snapshot.Round != _announcedRound || snapshot.AreaIndex != _announcedArea)
            {
                _announcedRound = snapshot.Round;
                _announcedArea = snapshot.AreaIndex;
                _announcedGate = false;
                ShowBanner(isBossArea ? "ボスエリア！" : $"エリア {snapshot.AreaIndex + 1}");
                return;
            }

            if (snapshot.IsGateOpen && !_announcedGate)
            {
                _announcedGate = true;
                ShowBanner("すすめ →");
            }
        }

        private void ShowBanner(string message)
        {
            _bannerText.text = message;
            _bannerTimer = BannerSeconds;
        }

        private void UpdateBanner()
        {
            _bannerTimer -= Time.deltaTime;
            var color = _bannerText.color;
            color.a = Mathf.Clamp01(_bannerTimer / BannerFadeSeconds);
            _bannerText.color = color;
        }

        private void SetBoss(StageSnapshot snapshot)
        {
            var hasBoss = snapshot.TryGetBoss(out var boss);
            _bossBar.SetActive(hasBoss);
            if (!hasBoss)
            {
                return;
            }

            var ratio = boss.MaxHp > 0 ? (float)boss.Hp / boss.MaxHp : 0f;
            _bossFill.anchorMax = new Vector2(ratio, 1f);
            _bossText.text = $"ボス  {PenguinPalette.SpeciesName(boss.SpeciesNo)}";
        }
    }
}
