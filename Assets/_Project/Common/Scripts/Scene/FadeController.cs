using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinScramble.Common.Scene
{
    /// <summary>
    /// 画面暗転・明転（フェード）を制御するコンポーネント
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _defaultDuration = 0.5f;

        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_fadeImage == null)
            {
                _fadeImage = GetComponentInChildren<Image>();
            }

            // 初期状態は透明 & クリックブロック解除
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 画面をフェードアウト（暗転）
        /// </summary>
        public IEnumerator FadeOutCoroutine(float duration = -1f, Action onComplete = null)
        {
            float targetDuration = duration > 0 ? duration : _defaultDuration;
            yield return FadeRoutine(0f, 1f, targetDuration);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 画面をフェードイン（明転）
        /// </summary>
        public IEnumerator FadeInCoroutine(float duration = -1f, Action onComplete = null)
        {
            float targetDuration = duration > 0 ? duration : _defaultDuration;
            yield return FadeRoutine(1f, 0f, targetDuration);
            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
        {
            IsFading = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = fromAlpha;

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                    yield return null;
                }

                _canvasGroup.alpha = toAlpha;
                _canvasGroup.blocksRaycasts = toAlpha > 0.01f;
            }
            IsFading = false;
        }
    }
}
