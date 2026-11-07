using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 投げる子: マウンドの上でホームを向いて立つ。振りかぶる（WindUp）→ 投げる（Release。ここで球が手を離れる）→ 球を目で追う。
    /// 三振・アウトで喜び、ヒットで悔しがる。種類ごとの体は一度作ったら取っておく（投げる子を替えても作り直さない）
    /// </summary>
    public sealed class BaseballPitcherView
    {
        // マウンドの球の出る所より少し後ろに立つ（投げた球が体から出てくるように）
        private const float StandBehind = 0.25f;
        // マウンドの丸い山の上に立つ高さ（子たちは盤から少し持ち上げてあるので、その分を引いた残り）
        private const float MoundTop = 0.03f;

        private readonly Dictionary<(int, bool), BaseballPenguin> _bodies = new Dictionary<(int, bool), BaseballPenguin>();
        private readonly BaseballFieldDef _field;
        private readonly ToonMaterials _materials;
        private readonly Transform _parent;
        private BaseballPenguin _current;

        public BaseballPitcherView(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            _field = field;
            _materials = materials;
            _parent = parent;
        }

        private Vector3 StandPoint => BaseballSpace.ToWorld(_field.Mound, MoundTop) + new Vector3(0f, 0f, StandBehind);

        public void Use(int species, bool wild)
        {
            // 同じ種類が両方のチームにいることがあるので、CPU の暗い色の体とは別に持つ
            var key = (species, wild);
            if (!_bodies.TryGetValue(key, out var body))
            {
                body = BaseballPenguin.Build($"Pitcher{species}", species, wild, _materials, _parent);
                _bodies.Add(key, body);
            }

            var changed = _current != body;
            _current?.SetActive(false);
            _current = body;
            body.SetActive(true);
            Ready(changed);
        }

        /// <summary>マウンドでホームを向いて構える</summary>
        public void Ready(bool pop)
        {
            if (_current == null)
            {
                return;
            }

            _current.Motion.ResetPose();
            _current.PlaceAt(StandPoint, Vector3.back, pop);
        }

        /// <summary>振りかぶりの深さ（0〜1）</summary>
        public void WindUp(float depth)
        {
            if (_current != null)
            {
                _current.Motion.ThrowCharge = depth;
            }
        }

        public void Release() => _current?.Motion.PlayThrow();

        public void Tick(BaseballBoard board, Vector3 ball, bool ballVisible, float deltaTime)
        {
            if (_current == null)
            {
                return;
            }

            _current.Tick(deltaTime);
            if (ballVisible && board.Phase == BaseballBoardPhase.BallInPlay)
            {
                _current.Face(ball);
            }
        }

        public void React(bool cheer, bool despair) => _current?.React(cheer, despair);
    }
}
