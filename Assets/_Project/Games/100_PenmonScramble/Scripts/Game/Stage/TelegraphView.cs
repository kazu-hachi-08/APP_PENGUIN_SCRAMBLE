using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 構えている敵の、地面のうすい予告。近接・拡散は扇、周囲・設置は円、弾・突進は向きの帯で描く。
    /// うすい下地の上に、技が出るまでに中から満ちていく面を重ねて、いつ来るか分かるようにする。
    /// 敵は何体も同時に構えるので、大ボスの赤い範囲（WarningView）より色を淡く・薄くして画面をうるさくしない
    /// </summary>
    public class TelegraphView : MonoBehaviour
    {
        private static readonly Color TelegraphColor = new Color(1f, 0.92f, 0.7f);
        private const float BaseAlpha = 0.1f;
        private const float FillAlpha = 0.2f;
        // 細い弾でも床の上で見えるよう、帯はこの幅より細くしない（マス）
        private const float MinBandWidth = 0.3f;

        private SpriteRenderer _base;
        private SpriteRenderer _fill;
        private TelegraphState _state;
        private float _progress;

        public static TelegraphView Create(Transform parent, StageMaterials materials, TelegraphState telegraph)
        {
            var view = new GameObject($"Telegraph{telegraph.UnitId}_{telegraph.Shape}").AddComponent<TelegraphView>();
            view.transform.SetParent(parent, false);
            view._state = telegraph;
            // 落下は技が出た後に落ちる場所の予告（EffectView）が出るので、構えの間は地面に何も描かない
            if (telegraph.Shape != SkillShape.Drop)
            {
                var sprite = SpriteOf(telegraph);
                view._base = view.CreatePart(materials, "Base", sprite, BaseAlpha, 0);
                view._fill = view.CreatePart(materials, "Fill", sprite, FillAlpha, 1);
            }

            view.PushState(telegraph);
            view.Layout();
            return view;
        }

        private static Sprite SpriteOf(TelegraphState telegraph)
        {
            switch (telegraph.Shape)
            {
                case SkillShape.Melee:
                case SkillShape.Spread:
                    return DecalSprites.Sector(telegraph.HalfAngleDegrees);
                case SkillShape.Projectile:
                case SkillShape.Dash:
                    return DecalSprites.Square;
                default:
                    return DecalSprites.Circle;
            }
        }

        private SpriteRenderer CreatePart(StageMaterials materials, string partName, Sprite sprite, float alpha, int order)
        {
            var color = new Color(TelegraphColor.r, TelegraphColor.g, TelegraphColor.b, alpha);
            var part = FloorDecal.Create(transform, partName, sprite, materials.Decal, color, Vector3.zero, Vector2.one);
            part.sortingOrder = SortingDepth.Ground + order;
            return part;
        }

        public void PushState(TelegraphState telegraph)
        {
            _state = telegraph;
            _progress = telegraph.Progress;
        }

        private void Update()
        {
            // 毎秒15回しか届かないので、その間は構えの長さから進める
            if (_state.Seconds > 0f)
            {
                _progress = Mathf.Min(1f, _progress + Time.deltaTime / _state.Seconds);
            }

            Layout();
        }

        private void Layout()
        {
            if (_base == null)
            {
                return;
            }

            var origin = _state.Origin.ToVector2();
            var facing = _state.Facing.ToVector2();
            var yaw = FloorDecal.YawOf(facing);
            switch (_state.Shape)
            {
                case SkillShape.Projectile:
                case SkillShape.Dash:
                    var width = Mathf.Max(MinBandWidth, _state.HalfWidth * 2f);
                    PlaceBand(_base, origin, facing, _state.Reach, width, yaw);
                    PlaceBand(_fill, origin, facing, _state.Reach * _progress, width, yaw);
                    break;
                case SkillShape.Zone:
                    var center = origin + facing * _state.Reach;
                    PlaceCentered(_base, center, _state.HalfWidth, yaw);
                    PlaceCentered(_fill, center, _state.HalfWidth * _progress, yaw);
                    break;
                default:
                    // 扇・円は要（自分の中心）から広がる
                    PlaceCentered(_base, origin, _state.Reach, yaw);
                    PlaceCentered(_fill, origin, _state.Reach * _progress, yaw);
                    break;
            }
        }

        private static void PlaceCentered(SpriteRenderer part, Vector2 center, float radius, float yaw)
        {
            part.transform.position = center.ToFloor(FloorDecal.AreaHeight);
            part.transform.rotation = FloorDecal.Facing(yaw);
            FloorDecal.SetSize(part, Vector2.one * (radius * 2f));
        }

        /// <summary>origin から facing へ length だけ伸びる帯</summary>
        private static void PlaceBand(SpriteRenderer part, Vector2 origin, Vector2 facing, float length, float width, float yaw)
        {
            part.transform.position = (origin + facing * (length * 0.5f)).ToFloor(FloorDecal.AreaHeight);
            part.transform.rotation = FloorDecal.Facing(yaw);
            FloorDecal.SetSize(part, new Vector2(length, width));
        }
    }
}
