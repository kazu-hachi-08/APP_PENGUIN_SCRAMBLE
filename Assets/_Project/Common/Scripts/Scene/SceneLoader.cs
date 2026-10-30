using System;
using System.Collections;
using PenmonScramble.Common.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenmonScramble.Common.Scene
{
    /// <summary>
    /// シーン非同期ロードおよび画面遷移マネージャー
    /// </summary>
    public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
    {
        [Header("References")]
        [SerializeField] private FadeController _fadeController;

        public bool IsLoading { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (_fadeController == null)
            {
                _fadeController = GetComponentInChildren<FadeController>();
                if (_fadeController == null)
                {
                    // フェード用のCanvas/GameObjectを自動生成
                    var fadeObj = new GameObject("FadeCanvas");
                    fadeObj.transform.SetParent(transform);
                    var canvas = fadeObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 999; // 最前面
                    fadeObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    fadeObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                    var imageObj = new GameObject("FadeImage");
                    imageObj.transform.SetParent(fadeObj.transform, false);
                    var img = imageObj.AddComponent<UnityEngine.UI.Image>();
                    img.color = Color.black;
                    var rect = img.rectTransform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;

                    _fadeController = fadeObj.AddComponent<FadeController>();
                }
            }
        }

        /// <summary>
        /// フェード演出を挟んで指定したシーンへ遷移
        /// </summary>
        public void LoadScene(string sceneName, float fadeDuration = 0.4f, Action onLoaded = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] 既にロード処理が進行中です: {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, fadeDuration, onLoaded));
        }

        /// <summary>
        /// 現在のシーンをリスタート
        /// </summary>
        public void RestartCurrentScene(float fadeDuration = 0.3f, Action onLoaded = null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            LoadScene(currentSceneName, fadeDuration, onLoaded);
        }

        /// <summary>
        /// タイトルシーンへ戻る
        /// </summary>
        public void LoadTitleScene(float fadeDuration = 0.4f, Action onLoaded = null)
        {
            LoadScene(SceneNames.Title, fadeDuration, onLoaded);
        }

        private IEnumerator LoadSceneRoutine(string sceneName, float fadeDuration, Action onLoaded)
        {
            IsLoading = true;

            // 1. フェードアウト（暗転）
            if (_fadeController != null)
            {
                yield return _fadeController.FadeOutCoroutine(fadeDuration);
            }

            // タイムスケールを通常に戻す（ポーズ中のリスタート等に対応）
            Time.timeScale = 1.0f;

            // 2. シーン非同期ロード
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogError($"[SceneLoader] シーン '{sceneName}' のロードに失敗しました。Build Settingsに追加されているか確認してください。");
            }

            // 1フレーム待機して初期化を確実にする
            yield return null;

            onLoaded?.Invoke();

            // 3. フェードイン（明転）
            if (_fadeController != null)
            {
                yield return _fadeController.FadeInCoroutine(fadeDuration);
            }

            IsLoading = false;
        }
    }
}
