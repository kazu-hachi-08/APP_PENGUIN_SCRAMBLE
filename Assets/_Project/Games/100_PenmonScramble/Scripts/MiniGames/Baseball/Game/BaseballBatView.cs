using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// バット: 軸（盤に打った金具の丸）を中心に、盤の上を水平に回る木のバット（おもちゃの盤のバットと同じ。当たりは Core と同じ形）。
    /// 握りの細い柄は軸から当たり始める所まで、太い所（卵の形を長く伸ばした物。先が太い）はそこから先まで。
    /// 芯の所に細い色の帯を巻いて、どこで打てば強いかを見せる。
    /// 軸の金具は盤に切った横長の溝に差してあり、打つ子が左右へ動くと金具ごと溝の中を滑る（動ける幅が溝で見える）
    /// </summary>
    public sealed class BaseballBatView
    {
        private const float BarrelDiameter = 0.2f;
        private const float HandleDiameter = 0.08f;
        private const float BandWidth = 0.05f;
        private const float BandDiameter = 0.205f;
        private const float KnobDiameter = 0.34f;
        private const float KnobHeight = 0.14f;
        private const float Gloss = 0.45f;
        private static readonly Color WoodColor = new Color(0.90f, 0.70f, 0.44f);
        private static readonly Color HandleColor = new Color(0.40f, 0.26f, 0.18f);
        private static readonly Color SweetBandColor = new Color(0.95f, 0.30f, 0.25f);
        private static readonly Color KnobColor = new Color(0.55f, 0.58f, 0.64f);
        private static readonly Color SlotColor = new Color(0.22f, 0.15f, 0.10f);
        private const float SlotWidth = 0.16f;
        private const float SlotHeight = 0.01f;
        // 盤の土と線（重ねた薄い板）より上に出す高さ
        private const float SlotLift = 0.045f;

        private readonly Transform _rig;
        private readonly Transform _bat;
        private readonly Vector3 _pivot;

        public BaseballBatView(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            _pivot = BaseballSpace.ToWorld(field.BatPivot);
            var slotLength = BaseballBalance.BatterMoveRange * 2f + SlotWidth;
            Part("BatSlot", ToyMeshes.Puck, materials.Get(SlotColor, 0f), parent, _pivot + Vector3.up * SlotLift,
                Quaternion.identity, new Vector3(slotLength, SlotHeight, SlotWidth));

            _rig = new GameObject("BatRig").transform;
            _rig.SetParent(parent, false);
            _rig.localPosition = _pivot;
            Part("BatKnob", ToyMeshes.Puck, materials.Get(KnobColor, Gloss), _rig, Vector3.up * (KnobHeight * 0.5f),
                Quaternion.identity, new Vector3(KnobDiameter, KnobHeight, KnobDiameter));

            _bat = new GameObject("Bat").transform;
            _bat.SetParent(_rig, false);
            _bat.localPosition = Vector3.up * BaseballBalance.BallRadius;
            // 卵の形は Y が長さの向きで下が太い。Z を 90° 回すと下（太い方）が +X（先）へ向く
            var lying = Quaternion.Euler(0f, 0f, 90f);
            var barrel = BaseballBalance.BatLength - BaseballBalance.BatInner;
            Part("Barrel", ToyMeshes.Egg, materials.Get(WoodColor, Gloss), _bat, new Vector3(BaseballBalance.BatInner + barrel * 0.5f, 0f, 0f),
                lying, new Vector3(BarrelDiameter, barrel, BarrelDiameter));
            Part("Handle", ToyMeshes.Puck, materials.Get(HandleColor, Gloss), _bat, new Vector3(BaseballBalance.BatInner * 0.5f, 0f, 0f),
                lying, new Vector3(HandleDiameter, BaseballBalance.BatInner + BarrelDiameter * 0.3f, HandleDiameter));
            Part("SweetBand", ToyMeshes.Puck, materials.Get(SweetBandColor, Gloss), _bat, new Vector3(BaseballBalance.SweetSpot, 0f, 0f),
                lying, new Vector3(BandDiameter, BandWidth, BandDiameter));
        }

        /// <summary>degrees は補間したバットの角度（BaseballBat.Degrees。正が投手の方。Unity の Y の回転とは向きが逆）、offset は軸の横のずれ</summary>
        public void Sync(BaseballBoard board, float degrees, float offset)
        {
            _rig.localPosition = _pivot + Vector3.right * offset;
            _bat.localRotation = Quaternion.Euler(0f, -degrees, 0f);
        }

        private static void Part(string name, Mesh mesh, Material material, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
