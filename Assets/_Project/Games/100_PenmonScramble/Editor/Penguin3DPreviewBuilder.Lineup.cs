using System.IO;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 20種類を並べたスクショ（PLAN_3D フェーズ1。どれがどの種類か見分けられるかを人が確かめる）。
    /// lineup.png: 1体ずつ斜め前から（横に5種類・縦は属性の4列。左上が No.1）。
    /// lineup_back.png: 同じ並びで斜め後ろから（マント・リュックなど背中の飾り）。
    /// lineup_area.png: ゲームのカメラでエリア全体（ゲーム中の大きさで見分けられるか）
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const int LineupCellSize = 320;
        private const int LineupColumns = 5;
        private const int LineupAreaWidth = 1920;
        private const int LineupAreaHeight = 1080;

        // 雷雲・王冠のように頭の上に高い飾りがあるので、姿勢のスクショより少し引いて上を狙う
        private const float LineupCameraDistance = 5.8f;
        private static readonly Vector3 LineupCameraTarget = new Vector3(0f, 0.95f, 0f);

        [MenuItem("Tools/PenmonScramble/3D Preview Lineup Capture")]
        public static void CaptureLineup()
        {
            var stage = Object.FindAnyObjectByType<PenguinPreviewStage>();
            if (stage == null)
            {
                Open();
                stage = Object.FindAnyObjectByType<PenguinPreviewStage>();
                if (stage == null) return;
            }

            Directory.CreateDirectory(CaptureFolder);
            var shadows = stage.RealShadows;
            var preset = stage.CameraPreset;
            try
            {
                stage.SetRealShadows(false);
                stage.SetOutline(true);
                Save(CaptureLineupCells(stage, 180f), "lineup.png");
                Save(CaptureLineupCells(stage, 0f), "lineup_back.png");
                Save(CaptureLineupArea(stage), "lineup_area.png");
            }
            finally
            {
                stage.SetRealShadows(shadows);
                stage.SetCameraPreset(preset);
                foreach (var penguin in stage.Penguins)
                {
                    penguin.gameObject.SetActive(true);
                    penguin.transform.localRotation = Quaternion.identity;
                    penguin.ResetPose();
                }
            }

            Debug.Log($"[Penguin3DPreviewBuilder] {Path.GetFullPath(CaptureFolder)} に20種類のスクショを保存しました");
        }

        /// <summary>
        /// 1体ずつ、ほかを隠してエリアのまん中に連れてきて撮る（背景の床をそろえる）。yaw 180 で顔がカメラ側、0 で背中がカメラ側。
        /// 向きは根元を回して決める（Apply の Idle は体の向きが 0 なので）
        /// </summary>
        private static Texture2D CaptureLineupCells(PenguinPreviewStage stage, float yaw)
        {
            var count = stage.Penguins.Count;
            var rows = (count + LineupColumns - 1) / LineupColumns;
            var sheet = new Texture2D(LineupCellSize * LineupColumns, LineupCellSize * rows, TextureFormat.RGB24, false);
            stage.Camera.aspect = 1f;
            for (var i = 0; i < count; i++)
            {
                var penguin = stage.Penguins[i];
                foreach (var other in stage.Penguins)
                {
                    other.gameObject.SetActive(other == penguin);
                }

                var spot = penguin.transform.localPosition;
                penguin.transform.localPosition = Vector3.zero;
                penguin.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                penguin.Apply(PenguinPose.Idle);
                PlaceLineupCamera(stage.Camera, penguin.transform.position);
                var col = i % LineupColumns;
                var row = i / LineupColumns;
                Blit(sheet, Render(stage.Camera, LineupCellSize, LineupCellSize), col * LineupCellSize, (rows - 1 - row) * LineupCellSize);
                penguin.transform.localPosition = spot;
            }

            stage.Camera.ResetAspect();
            return sheet;
        }

        private static void PlaceLineupCamera(Camera camera, Vector3 penguin)
        {
            camera.orthographic = false;
            camera.fieldOfView = PoseCameraFov;
            var rotation = Quaternion.Euler(PoseCameraPitch, PoseCameraYaw, 0f);
            var target = penguin + LineupCameraTarget;
            camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * LineupCameraDistance, rotation);
        }

        private static Texture2D CaptureLineupArea(PenguinPreviewStage stage)
        {
            foreach (var penguin in stage.Penguins)
            {
                penguin.gameObject.SetActive(true);
                penguin.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                penguin.Apply(PenguinPose.Idle);
            }

            stage.Camera.aspect = LineupAreaWidth / (float)LineupAreaHeight;
            stage.ApplyCamera(stage.Camera, PreviewCameraPresets.Chosen);
            var texture = Render(stage.Camera, LineupAreaWidth, LineupAreaHeight);
            stage.Camera.ResetAspect();
            return texture;
        }
    }
}
