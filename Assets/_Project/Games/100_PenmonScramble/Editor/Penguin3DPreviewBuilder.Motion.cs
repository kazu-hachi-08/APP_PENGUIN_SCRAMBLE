using System;
using System.IO;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ペンギンの動きを並べたスクショ（動きを人が確かめる）。
    /// motions.png: 縦に動き（MotionRows の順）、横にその動きの始めから終わりまでを等間隔に撮った姿勢。
    /// PenguinMotion.Tick で時刻を進めて撮るので、ゲーム中と同じ流れになる
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const int MotionCellSize = 220;
        private const int MotionSamples = 6;
        // 撃破で昇る・必殺技で跳ぶ分まで入るよう、姿勢のスクショより引いて少し上を狙う
        private const float MotionCameraDistance = 7.5f;
        private static readonly Vector3 MotionCameraTarget = new Vector3(0f, 1.0f, 0f);

        private const float WalkSpeed = 2.5f;
        private const float KnockSpeed = 11f;
        // 突進の予備動作の後からすべらせる
        private const float SlideLead = 0.2f;
        private const float DownFlailCycle = 0.33f;

        private readonly struct MotionRow
        {
            public readonly string Name;
            public readonly float Seconds;
            public readonly float PreRoll;
            public readonly Action<PenguinMotion> Start;

            /// <param name="preRoll">撮り始める前に進めておく秒数（歩き・ダウンのように、切り替わってから続く動き）</param>
            public MotionRow(string name, float seconds, Action<PenguinMotion> start, float preRoll = 0f)
            {
                Name = name;
                Seconds = seconds;
                PreRoll = preRoll;
                Start = start;
            }
        }

        private static readonly MotionRow[] MotionRows =
        {
            new MotionRow("まばたき", 0.15f, m => m.PlayBlink()),
            new MotionRow("歩く", 1f / (WalkSpeed * 0.9f), m => m.Velocity = Vector3.back * WalkSpeed, preRoll: 0.5f),
            AttackRow("技: 近接", SkillShape.Melee),
            AttackRow("技: 弾", SkillShape.Projectile),
            AttackRow("技: 突進の出だし", SkillShape.Dash),
            AttackRow("技: 拡散", SkillShape.Spread),
            AttackRow("技: 周囲", SkillShape.Nova),
            AttackRow("技: 設置", SkillShape.Zone),
            AttackRow("技: 落下", SkillShape.Drop),
            new MotionRow("すべる（突進中）", 0.5f, m => m.Sliding = true, preRoll: SlideLead),
            new MotionRow("必殺技・合体技", PenguinMotion.SpecialSeconds, m => m.PlaySpecial()),
            new MotionRow("被弾", PenguinMotion.HitSeconds, m => m.PlayHit()),
            new MotionRow("吹っ飛び", KnockSpeed / Balance.KnockbackDeceleration, m => m.PlayKnockback(Vector3.left * KnockSpeed)),
            new MotionRow("ダウン", DownFlailCycle, m => m.Down = true, preRoll: 0.6f),
            new MotionRow("撃破", PenguinMotion.DefeatSeconds, m => m.PlayDefeat()),
        };

        private static MotionRow AttackRow(string name, SkillShape shape) => new MotionRow(name, PenguinMotion.AttackSeconds, m => m.PlayAttack(shape));

        [MenuItem(EditorMenu.Capture + "Penguin Motions", priority = EditorMenu.CapturePriority + 3)]
        public static void CaptureMotions()
        {
            var stage = FindOrOpenPreviewStage();
            if (stage == null) return;

            Directory.CreateDirectory(CaptureFolder);
            var shadows = stage.RealShadows;
            var player = stage.Player;
            var spot = player.transform.localPosition;
            try
            {
                BeginPreviewCapture(stage);
                Save(CaptureMotionSheet(stage, player), "motions.png");
            }
            finally
            {
                stage.SetRealShadows(shadows);
                player.transform.localPosition = spot;
                foreach (var penguin in stage.Penguins)
                {
                    penguin.gameObject.SetActive(true);
                    penguin.ResetPose();
                }
            }

            Debug.Log($"[Penguin3DPreviewBuilder] {Path.GetFullPath(CaptureFolder)} に動きのスクショを保存しました（上から {RowNames()}）");
        }

        private static Texture2D CaptureMotionSheet(PenguinPreviewStage stage, PenguinMotion player)
        {
            var sheet = new Texture2D(MotionCellSize * MotionSamples, MotionCellSize * MotionRows.Length, TextureFormat.RGB24, false);
            foreach (var penguin in stage.Penguins)
            {
                penguin.gameObject.SetActive(penguin == player);
            }

            player.transform.localPosition = Vector3.zero;
            player.ResetPose();
            player.SnapFacing(Vector3.back);
            stage.Camera.aspect = 1f;
            PlaceCamera(stage.Camera, player.transform.position, MotionCameraTarget, MotionCameraDistance);
            for (var row = 0; row < MotionRows.Length; row++)
            {
                CaptureMotionRow(stage, player, MotionRows[row], sheet, MotionRows.Length - 1 - row);
            }

            stage.Camera.ResetAspect();
            return sheet;
        }

        /// <summary>1つの動きを始めから終わりまで等間隔に撮って、sheet の y 段目（下から）に並べる</summary>
        private static void CaptureMotionRow(PenguinPreviewStage stage, PenguinMotion player, MotionRow row, Texture2D sheet, int y)
        {
            player.ResetPose();
            row.Start(player);
            SampleRow(stage.Camera, player.Tick, row.PreRoll, row.Seconds, sheet, MotionCellSize, y);
        }

        /// <summary>preRoll 進めてから seconds かけて MotionSamples 枚撮り、cellSize 四方のセルを sheet の y 段目に並べる（ボスの動きと共通）</summary>
        private static void SampleRow(Camera camera, Action<float> tick, float preRoll, float seconds, Texture2D sheet, int cellSize, int y)
        {
            tick(preRoll);
            var step = seconds / (MotionSamples - 1);
            for (var col = 0; col < MotionSamples; col++)
            {
                if (col > 0)
                {
                    tick(step);
                }

                Blit(sheet, Render(camera, cellSize, cellSize), col * cellSize, y * cellSize);
            }
        }

        private static void PlaceCamera(Camera camera, Vector3 penguin, Vector3 target, float distance)
        {
            camera.orthographic = false;
            camera.fieldOfView = PoseCameraFov;
            var rotation = Quaternion.Euler(PoseCameraPitch, PoseCameraYaw, 0f);
            camera.transform.SetPositionAndRotation(penguin + target - rotation * Vector3.forward * distance, rotation);
        }

        private static string RowNames()
        {
            var names = new string[MotionRows.Length];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = MotionRows[i].Name;
            }

            return string.Join("・", names);
        }
    }
}
