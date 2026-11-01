using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニューの後ろ。いつも暗い幕を掛け、ソロ（止まった画面）ではその1枚を撮ってぼかした絵を敷く。
    /// ぼかしは撮った絵を半分ずつ縮めて引き伸ばすだけ（後処理のシェーダーを足さずに、スマホ・ブラウザでも軽く出せる）。
    /// 協力は後ろが動き続けるので、撮った絵を敷くと止まって見えてしまう。暗い幕だけにする
    /// </summary>
    public class StageMenuBackdrop : MonoBehaviour
    {
        [SerializeField] private RawImage _blurred;
        [SerializeField] private Graphic _dim;
        [Tooltip("半分に縮める回数。多いほど強くぼける")]
        [SerializeField] private int _halvings = 4;

        private RenderTexture _texture;
        private Coroutine _capture;

        /// <summary>
        /// 撮る間はメニューを出さない（メニューごと撮ってしまうため）。撮り終わったら、または撮らないならすぐに onReady を呼ぶ
        /// </summary>
        public void Show(bool blur, Action onReady)
        {
            gameObject.SetActive(true);
            Release();
            _blurred.enabled = false;
            // 撮る絵に幕まで写ると、敷いた上にもう一度幕を掛けて暗くなりすぎるので、撮り終わるまで外す
            _dim.enabled = !blur;
            if (!blur)
            {
                onReady();
                return;
            }

            _capture = StartCoroutine(Capture(onReady));
        }

        public void Hide()
        {
            Release();
            gameObject.SetActive(false);
        }

        private void OnDestroy() => Release();

        private IEnumerator Capture(Action onReady)
        {
            // 描き終わった絵を撮る（UI まで描いた後でないと、画面と同じにならない）
            yield return new WaitForEndOfFrame();
            _capture = null;
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            _texture = Shrink(shot);
            Destroy(shot);
            _blurred.texture = _texture;
            _blurred.enabled = true;
            _dim.enabled = true;
            onReady();
        }

        /// <summary>
        /// 一度に小さくすると間の画素を飛ばしてちらつくので、半分ずつ縮める（毎回となりの画素と混ぜるので、なめらかにぼける）
        /// </summary>
        private RenderTexture Shrink(Texture source)
        {
            Texture current = source;
            RenderTexture previous = null;
            for (var i = 0; i < _halvings; i++)
            {
                var next = new RenderTexture(Mathf.Max(1, current.width / 2), Mathf.Max(1, current.height / 2), 0)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                Graphics.Blit(current, next);
                if (previous != null)
                {
                    Destroy(previous);
                }

                previous = next;
                current = next;
            }

            return previous;
        }

        private void Release()
        {
            if (_capture != null)
            {
                StopCoroutine(_capture);
                _capture = null;
            }

            if (_texture != null)
            {
                _blurred.texture = null;
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}
