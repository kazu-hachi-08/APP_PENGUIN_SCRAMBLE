using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボスの攻撃の前の赤い範囲（§6.2）。長さ0なら円、それ以外は帯（四角）で描く。
    /// 当たりは両端が丸い形だが、半透明を重ねると濃い所ができるので、帯は端の丸を含む四角で少し広めに見せる。
    /// 攻撃が近づくほど濃くして、いつ来るか分かるようにする
    /// </summary>
    public class WarningView : MonoBehaviour
    {
        private static readonly Color WarningColor = new Color(1f, 0.15f, 0.1f);
        private const float StartAlpha = 0.15f;
        private const float EndAlpha = 0.55f;
        private const float BlinkSpeed = 14f;
        private const float BlinkAmount = 0.08f;

        private SpriteRenderer _part;
        private float _progress;

        public static WarningView Create(Transform parent, WarningState warning)
        {
            var view = new GameObject($"Warning{warning.Id}").AddComponent<WarningView>();
            view.transform.SetParent(parent, false);
            var from = warning.From.ToVector2();
            var to = warning.To.ToVector2();
            var width = warning.HalfWidth * 2f;
            var direction = to - from;
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                view._part = view.CreatePart(PlaceholderSprites.Circle, from, Vector2.one * width);
            }
            else
            {
                view._part = view.CreatePart(PlaceholderSprites.Square, (from + to) * 0.5f, new Vector2(direction.magnitude + width, width));
                view._part.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }

            view.PushState(warning);
            return view;
        }

        private SpriteRenderer CreatePart(Sprite sprite, Vector2 position, Vector2 size)
        {
            var obj = new GameObject("Area");
            obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingDepth.Ground;
            return renderer;
        }

        public void PushState(WarningState warning)
        {
            _progress = warning.Progress;
        }

        private void Update()
        {
            var alpha = Mathf.Lerp(StartAlpha, EndAlpha, _progress) + Mathf.Sin(Time.time * BlinkSpeed) * BlinkAmount;
            _part.color = new Color(WarningColor.r, WarningColor.g, WarningColor.b, alpha);
        }
    }
}
