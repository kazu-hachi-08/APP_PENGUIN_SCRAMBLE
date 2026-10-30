using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 試作の場の操作（Play 中だけ）。1体（No.1）をキーで動かし、ほかのペンギンは半分が決まった円を歩き、半分がその場で動きを順にくり返す。
    /// 歩く姿・全部の動きを色々な向きから見られるようにする
    /// </summary>
    [RequireComponent(typeof(PenguinPreviewStage))]
    public sealed class PenguinPreviewInput : MonoBehaviour
    {
        private const float PlayerSpeed = 4f;
        // 突進ですべる速さ
        private const float SlideSpeed = 8f;
        private const float WanderSpeed = 1.5f;
        // 並んだ隣のペンギンにぶつからない大きさ
        private const float WanderRadius = 0.7f;
        // その場で動きをくり返すペンギンの、始める動きのずらし方（全員そろうと見づらいので）
        private const int DemoStagger = 3;
        // 吹っ飛びの初速（マス/秒）。強いと転がり、弱いとのけぞるだけ
        private const float StrongKnock = 12f;
        private const float WeakKnock = 5f;

        private static readonly Rect HelpRect = new Rect(16f, 16f, 560f, 260f);

        private static readonly (Key key, SkillShape shape)[] AttackKeys =
        {
            (Key.Digit1, SkillShape.Melee),
            (Key.Digit2, SkillShape.Projectile),
            (Key.Digit3, SkillShape.Dash),
            (Key.Digit4, SkillShape.Spread),
            (Key.Digit5, SkillShape.Nova),
            (Key.Digit6, SkillShape.Zone),
            (Key.Digit7, SkillShape.Drop),
        };

        private readonly List<PenguinMotionDemo> _demos = new List<PenguinMotionDemo>();
        private PenguinPreviewStage _stage;
        private PenguinMotionDemo _playerDemo;
        private Vector3[] _spots;
        private Vector3 _knockVelocity;
        private float _time;

        private void Start()
        {
            _stage = GetComponent<PenguinPreviewStage>();
            _spots = new Vector3[_stage.Penguins.Count];
            for (var i = 0; i < _spots.Length; i++)
            {
                _spots[i] = _stage.Penguins[i].transform.localPosition;
                if (i > 0 && i % 2 == 0)
                {
                    var demo = new PenguinMotionDemo(_stage.Penguins[i]);
                    demo.Start(i / 2 * DemoStagger);
                    _demos.Add(demo);
                }
            }

            if (_stage.Player != null)
            {
                _playerDemo = new PenguinMotionDemo(_stage.Player);
                _playerDemo.Knocked += velocity => _knockVelocity = velocity;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _stage.Player == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            _time += dt;
            HandlePlayerActions(keyboard);
            MovePlayer(keyboard, dt);
            _playerDemo.Update(dt, loop: false);
            HandleToggles(keyboard);
            MoveOthers(dt);
        }

        private void HandlePlayerActions(Keyboard keyboard)
        {
            var player = _stage.Player;
            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (_playerDemo.Running) _playerDemo.Stop();
                else _playerDemo.Start();
            }

            if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) player.PlayAttack(SkillShape.Melee);
            foreach (var (key, shape) in AttackKeys)
            {
                if (keyboard[key].wasPressedThisFrame) player.PlayAttack(shape);
            }

            if (keyboard.lKey.wasPressedThisFrame) player.PlaySpecial();
            if (keyboard.kKey.wasPressedThisFrame) player.PlayHit();
            if (keyboard.bKey.wasPressedThisFrame) Knock(player, StrongKnock);
            if (keyboard.gKey.wasPressedThisFrame) Knock(player, WeakKnock);
            if (keyboard.xKey.wasPressedThisFrame) player.Down = !player.Down;
            if (keyboard.vKey.wasPressedThisFrame) player.PlayDefeat();
            // 撃破の見本が終わったら、また立たせる
            if (player.IsDefeatFinished && !_playerDemo.Running) player.ResetPose();
        }

        /// <summary>今の向きの後ろへ飛ばす（前から攻撃を受けた向き）</summary>
        private void Knock(PenguinMotion player, float speed)
        {
            var back = player.transform.rotation * (player.GetComponent<PenguinModel>().Figure.localRotation * Vector3.back);
            _knockVelocity = new Vector3(back.x, 0f, back.z).normalized * speed;
            player.PlayKnockback(_knockVelocity);
        }

        private void MovePlayer(Keyboard keyboard, float dt)
        {
            var player = _stage.Player;
            var input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;

            // 吹っ飛び中は Core と同じく減速しながら飛ばされ、速いうちは動けない
            var stunned = _knockVelocity.magnitude > Balance.StunSpeed;
            _knockVelocity = Vector3.MoveTowards(_knockVelocity, Vector3.zero, Balance.KnockbackDeceleration * dt);
            var sliding = keyboard.shiftKey.isPressed && input != Vector2.zero && !player.Down;
            var speed = sliding ? SlideSpeed : PlayerSpeed;
            var velocity = stunned || player.Down || player.IsDefeatFinished ? Vector3.zero : new Vector3(input.x, 0f, input.y).normalized * speed;
            if (!_playerDemo.Running)
            {
                player.Sliding = sliding;
            }

            var position = player.transform.localPosition + (velocity + _knockVelocity) * dt;
            position.x = Mathf.Clamp(position.x, -Balance.AreaWidth * 0.5f, Balance.AreaWidth * 0.5f);
            position.z = Mathf.Clamp(position.z, -Balance.AreaHeight * 0.5f, Balance.AreaHeight * 0.5f);
            player.transform.localPosition = position;
            player.Velocity = velocity;
        }

        private void HandleToggles(Keyboard keyboard)
        {
            if (keyboard.tKey.wasPressedThisFrame) _stage.SetRealShadows(!_stage.RealShadows);
            if (keyboard.oKey.wasPressedThisFrame) _stage.SetOutline(!_stage.Outline);
            if (keyboard.cKey.wasPressedThisFrame) _stage.SetCameraPreset(_stage.CameraPreset + 1);
        }

        /// <summary>ほかのペンギン: 奇数番は円を歩き、偶数番はその場で動きを順にくり返す</summary>
        private void MoveOthers(float dt)
        {
            for (var i = 1; i < _stage.Penguins.Count; i++)
            {
                if (i % 2 == 1)
                {
                    Wander(_stage.Penguins[i], i);
                }
            }

            foreach (var demo in _demos)
            {
                demo.Update(dt, loop: true);
            }
        }

        private void Wander(PenguinMotion penguin, int index)
        {
            var angle = _time * WanderSpeed / WanderRadius + index;
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * WanderRadius;
            penguin.transform.localPosition = _spots[index] + offset;
            penguin.Velocity = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * WanderSpeed;
        }

        private void OnGUI()
        {
            var shadow = _stage.RealShadows ? "本物の影" : "丸い影";
            var outline = _stage.Outline ? "あり" : "なし";
            var demo = _playerDemo != null && _playerDemo.Running ? $"再生中: {_playerDemo.StepName}" : "P: 全部の動きを順に再生";
            GUI.Label(HelpRect,
                "WASD / 矢印: 歩く　Shift+移動: すべる（突進）\n" +
                "J / Space: 技（近接）　1〜7: 技（近接・弾・突進・拡散・周囲・設置・落下）\n" +
                "L: 必殺技　K: 被弾　B: 吹っ飛び（強）　G: 吹っ飛び（弱）　X: ダウン　V: 撃破\n" +
                $"{demo}\n" +
                $"T: 影の切り替え（{shadow}）　O: 縁の線（{outline}）\n" +
                $"C: カメラの切り替え（{PreviewCameraPresets.Name(_stage.CameraPreset)}）");
        }
    }
}
