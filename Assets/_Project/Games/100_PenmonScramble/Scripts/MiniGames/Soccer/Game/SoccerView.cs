using System.Collections.Generic;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// サッカーの見た目（おもちゃのフットサル場）。ピッチの定義から地面・柵・ゴールを組み、毎フレーム球・子・キーパーを写す。
    /// 地面と白い線は SoccerView.Ground.cs、柵は .Boards.cs、ゴールの枠と網は .Goals.cs、
    /// 起きたこと（蹴った・ゴール・キーパー）への見た目の反応は .Events.cs。球・子・キーパーはそれぞれ別の部品
    /// </summary>
    public partial class SoccerView : MonoBehaviour
    {
        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Texture> _textures = new List<Texture>();
        private readonly List<SoccerPlayerView> _players = new List<SoccerPlayerView>();
        private readonly List<SoccerKeeperView> _keepers = new List<SoccerKeeperView>();
        private ToonMaterials _materials;
        private Transform _pitch;
        private SoccerBallView _ball;

        public ToonMaterials Materials => _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);

        /// <summary>
        /// ピッチを組み直す（前の物は消す）。world は景色（柵の色・外の地面の絵）を決めるワールドの番号、
        /// species は子ごとのペンモンの種類（world.Players と同じ並び）、wildTeam は CPU のチーム（暗い色にする。いなければ −1）
        /// </summary>
        public void Build(SoccerWorld world, int stadiumWorld, IReadOnlyList<int> species, int wildTeam = -1)
        {
            Clear();
            _pitch = new GameObject("Pitch").transform;
            _pitch.SetParent(transform, false);
            var theme = StageTheme.ForWorld(stadiumWorld);
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.Background;
            }

            var pitch = world.Pitch;
            BuildGround(pitch, stadiumWorld, theme);
            BuildBoards(pitch, theme);
            BuildGoals(pitch);
            _ball = new SoccerBallView(Materials, _pitch);
            foreach (var player in world.Players)
            {
                var speciesNo = player.Index < species.Count ? species[player.Index] : 1;
                _players.Add(new SoccerPlayerView(player, speciesNo, player.Team == wildTeam, Materials, _pitch));
            }

            foreach (var keeper in world.Keepers)
            {
                _keepers.Add(new SoccerKeeperView(keeper, Materials, _pitch));
            }
        }

        /// <summary>
        /// 人が動かしている子の印（足元の席の色の輪）。seatShown は席ごとの、印を付ける子（−1 なら無し）
        /// </summary>
        public void SetControllers(IReadOnlyList<int> seatShown)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                var seat = seatShown != null ? IndexOf(seatShown, i) : -1;
                _players[i].SetControlled(seat >= 0, seat >= 0 ? MiniGameSeats.ColorOf(seat) : Color.white);
            }
        }

        /// <summary>子の足元の位置（世界の座標。名前を頭の上に出すのに使う）。いなければ null</summary>
        public Vector3? PlayerPosition(int index) => index >= 0 && index < _players.Count ? _players[index].Position : (Vector3?)null;

        private static int IndexOf(IReadOnlyList<int> list, int value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>前の歩と今の歩の写しの間（t）を描く</summary>
        public void Draw(SoccerWorld world, SoccerFrame previous, SoccerFrame current, float t, float deltaTime)
        {
            var special = world.Special;
            Color? trail = special != null ? PenguinPalette.ElementColor(special.Element) : (Color?)null;
            _ball.SetSpecial(trail, special != null && special.IsHidden);
            _ball.Sync(Vector3.Lerp(previous.Ball, current.Ball, t));
            for (var i = 0; i < _players.Count; i++)
            {
                _players[i].Sync(world.Players[i], SoccerFrame.Lerp(previous.Players, current.Players, i, t), deltaTime);
            }

            for (var i = 0; i < _keepers.Count; i++)
            {
                _keepers[i].Sync(world.Keepers[i], SoccerFrame.Lerp(previous.Keepers, current.Keepers, i, t), world.Ball.KeeperTeam == world.Keepers[i].Team, deltaTime);
            }

            TickNets(deltaTime);
        }

        /// <summary>Unity の基本の形の代わりに、ToyMeshes の形を置く（当たりは Core が持つので Collider は付けない）</summary>
        private Transform AddPart(string objectName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj.transform;
        }

        /// <summary>このビューが作ったメッシュを置く（消すときに一緒に消す）</summary>
        private MeshRenderer AddOwnedMesh(string objectName, Mesh mesh, params Material[] materials)
        {
            _meshes.Add(mesh);
            var obj = new GameObject(objectName);
            obj.transform.SetParent(_pitch, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private void Clear()
        {
            if (_pitch != null)
            {
                Destroy(_pitch.gameObject);
            }

            foreach (var mesh in _meshes)
            {
                Destroy(mesh);
            }

            foreach (var texture in _textures)
            {
                Destroy(texture);
            }

            foreach (var player in _players)
            {
                player.Destroy();
            }

            _meshes.Clear();
            _textures.Clear();
            _players.Clear();
            _keepers.Clear();
            _nets.Clear();
            _ball = null;
        }

        private void OnDestroy()
        {
            Clear();
            _materials?.DestroyAll();
        }
    }
}
