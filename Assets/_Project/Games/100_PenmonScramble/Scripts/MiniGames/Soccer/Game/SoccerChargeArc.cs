using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// シュートのためゲージ: 足元の輪（暗い下地）に、前から右回りに黄色い弧がたまる。満タンでは白とオレンジに点滅する
    /// （いちばん強い浮いた球になる合図）。ためていないときは隠す
    /// </summary>
    public sealed class SoccerChargeArc
    {
        private const float InnerRadius = 0.66f;
        private const float OuterRadius = 0.82f;
        private const float Lift = 0.035f;
        private const int Segments = 48;
        // 形を作り直す、ための変わり目（少しの変化で毎フレーム作り直さないように）
        private const float RebuildStep = 0.01f;
        private const float FullBlinkRate = 10f;
        private const float FullFrom = 0.98f;

        private static readonly Color BackColor = new Color(0.12f, 0.14f, 0.2f);
        private static readonly Color FillColor = new Color(1f, 0.86f, 0.2f);
        private static readonly Color FullColor = new Color(1f, 0.5f, 0.15f);
        private static readonly Color FullBlinkColor = Color.white;

        private readonly Transform _root;
        private readonly MeshRenderer _fill;
        private readonly Mesh _fillMesh;
        private readonly Mesh _backMesh;
        private readonly Material _fillMaterial;
        private readonly Material _fullMaterial;
        private readonly Material _blinkMaterial;
        private float _shown = -1f;
        private float _time;

        public SoccerChargeArc(ToonMaterials materials, Transform parent)
        {
            _root = new GameObject("Charge").transform;
            _root.SetParent(parent, false);
            _root.localPosition = Vector3.up * Lift;
            _backMesh = BuildArc(new Mesh { name = "SoccerChargeBack" }, 1f, 0f);
            var back = Add("Back", _backMesh, materials.Floor(Texture2D.whiteTexture, BackColor));
            back.transform.localPosition = Vector3.down * (Lift * 0.3f);
            _fillMesh = new Mesh { name = "SoccerChargeFill" };
            _fillMesh.MarkDynamic();
            _fillMaterial = materials.Floor(Texture2D.whiteTexture, FillColor);
            _fullMaterial = materials.Floor(Texture2D.whiteTexture, FullColor);
            _blinkMaterial = materials.Floor(Texture2D.whiteTexture, FullBlinkColor);
            _fill = Add("Fill", _fillMesh, _fillMaterial);
            _root.gameObject.SetActive(false);
        }

        /// <summary>charge はため（0〜1。0 で隠す）、forward は前（弧の始まり）の向き</summary>
        public void Show(float charge, Vector3 forward, float deltaTime)
        {
            var visible = charge > 0f;
            _root.gameObject.SetActive(visible);
            if (!visible)
            {
                _shown = -1f;
                return;
            }

            if (forward.sqrMagnitude > 0f)
            {
                _root.localRotation = Quaternion.LookRotation(forward);
            }

            if (Mathf.Abs(charge - _shown) >= RebuildStep || (charge >= 1f && _shown < 1f))
            {
                _shown = charge;
                BuildArc(_fillMesh, charge, 0f);
            }

            _time += deltaTime;
            var full = charge >= FullFrom;
            var blink = Mathf.Repeat(_time * FullBlinkRate, 1f) < 0.5f;
            _fill.sharedMaterial = full ? (blink ? _blinkMaterial : _fullMaterial) : _fillMaterial;
        }

        public void Destroy()
        {
            Object.Destroy(_fillMesh);
            Object.Destroy(_backMesh);
        }

        /// <summary>前（+Z）から右回りに、amount（0〜1）周の平たい弧</summary>
        private static Mesh BuildArc(Mesh mesh, float amount, float lift)
        {
            var count = Mathf.Max(1, Mathf.CeilToInt(Segments * amount));
            var vertices = new Vector3[(count + 1) * 2];
            var triangles = new int[count * 6];
            for (var i = 0; i <= count; i++)
            {
                // 上から見て右回り（+Z から +X へ）
                var angle = Mathf.PI * 2f * amount * i / count;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                vertices[i * 2] = direction * InnerRadius + Vector3.up * lift;
                vertices[i * 2 + 1] = direction * OuterRadius + Vector3.up * lift;
            }

            for (var i = 0; i < count; i++)
            {
                var t = i * 6;
                var a = i * 2;
                // 内→外→次の内（上から見て右回りが表）
                triangles[t] = a;
                triangles[t + 1] = a + 1;
                triangles[t + 2] = a + 2;
                triangles[t + 3] = a + 2;
                triangles[t + 4] = a + 1;
                triangles[t + 5] = a + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private MeshRenderer Add(string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }
    }
}
