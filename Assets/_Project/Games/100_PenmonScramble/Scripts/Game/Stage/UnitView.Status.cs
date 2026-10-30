using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニットの状態の見た目（§9）: 野生のペンギンは暗い色・被弾で一瞬白く光る・凍ると水色で氷に包まれる・ダウンは灰色・
    /// 無敵は点滅・突進は残像・交代は煙が出て、新しい子がぽんと出てくる
    /// </summary>
    public partial class UnitView
    {
        // 野生のペンギンは同じ形を暗い色で出す（大ボスは野生ではないので暗くしない）。
        // 2D の絵より暗く見えやすい（トゥーンの影の段でさらに暗くなる）ので、2D の 0.55 より少し浅くする
        private const float EnemyDarkness = 0.45f;

        private const float FlashSeconds = 0.1f;
        private const float FlashAmount = 0.85f;
        private static readonly Color FrozenTint = new Color(0.62f, 0.9f, 1f);
        private const float FrozenTintAmount = 0.55f;
        // 2D（0.6）より強くする。赤い種類でも、倒れていると一目で分かる灰色に寄せるため
        private static readonly Color DownTint = new Color(0.55f, 0.55f, 0.6f);
        private const float DownTintAmount = 0.75f;
        // 無敵の間は点滅させて、攻撃が効かないことを分かるようにする
        private const float InvincibleBlinkPerSecond = 12f;

        // 交代で出てきた子は、小さく出てから一度はみ出して元の大きさに落ち着く
        private const float PopSeconds = 0.3f;
        private const float PopStart = 0.5f;

        private float _darkness;
        private float _flashTimer;
        private float _popAge = PopSeconds;
        private float _statusTime;
        private IceBlock _iceBlock;
        private AfterimageTrail _afterimage;

        private bool IsDefeated => _defeatTimer >= 0f;
        private bool IsFrozen => (_flags & UnitFlags.Frozen) != 0 && !IsDefeated;
        private bool IsDashing => (_flags & UnitFlags.Dashing) != 0 && !IsDefeated;
        private bool IsInvincible => (_flags & UnitFlags.Invincible) != 0 && !IsDefeated;

        /// <summary>状態の見た目を dt 秒進める（毎フレーム。テストからも呼ぶ）</summary>
        public void TickStatus(float dt)
        {
            _statusTime += dt;
            _flashTimer -= dt;
            TickPop(dt);
            TickUnderground(dt);
            ApplyStatus();
        }

        /// <summary>組み直したら、暗さ・氷の大きさ・残像の写し元を新しい体に合わせる</summary>
        private void OnModelBuilt(UnitState unit)
        {
            _darkness = unit.Team == Team.Enemy && BigBossTable.Get(unit.SpeciesNo) == null ? EnemyDarkness : 0f;
            if (_iceBlock != null)
            {
                FitIce();
            }

            if (_afterimage != null)
            {
                _afterimage.Bind(_model, _materials.Toon.Ghost, transform.parent);
            }

            // 1フレーム目から暗い色・凍った姿で出す
            ApplyStatus();
        }

        private void ApplyStatus()
        {
            _model.SetTint(CurrentTint());
            SetFigureVisible(IsFigureVisible());
            _motion.Frozen = IsFrozen;
            UpdateIce();
            UpdateAfterimage();
        }

        /// <summary>白く光る ＞ ダウンの灰色 ＞ 凍った水色 の順に優先する（同時になったら、より新しい出来事を見せる）</summary>
        private PenguinTint CurrentTint()
        {
            var tint = PenguinTint.Dark(_darkness);
            if (_flashTimer > 0f)
            {
                return tint.Toward(Color.white, FlashAmount);
            }

            if (IsDown)
            {
                return tint.Toward(DownTint, DownTintAmount);
            }

            return IsFrozen ? tint.Toward(FrozenTint, FrozenTintAmount) : tint;
        }

        private bool IsFigureVisible()
        {
            if (IsDefeated)
            {
                return true;
            }

            // もぐっている大ボスは、床の下へ沈みきったら体を出さず、床の暗い影だけ見せる（UnitView.BigBoss.cs）
            if (IsSubmerged)
            {
                return _bigBoss != null && !_bigBoss.IsUnderground;
            }

            return !IsInvincible || Mathf.Repeat(_statusTime * InvincibleBlinkPerSecond, 1f) < 0.5f;
        }

        private void SetFigureVisible(bool visible)
        {
            var figure = _model.Figure.gameObject;
            if (figure.activeSelf != visible)
            {
                figure.SetActive(visible);
            }
        }

        /// <summary>氷は初めて凍ったときに作り、あとは出し入れする（凍らない敵の分まで作らないように）</summary>
        private void UpdateIce()
        {
            if (_iceBlock == null)
            {
                if (!IsFrozen)
                {
                    return;
                }

                _iceBlock = IceBlock.Create(transform, transform.parent, _materials);
                FitIce();
            }

            _iceBlock.Show(IsFrozen);
        }

        private void FitIce() => _iceBlock.Fit(_radius * 2f, _model.TopHeight * _modelScale);

        /// <summary>残像も初めて突進したときに作る</summary>
        private void UpdateAfterimage()
        {
            var emitting = IsDashing && !IsSubmerged;
            if (_afterimage == null)
            {
                if (!emitting)
                {
                    return;
                }

                _afterimage = gameObject.AddComponent<AfterimageTrail>();
                _afterimage.Bind(_model, _materials.Toon.Ghost, transform.parent);
            }

            _afterimage.Emitting = emitting;
        }

        /// <summary>ぽんと煙が出る（交代・ダウン・起き上がり）</summary>
        private void Poof()
        {
            SmokePuff.Spawn(transform.parent, _materials, _to, _radius, _model.TopHeight * _modelScale);
        }

        private void PopIn()
        {
            _popAge = 0f;
            TickPop(0f);
        }

        private void TickPop(float dt)
        {
            if (_popAge >= PopSeconds)
            {
                return;
            }

            _popAge += dt;
            _model.transform.localScale = Vector3.one * (_modelScale * ToyEasing.Pop(_popAge / PopSeconds, PopStart));
        }
    }
}
