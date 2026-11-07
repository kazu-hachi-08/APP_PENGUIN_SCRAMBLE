using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 守備の子（飾りと演出。結果は穴だけで決まる）: アウトの穴の後ろに1体ずつ、ホームの方を向いて構える。キャッチャーはホームの後ろにしゃがむ。
    /// 打球を目で追い、アウトの穴に入ったらその穴の子が捕って喜ぶ。ヒットになったら球のいちばん近くの子が悔しがる。
    /// 投げた球がミットに収まったらキャッチャーが捕る
    /// </summary>
    public sealed class BaseballFielders
    {
        // 穴の縁から後ろへ離す長さ・キャッチャーの位置（バットの先が届かない所）
        private const float BehindHole = 0.38f;
        private static readonly Vector3 CatcherPoint = new Vector3(0f, 0f, -2.15f);
        private static readonly Vector3 MittOffset = new Vector3(0f, 0.3f, 0.3f);
        private const float ReactSeconds = 1.4f;

        private readonly BaseballFieldDef _field;
        private readonly ToonMaterials _materials;
        private readonly Transform _parent;
        private readonly List<(int Hole, Vector3 Spot)> _spots = new List<(int, Vector3)>();
        private readonly List<BaseballPenguin> _fielders = new List<BaseballPenguin>();
        private BaseballPenguin _catcher;
        private BaseballPenguin _reacting;
        private float _reactTimer;

        /// <summary>キャッチャーのミットの所（投げた球はここへ収まって消える）</summary>
        public static Vector3 MittPoint => CatcherPoint + MittOffset;

        public BaseballFielders(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            _field = field;
            _materials = materials;
            _parent = parent;
            for (var i = 0; i < field.Holes.Count; i++)
            {
                var hole = field.Holes[i];
                if (hole.IsOut)
                {
                    _spots.Add((i, SpotBehind(hole, hole.Center)));
                }
            }
        }

        private static Vector3 SpotBehind(BaseballHole hole, Vec2 center)
        {
            return BaseballSpace.ToWorld(center + center.Normalized * (hole.Radius + BehindHole));
        }

        /// <summary>動く穴（嵐の崖）の後ろの子を、穴の見えている位置について行かせる</summary>
        public void FollowHoles(System.Func<int, Vec2> shownAt)
        {
            for (var i = 0; i < _spots.Count && i < _fielders.Count; i++)
            {
                var hole = _field.Holes[_spots[i].Hole];
                if (hole.Moves)
                {
                    _fielders[i].Root.localPosition = SpotBehind(hole, shownAt(_spots[i].Hole));
                }
            }
        }

        /// <summary>
        /// 守る側の子を並べ直す（前の子は消す）。species は守る側の編成（足りない分はくり返して使う）
        /// </summary>
        public void Use(IReadOnlyList<int> species, bool wild)
        {
            Clear();
            for (var i = 0; i < _spots.Count; i++)
            {
                var body = BaseballPenguin.Build($"Fielder{i}", species[i % species.Count], wild, _materials, _parent);
                body.PlaceAt(_spots[i].Spot, -_spots[i].Spot, true);
                _fielders.Add(body);
            }

            _catcher = BaseballPenguin.Build("Catcher", species[species.Count - 1], wild, _materials, _parent);
            _catcher.PlaceAt(CatcherPoint, Vector3.forward, true);
            Ready();
        }

        /// <summary>みんな構えに戻る（守備の子はホームを、キャッチャーはマウンドを向く）</summary>
        public void Ready()
        {
            for (var i = 0; i < _fielders.Count; i++)
            {
                var body = _fielders[i];
                body.React(false, false);
                body.Motion.Facing = -_spots[i].Spot;
                body.Motion.Fielding = true;
            }

            if (_catcher != null)
            {
                _catcher.React(false, false);
                _catcher.Motion.Facing = Vector3.forward;
                _catcher.Motion.Fielding = true;
            }

            _reacting = null;
            _reactTimer = 0f;
        }

        public void Tick(BaseballBoard board, Vector3 ball, bool ballVisible, float deltaTime)
        {
            var watching = ballVisible && board.Phase == BaseballBoardPhase.BallInPlay;
            foreach (var body in _fielders)
            {
                body.Tick(deltaTime);
                if (watching)
                {
                    body.Face(ball);
                }
            }

            _catcher?.Tick(deltaTime);
            TickReaction(deltaTime);
        }

        public void OnEvent(BaseballEvent e)
        {
            switch (e.Kind)
            {
                case BaseballEventKind.Catch:
                    _catcher?.Motion.PlayCatch();
                    break;
                case BaseballEventKind.HoleIn when BaseballResults.IsOut(e.Result):
                    Catch(e.Index);
                    break;
                case BaseballEventKind.HoleIn:
                case BaseballEventKind.HomeRun:
                    Miss(e.Position);
                    break;
                case BaseballEventKind.Stop when !BaseballResults.IsOut(e.Result):
                    Miss(e.Position);
                    break;
            }
        }

        /// <summary>アウトの穴の子が捕って、少しして喜ぶ</summary>
        private void Catch(int hole)
        {
            var index = _spots.FindIndex(s => s.Hole == hole);
            if (index < 0)
            {
                return;
            }

            var body = _fielders[index];
            body.Motion.Fielding = false;
            body.Motion.PlayCatch();
            body.React(true, false);
            StartReaction(body);
        }

        /// <summary>ヒットの球にいちばん近い守備の子が悔しがる</summary>
        private void Miss(Vec2 at)
        {
            var point = BaseballSpace.ToWorld(at);
            BaseballPenguin nearest = null;
            var best = float.MaxValue;
            foreach (var body in _fielders)
            {
                var distance = (body.Root.localPosition - point).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = body;
                }
            }

            if (nearest == null)
            {
                return;
            }

            nearest.Motion.Fielding = false;
            nearest.React(false, true);
            StartReaction(nearest);
        }

        private void StartReaction(BaseballPenguin body)
        {
            _reacting = body;
            _reactTimer = ReactSeconds;
        }

        private void TickReaction(float deltaTime)
        {
            if (_reacting == null)
            {
                return;
            }

            _reactTimer -= deltaTime;
            if (_reactTimer > 0f)
            {
                return;
            }

            _reacting.React(false, false);
            _reacting.Motion.Fielding = true;
            _reacting = null;
        }

        private void Clear()
        {
            foreach (var body in _fielders)
            {
                Object.Destroy(body.Root.gameObject);
            }

            _fielders.Clear();
            if (_catcher != null)
            {
                Object.Destroy(_catcher.Root.gameObject);
                _catcher = null;
            }

            _reacting = null;
        }
    }
}
