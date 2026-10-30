using System.IO;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 試作の場のスクショを並べて PNG にする（見た目を人が決めるため）。
    /// poses.png: 横に動きの姿勢、縦に「本物の影＋線」「丸い影＋線」「本物の影・線なし」。
    /// area.png: 縦にカメラの置き方、左が本物の影・右が丸い影
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const string CaptureFolder = "Screenshots/Preview3D";
        private const int PoseCellSize = 360;
        private const int AreaWidth = 960;
        private const int AreaHeight = 540;

        // 姿勢を撮るカメラ。ゲームのカメラに近い見下ろし方で、斜め前から
        private const float PoseCameraPitch = 40f;
        private const float PoseCameraYaw = -25f;
        private const float PoseCameraDistance = 5f;
        private const float PoseCameraFov = 30f;
        private static readonly Vector3 PoseCameraTarget = new Vector3(0f, 0.75f, 0f);

        private static readonly PenguinPose[] Poses =
        {
            PenguinPose.Idle,
            Walking(0f),
            Walking(0.25f),
            Walking(0.75f),
            Attacking(0.3f),
            Attacking(0.5f),
            Hit(0.08f),
        };

        private static PenguinPose Walking(float phase)
        {
            var pose = PenguinPose.Idle;
            pose.Walk = 1f;
            pose.WalkPhase = phase;
            return pose;
        }

        private static PenguinPose Attacking(float progress)
        {
            var pose = PenguinPose.Idle;
            pose.Attack = progress;
            return pose;
        }

        private static PenguinPose Hit(float progress)
        {
            var pose = PenguinPose.Idle;
            pose.Hit = progress;
            pose.Time = 0.02f;
            return pose;
        }

        [MenuItem("Tools/PenmonScramble/3D Preview Capture")]
        public static void Capture()
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
            var outline = stage.Outline;
            var preset = stage.CameraPreset;
            try
            {
                Save(CapturePoses(stage), "poses.png");
                Save(CaptureAreas(stage), "area.png");
            }
            finally
            {
                stage.SetRealShadows(shadows);
                stage.SetOutline(outline);
                stage.SetCameraPreset(preset);
                foreach (var penguin in stage.Penguins)
                {
                    penguin.gameObject.SetActive(true);
                    penguin.ResetPose();
                }
            }

            Debug.Log($"[Penguin3DPreviewBuilder] {Path.GetFullPath(CaptureFolder)} にスクショを保存しました");
        }

        private static Texture2D CapturePoses(PenguinPreviewStage stage)
        {
            var rows = new (bool shadows, bool outline)[] { (true, true), (false, true), (true, false) };
            var sheet = new Texture2D(PoseCellSize * Poses.Length, PoseCellSize * rows.Length, TextureFormat.RGB24, false);
            var player = stage.Player;
            foreach (var penguin in stage.Penguins)
            {
                penguin.gameObject.SetActive(penguin == player);
            }

            var rotation = player.transform.localRotation;
            player.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            stage.Camera.aspect = 1f;
            PlacePoseCamera(stage.Camera, player.transform.position);
            for (var row = 0; row < rows.Length; row++)
            {
                stage.SetRealShadows(rows[row].shadows);
                stage.SetOutline(rows[row].outline);
                for (var col = 0; col < Poses.Length; col++)
                {
                    player.Apply(Poses[col]);
                    // 上の行から並べたいので、テクスチャの下（y=0）から逆に詰める
                    Blit(sheet, Render(stage.Camera, PoseCellSize, PoseCellSize), col * PoseCellSize, (rows.Length - 1 - row) * PoseCellSize);
                }
            }

            player.transform.localRotation = rotation;
            stage.Camera.ResetAspect();
            return sheet;
        }

        private static void PlacePoseCamera(Camera camera, Vector3 penguin)
        {
            camera.orthographic = false;
            camera.fieldOfView = PoseCameraFov;
            var rotation = Quaternion.Euler(PoseCameraPitch, PoseCameraYaw, 0f);
            var target = penguin + PoseCameraTarget;
            camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * PoseCameraDistance, rotation);
        }

        /// <summary>エリア全体。ほかのペンギンには色々な姿勢を取らせて、動きが混ざったときの見え方を見る</summary>
        private static Texture2D CaptureAreas(PenguinPreviewStage stage)
        {
            var sheet = new Texture2D(AreaWidth * 2, AreaHeight * PreviewCameraPresets.Count, TextureFormat.RGB24, false);
            for (var i = 0; i < stage.Penguins.Count; i++)
            {
                stage.Penguins[i].gameObject.SetActive(true);
                // 根元は回していないので、体の向きで顔をカメラ側へ向ける
                var pose = Poses[i % Poses.Length];
                pose.Yaw = 180f;
                stage.Penguins[i].Apply(pose);
            }

            stage.SetOutline(true);
            for (var preset = 0; preset < PreviewCameraPresets.Count; preset++)
            {
                for (var col = 0; col < 2; col++)
                {
                    stage.SetRealShadows(col == 0);
                    stage.Camera.aspect = AreaWidth / (float)AreaHeight;
                    stage.ApplyCamera(stage.Camera, preset);
                    Blit(sheet, Render(stage.Camera, AreaWidth, AreaHeight), col * AreaWidth, (PreviewCameraPresets.Count - 1 - preset) * AreaHeight);
                }
            }

            stage.Camera.ResetAspect();
            return sheet;
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            // リニアの色空間でも PNG の色が画面と同じになるよう sRGB で受ける
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            var previous = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previous;

            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }

        private static void Blit(Texture2D sheet, Texture2D cell, int x, int y)
        {
            sheet.SetPixels(x, y, cell.width, cell.height, cell.GetPixels());
            Object.DestroyImmediate(cell);
        }

        private static void Save(Texture2D sheet, string fileName)
        {
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(CaptureFolder, fileName), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }
    }
}
