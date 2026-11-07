using System;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果のコイン: 見出しのあたりからぱっと散って、少し遅れてコインの札のアイコンへ吸い込まれる。
    /// 1枚届くたびに Arrived を呼ぶ（受け取った側が札をふくらませ、音を鳴らす）。コインは最初にまとめて作って使い回す
    /// </summary>
    public class ResultCoinBurst : MonoBehaviour
    {
        private const int CoinCount = 12;
        private const float CoinSize = 56f;
        private const float ScatterSpeedMin = 500f;
        private const float ScatterSpeedMax = 900f;
        private const float ScatterDrag = 5f;
        // 散ってから吸い込まれ始めるまで（1枚ずつずらす）と、吸い込まれる長さ（秒）
        private const float HomeDelayMin = 0.25f;
        private const float HomeDelayStep = 0.04f;
        private const float HomeSeconds = 0.35f;

        [SerializeField] private Sprite _coinSprite;

        private Coin[] _coins;
        private RectTransform _target;

        /// <summary>1枚届いた</summary>
        public event Action Arrived;

        private struct Coin
        {
            public RectTransform Rect;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float HomeAt;
            public Vector2 HomeFrom;
        }

        /// <param name="from">散らす場所（ワールド座標）</param>
        /// <param name="target">吸い込まれる先（コインの札のアイコン）</param>
        public void Play(Vector3 from, RectTransform target)
        {
            Build();
            _target = target;
            Vector2 start = transform.InverseTransformPoint(from);
            for (var i = 0; i < _coins.Length; i++)
            {
                var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                ref var coin = ref _coins[i];
                coin.Position = start;
                coin.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(ScatterSpeedMin, ScatterSpeedMax);
                coin.Age = 0f;
                coin.HomeAt = HomeDelayMin + HomeDelayStep * i;
                coin.Rect.anchoredPosition = start;
                coin.Rect.localScale = Vector3.one;
                coin.Rect.gameObject.SetActive(true);
            }
        }

        /// <summary>結果を飛ばした・閉じたとき。飛んでいる途中のコインを消す</summary>
        public void Clear()
        {
            if (_coins == null)
            {
                return;
            }

            foreach (var coin in _coins)
            {
                coin.Rect.gameObject.SetActive(false);
            }
        }

        private void Build()
        {
            if (_coins != null)
            {
                return;
            }

            _coins = new Coin[CoinCount];
            for (var i = 0; i < CoinCount; i++)
            {
                var obj = new GameObject($"Coin{i}", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(transform, false);
                var image = obj.GetComponent<Image>();
                image.sprite = _coinSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                ((RectTransform)obj.transform).sizeDelta = Vector2.one * CoinSize;
                obj.SetActive(false);
                _coins[i] = new Coin { Rect = (RectTransform)obj.transform };
            }
        }

        private void Update()
        {
            if (_coins == null || _target == null)
            {
                return;
            }

            Vector2 target = transform.InverseTransformPoint(_target.position);
            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _coins.Length; i++)
            {
                ref var coin = ref _coins[i];
                if (coin.Rect.gameObject.activeSelf)
                {
                    Step(ref coin, target, dt);
                }
            }
        }

        private void Step(ref Coin coin, Vector2 target, float dt)
        {
            coin.Age += dt;
            if (coin.Age < coin.HomeAt)
            {
                coin.Velocity *= Mathf.Exp(-ScatterDrag * dt);
                coin.Position += coin.Velocity * dt;
                coin.HomeFrom = coin.Position;
                coin.Rect.anchoredPosition = coin.Position;
                return;
            }

            var t = (coin.Age - coin.HomeAt) / HomeSeconds;
            if (t >= 1f)
            {
                coin.Rect.gameObject.SetActive(false);
                Arrived?.Invoke();
                return;
            }

            // だんだん速く吸い込まれ、届く手前で小さくなる
            coin.Rect.anchoredPosition = Vector2.Lerp(coin.HomeFrom, target, t * t);
            coin.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
        }
    }
}
