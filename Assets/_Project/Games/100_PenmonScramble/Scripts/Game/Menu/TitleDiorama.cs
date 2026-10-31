using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルの後ろで動き続ける乱戦のジオラマ。ゲーム中と同じ3Dのペンギンを使い、
    /// まん中の味方3体（自分の編成）へ野生のペンギンが四方から寄ってきては、技で吹っ飛ばされてコインと星になる。
    /// タイトル（このコンポーネントを付けた UI）が閉じたら全部消し、ステージのカメラに戻す。
    /// 見た目だけなので Core のステージは使わず、乱数も UnityEngine.Random（役者の動きは TitleDiorama.Actors.cs）
    /// </summary>
    public sealed partial class TitleDiorama : MonoBehaviour
    {
        // 斜め上から寄って見下ろす（ステージの 55° より低く、ペンギンの顔が見える）。地平線が映らない角度にする
        public const float CameraPitch = 38f;
        public const float CameraFieldOfView = 30f;
        public const float CameraDistance = 12.5f;
        // カメラが見る床の点。乱戦の中心より少し奥を見て、乱戦がロゴの下・ロビーの右下のボタンの左に来るようにする
        private static readonly Vector3 CameraTarget = new Vector3(0f, 0f, 0.6f);
        private static readonly Color BackgroundColor = new Color(0.35f, 0.65f, 0.85f);

        // 床（ワールド1の地面の絵）。ステージと同じく1枚を1エリア（16×9マス）に敷き、どの縦横比の画面でも端が映らないよう並べる
        // （1枚を広く引き伸ばすと、寄ったカメラでは絵がぼやける）
        private const int GroundWorld = 1;
        private static readonly Vector2 FloorTile = new Vector2(Balance.AreaWidth, Balance.AreaHeight);
        private static readonly Vector2 FloorCenter = new Vector2(0f, 6f);
        private const int FloorTilesX = 3;
        private const int FloorTilesY = 3;

        // 編成が空のとき（初回の無料ガチャの前）に並べる3体: アデリン・ジェンツ・オヤブン
        private static readonly int[] DefaultHeroes = { 1, 3, 5 };
        // 先頭の子を手前のまん中に、2番目・3番目を左右の少し奥に置く
        private static readonly Vector2[] HeroSpots = { new Vector2(0f, -0.6f), new Vector2(-1.9f, 0.5f), new Vector2(1.9f, 0.5f) };

        [SerializeField] private Camera _camera;
        // タイトルの間は止めておくステージのカメラ（何も無い所を描かないように）
        [SerializeField] private Camera _stageCamera;
        // シェーダーはシーンの参照で持つ（Shader.Find だとビルドに入らないことがあるため）
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;
        [SerializeField] private Shader _fxShader;

        private readonly List<Hero> _heroes = new List<Hero>();
        private readonly List<Wild> _wilds = new List<Wild>();
        private StageMaterials _materials;
        private Transform _world;
        private float _spawnTimer;
        private float _specialTimer;

        private void OnEnable()
        {
            _materials ??= new StageMaterials(_toonShader, _blobShadowShader, _fxShader);
            _world = new GameObject("TitleDiorama").transform;
            PlaceCamera();
            CreateFloor();
            CreateHeroes();
            for (var i = 0; i < StartingWilds; i++)
            {
                SpawnWild(StartingSpawnDistance);
            }

            _spawnTimer = SpawnInterval();
            _specialTimer = SpecialInterval();
            SetCameras(true);
        }

        private void OnDisable()
        {
            SetCameras(false);
            _heroes.Clear();
            _wilds.Clear();
            if (_world != null)
            {
                Destroy(_world.gameObject);
                _world = null;
            }
        }

        private void OnDestroy()
        {
            _materials?.DestroyAll();
        }

        private void SetCameras(bool title)
        {
            if (_camera != null) _camera.enabled = title;
            if (_stageCamera != null) _stageCamera.enabled = !title;
        }

        /// <summary>カメラの構え。アイコンを撮るときも同じ構えを使う</summary>
        public static void Aim(Transform camera, Vector3 target, float pitch, float distance) => AreaCameraFit.Aim(camera, target, pitch, distance);

        private void PlaceCamera()
        {
            _camera.orthographic = false;
            _camera.fieldOfView = CameraFieldOfView;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = BackgroundColor;
            Aim(_camera.transform, CameraTarget, CameraPitch, CameraDistance);
        }

        private void CreateFloor()
        {
            var art = CreatureArt.Ground(GroundWorld);
            var material = _materials.Toon.Floor(art != null ? art.texture : Texture2D.whiteTexture, Color.white);
            for (var y = 0; y < FloorTilesY; y++)
            {
                for (var x = 0; x < FloorTilesX; x++)
                {
                    var offset = new Vector2(x - (FloorTilesX - 1) * 0.5f, y - (FloorTilesY - 1) * 0.5f);
                    // _world は原点・無回転の親なので、ローカルの位置は床の位置と同じ
                    ToyParts.FloorPlate("Floor", _world, (FloorCenter + Vector2.Scale(offset, FloorTile)).ToFloor(), new Vector3(FloorTile.x, 1f, FloorTile.y), material);
                }
            }
        }

        private void CreateHeroes()
        {
            var party = GameSave.Record.PartyPenguins();
            for (var i = 0; i < HeroSpots.Length; i++)
            {
                var speciesNo = i < party.Count ? party[i].speciesNo : DefaultHeroes[i];
                _heroes.Add(new Hero(this, speciesNo, HeroSpots[i]));
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            TickSpawns(dt);
            TickSpecial(dt);
            foreach (var hero in _heroes)
            {
                hero.Tick(dt);
            }

            for (var i = _wilds.Count - 1; i >= 0; i--)
            {
                if (!_wilds[i].Tick(dt))
                {
                    _wilds.RemoveAt(i);
                }
            }
        }

        private void TickSpawns(float dt)
        {
            _spawnTimer -= dt;
            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = SpawnInterval();
            if (_wilds.Count < MaxWilds)
            {
                SpawnWild(SpawnDistance);
            }
        }

        /// <summary>ときどき誰かが必殺技で跳び、着地のまわりの野生のペンギンをまとめて吹っ飛ばす</summary>
        private void TickSpecial(float dt)
        {
            _specialTimer -= dt;
            if (_specialTimer > 0f)
            {
                return;
            }

            _specialTimer = SpecialInterval();
            _heroes[Random.Range(0, _heroes.Count)].PlaySpecial();
        }

        /// <summary>手前（ボタンの帯の下）以外の向きから、まわりの輪の上に出す</summary>
        private void SpawnWild(float distance)
        {
            var angle = Random.Range(SpawnAngleMin, SpawnAngleMax) * Mathf.Deg2Rad;
            var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            var speciesNo = Random.Range(1, SpeciesTable.Count + 1);
            _wilds.Add(new Wild(this, speciesNo, position));
        }

        /// <summary>この味方へ向かっている野生のペンギンのうち、技の届く所まで来たもの</summary>
        private Wild FindReached(Hero hero)
        {
            foreach (var wild in _wilds)
            {
                if (wild.Target == hero && wild.IsWaiting)
                {
                    return wild;
                }
            }

            return null;
        }

        /// <summary>寄ってくる相手は、いちばん近い味方</summary>
        private Hero NearestHero(Vector2 position)
        {
            Hero nearest = null;
            var best = float.MaxValue;
            foreach (var hero in _heroes)
            {
                var distance = (hero.Position - position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = hero;
                }
            }

            return nearest;
        }

        /// <summary>着地のまわりにいる野生のペンギンを、外へ強く吹っ飛ばす</summary>
        private void Shockwave(Vector2 center)
        {
            foreach (var wild in _wilds)
            {
                var offset = wild.Position - center;
                if (wild.CanBeHit && offset.sqrMagnitude < ShockwaveRadius * ShockwaveRadius)
                {
                    wild.Knock(offset, Random.Range(KnockSpeedMin, KnockSpeedMax) * ShockwaveBoost);
                }
            }
        }

        private static float SpawnInterval() => Random.Range(SpawnIntervalMin, SpawnIntervalMax);

        private static float SpecialInterval() => Random.Range(SpecialIntervalMin, SpecialIntervalMax);
    }
}
