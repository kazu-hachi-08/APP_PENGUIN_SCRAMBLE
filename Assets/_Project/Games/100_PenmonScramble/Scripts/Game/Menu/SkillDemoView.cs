using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技の実演の小窓。本番と同じ計算の練習場（SkillPractice）を進め、
    /// 実演用の StageView（_preview）で描いたものを専用のカメラで撮って、RawImage に映す。音は出さない。
    /// 練習場は3Dの空間の離れた所にあるので、タイトルのジオラマとは映り合わない
    /// </summary>
    public sealed class SkillDemoView : MonoBehaviour
    {
        // 看板（ダメージの数字・光）はカメラの向きにそろうので、タイトルのジオラマと同じ角度にする（FaceCamera.Override）
        private const float CameraPitch = TitleDiorama.CameraPitch;
        private const float CameraFieldOfView = TitleDiorama.CameraFieldOfView;
        private const float CameraMargin = 0f;

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

        private void OnDestroy() => AreaCameraFit.ReleaseTexture(ref _texture);

        private void Update()
        {
            if (_practice == null)
            {
                return;
            }

            var interpolation = StageHost.StepFixedTicks(ref _accumulator, Time.deltaTime, null, () =>
            {
                _practice.Tick();
                _view.Apply(_recorder.Capture());
            });
            _view.Render(interpolation);
        }

        /// <summary>小窓の大きさの絵を撮るようにして、練習場の映す範囲がちょうど収まる位置に構える</summary>
        private void PrepareCamera()
        {
            AreaCameraFit.EnsureScreenTexture(ref _texture, _screen, _camera, "SkillDemo");
            _camera.targetTexture = _texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = StageTheme.ForWorld(StageView.PreviewWorld).Background;
            var center = (SkillPractice.Center + SkillPractice.ViewOffset).ToVector2().ToFloor();
            AreaCameraFit.FitPerspective(_camera, center, SkillPractice.ViewSize.ToVector2(), CameraPitch, CameraFieldOfView, StageCamera.Headroom, CameraMargin);
        }
    }
}
