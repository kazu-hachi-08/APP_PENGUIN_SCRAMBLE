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
    /// 大ボス3体のスクショ（形と動きを人が確かめる）。試作の場に一時的に組んで撮り、撮り終えたら消す。
    /// boss_skua.png / boss_seal.png / boss_orca.png: 縦に動き、横にその動きの始めから終わりまでを6枚。
    /// bosses_area.png: ゲームのカメラでエリア全体（ペンギンと並べた大きさ）
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const int BossCellSize = 240;
        private const float BossCameraDistance = 12f;
        private static readonly Vector3 BossCameraTarget = new Vector3(0f, 1.1f, 0f);
        private const int BossAreaWidth = 1920;
        private const int BossAreaHeight = 1080;
        private const float BossMoveSpeed = 2f;

        private readonly struct BossRow
        {
            public readonly string Name;
            public readonly float Seconds;
            public readonly float PreRoll;
            public readonly Action<BigBossMotion> Start;

            public BossRow(string name, float seconds, Action<BigBossMotion> start, float preRoll = 0f)
            {
                Name = name;
                Seconds = seconds;
                PreRoll = preRoll;
                Start = start;
            }
        }

        private static readonly BossRow[] SkuaRows =
        {
            new BossRow("羽ばたき", 1f / 1.6f, m => { }),
            new BossRow("移動", 1f / 2.1f, m => m.Velocity = Vector3.back * BossMoveSpeed, preRoll: 0.5f),
            new BossRow("急降下の予告", 0.6f, m => m.Telegraph = BossAttackKind.Charge),
            new BossRow("急降下（突進）", 0.5f, m => { m.PlayStrike(BossAttackKind.Charge); m.Sliding = true; }),
            new BossRow("羽の雨の予告", 0.6f, m => m.Telegraph = BossAttackKind.Rain),
            new BossRow("羽の雨が出た", BigBossMotion.StrikeSeconds, m => m.PlayStrike(BossAttackKind.Rain)),
            new BossRow("被弾", BigBossMotion.HitSeconds, m => m.PlayHit()),
            new BossRow("撃破", BigBossMotion.DefeatSeconds, m => m.PlayDefeat()),
        };

        private static readonly BossRow[] SealRows =
        {
            new BossRow("休み", 1f, m => { }),
            new BossRow("這う", 1f / (0.35f + BossMoveSpeed * 0.55f), m => m.Velocity = Vector3.back * BossMoveSpeed, preRoll: 0.5f),
            new BossRow("体当たりの予告", 0.6f, m => m.Telegraph = BossAttackKind.Charge),
            new BossRow("体当たり（突進）", 0.5f, m => { m.PlayStrike(BossAttackKind.Charge); m.Sliding = true; }),
            new BossRow("もぐる", BigBossMotion.SubmergeSeconds, m => { m.Telegraph = BossAttackKind.Ambush; m.Submerged = true; }),
            new BossRow("飛び出す", BigBossMotion.EmergeSeconds, m => m.Submerged = false, preRoll: 0f),
            new BossRow("被弾", BigBossMotion.HitSeconds, m => m.PlayHit()),
            new BossRow("撃破", BigBossMotion.DefeatSeconds, m => m.PlayDefeat()),
        };

        private static readonly BossRow[] OrcaRows =
        {
            new BossRow("休み", 1f / 0.5f, m => { }),
            new BossRow("泳ぐ", 1f / (0.5f + BossMoveSpeed * 0.35f), m => m.Velocity = Vector3.back * BossMoveSpeed, preRoll: 0.5f),
            new BossRow("波の予告", 0.6f, m => m.Telegraph = BossAttackKind.Wave),
            new BossRow("波が出た", BigBossMotion.StrikeSeconds, m => { m.Telegraph = BossAttackKind.Wave; m.Tick(0.6f); m.Telegraph = null; m.PlayStrike(BossAttackKind.Wave); }),
            new BossRow("突進の予告", 0.6f, m => m.Telegraph = BossAttackKind.Charge),
            new BossRow("突進", 0.5f, m => { m.PlayStrike(BossAttackKind.Charge); m.Sliding = true; }),
            new BossRow("被弾", BigBossMotion.HitSeconds, m => m.PlayHit()),
            new BossRow("撃破", BigBossMotion.DefeatSeconds, m => m.PlayDefeat()),
        };

        [MenuItem(EditorMenu.Capture + "Boss Motions", priority = EditorMenu.CapturePriority + 4)]
        public static void CaptureBosses()
        {
            var stage = FindOrOpenPreviewStage();
            if (stage == null) return;

            Directory.CreateDirectory(CaptureFolder);
            var shadows = stage.RealShadows;
            var preset = stage.CameraPreset;
            var holder = new GameObject("BossCapture");
            holder.transform.SetParent(stage.Generated, false);
            try
            {
                BeginPreviewCapture(stage);
                var skua = AddBoss(stage, holder.transform, BigBossTable.SkuaNo);
                var seal = AddBoss(stage, holder.transform, BigBossTable.LeopardSealNo);
                var orca = AddBoss(stage, holder.transform, BigBossTable.OrcaNo);
                Save(CaptureBossSheet(stage, skua, SkuaRows), "boss_skua.png");
                Save(CaptureBossSheet(stage, seal, SealRows), "boss_seal.png");
                Save(CaptureBossSheet(stage, orca, OrcaRows), "boss_orca.png");
                Save(CaptureBossArea(stage, skua, seal, orca), "bosses_area.png");
            }
            finally
            {
                Object.DestroyImmediate(holder);
                stage.SetRealShadows(shadows);
                stage.SetCameraPreset(preset);
                foreach (var penguin in stage.Penguins)
                {
                    penguin.gameObject.SetActive(true);
                    penguin.ResetPose();
                }
            }

            Debug.Log($"[Penguin3DPreviewBuilder] {Path.GetFullPath(CaptureFolder)} に大ボスのスクショを保存しました");
        }

        /// <summary>ゲームと同じ大きさ（当たりの半径）で組む</summary>
        private static BigBossMotion AddBoss(PenguinPreviewStage stage, Transform parent, int no)
        {
            var model = BigBossModelBuilder.Build(no, stage.Materials, parent);
            model.transform.localScale = Vector3.one * BigBossModelBuilder.ScaleForRadius(BigBossTable.Get(no).Radius);
            model.SetRealShadows(false);
            model.gameObject.SetActive(false);
            return model.gameObject.AddComponent<BigBossMotion>();
        }

        private static Texture2D CaptureBossSheet(PenguinPreviewStage stage, BigBossMotion boss, BossRow[] rows)
        {
            var sheet = new Texture2D(BossCellSize * MotionSamples, BossCellSize * rows.Length, TextureFormat.RGB24, false);
            foreach (var penguin in stage.Penguins)
            {
                penguin.gameObject.SetActive(false);
            }

            boss.gameObject.SetActive(true);
            boss.transform.localPosition = Vector3.zero;
            stage.Camera.aspect = 1f;
            PlaceCamera(stage.Camera, boss.transform.position, BossCameraTarget, BossCameraDistance);
            for (var row = 0; row < rows.Length; row++)
            {
                CaptureBossRow(stage, boss, rows[row], sheet, rows.Length - 1 - row);
            }

            boss.gameObject.SetActive(false);
            stage.Camera.ResetAspect();
            return sheet;
        }

        private static void CaptureBossRow(PenguinPreviewStage stage, BigBossMotion boss, BossRow row, Texture2D sheet, int y)
        {
            boss.ResetPose();
            boss.SnapFacing(Vector3.back);
            // 飛び出すには、先にもぐっておく
            if (row.Name == "飛び出す")
            {
                boss.Submerged = true;
                boss.Tick(BigBossMotion.SubmergeSeconds);
            }

            row.Start(boss);
            SampleRow(stage.Camera, boss.Tick, row.PreRoll, row.Seconds, sheet, BossCellSize, y);
        }

        /// <summary>エリアに3体とペンギンを並べて、ゲームのカメラで撮る</summary>
        private static Texture2D CaptureBossArea(PenguinPreviewStage stage, BigBossMotion skua, BigBossMotion seal, BigBossMotion orca)
        {
            for (var i = 0; i < stage.Penguins.Count; i++)
            {
                // 手前の列だけ残して、大ボスとの大きさを比べる
                stage.Penguins[i].gameObject.SetActive(i >= stage.Penguins.Count - 5);
            }

            var bosses = new[] { skua, seal, orca };
            for (var i = 0; i < bosses.Length; i++)
            {
                bosses[i].gameObject.SetActive(true);
                bosses[i].transform.localPosition = new Vector3((i - 1) * 5f, 0f, 1.5f);
                bosses[i].ResetPose();
                bosses[i].SnapFacing(Vector3.back);
                bosses[i].Tick(0.1f);
            }

            stage.Camera.aspect = BossAreaWidth / (float)BossAreaHeight;
            stage.ApplyCamera(stage.Camera, PreviewCameraPresets.Chosen);
            var texture = Render(stage.Camera, BossAreaWidth, BossAreaHeight);
            stage.Camera.ResetAspect();
            return texture;
        }
    }
}
