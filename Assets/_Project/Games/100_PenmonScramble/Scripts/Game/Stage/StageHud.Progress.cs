using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// エリアの進み具合（右上のつぶと残りの敵・画面の真ん中の知らせ）とボスの HP
    /// </summary>
    public partial class StageHud
    {
        private const float BannerSeconds = 1.5f;
        // 消える前のこの時間で薄くする
        private const float BannerFadeSeconds = 0.4f;
        // 出た瞬間に大きく出て、この時間で元の大きさへ縮む
        private const float BannerPopSeconds = 0.18f;
        private const float BannerPopScale = 0.35f;
        private const string GateOpenMessage = "すすめ →";
        // 残りの敵のアイコンは、雑魚と同じく暗くしたペンモンの顔
        private const int EnemyIconSpecies = 1;
        private static readonly Color WildFaceColor = Color.Lerp(Color.white, Color.black, UnitView.EnemyDarkness);

        [SerializeField] private HudAreaPips _areaPips;
        [SerializeField] private Image _enemyIcon;
        [SerializeField] private Text _enemyText;
        [SerializeField] private Color _enemyTextColor = Color.white;
        [SerializeField] private Color _gateTextColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private Text _bannerText;
        [SerializeField] private GameObject _bossPlate;
        [SerializeField] private HudBar _bossBar;
        [SerializeField] private Image _bossFace;
        [SerializeField] private Text _bossText;

        private float _bannerTimer;
        private int _announcedRound = -1;
        private int _announcedArea = -1;
        private bool _announcedGate;
        private StageSnapshot _comboSnapshot;
        private int _bossId = -1;

        private void Start()
        {
            CreatureArt.ShowFace(_enemyIcon, EnemyIconSpecies);
            _enemyIcon.color = WildFaceColor;
        }

        public void SetProgress(StageSnapshot snapshot)
        {
            _areaPips.Set(snapshot.AreaIndex, snapshot.AreaCount);
            var gateOpen = snapshot.IsGateOpen;
            _enemyIcon.gameObject.SetActive(!gateOpen);
            _enemyText.text = gateOpen ? GateOpenMessage : $"のこり {snapshot.CountAlive(Team.Enemy)}";
            _enemyText.color = gateOpen ? _gateTextColor : _enemyTextColor;
            Announce(snapshot, snapshot.IsBossArea);
            AnnounceCombos(snapshot);
            SetBoss(snapshot);
        }

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
                ShowBanner(GateOpenMessage);
            }
        }

        /// <summary>同じ回にいくつ起きても、最後の1つだけ出す（文字が重なって読めなくなるため）</summary>
        private void AnnounceCombos(StageSnapshot snapshot)
        {
            // HUD は毎フレーム、同じスナップショットで呼ばれる。出し直して消えなくならないよう、1つのスナップショットで1回だけ
            if (ReferenceEquals(snapshot, _comboSnapshot))
            {
                return;
            }

            _comboSnapshot = snapshot;
            if (snapshot.Combos.Count > 0)
            {
                var last = snapshot.Combos[snapshot.Combos.Count - 1];
                ShowBanner($"合体！{ComboMoves.NameOf(last.Kind)}");
            }
        }

        private void ShowBanner(string message)
        {
            _bannerText.text = message;
            _bannerTimer = BannerSeconds;
        }

        private void UpdateBanner()
        {
            _bannerTimer -= Time.unscaledDeltaTime;
            var color = _bannerText.color;
            color.a = Mathf.Clamp01(_bannerTimer / BannerFadeSeconds);
            _bannerText.color = color;
            var pop = Mathf.Clamp01((_bannerTimer - (BannerSeconds - BannerPopSeconds)) / BannerPopSeconds);
            _bannerText.rectTransform.localScale = Vector3.one * (1f + BannerPopScale * pop * pop);
        }

        /// <summary>ボスが出たら、上から板が滑り込む（UiSlideIn。出し直すたびに動く）</summary>
        private void SetBoss(StageSnapshot snapshot)
        {
            var hasBoss = snapshot.TryGetBoss(out var boss);
            if (!hasBoss)
            {
                _bossId = -1;
                _bossPlate.SetActive(false);
                return;
            }

            var ratio = HpRatio(boss.Hp, boss.MaxHp);
            if (boss.Id != _bossId)
            {
                _bossId = boss.Id;
                _bossPlate.SetActive(true);
                CreatureArt.ShowFace(_bossFace, boss.SpeciesNo);
                // ステージのボスは野生のペンモンなので暗く、大ボスはそのままの色
                _bossFace.color = BigBossTable.Get(boss.SpeciesNo) == null ? WildFaceColor : Color.white;
                _bossText.text = $"BOSS  {PenguinPalette.SpeciesName(boss.SpeciesNo)}";
                _bossBar.Snap(ratio);
                return;
            }

            _bossBar.SetRatio(ratio);
        }
    }
}
