using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 試作の場の操作（Play 中だけ）。1体（No.1）をキーで動かし、ほかのペンギンは決まった円を歩かせて、歩く姿を色々な向きから見られるようにする
    /// </summary>
    [RequireComponent(typeof(PenguinPreviewStage))]
    public sealed class PenguinPreviewInput : MonoBehaviour
    {
        private const float PlayerSpeed = 4f;
        private const float WanderSpeed = 1.5f;
        // 並んだ隣のペンギンにぶつからない大きさ
        private const float WanderRadius = 0.7f;
        // ほかのペンギンが技・被弾をくり返す間隔（秒）。全員そろうと見づらいので1体ずつずらす
        private const float ActionInterval = 2.5f;

        private static readonly Rect HelpRect = new Rect(16f, 16f, 520f, 200f);

        private PenguinPreviewStage _stage;
        private Vector3[] _spots;
        private float _time;

        private void Start()
        {
            _stage = GetComponent<PenguinPreviewStage>();
            _spots = new Vector3[_stage.Penguins.Count];
            for (var i = 0; i < _spots.Length; i++)
            {
                _spots[i] = _stage.Penguins[i].transform.localPosition;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _stage.Player == null)
            {
                return;
            }

            _time += Time.deltaTime;
            MovePlayer(keyboard);
            HandleToggles(keyboard);
            MoveOthers();
        }

        private void MovePlayer(Keyboard keyboard)
        {
            var input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            var velocity = new Vector3(input.x, 0f, input.y).normalized * PlayerSpeed;

            var player = _stage.Player;
            var position = player.transform.localPosition + velocity * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -Balance.AreaWidth * 0.5f, Balance.AreaWidth * 0.5f);
            position.z = Mathf.Clamp(position.z, -Balance.AreaHeight * 0.5f, Balance.AreaHeight * 0.5f);
            player.transform.localPosition = position;
            player.Velocity = velocity;

            if (keyboard.jKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) player.PlayAttack();
            if (keyboard.kKey.wasPressedThisFrame) player.PlayHit();
        }

        private void HandleToggles(Keyboard keyboard)
        {
            if (keyboard.tKey.wasPressedThisFrame) _stage.SetRealShadows(!_stage.RealShadows);
            if (keyboard.oKey.wasPressedThisFrame) _stage.SetOutline(!_stage.Outline);
            if (keyboard.cKey.wasPressedThisFrame) _stage.SetCameraPreset(_stage.CameraPreset + 1);
        }

        /// <summary>ほかのペンギン: 半分は円を歩き、半分はその場で技・被弾をくり返す</summary>
        private void MoveOthers()
        {
            for (var i = 1; i < _stage.Penguins.Count; i++)
            {
                var penguin = _stage.Penguins[i];
                if (i % 2 == 1)
                {
                    Wander(penguin, i);
                }
                else if (Crossed(i))
                {
                    if (i % 4 == 0) penguin.PlayAttack();
                    else penguin.PlayHit();
                }
            }
        }

        private void Wander(PenguinMotion penguin, int index)
        {
            var angle = _time * WanderSpeed / WanderRadius + index;
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * WanderRadius;
            penguin.transform.localPosition = _spots[index] + offset;
            penguin.Velocity = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * WanderSpeed;
        }

        private bool Crossed(int index)
        {
            var offset = index * 0.37f;
            return Mathf.Floor((_time + offset) / ActionInterval) != Mathf.Floor((_time + offset - Time.deltaTime) / ActionInterval);
        }

        private void OnGUI()
        {
            var shadow = _stage.RealShadows ? "本物の影" : "丸い影";
            var outline = _stage.Outline ? "あり" : "なし";
            GUI.Label(HelpRect,
                "WASD / 矢印: 歩く　J / Space: 技　K: 被弾\n" +
                $"T: 影の切り替え（{shadow}）\n" +
                $"O: 縁の線（{outline}）\n" +
                $"C: カメラの切り替え（{PreviewCameraPresets.Name(_stage.CameraPreset)}）");
        }
    }
}
