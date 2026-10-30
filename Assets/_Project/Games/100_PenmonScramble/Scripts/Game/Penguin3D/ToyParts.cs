using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>形の部品を作って ToyModel に登録する（ペンギンと大ボスの組み立て役で共通）</summary>
    internal readonly struct ToyParts
    {
        private readonly ToyModel _model;
        private readonly ToonMaterials _materials;

        public ToyParts(ToyModel model, ToonMaterials materials)
        {
            _model = model;
            _materials = materials;
        }

        public Transform Add(string objName, Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Color color, float gloss)
        {
            return Add(objName, mesh, parent, position, Quaternion.identity, scale, color, gloss);
        }

        public Transform Add(string objName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float gloss)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _materials.Get(color, gloss);
            _model.AddPart(renderer, color, gloss);
            return obj.transform;
        }

        /// <summary>形を持たない回転の軸（関節）</summary>
        public static Transform Pivot(string objName, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(objName).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        /// <summary>足元の丸い影。床と同じ高さだとちらつくので少し浮かせる</summary>
        public static Renderer BlobShadow(ToonMaterials materials, Transform root, Vector2 size)
        {
            var obj = new GameObject("BlobShadow");
            obj.transform.SetParent(root, false);
            obj.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            obj.transform.localScale = new Vector3(size.x, 1f, size.y);
            obj.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorQuad;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.BlobShadow;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
