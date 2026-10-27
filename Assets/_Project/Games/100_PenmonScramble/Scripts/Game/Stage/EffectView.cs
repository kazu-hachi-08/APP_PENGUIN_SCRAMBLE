using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 地面に置かれた範囲の仮の見た目。設置は属性の色でうっすら、落下は落ちる場所を赤っぽく点滅させて知らせる
    /// </summary>
    public class EffectView : MonoBehaviour
    {
        private const float ZoneFillAlpha = 0.25f;
        private const float DropFillAlpha = 0.2f;
        private const float PulseSpeed = 10f;
        private const float PulseAmount = 0.06f;
        private static readonly Color DropWarningColor = new Color(1f, 0.25f, 0.2f);

        private SpriteRenderer _fill;
        private SpriteRenderer _ring;
        private float _radius;
        private float _age;

        public static EffectView Create(Transform parent, EffectState effect)
        {
            var view = new GameObject($"Effect{effect.Id}_{effect.Kind}").AddComponent<EffectView>();
            view.transform.SetParent(parent, false);
            view.transform.position = effect.Position.ToVector2();
            view._radius = effect.Radius;

            var element = PenguinPalette.ElementColor(effect.Element);
            var isDrop = effect.Kind == AreaEffectKind.Drop;
            var ringColor = isDrop ? Color.Lerp(element, DropWarningColor, 0.6f) : element;
            var fillAlpha = isDrop ? DropFillAlpha : ZoneFillAlpha;
            view._fill = view.CreatePart("Fill", PlaceholderSprites.Circle, new Color(ringColor.r, ringColor.g, ringColor.b, fillAlpha));
            view._ring = view.CreatePart("Ring", PlaceholderSprites.Ring, ringColor);
            view.Apply();
            return view;
        }

        private SpriteRenderer CreatePart(string partName, Sprite sprite, Color color)
        {
            var obj = new GameObject(partName);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = SortingDepth.Ground;
            return renderer;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            var pulse = 1f + Mathf.Sin(_age * PulseSpeed) * PulseAmount;
            transform.localScale = Vector3.one * (_radius * 2f * pulse);
        }
    }
}
