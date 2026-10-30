using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 床に置かれた範囲（床に寝かせた円と輪）。設置は属性の色でうっすら、落下は落ちる場所を赤っぽく脈打たせて知らせる
    /// </summary>
    public class EffectView : MonoBehaviour
    {
        private const float ZoneFillAlpha = 0.25f;
        private const float DropFillAlpha = 0.2f;
        private const float PulseSpeed = 10f;
        private const float PulseAmount = 0.06f;
        // 動く範囲（雷雲）が、届いた位置へ寄っていく速さ。毎秒15回しか届かないので、そのまま置くとかくかくする
        private const float FollowSharpness = 12f;
        private static readonly Color DropWarningColor = new Color(1f, 0.25f, 0.2f);

        private float _radius;
        private float _age;
        private Vector3 _target;

        public static EffectView Create(Transform parent, StageMaterials materials, EffectState effect)
        {
            var view = new GameObject($"Effect{effect.Id}_{effect.Kind}").AddComponent<EffectView>();
            view.transform.SetParent(parent, false);
            view._target = effect.Position.ToFloor(FloorDecal.AreaHeight);
            view.transform.position = view._target;
            view._radius = effect.Radius;

            var element = PenguinPalette.ElementColor(effect.Element);
            var isDrop = effect.Kind == AreaEffectKind.Drop;
            var ringColor = isDrop ? Color.Lerp(element, DropWarningColor, 0.6f) : element;
            var fillAlpha = isDrop ? DropFillAlpha : ZoneFillAlpha;
            view.CreatePart(materials, "Fill", DecalSprites.Circle, new Color(ringColor.r, ringColor.g, ringColor.b, fillAlpha), 0);
            // 輪は円の上に描く（同じ高さなので、順番で決める）
            view.CreatePart(materials, "Ring", DecalSprites.Ring, ringColor, 1);
            view.Apply();
            return view;
        }

        /// <summary>1マスの大きさで作り、根元の大きさで半径に合わせる（脈打たせるため）</summary>
        private void CreatePart(StageMaterials materials, string partName, Sprite sprite, Color color, int order)
        {
            var part = FloorDecal.Create(transform, partName, sprite, materials.Decal, color, transform.position, Vector2.one);
            part.sortingOrder = SortingDepth.Ground + order;
        }

        public void PushState(EffectState effect)
        {
            _target = effect.Position.ToFloor(FloorDecal.AreaHeight);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var follow = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _target, follow);
            Apply();
        }

        private void Apply()
        {
            var pulse = 1f + Mathf.Sin(_age * PulseSpeed) * PulseAmount;
            transform.localScale = Vector3.one * (_radius * 2f * pulse);
        }
    }
}
