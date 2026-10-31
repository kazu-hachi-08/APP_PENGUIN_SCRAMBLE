using System;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 試作の場で、1体にペンギンの動きを順に全部やらせる。歩くのは別（試作の場で歩かせる）
    /// </summary>
    public sealed class PenguinMotionDemo
    {
        // 吹っ飛びの見本の初速（マス/秒）。1回転するくらいの強さ
        private const float KnockSpeed = 11f;
        private const float SlideSeconds = 0.6f;
        private const float DownSeconds = 2.2f;
        // 突進の予備動作が終わってからすべり始める
        private const float MotionAttackLead = PenguinMotion.AttackSeconds * 0.45f;

        private readonly struct Step
        {
            public readonly string Name;
            public readonly float Seconds;
            public readonly Action<PenguinMotion> Start;
            public readonly Action<PenguinMotion, float> During;

            public Step(string name, float seconds, Action<PenguinMotion> start, Action<PenguinMotion, float> during = null)
            {
                Name = name;
                Seconds = seconds;
                Start = start;
                During = during;
            }
        }

        private readonly Step[] _steps;
        private readonly PenguinMotion _motion;
        private int _index = -1;
        private float _elapsed;

        /// <summary>吹っ飛びの見本を始めた（初速）。試作の場で実際に飛ばして動かすため</summary>
        public event Action<Vector3> Knocked;

        public PenguinMotionDemo(PenguinMotion motion)
        {
            _motion = motion;
            _steps = new[]
            {
                new Step("立つ・まばたき", 1.2f, m => m.PlayBlink()),
                Attack("技: 近接", SkillShape.Melee),
                Attack("技: 弾", SkillShape.Projectile),
                Attack("技: 拡散", SkillShape.Spread),
                Attack("技: 周囲", SkillShape.Nova),
                Attack("技: 設置", SkillShape.Zone),
                Attack("技: 落下", SkillShape.Drop),
                new Step("技: 突進（すべる）", 1.4f, m => m.PlayAttack(SkillShape.Dash), (m, t) => m.Sliding = t > MotionAttackLead && t < MotionAttackLead + SlideSeconds),
                new Step("必殺技・合体技", 1.4f, m => m.PlaySpecial()),
                new Step("被弾", 0.8f, m => m.PlayHit()),
                new Step("吹っ飛び", 1.4f, Knock),
                new Step("ダウン", DownSeconds + 0.8f, m => m.Down = true, (m, t) => m.Down = t < DownSeconds),
                new Step("撃破", 1.2f, m => m.PlayDefeat()),
            };
        }

        public bool Running => _index >= 0;

        public string StepName => Running ? $"{_index + 1}/{_steps.Length} {_steps[_index].Name}" : string.Empty;

        public void Start(int from = 0)
        {
            _index = Mathf.Clamp(from, 0, _steps.Length - 1) - 1;
            Next();
        }

        public void Stop()
        {
            _index = -1;
            _motion.ResetPose();
        }

        /// <param name="loop">最後まで行ったら最初からくり返す</param>
        public void Update(float dt, bool loop)
        {
            if (!Running)
            {
                return;
            }

            _elapsed += dt;
            var step = _steps[_index];
            step.During?.Invoke(_motion, _elapsed);
            if (_elapsed < step.Seconds)
            {
                return;
            }

            if (_index == _steps.Length - 1 && !loop)
            {
                Stop();
                return;
            }

            Next();
        }

        private void Next()
        {
            // 撃破の後などで残った状態を消してから次へ
            _motion.ResetPose();
            _index = (_index + 1) % _steps.Length;
            _elapsed = 0f;
            _steps[_index].Start(_motion);
        }

        private static Step Attack(string name, SkillShape shape) => new Step(name, 0.9f, m => m.PlayAttack(shape));

        /// <summary>今向いている方向の右へ飛ばす（飛ばされた側へ向き直るので、カメラから横向きに転がるのが見える）</summary>
        private void Knock(PenguinMotion motion)
        {
            var velocity = motion.transform.rotation * (motion.GetComponent<PenguinModel>().Figure.localRotation * Vector3.right) * KnockSpeed;
            motion.PlayKnockback(velocity);
            Knocked?.Invoke(velocity);
        }
    }
}
