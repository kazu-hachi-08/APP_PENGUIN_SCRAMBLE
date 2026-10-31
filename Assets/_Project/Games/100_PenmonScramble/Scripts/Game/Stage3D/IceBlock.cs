using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 凍ったペンギンを包む半透明の氷の塊。凍るとぽんと現れ、溶けると砕けて飛び散る（IceShatter）。
    /// 氷はペンギンの体ではなくユニットに付ける。凍ったまま飛ばされたときは、塊ごとすべって見える
    /// </summary>
    public sealed class IceBlock : MonoBehaviour
    {
        // ペンギンを少し余らせて包む（ヒレ・飾りがはみ出さないように）
        private const float WidthRatio = 1.4f;
        private const float HeightRatio = 1.12f;
        private const float PopSeconds = 0.25f;
        private const float PopStart = 0.6f;
        // 塊の向きをばらつかせる角度（六角なので 60°で一回り）
        private const float MaxYaw = 60f;

        private StageMaterials _materials;
        private Transform _stage;
        private Vector3 _size = Vector3.one;
        private float _popAge = PopSeconds;

        public bool IsShown => gameObject.activeSelf;

        /// <param name="stage">砕けたかけらを置く場所（ユニットが消えても、かけらは最後まで飛ぶように）</param>
        public static IceBlock Create(Transform parent, Transform stage, StageMaterials materials)
        {
            var obj = new GameObject("IceBlock");
            obj.transform.SetParent(parent, false);
            // どの塊も同じ向きで並ぶと作り物っぽいので、向きを少しずつ変える（見た目だけなので Core の乱数とは関係ない）
            obj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, MaxYaw), 0f);
            var renderer = ToyParts.AddMesh(obj, ToyMeshes.IceChunk, materials.Toon.Ice);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = obj.AddComponent<IceBlock>();
            block._materials = materials;
            block._stage = stage;
            obj.SetActive(false);
            return block;
        }

        /// <param name="width">ペンギンの胴の太さ</param>
        /// <param name="height">ペンギンの頭のてっぺんの高さ</param>
        public void Fit(float width, float height)
        {
            _size = new Vector3(width * WidthRatio, height * HeightRatio, width * WidthRatio);
            ApplyScale();
        }

        /// <summary>凍ったら現れ、溶けたら砕ける。同じ状態なら何もしない</summary>
        public void Show(bool frozen)
        {
            if (frozen == IsShown)
            {
                return;
            }

            if (!frozen)
            {
                IceShatter.Spawn(_stage, _materials, transform.position, _size);
            }

            _popAge = 0f;
            gameObject.SetActive(frozen);
            ApplyScale();
        }

        private void Update()
        {
            if (_popAge >= PopSeconds)
            {
                return;
            }

            _popAge += Time.deltaTime;
            ApplyScale();
        }

        private void ApplyScale() => transform.localScale = _size * ToyEasing.Pop(_popAge / PopSeconds, PopStart);
    }
}
