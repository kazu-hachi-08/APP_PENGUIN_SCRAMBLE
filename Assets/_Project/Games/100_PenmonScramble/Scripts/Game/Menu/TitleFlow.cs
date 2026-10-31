using PenmonScramble.Common.Profile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルの2段: 起動したら大きなロゴと「タップしてスタート」だけを見せ、押すとロビー（TitleMenu）へ移る。
    /// ステージから戻ったときは、もう一度押させずにロビーから始める。
    /// ロビーに入ったら、名前がまだ無ければ名前を決めてもらい、そのあと無料ガチャがあれば開く
    /// </summary>
    public class TitleFlow : MonoBehaviour
    {
        private const string TouchPrompt = "画面をタップしてスタート";
        private const string DesktopPrompt = "クリック / Enter でスタート";

        [SerializeField] private GameObject _splash;
        [Tooltip("画面全体を覆う透明なボタン。どこを押してもスタートする")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Text _promptText;
        [SerializeField] private GameObject _lobby;
        [SerializeField] private TitleLogoMotion _logo;
        [SerializeField] private Vector2 _splashLogoPosition = new Vector2(0f, 200f);
        [SerializeField] private Vector2 _lobbyLogoPosition = new Vector2(0f, 420f);
        [SerializeField] private float _lobbyLogoScale = 0.5f;
        [SerializeField] private TitleMenu _menu;
        [SerializeField] private NameEntryDialog _nameEntry;
        [SerializeField] private GachaScreen _gacha;

        // シーンを読み直しても（ステージから戻っても）覚えておく。アプリを起動し直したら、また見せる
        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // エディタで「ドメインを読み直さない」設定でも、再生のたびにスタート画面から始める
            _started = false;
        }

        private void Awake()
        {
            _startButton.onClick.AddListener(Begin);
            _promptText.text = Application.isMobilePlatform ? TouchPrompt : DesktopPrompt;
        }

        private void Start()
        {
            if (_started)
            {
                EnterLobby(instant: true);
                return;
            }

            _splash.SetActive(true);
            _lobby.SetActive(false);
            _logo.SetPose(_splashLogoPosition, 1f, instant: true);
        }

        private void Update()
        {
            if (_splash.activeSelf && StartKeyPressed())
            {
                Begin();
            }
        }

        /// <summary>キーボードはどのキーでも、コントローラーは決定（×）か OPTIONS</summary>
        private static bool StartKeyPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame);
        }

        private void Begin()
        {
            if (!_splash.activeSelf)
            {
                return;
            }

            _started = true;
            GameSounds.Play(GameSound.TitleStart);
            EnterLobby(instant: false);
        }

        private void EnterLobby(bool instant)
        {
            _splash.SetActive(false);
            _lobby.SetActive(true);
            _logo.SetPose(_lobbyLogoPosition, _lobbyLogoScale, instant);
            if (!UserProfile.HasName)
            {
                // 初回は決めるまで閉じられない（協力で頭の上に出す名前なので、最初に決めておく）
                _nameEntry.Show(OnFirstNameDecided);
                return;
            }

            OpenFreeGacha();
        }

        private void OnFirstNameDecided()
        {
            _menu.Refresh();
            OpenFreeGacha();
        }

        /// <summary>初回は無料ガチャを3回引くまで閉じられない。引いた3体がそのまま編成になる</summary>
        private void OpenFreeGacha()
        {
            if (GameSave.Record.FreePullsLeft > 0)
            {
                _gacha.Open();
            }
        }
    }
}
