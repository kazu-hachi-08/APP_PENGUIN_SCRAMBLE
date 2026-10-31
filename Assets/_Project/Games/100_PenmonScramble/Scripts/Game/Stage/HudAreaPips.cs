using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// HUD の右上のエリアの進み具合（§6.1・§9.8）: エリアをつぶの列で出す（済み＝金・今＝白で脈打つ・まだ＝暗い）。
    /// エリアが2つ以上あれば最後はボスエリアなので、最後のつぶの代わりに赤い「ボス」の札を出す
    /// </summary>
    public class HudAreaPips : MonoBehaviour
    {
        [Tooltip("左から順に使う。ステージのエリアより多い分は隠す")]
        [SerializeField] private Image[] _pips;
        [SerializeField] private Image _bossTag;
        [SerializeField] private Color _doneColor = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private Color _currentColor = Color.white;
        [SerializeField] private Color _futureColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color _bossColor = new Color(0.9f, 0.22f, 0.22f);
        [SerializeField] private Color _bossFutureColor = new Color(0.5f, 0.2f, 0.22f);
        [SerializeField] private float _currentScale = 1.35f;
        [SerializeField] private float _pulseScale = 0.12f;
        [SerializeField] private float _pulsePerSecond = 1.5f;

        private RectTransform _current;
        private float _currentBaseScale = 1f;
        private float _elapsed;

        /// <summary>毎フレーム呼ばれる。今のつぶの大きさは Update が脈打たせるので、ここでは触らない</summary>
        public void Set(int areaIndex, int areaCount)
        {
            var hasBoss = areaCount > 1;
            var normalCount = Mathf.Min(hasBoss ? areaCount - 1 : areaCount, _pips.Length);
            _current = null;
            for (var i = 0; i < _pips.Length; i++)
            {
                var pip = _pips[i];
                pip.gameObject.SetActive(i < normalCount);
                pip.color = i < areaIndex ? _doneColor : i == areaIndex ? _currentColor : _futureColor;
                if (i == areaIndex && i < normalCount)
                {
                    SetCurrent(pip.rectTransform, _currentScale);
                }
                else
                {
                    pip.rectTransform.localScale = Vector3.one;
                }
            }

            _bossTag.gameObject.SetActive(hasBoss);
            var inBoss = hasBoss && areaIndex >= areaCount - 1;
            _bossTag.color = inBoss ? _bossColor : _bossFutureColor;
            if (inBoss)
            {
                SetCurrent(_bossTag.rectTransform, 1f);
            }
            else
            {
                _bossTag.rectTransform.localScale = Vector3.one;
            }
        }

        private void SetCurrent(RectTransform rect, float scale)
        {
            _current = rect;
            _currentBaseScale = scale;
        }

        private void Update()
        {
            if (_current == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            var wave = 0.5f + 0.5f * Mathf.Sin(_elapsed * _pulsePerSecond * Mathf.PI * 2f);
            _current.localScale = Vector3.one * _currentBaseScale * (1f + _pulseScale * wave);
        }
    }
}
