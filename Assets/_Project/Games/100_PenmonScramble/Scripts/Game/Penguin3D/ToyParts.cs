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
            var renderer = AddMesh(obj, mesh, _materials.Get(color, gloss));
            _model.AddPart(renderer, color, gloss);
            return obj.transform;
        }

        /// <summary>メッシュの絵を付ける（MeshFilter → MeshRenderer の順）。影の設定は呼び出し側で返った renderer に行う</summary>
        public static MeshRenderer AddMesh(GameObject obj, Mesh mesh, Material material)
        {
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>床や影に使う四角い板を、親の下に位置と大きさを決めて置く（寝かせた向きは FloorQuad の形で決まる）</summary>
        public static MeshRenderer FloorPlate(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;
            return AddMesh(obj, ToyMeshes.FloorQuad, material);
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
            var renderer = FloorPlate("BlobShadow", root, new Vector3(0f, 0.005f, 0f), new Vector3(size.x, 1f, size.y), materials.BlobShadow);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
