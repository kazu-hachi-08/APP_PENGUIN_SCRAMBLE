using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D の試作の場（Tools/PenmonScramble/3D Preview）。床とペンギンを実行時に組み立てる。
    /// 組み立てた物はシーンに保存しない（メッシュと素材がアセットではないため）。編集中も見えるよう ExecuteAlways にしている
    /// </summary>
    [ExecuteAlways]
    public sealed class PenguinPreviewStage : MonoBehaviour
    {
        private static readonly Color AreaColor = Color.white;
        private static readonly Color OutsideColor = new Color(0.55f, 0.6f, 0.7f);
        private const float OutsideSize = 60f;
        private const float OutsideDepth = -0.01f;

        // ほかのペンギンを置く位置（エリアの中心からのマス）。奥・手前・端の見え方を比べられるよう散らす
        private static readonly Vector2[] OtherSpots =
        {
            new Vector2(-6.5f, 3.5f), new Vector2(6.5f, 3.5f), new Vector2(-6.5f, -3.5f), new Vector2(6.5f, -3.5f),
            new Vector2(-3f, 1.5f), new Vector2(3f, -1.5f), new Vector2(0f, 3.8f),
        };

        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;
        [SerializeField] private Texture _floorTexture;
        [SerializeField] private Light _light;
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _realShadows;
        [SerializeField] private bool _outline = true;
        [SerializeField] private int _cameraPreset = PreviewCameraPresets.Chosen;
        [SerializeField] private float _outlineWidth = 0.012f;

        private readonly List<PenguinMotion> _penguins = new List<PenguinMotion>();
        private ToonMaterials _materials;
        private GameObject _generated;
        private bool _dirty;

        public IReadOnlyList<PenguinMotion> Penguins => _penguins;
        public PenguinMotion Player => _penguins.Count > 0 ? _penguins[0] : null;
        public Camera Camera => _camera;
        public bool RealShadows => _realShadows;
        public int CameraPreset => _cameraPreset;

        private void OnEnable()
        {
            if (_toonShader == null || _blobShadowShader == null)
            {
                return;
            }

            Build();
            ApplyShadows();
            ApplyCamera(_camera, _cameraPreset);
        }

        private void OnDisable() => Clear();

        // インスペクターで切り替えたときにすぐ見えるようにする。OnValidate の中でほかの物を触ると警告が出るので、次の Update で直す
        private void OnValidate() => _dirty = true;

        private void Update()
        {
            if (!_dirty || _generated == null) return;
            _dirty = false;
            ApplyShadows();
            ApplyCamera(_camera, _cameraPreset);
            _materials.SetOutlineWidth(_outline ? _outlineWidth : 0f);
        }

        public void SetRealShadows(bool real)
        {
            _realShadows = real;
            ApplyShadows();
        }

        public void SetOutline(bool on)
        {
            _outline = on;
            _materials.SetOutlineWidth(on ? _outlineWidth : 0f);
        }

        public bool Outline => _outline;

        public void SetCameraPreset(int preset)
        {
            _cameraPreset = (preset % PreviewCameraPresets.Count + PreviewCameraPresets.Count) % PreviewCameraPresets.Count;
            ApplyCamera(_camera, _cameraPreset);
        }

        public void ApplyCamera(Camera target, int preset)
        {
            if (target != null)
            {
                PreviewCameraPresets.Apply(target, preset, transform.position);
            }
        }

        private void Build()
        {
            Clear();
            _materials = new ToonMaterials(_toonShader, _blobShadowShader);
            _generated = new GameObject("Generated");
            _generated.transform.SetParent(transform, false);
            BuildFloor();

            var scale = PenguinModelBuilder.ScaleForRadius(Balance.PenguinRadius);
            AddPenguin("Player", Vector2.zero, scale);
            foreach (var spot in OtherSpots)
            {
                AddPenguin("Other", spot, scale);
            }

            _materials.SetOutlineWidth(_outline ? _outlineWidth : 0f);
            HideRecursively(_generated.transform);
        }

        private void BuildFloor()
        {
            AddFloor("Outside", new Vector2(OutsideSize, OutsideSize), OutsideDepth, _materials.Floor(_floorTexture, OutsideColor));
            AddFloor("Area", new Vector2(Balance.AreaWidth, Balance.AreaHeight), 0f, _materials.Floor(_floorTexture, AreaColor));
        }

        private void AddFloor(string objName, Vector2 size, float height, Material material)
        {
            var floor = new GameObject(objName);
            floor.transform.SetParent(_generated.transform, false);
            floor.transform.localPosition = new Vector3(0f, height, 0f);
            floor.transform.localScale = new Vector3(size.x, 1f, size.y);
            floor.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorQuad;
            floor.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void AddPenguin(string objName, Vector2 spot, float scale)
        {
            var model = PenguinModelBuilder.Build(PenguinLook.Default, _materials, _generated.transform);
            model.name = objName;
            model.transform.localPosition = new Vector3(spot.x, 0f, spot.y);
            model.transform.localScale = Vector3.one * scale;
            // 手前（カメラ側）を向かせて顔が見えるようにする
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var motion = model.gameObject.AddComponent<PenguinMotion>();
            motion.Apply(PenguinPose.Idle);
            _penguins.Add(motion);
        }

        private void ApplyShadows()
        {
            if (_light != null)
            {
                _light.shadows = _realShadows ? LightShadows.Soft : LightShadows.None;
            }

            foreach (var penguin in _penguins)
            {
                penguin.GetComponent<PenguinModel>().SetRealShadows(_realShadows);
            }
        }

        private void Clear()
        {
            _penguins.Clear();
            if (_generated != null)
            {
                DestroyImmediate(_generated);
                _generated = null;
            }

            _materials?.DestroyAll();
            _materials = null;
        }

        private static void HideRecursively(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.DontSave;
            foreach (Transform child in t)
            {
                HideRecursively(child);
            }
        }
    }
}
