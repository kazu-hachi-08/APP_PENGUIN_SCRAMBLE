using UnityEngine;

namespace PenmonScramble.Common.Core
{
    /// <summary>
    /// アプリ終了中かどうか（全シングルトン共通）。
    /// Domain Reload を切っていると static が前回の再生から残る。SingletonMonoBehaviour&lt;T&gt; は総称型なので
    /// RuntimeInitializeOnLoadMethod を置けず、型ごとのフラグだと「前回の再生終了で true のまま、今回 Awake が来ない型」の
    /// Instance が null を返してしまうため、非総称のここで再生開始ごとに必ず戻す
    /// </summary>
    internal static class SingletonLifecycle
    {
        public static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => IsQuitting = true;
    }

    /// <summary>
    /// 型安全なSingleton MonoBehaviour基底クラス
    /// </summary>
    /// <typeparam name="T">継承先クラスの型</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        [Header("Singleton Settings")]
        [SerializeField] private bool _dontDestroyOnLoad = true;

        public static T Instance
        {
            get
            {
                // 終了中に作ると、シーンの後片付けのあとに GameObject が残ってしまう
                if (SingletonLifecycle.IsQuitting)
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

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
