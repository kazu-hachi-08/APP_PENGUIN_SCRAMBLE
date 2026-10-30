using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボスの見た目（§6.2）: 赤い範囲から攻撃の構えを読み取って動きに渡し、範囲が消えた（攻撃が出た）瞬間に攻撃の動きをさせる。
    /// もぐっている間は床の下へ沈み、暗い影が波紋を立てながら飛び出す場所へ進む（見た目だけ。Core の位置は飛び出すまで変わらない）
    /// </summary>
    public partial class UnitView
    {
        private static readonly Color UnderShadowColor = new Color(0.04f, 0.07f, 0.14f, 0.6f);
        // 暗い影は当たりの円より少し大きく、ゆらして床の下で動いているように見せる
        private const float UnderShadowRatio = 1.3f;
        private const float UnderShadowWobble = 0.08f;
        private const float UnderShadowWobbleSpeed = 9f;
        private static readonly Color RippleColor = new Color(1f, 1f, 1f, 0.5f);
        private const float RippleInterval = 0.22f;
        private const float RippleSeconds = 0.5f;
        // 予告の間の、影が進み始める・着く割合。沈みきってから動き出し、飛び出す少し前に着いて、出る場所を読めるようにする
        private const float TravelStart = 0.35f;
        private const float TravelEnd = 0.9f;

        private BigBossMotion _bigBoss;
        private BossAttackKind? _telegraph;
        private SpriteRenderer _underShadow;
        private Vector2 _ambushSpot;
        private bool _hasAmbushSpot;
        private float _travelFrom;
        private float _travelTo;
        private float _rippleTimer;

        public bool IsBigBoss => _bigBoss != null;

        private void BuildBigBoss()
        {
            var boss = BigBossModelBuilder.Build(SpeciesNo, _materials.Toon, transform);
            _bigBoss = boss.gameObject.AddComponent<BigBossMotion>();
            _model = boss;
            _motion = _bigBoss;
            _modelScale = BigBossModelBuilder.ScaleForRadius(_radius);
        }

        /// <summary>
        /// スナップショットの赤い範囲を受け取る（StageView が大ボスにだけ渡す。PushState の後に呼ぶ）。
        /// 範囲が消えたら、構えていた攻撃が出た
        /// </summary>
        public void PushWarnings(IReadOnlyList<WarningState> warnings)
        {
            var kind = BigBossTelegraph.Read(warnings, _to, IsSubmerged);
            if (_telegraph.HasValue && !kind.HasValue)
            {
                _bigBoss.PlayStrike(_telegraph.Value);
            }

            _telegraph = kind;
            _bigBoss.Telegraph = kind;
            _travelFrom = _travelTo;
            _hasAmbushSpot = IsSubmerged && BigBossTelegraph.TryGetAmbushSpot(warnings, out _ambushSpot, out _travelTo);
            if (!_hasAmbushSpot)
            {
                _travelFrom = _travelTo = 0f;
            }
        }

        /// <summary>もぐった・飛び出した瞬間に、しぶき（煙）を上げる</summary>
        private void OnSubmergedChanged()
        {
            Poof();
            if (!IsSubmerged)
            {
                // Core は飛び出す場所へ一気に移したので、もぐった場所から補間しない（影はもう着いている）
                _from = _to;
                _hasAmbushSpot = false;
            }
        }

        /// <summary>もぐっている間は、暗い影をもぐった場所から飛び出す場所へ進める</summary>
        private Vector2 RenderPosition(float alpha)
        {
            var position = Vector2.Lerp(_from, _to, alpha);
            if (!_hasAmbushSpot || IsDefeated)
            {
                return position;
            }

            var progress = Mathf.Lerp(_travelFrom, _travelTo, alpha);
            var travel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TravelStart, TravelEnd, progress));
            return Vector2.Lerp(position, _ambushSpot, travel);
        }

        private void TickUnderground(float dt)
        {
            if (_bigBoss == null)
            {
                return;
            }

            var under = IsSubmerged && !IsDefeated;
            if (_underShadow == null)
            {
                if (!under)
                {
                    return;
                }

                _underShadow = CreateDecal("UnderShadow", PlaceholderSprites.Circle, UnderShadowColor, FloorDecal.ReviveHeight, 1f);
            }

            _underShadow.enabled = under;
            if (!under)
            {
                return;
            }

            // 沈むにつれて濃くなる
            var depth = _bigBoss.Pose.Submerge;
            var wobble = 1f + UnderShadowWobble * Mathf.Sin(_statusTime * UnderShadowWobbleSpeed);
            FloorDecal.SetSize(_underShadow, new Vector2(wobble, 2f - wobble) * (_radius * 2f * UnderShadowRatio));
            _underShadow.color = new Color(UnderShadowColor.r, UnderShadowColor.g, UnderShadowColor.b, UnderShadowColor.a * depth);
            EmitRipples(dt);
        }

        /// <summary>床の下を進む間、影のまわりに波紋を立てる</summary>
        private void EmitRipples(float dt)
        {
            if (!_bigBoss.IsUnderground)
            {
                _rippleTimer = 0f;
                return;
            }

            _rippleTimer -= dt;
            if (_rippleTimer > 0f)
            {
                return;
            }

            _rippleTimer = RippleInterval;
            var floor = new Vector2(transform.position.x, transform.position.z);
            BurstFx.SpawnOnFloor(transform.parent, PlaceholderSprites.Ring, _materials.Decal, RippleColor, floor, _radius * 0.6f, _radius * UnderShadowRatio * 1.3f, RippleSeconds);
        }
    }
}
