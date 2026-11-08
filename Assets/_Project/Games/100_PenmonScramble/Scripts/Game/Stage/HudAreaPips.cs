using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// HUD の右上のエリアの進み具合: エリアをつぶの列で出す（済み＝金・今＝白で脈打つ・まだ＝暗い）。
    /// エリアが2つ以上あれば最後はボスエリアなので、最後のつぶの代わりに赤い「ボス」の札を出す。
    /// 闘技場の連戦（ボスラッシュ）は、見出しを「ROUND」にして、最後の戦いのつぶの代わりに赤い王冠を出す
    /// </summary>
    public class HudAreaPips : MonoBehaviour
    {
        private const string AreaLabel = "エリア";
        private const string RoundLabel = "ROUND";

        [Tooltip("左から順に使う。ステージのエリアより多い分は隠す")]
        [SerializeField] private Image[] _pips;
        [SerializeField] private Image _bossTag;
        [Tooltip("ボスラッシュの3戦目（本気の大ボス）の印")]
        [SerializeField] private Image _crown;
        [Tooltip("「エリア」「ROUND」の見出し。無ければ書き換えない")]
        [SerializeField] private Text _label;
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
        /// <param name="rounds">闘技場の連戦（エリア＝戦い）。最後のつぶを「ボス」の札ではなく王冠にする</param>
        public void Set(int areaIndex, int areaCount, bool rounds = false)
        {
            if (_label != null)
            {
                _label.text = rounds ? RoundLabel : AreaLabel;
            }

            var hasBoss = areaCount > 1;
            var lastTag = rounds && _crown != null ? _crown : _bossTag;
            var otherTag = lastTag == _bossTag ? _crown : _bossTag;
            if (otherTag != null)
            {
                otherTag.gameObject.SetActive(false);
            }
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

            lastTag.gameObject.SetActive(hasBoss);
            var inBoss = hasBoss && areaIndex >= areaCount - 1;
            lastTag.color = inBoss ? _bossColor : _bossFutureColor;
            if (inBoss)
            {
                SetCurrent(lastTag.rectTransform, 1f);
            }
            else
            {
                lastTag.rectTransform.localScale = Vector3.one;
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
            var wave = ToyEasing.Pulse01(_elapsed, _pulsePerSecond);
            _current.localScale = Vector3.one * _currentBaseScale * (1f + _pulseScale * wave);
        }
    }
}
