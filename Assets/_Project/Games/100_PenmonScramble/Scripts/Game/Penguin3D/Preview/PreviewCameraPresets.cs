using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 試作の場で見比べるカメラの置き方。透視（奥が小さく見えて立体感が出る）と平行投影（端まで同じ大きさ）を、見下ろす角度を変えて並べる
    /// </summary>
    public static class PreviewCameraPresets
    {
        private readonly struct Preset
        {
            public readonly string Name;
            public readonly bool Orthographic;
            public readonly float Pitch;
            public readonly float FieldOfView;

            public Preset(string name, bool orthographic, float pitch, float fieldOfView)
            {
                Name = name;
                Orthographic = orthographic;
                Pitch = pitch;
                FieldOfView = fieldOfView;
            }
        }

        private static readonly Preset[] Presets =
        {
            new Preset("透視 45° 画角30", false, 45f, 30f),
            new Preset("透視 55° 画角30", false, StageCamera.Pitch, StageCamera.FieldOfView),
            new Preset("透視 65° 画角30", false, 65f, 30f),
            new Preset("平行投影 55°", true, 55f, 0f),
        };

        // 奥の端の余裕と上下の余白は、ステージのカメラと同じにする
        private const float Headroom = StageCamera.Headroom;
        private const float Margin = StageCamera.Margin;

        /// <summary>カメラ（透視 55°）。ステージのカメラ（StageCamera）と同じ構え</summary>
        public const int Chosen = 1;

        public static int Count => Presets.Length;

        public static string Name(int preset) => Presets[preset].Name;

        public static void Apply(Camera camera, int preset, Vector3 center)
        {
            var p = Presets[preset];
            var size = new Vector2(Balance.AreaWidth, Balance.AreaHeight);
            if (p.Orthographic)
            {
                AreaCameraFit.FitOrthographic(camera, center, size, p.Pitch, Headroom, Margin);
            }
            else
            {
                AreaCameraFit.FitPerspective(camera, center, size, p.Pitch, p.FieldOfView, Headroom, Margin);
            }
        }
    }
}
