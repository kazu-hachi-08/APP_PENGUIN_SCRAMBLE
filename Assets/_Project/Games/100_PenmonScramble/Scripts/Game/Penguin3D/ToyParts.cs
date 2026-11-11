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
            var obj = NewChild(objName, parent, position);
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            var renderer = AddMesh(obj, mesh, _materials.Get(color, gloss));
            _model.AddPart(renderer, color, gloss);
            return obj.transform;
        }

        /// <summary>親の下に名前と位置を付けて作る（回転・大きさは呼び出し側で決める）。位置は親からの相対で置きたいので、SetParent はワールド座標を残さない（false）</summary>
        internal static GameObject NewChild(string objName, Transform parent, Vector3 localPosition)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            return obj;
        }

        /// <summary>メッシュの絵を付ける（MeshFilter → MeshRenderer の順）。影の設定は呼び出し側で返った renderer に行う</summary>
        public static MeshRenderer AddMesh(GameObject obj, Mesh mesh, Material material)
        {
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>
        /// AddMesh のうえ、本物の影を落とさない版。影は足元の丸い影（BlobShadow）で表す方針の部品に使う。
        /// 影の設定を各所で書き忘れると本物の影が落ちて見た目が変わるので、1か所にまとめている
        /// </summary>
        internal static MeshRenderer AddMeshNoShadow(GameObject obj, Mesh mesh, Material material)
        {
            var renderer = AddMesh(obj, mesh, material);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        /// <summary>床や影に使う四角い板を、親の下に位置と大きさを決めて置く（寝かせた向きは FloorQuad の形で決まる）</summary>
        public static MeshRenderer FloorPlate(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = NewChild(name, parent, localPosition);
            obj.transform.localScale = localScale;
            return AddMesh(obj, ToyMeshes.FloorQuad, material);
        }

        /// <summary>形を持たない回転の軸（関節）</summary>
        public static Transform Pivot(string objName, Transform parent, Vector3 localPosition)
        {
            return NewChild(objName, parent, localPosition).transform;
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
