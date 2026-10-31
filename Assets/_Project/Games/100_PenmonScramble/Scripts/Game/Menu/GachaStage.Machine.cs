using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャの部屋とマシンの組み立て。どれもペンギンと同じトゥーンの形（ToyMeshes）の組み合わせで、おもちゃのガチャマシンに見せる。
    /// 前（カメラの方）は -Z。左にレバー、右に取り出し口
    /// </summary>
    public sealed partial class GachaStage
    {
        private const float Gloss = 0.6f;
        private const float MattGloss = 0f;

        // マシン: 丸い胴の上にガラスの丸い窓（中にカプセル）、てっぺんに金のふた
        private static readonly Color BodyColor = new Color(0.95f, 0.42f, 0.62f);
        private static readonly Color FootColor = new Color(0.78f, 0.28f, 0.48f);
        private static readonly Color TrimColor = new Color(1f, 0.97f, 0.92f);
        private static readonly Color GoldColor = new Color(1f, 0.80f, 0.25f);
        private static readonly Color HoleColor = new Color(0.12f, 0.10f, 0.22f);
        private const float BodyRadius = 1f;
        private const float BodyHeight = 1.5f;
        private const float FootHeight = 0.25f;
        private const float DomeRadius = 0.95f;
        private const float DomeCenterY = BodyHeight + 0.75f;
        private const float LeverX = -0.45f;
        private const float LeverY = 0.9f;
        private const float LeverDiscSize = 0.56f;
        private const float LeverBarLength = 0.58f;
        private const float ChuteX = 0.45f;
        private const float ChuteY = 0.62f;
        private const float ChuteSize = 0.5f;
        private const float EmblemY = 1.25f;
        private const float EmblemSize = 0.36f;

        // 窓の中のカプセル。上の殻の色をばらして、いろんな子が入っていそうに見せる
        private const int InnerCapsules = 13;
        private const float InnerCapsuleSize = 0.4f;
        private const float InnerSpreadRadius = 0.62f;
        private const float InnerLowestY = BodyHeight + 0.15f;
        private const float InnerHighestY = DomeCenterY + 0.35f;
        private static readonly Color[] InnerTopColors =
        {
            new Color(0.62f, 0.88f, 1f), new Color(1f, 0.62f, 0.22f), new Color(1f, 0.55f, 0.75f),
            new Color(1f, 0.88f, 0.35f), new Color(0.55f, 0.90f, 0.55f), new Color(0.75f, 0.65f, 1f),
        };
        private static readonly Color InnerBottomColor = new Color(0.98f, 0.96f, 0.92f);
        // 待っている間のゆれと、レバーを回している間のゆれ
        private const float IdleBob = 0.015f;
        private const float IdleBobSpeed = 1.6f;
        private const float StirAmount = 0.07f;
        private const float StirSpeed = 22f;

        // 部屋: 楕円の床・台・奥に浮かぶ星・マシンを照らす光
        private static readonly Color FloorColor = new Color(0.27f, 0.24f, 0.52f);
        private static readonly Color RareFloorColor = new Color(0.10f, 0.09f, 0.20f);
        private static readonly Vector3 FloorCenter = new Vector3(1f, 0f, -0.3f);
        private static readonly Vector3 FloorSize = new Vector3(12f, 0.3f, 7f);
        private static readonly Color PedestalColor = new Color(0.42f, 0.36f, 0.75f);
        private static readonly Vector3 PedestalSize = new Vector3(2f, 0.1f, 2f);
        // 台の上の面の高さ（床から少しだけ上げて、床と同じ高さでちらつかないように）
        private const float PedestalTop = 0.06f;
        private static readonly Color PedestalRingColor = new Color(1f, 0.85f, 0.35f, 0.8f);
        private const float PedestalRingSize = 2.3f;
        private static readonly Color[] BackStarColors = { GoldColor, new Color(1f, 0.55f, 0.75f), new Color(0.55f, 0.85f, 1f) };
        private const int BackStars = 9;
        private static readonly Vector2 BackStarX = new Vector2(-6f, 9f);
        private static readonly Vector2 BackStarY = new Vector2(0.5f, 5.5f);
        private static readonly Vector2 BackStarZ = new Vector2(5f, 10f);
        private static readonly Vector2 BackStarSize = new Vector2(0.5f, 1.1f);
        private const float BackStarSpin = 40f;
        private const float BackStarBob = 0.25f;
        private static readonly Color SpotColor = new Color(1f, 0.85f, 0.95f, 0.22f);
        // マシンの真後ろから照らす（手前の物に隠れるので、輪郭のまわりが光って見える）
        private static readonly Vector3 SpotPosition = new Vector3(0f, 2.2f, 2.5f);
        private const float SpotSize = 5.5f;

        private readonly List<Transform> _glows = new List<Transform>();
        private readonly List<Transform> _innerCapsules = new List<Transform>();
        private readonly List<Vector3> _innerHomes = new List<Vector3>();
        private readonly List<Transform> _backStars = new List<Transform>();
        private Renderer _floor;
        private Transform _leverPivot;
        private float _stir;
        private float _time;

        /// <summary>取り出し口の手前（カプセルの中心が出てくる所）。部屋の中の位置</summary>
        private static Vector3 ChuteMouth => new Vector3(ChuteX, ChuteY, FrontZ(ChuteX) - 0.15f);

        /// <summary>レバーの角度（度）。Reveal が回す</summary>
        private float LeverAngle
        {
            set => _leverPivot.localRotation = Quaternion.Euler(0f, 0f, -value);
        }

        private void BuildRoom()
        {
            _glows.Clear();
            _backStars.Clear();
            _floor = Part("Floor", ToyMeshes.Puck, _world, FloorCenter + Vector3.down * (FloorSize.y * 0.5f), Quaternion.identity, FloorSize, FloorColor, MattGloss).GetComponent<Renderer>();
            Part("Pedestal", ToyMeshes.Puck, _world, RevealSpot + Vector3.up * (PedestalTop - PedestalSize.y * 0.5f), Quaternion.identity, PedestalSize, PedestalColor, MattGloss);
            FloorDecal.Create(_world, "PedestalRing", DecalSprites.Ring, _materials.Glow, PedestalRingColor,
                World(RevealSpot) + Vector3.up * (PedestalTop + FloorDecal.AreaHeight), Vector2.one * PedestalRingSize);
            AddGlow("Spot", SpotColor, SpotPosition, Vector2.one * SpotSize);
            for (var i = 0; i < BackStars; i++)
            {
                var position = new Vector3(Random.Range(BackStarX.x, BackStarX.y), Random.Range(BackStarY.x, BackStarY.y), Random.Range(BackStarZ.x, BackStarZ.y));
                var color = BackStarColors[i % BackStarColors.Length];
                var star = Part("BackStar", ToyMeshes.Star, _world, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), Vector3.one * Random.Range(BackStarSize.x, BackStarSize.y), color, Gloss);
                _backStars.Add(star);
            }
        }

        private void BuildMachine()
        {
            _innerCapsules.Clear();
            _innerHomes.Clear();
            var machine = new GameObject("Machine").transform;
            machine.SetParent(_world, false);
            Part("Foot", ToyMeshes.Puck, machine, new Vector3(0f, FootHeight * 0.5f, 0f), Quaternion.identity, new Vector3(2.3f, FootHeight, 2.3f), FootColor, Gloss);
            Part("Body", ToyMeshes.Puck, machine, new Vector3(0f, BodyHeight * 0.5f + 0.05f, 0f), Quaternion.identity, new Vector3(BodyRadius * 2f, BodyHeight, BodyRadius * 2f), BodyColor, Gloss);
            Part("Trim", ToyMeshes.Torus, machine, new Vector3(0f, BodyHeight, 0f), Quaternion.identity, Vector3.one * (BodyRadius * 2.08f), TrimColor, Gloss);
            BuildDome(machine);
            BuildLever(machine);
            BuildChute(machine);
            Part("Emblem", ToyMeshes.Star, machine, new Vector3(0f, EmblemY, FrontZ(0f) - 0.03f), Quaternion.identity, Vector3.one * EmblemSize, GoldColor, 1f);
        }

        /// <summary>ガラスの丸い窓（氷と同じ半透明の素材で、縁が光る）と、中のカプセル・てっぺんのふた</summary>
        private void BuildDome(Transform machine)
        {
            for (var i = 0; i < InnerCapsules; i++)
            {
                // 下から順に積んで、窓の下の方に溜まって見えるようにする
                var height = Mathf.Lerp(InnerLowestY, InnerHighestY, i / (float)InnerCapsules);
                var room = Mathf.Sqrt(Mathf.Max(0f, DomeRadius * DomeRadius - (height - DomeCenterY) * (height - DomeCenterY))) - InnerCapsuleSize * 0.5f;
                var flat = Random.insideUnitCircle * Mathf.Min(InnerSpreadRadius, room);
                var home = new Vector3(flat.x, height, flat.y);
                var capsule = BuildCapsule(machine, "InnerCapsule", InnerCapsuleSize, InnerTopColors[i % InnerTopColors.Length], InnerBottomColor, withSeam: false);
                capsule.Root.localPosition = home;
                capsule.Root.localRotation = Random.rotation;
                _innerCapsules.Add(capsule.Root);
                _innerHomes.Add(home);
            }

            var dome = Part("Dome", ToyMeshes.Sphere, machine, new Vector3(0f, DomeCenterY, 0f), Quaternion.identity, Vector3.one * (DomeRadius * 2f), Color.white, 1f);
            dome.GetComponent<Renderer>().sharedMaterial = _materials.Toon.Ice;
            var top = DomeCenterY + DomeRadius;
            Part("Lid", ToyMeshes.Puck, machine, new Vector3(0f, top - 0.05f, 0f), Quaternion.identity, new Vector3(0.75f, 0.25f, 0.75f), GoldColor, 1f);
            Part("Knob", ToyMeshes.Sphere, machine, new Vector3(0f, top + 0.15f, 0f), Quaternion.identity, Vector3.one * 0.3f, BodyColor, 1f);
        }

        /// <summary>前の白い円盤と、それごと回る持ち手（先に金の玉）</summary>
        private void BuildLever(Transform machine)
        {
            var front = FrontZ(LeverX);
            var facingCamera = Quaternion.Euler(90f, 0f, 0f);
            Part("LeverDisc", ToyMeshes.Puck, machine, new Vector3(LeverX, LeverY, front), facingCamera, new Vector3(LeverDiscSize, 0.14f, LeverDiscSize), TrimColor, Gloss);
            _leverPivot = new GameObject("LeverPivot").transform;
            _leverPivot.SetParent(machine, false);
            _leverPivot.localPosition = new Vector3(LeverX, LeverY, front - 0.1f);
            Part("LeverBar", ToyMeshes.Puck, _leverPivot, Vector3.zero, Quaternion.Euler(0f, 0f, 90f), new Vector3(0.16f, LeverBarLength, 0.16f), BodyColor, Gloss);
            Part("LeverKnob", ToyMeshes.Sphere, _leverPivot, new Vector3(LeverBarLength * 0.5f, 0f, -0.06f), Quaternion.identity, Vector3.one * 0.2f, GoldColor, 1f);
        }

        /// <summary>取り出し口: 白い縁と暗い穴</summary>
        private void BuildChute(Transform machine)
        {
            var front = FrontZ(ChuteX);
            var facingCamera = Quaternion.Euler(90f, 0f, 0f);
            Part("ChuteRim", ToyMeshes.Puck, machine, new Vector3(ChuteX, ChuteY, front), facingCamera, new Vector3(ChuteSize + 0.12f, 0.1f, ChuteSize + 0.12f), TrimColor, Gloss);
            Part("ChuteHole", ToyMeshes.Puck, machine, new Vector3(ChuteX, ChuteY, front - 0.04f), facingCamera, new Vector3(ChuteSize, 0.1f, ChuteSize), HoleColor, MattGloss);
        }

        /// <summary>カプセル: 上と下の半球の殻（と、合わせ目の白い輪）。Root を床の点に置くと、Pivot が殻の中心の高さに来る</summary>
        private Capsule BuildCapsule(Transform parent, string name, float size, Color top, Color bottom, bool withSeam)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = Vector3.up * (size * 0.5f);
            var topShell = Part("Top", ToyMeshes.Hemisphere, pivot, Vector3.zero, Quaternion.identity, Vector3.one * size, top, 1f);
            var bottomShell = Part("Bottom", ToyMeshes.Hemisphere, pivot, Vector3.zero, Quaternion.Euler(180f, 0f, 0f), Vector3.one * size, bottom, 1f);
            if (withSeam)
            {
                Part("Seam", ToyMeshes.Torus, pivot, Vector3.zero, Quaternion.identity, Vector3.one * (size * 1.04f), Color.white, Gloss);
            }

            return new Capsule(root, pivot, topShell, bottomShell);
        }

        /// <summary>カプセルの部品。割れるときは上と下を別々に飛ばす</summary>
        private readonly struct Capsule
        {
            public readonly Transform Root;
            public readonly Transform Pivot;
            public readonly Transform Top;
            public readonly Transform Bottom;

            public Capsule(Transform root, Transform pivot, Transform top, Transform bottom)
            {
                Root = root;
                Pivot = pivot;
                Top = top;
                Bottom = bottom;
            }
        }

        /// <summary>丸い胴（半径 BodyRadius）の前の面の Z。横へずれるほど奥へ下がる</summary>
        private static float FrontZ(float x) => -Mathf.Sqrt(Mathf.Max(0f, BodyRadius * BodyRadius - x * x));

        private Transform Part(string objName, Mesh mesh, Transform parent, Vector3 localPosition, Quaternion rotation, Vector3 scale, Color color, float gloss)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _materials.Toon.Get(color, gloss);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return obj.transform;
        }

        /// <summary>足して光らせる丸。いつもカメラの方を向ける（FaceGlows）</summary>
        private SpriteRenderer AddGlow(string objName, Color color, Vector3 localPosition, Vector2 size)
        {
            var renderer = new GameObject(objName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(_world, false);
            renderer.transform.localPosition = localPosition;
            renderer.sprite = DecalSprites.Glow;
            renderer.sharedMaterial = _materials.Glow;
            renderer.color = color;
            FloorDecal.SetSize(renderer, size);
            _glows.Add(renderer.transform);
            return renderer;
        }

        private void FaceGlows()
        {
            var rotation = _camera.transform.rotation;
            foreach (var glow in _glows)
            {
                if (glow != null)
                {
                    glow.rotation = rotation;
                }
            }
        }

        /// <summary>★3 の間は部屋を暗くして、光の柱を目立たせる</summary>
        private void SetRareDark(float amount)
        {
            SetBackground(Color.Lerp(BackgroundColor, RareBackgroundColor, amount));
            // 床の素材は色ごとに1つなので、暗い色と元の色の2つだけを切り替える
            _floor.sharedMaterial = _materials.Toon.Get(amount > 0.5f ? RareFloorColor : FloorColor, MattGloss);
        }

        private void TickRoom(float dt)
        {
            _time += dt;
            for (var i = 0; i < _backStars.Count; i++)
            {
                var star = _backStars[i];
                star.Rotate(0f, BackStarSpin * dt * (i % 2 == 0 ? 1f : -1f), 0f, Space.World);
                var position = star.localPosition;
                position.y += Mathf.Sin(_time + i) * BackStarBob * dt;
                star.localPosition = position;
            }
        }

        /// <summary>窓の中のカプセルをゆらす。レバーを回している間（_stir）は大きく揺らして、かき混ぜているように見せる</summary>
        private void TickMachine(float dt)
        {
            for (var i = 0; i < _innerCapsules.Count; i++)
            {
                var phase = i * 1.7f;
                var bob = Vector3.up * (Mathf.Sin(_time * IdleBobSpeed + phase) * IdleBob);
                var stir = new Vector3(Mathf.Sin(_time * StirSpeed + phase), Mathf.Abs(Mathf.Sin(_time * StirSpeed * 0.7f + phase)), Mathf.Cos(_time * StirSpeed * 0.9f + phase)) * (StirAmount * _stir);
                _innerCapsules[i].localPosition = _innerHomes[i] + bob + stir;
                if (_stir > 0f)
                {
                    _innerCapsules[i].Rotate(Random.insideUnitSphere * (360f * _stir * dt), Space.Self);
                }
            }
        }
    }
}
