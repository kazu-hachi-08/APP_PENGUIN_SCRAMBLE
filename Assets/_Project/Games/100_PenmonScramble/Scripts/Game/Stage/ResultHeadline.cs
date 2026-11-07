using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果の見出し（「クリア！」「やられた…」とステージの名札）: まず画面のまん中に大きく叩きつけられて弾み、
    /// カメラがペンモンへ寄るのに合わせて、右の札の列の上へすべって小さくなる。クリアなら後ろで光の筋が回る。
    /// 姿は「出てからの秒」だけで決める（Evaluate）
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ResultHeadline : MonoBehaviour
    {
        private const float StampSeconds = 0.45f;
        // 叩きつけるときは、この倍から一度縮みすぎて、弾んで落ち着く（ToyEasing.Pop）
        private const float StampFrom = 2.4f;
        private const float FadeInSeconds = 0.08f;
        // まん中に出している間の大きさ（列の上に収まった後が 1）
        private const float CenterScale = 1.3f;
        private const float GlideSeconds = 0.45f;
        private const float RaySpinPerSecond = 25f;
        private const float RayPulseSeconds = 1.6f;
        private const float RayPulseScale = 0.06f;

        [SerializeField] private Text _title;
        [SerializeField] private RectTransform _rays;
        [SerializeField] private GameObject _stagePlate;
        [SerializeField] private Text _stageName;

        private CanvasGroup _group;
        private RectTransform _rect;
        private Vector2 _home;
        private Vector2 _center;
        private float _glideAt;
        private bool _showRays;

        private void Bind()
        {
            if (_group != null)
            {
                return;
            }

            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
        }

        /// <param name="screenCenter">画面のまん中（ワールド座標）。列は画面の右寄りにあるので、まん中の位置は画面の縦横比で変わる</param>
        public void Setup(string title, Color color, string stageName, bool showRays, Vector3 screenCenter, float glideAt)
        {
            Bind();
            _title.text = title;
            _title.color = color;
            _stageName.text = stageName;
            _showRays = showRays;
            _glideAt = glideAt;
            // 親（列）の真ん中に留めているので、親から見た位置がそのまま anchoredPosition になる
            _center = ((RectTransform)_rect.parent).InverseTransformPoint(screenCenter);
            Evaluate(-1f);
        }

        public void Evaluate(float t)
        {
            Bind();
            _group.alpha = t < 0f ? 0f : Mathf.Clamp01(t / FadeInSeconds);
            if (t < 0f)
            {
                return;
            }

            var glide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - _glideAt) / GlideSeconds));
            var stamp = ToyEasing.Pop(t / StampSeconds, StampFrom);
            _rect.anchoredPosition = Vector2.Lerp(_center, _home, glide);
            _title.rectTransform.localScale = Vector3.one * (stamp * Mathf.Lerp(CenterScale, 1f, glide));
            // 名札はまん中では出さず、列の上に収まってから見せる（まん中の大きな見出しを読みやすく）
            _stagePlate.SetActive(glide >= 1f);

            _rays.gameObject.SetActive(_showRays);
            if (_showRays)
            {
                var pulse = 1f + Mathf.Sin(t / RayPulseSeconds * Mathf.PI * 2f) * RayPulseScale;
                _rays.localRotation = Quaternion.Euler(0f, 0f, -t * RaySpinPerSecond);
                _rays.localScale = Vector3.one * (Mathf.Min(1f, t / StampSeconds) * pulse * Mathf.Lerp(CenterScale, 1f, glide));
            }
        }
    }
}
