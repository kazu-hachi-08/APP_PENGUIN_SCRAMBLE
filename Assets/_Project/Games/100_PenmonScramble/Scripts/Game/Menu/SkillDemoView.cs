using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技の実演の小窓（§7）。本番と同じ計算の練習場（SkillPractice）を進め、
    /// 実演用の StageView（_preview）で描いたものを専用のカメラで撮って、RawImage に映す。音は出さない。
    /// 練習場は3Dの空間の離れた所にあるので、タイトルのジオラマとは映り合わない
    /// </summary>
    public sealed class SkillDemoView : MonoBehaviour
    {
        // 看板（ダメージの数字・光）はカメラの向きにそろうので、タイトルのジオラマと同じ角度にする（FaceCamera.Override）
        private const float CameraPitch = TitleDiorama.CameraPitch;
        private const float CameraFieldOfView = TitleDiorama.CameraFieldOfView;
        private const float CameraMargin = 0f;
        private const int DepthBits = 24;
        private const int MaxTicksPerFrame = 5;

        [SerializeField] private RawImage _screen;
        [SerializeField] private Camera _camera;
        [SerializeField] private StageView _view;

        private SkillPractice _practice;
        private StageRecorder _recorder;
        private RenderTexture _texture;
        private float _accumulator;
        private int _round;

        /// <summary>この子の、この技（0 が技1）の実演を最初から始める</summary>
        public void Play(UnitStats penguin, int slot)
        {
            _recorder?.Detach();
            _practice = new SkillPractice(penguin, slot);
            // 練習場を作り直すたびに番号を進めて、前の実演の物を StageView に全部消させる
            _recorder = new StageRecorder(_practice.World, ++_round);
            _accumulator = 0f;
            _view.gameObject.SetActive(true);
            _camera.enabled = true;
            FaceCamera.Override = _camera;
            PrepareCamera();
            _view.Apply(_recorder.Capture());
            _view.Render(1f);
        }

        public void Stop()
        {
            _recorder?.Detach();
            _recorder = null;
            _practice = null;
            if (_camera != null) _camera.enabled = false;
            if (_view != null) _view.gameObject.SetActive(false);
            if (FaceCamera.Override == _camera) FaceCamera.Override = null;
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }

        private void Update()
        {
            if (_practice == null)
            {
                return;
            }

            _accumulator += Time.deltaTime;
            var ticks = 0;
            while (_accumulator >= Balance.TickSeconds && ticks < MaxTicksPerFrame)
            {
                _practice.Tick();
                _view.Apply(_recorder.Capture());
                _accumulator -= Balance.TickSeconds;
                ticks++;
            }

            // 重くて追いつけないときは、溜まった分を捨てて早送りしない
            _accumulator = Mathf.Min(_accumulator, Balance.TickSeconds);
            _view.Render(_accumulator / Balance.TickSeconds);
        }

        /// <summary>小窓の大きさの絵を撮るようにして、練習場の映す範囲がちょうど収まる位置に構える</summary>
        private void PrepareCamera()
        {
            EnsureTexture();
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = StageTheme.ForWorld(StageView.PreviewWorld).Background;
            var center = (SkillPractice.Center + SkillPractice.ViewOffset).ToVector2().ToFloor();
            AreaCameraFit.FitPerspective(_camera, center, SkillPractice.ViewSize.ToVector2(), CameraPitch, CameraFieldOfView, StageCamera.Headroom, CameraMargin);
        }

        /// <summary>画面の実際の画素数で撮る（基準解像度のままだと、大きな画面でぼやける）</summary>
        private void EnsureTexture()
        {
            var canvas = _screen.canvas;
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            var size = _screen.rectTransform.rect.size * scale;
            var width = Mathf.Max(1, Mathf.RoundToInt(size.x));
            var height = Mathf.Max(1, Mathf.RoundToInt(size.y));
            if (_texture != null && _texture.width == width && _texture.height == height)
            {
                return;
            }

            if (_texture != null)
            {
                _camera.targetTexture = null;
                _texture.Release();
                Destroy(_texture);
            }

            _texture = new RenderTexture(width, height, DepthBits)
            {
                name = "SkillDemo",
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing),
            };
            _screen.texture = _texture;
        }
    }
}
