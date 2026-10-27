using UnityEngine;

namespace PenmonScramble.Common.Core
{
    /// <summary>
    /// 型安全なSingleton MonoBehaviour基底クラス
    /// </summary>
    /// <typeparam name="T">継承先クラスの型</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isApplicationQuitting = false;

        [Header("Singleton Settings")]
        [SerializeField] private bool _dontDestroyOnLoad = true;

        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject($"[{typeof(T).Name}]");
                            _instance = singletonObject.AddComponent<T>();
                        }
                    }

                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            // Domain Reloadが無効な設定だと、前回Play終了時のフラグが残ったままになるため
            // 新しいセッション開始（Awake）のたびに必ずリセットする
            _isApplicationQuitting = false;

            if (_instance == null)
            {
                _instance = this as T;
                if (_dontDestroyOnLoad && transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] インスタンスが重複して生成されたため破棄します: {gameObject.name}");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
