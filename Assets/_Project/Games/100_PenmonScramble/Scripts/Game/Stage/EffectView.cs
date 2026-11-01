using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 床に置かれた範囲（床に寝かせた円と輪）。設置は属性の色でうっすら、落下は落ちる場所を赤っぽく脈打たせて知らせる。
    /// 竜巻は、近づくと吸い込まれる危ない所なので、落下と同じく赤みを混ぜて濃く出す。
    /// 溶岩のしぶき（サメ）は溶岩の色で濃く、2本目の背びれ（本気のサメ）は床から背びれを立てて走らせる
    /// </summary>
    public class EffectView : MonoBehaviour
    {
        private const float ZoneFillAlpha = 0.25f;
        private const float DropFillAlpha = 0.2f;
        private const float PulseSpeed = 10f;
        private const float PulseAmount = 0.06f;
        // 動く範囲（雷雲・竜巻・背びれ）が、届いた位置へ寄っていく速さ。毎秒15回しか届かないので、そのまま置くとかくかくする
        private const float FollowSharpness = 12f;
        private static readonly Color DropWarningColor = new Color(1f, 0.25f, 0.2f);
        private const float VortexRedness = 0.35f;
        private const float VortexFillAlpha = 0.4f;
        private static readonly Color LavaColor = new Color(1f, 0.42f, 0.08f);
        private const float LavaFillAlpha = 0.5f;

        // 背びれ: 当たりの円（直径1）に対する大きさ。床から立てて、走る向きへ先を倒す
        private static readonly Vector3 FinSize = new Vector3(0.75f, 1.4f, 0.16f);
        private const float FinLean = 30f;
        private const float FinFoamAlpha = 0.55f;
        private const float FinTurnSharpness = 14f;

        private float _radius;
        private float _age;
        private Vector3 _target;
        private Transform _fin;
        private bool _pulses = true;

        public static EffectView Create(Transform parent, StageMaterials materials, EffectState effect)
        {
            var view = new GameObject($"Effect{effect.Id}_{effect.Kind}").AddComponent<EffectView>();
            view.transform.SetParent(parent, false);
            view._target = effect.Position.ToFloor(FloorDecal.AreaHeight);
            view.transform.position = view._target;
            view._radius = effect.Radius;

            if (effect.Kind == AreaEffectKind.Fin)
            {
                view.BuildFin(materials);
            }
            else
            {
                var (ringColor, fillAlpha) = ColorsOf(effect);
                view.CreatePart(materials, "Fill", DecalSprites.Circle, new Color(ringColor.r, ringColor.g, ringColor.b, fillAlpha), 0);
                // 輪は円の上に描く（同じ高さなので、順番で決める）
                view.CreatePart(materials, "Ring", DecalSprites.Ring, ringColor, 1);
            }

            view.Apply();
            return view;
        }

        private static (Color ring, float fillAlpha) ColorsOf(EffectState effect)
        {
            var element = PenguinPalette.ElementColor(effect.Element);
            switch (effect.Kind)
            {
                case AreaEffectKind.Drop:
                    return (Color.Lerp(element, DropWarningColor, 0.6f), DropFillAlpha);
                case AreaEffectKind.Vortex:
                    return (Color.Lerp(element, DropWarningColor, VortexRedness), VortexFillAlpha);
                case AreaEffectKind.Lava:
                    return (LavaColor, LavaFillAlpha);
                default:
                    return (element, ZoneFillAlpha);
            }
        }

        /// <summary>背びれ（大ボスのサメと同じ色）と、根元の白い泡の輪。脈打たせると背びれまで伸び縮みするので止める</summary>
        private void BuildFin(StageMaterials materials)
        {
            _pulses = false;
            CreatePart(materials, "Foam", DecalSprites.Ring, new Color(1f, 1f, 1f, FinFoamAlpha), 0);
            _fin = ToyParts.Pivot("Fin", transform, Vector3.zero);
            var blade = new GameObject("Blade");
            blade.transform.SetParent(_fin, false);
            blade.transform.localPosition = Vector3.up * (FinSize.y * 0.35f);
            // しずく形の先を上（少し後ろへ倒す）、平たい面を横へ向ける（大ボスのヒレと同じ向きの決め方）
            blade.transform.localRotation = Quaternion.LookRotation(Vector3.right, Quaternion.Euler(-FinLean, 0f, 0f) * Vector3.up);
            blade.transform.localScale = FinSize;
            var renderer = ToyParts.AddMesh(blade, ToyMeshes.Drop, materials.Toon.Get(BigBossModelBuilder.SharkBack, BigBossModelBuilder.SharkGloss));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            var step = _target - transform.position;
            transform.position = Vector3.Lerp(transform.position, _target, follow);
            TurnFin(step);
            Apply();
        }

        /// <summary>背びれの先を、走っている向きへ向ける</summary>
        private void TurnFin(Vector3 step)
        {
            step.y = 0f;
            if (_fin == null || step.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var turn = 1f - Mathf.Exp(-FinTurnSharpness * Time.deltaTime);
            _fin.rotation = Quaternion.Slerp(_fin.rotation, Quaternion.LookRotation(step), turn);
        }

        private void Apply()
        {
            var pulse = _pulses ? 1f + Mathf.Sin(_age * PulseSpeed) * PulseAmount : 1f;
            transform.localScale = Vector3.one * (_radius * 2f * pulse);
        }
    }
}
