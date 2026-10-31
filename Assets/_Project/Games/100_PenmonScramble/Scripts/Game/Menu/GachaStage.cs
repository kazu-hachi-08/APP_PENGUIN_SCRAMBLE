using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャ画面の3D（§7）: ガラスの丸い窓にカプセルが詰まったガチャマシンと、引いた子が立つ台。
    /// 3Dの空間の離れた所に組み、専用のカメラで撮って画面いっぱいの RawImage に映す（後ろのタイトルを隠して、押せないようにする）。
    /// 引く流れは GachaStage.Reveal.cs、マシンの組み立ては GachaStage.Machine.cs。
    /// 見た目だけなので Core のステージは使わず、乱数も UnityEngine.Random
    /// </summary>
    public sealed partial class GachaStage : MonoBehaviour
    {
        // タイトルのジオラマ（原点）と技の実演の練習場（奥 1000）から離して、映り合わないようにする
        private static readonly Vector3 Origin = new Vector3(1000f, 0f, 0f);
        // 引いた子が立つ台。マシンの右手前
        private static readonly Vector3 RevealSpot = new Vector3(2.1f, 0f, -1.4f);

        // 少しだけ見下ろす（台の上の子の顔が正面から見える）
        private const float CameraPitch = 10f;
        private const float CameraFieldOfView = 30f;
        // 引く前: マシン全体を画面の左寄りに。引いた後: 台の子に寄る（右には結果のカードが出る）
        private static readonly Vector3 WideFocus = new Vector3(0f, 1.6f, 0f);
        private const float WideDistance = 11f;
        private const float WideViewportX = 0.3f;
        private static readonly Vector3 CloseFocus = RevealSpot + new Vector3(0f, 0.75f, 0f);
        private const float CloseDistance = 6f;
        private const float CloseViewportX = 0.36f;
        private const int DepthBits = 24;

        private static readonly Color BackgroundColor = new Color(0.13f, 0.15f, 0.32f);
        private static readonly Color RareBackgroundColor = new Color(0.03f, 0.03f, 0.08f);

        [SerializeField] private RawImage _screen;
        [SerializeField] private Camera _camera;
        // シェーダーはシーンの参照で持つ（Shader.Find だとビルドに入らないことがあるため）
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;
        [SerializeField] private Shader _fxShader;

        private StageMaterials _materials;
        private RenderTexture _texture;
        private Transform _world;
        private Shot _shot;

        /// <summary>カメラの構え（見る点・距離・その点を画面の横のどこに置くか）</summary>
        private readonly struct Shot
        {
            public readonly Vector3 Focus;
            public readonly float Distance;
            public readonly float ViewportX;

            public Shot(Vector3 focus, float distance, float viewportX)
            {
                Focus = focus;
                Distance = distance;
                ViewportX = viewportX;
            }

            public static Shot Lerp(Shot a, Shot b, float t)
            {
                return new Shot(Vector3.Lerp(a.Focus, b.Focus, t), Mathf.Lerp(a.Distance, b.Distance, t), Mathf.Lerp(a.ViewportX, b.ViewportX, t));
            }
        }

        private static Shot Wide => new Shot(Origin + WideFocus, WideDistance, WideViewportX);
        private static Shot Close => new Shot(Origin + CloseFocus, CloseDistance, CloseViewportX);

        /// <summary>画面を開いたときに組み立てて、カメラを点ける</summary>
        public void Show()
        {
            _materials ??= new StageMaterials(_toonShader, _blobShadowShader, _fxShader);
            if (_world == null)
            {
                _world = new GameObject("GachaStage").transform;
                _world.position = Origin;
                BuildRoom();
                BuildMachine();
            }

            ResetReveal();
            PrepareCamera();
            SetShot(Wide);
            _camera.enabled = true;
        }

        /// <summary>画面を閉じたら全部消す（開いていない間は何も描かない）</summary>
        public void Hide()
        {
            if (_camera != null) _camera.enabled = false;
            if (_world != null)
            {
                Destroy(_world.gameObject);
                _world = null;
            }

            ForgetReveal();
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            _materials?.DestroyAll();
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        private void Update()
        {
            if (_world == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            TickRoom(dt);
            TickMachine(dt);
            TickRevealIdle(dt);
        }

        private void LateUpdate()
        {
            if (_world != null)
            {
                FaceGlows();
            }
        }

        /// <summary>画面の実際の画素数で撮る（基準解像度のままだと、大きな画面でぼやける）</summary>
        private void PrepareCamera()
        {
            _camera.orthographic = false;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BackgroundColor;
            var canvas = _screen.canvas;
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            var size = _screen.rectTransform.rect.size * scale;
            var width = Mathf.Max(1, Mathf.RoundToInt(size.x));
            var height = Mathf.Max(1, Mathf.RoundToInt(size.y));
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                ReplaceTexture(width, height);
            }

            _camera.targetTexture = _texture;
            _camera.aspect = width / (float)height;
        }

        private void ReplaceTexture(int width, int height)
        {
            if (_texture != null)
            {
                _camera.targetTexture = null;
                _texture.Release();
                Destroy(_texture);
            }

            _texture = new RenderTexture(width, height, DepthBits)
            {
                name = "Gacha",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing),
            };
            _screen.texture = _texture;
        }

        /// <summary>見る点が画面の横 ViewportX の所に来るよう、カメラを横へずらして構える（画面の縦横比が変わっても、マシンが右のボタンに隠れない）</summary>
        private void SetShot(Shot shot)
        {
            _shot = shot;
            var rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
            var halfWidth = shot.Distance * Mathf.Tan(CameraFieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect;
            var shift = (0.5f - shot.ViewportX) * 2f * halfWidth;
            var position = shot.Focus - rotation * Vector3.forward * shot.Distance + rotation * Vector3.right * shift;
            _camera.transform.SetPositionAndRotation(position, rotation);
        }

        private void SetBackground(Color color) => _camera.backgroundColor = color;

        /// <summary>ガチャの部屋の中の位置（マシンの足元が原点）を、3Dの空間の位置にする</summary>
        private static Vector3 World(Vector3 local) => Origin + local;

        private static Vector2 Floor(Vector3 world) => new Vector2(world.x, world.z);
    }
}
