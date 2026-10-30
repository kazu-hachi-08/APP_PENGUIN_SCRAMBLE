using System.Text;
using UnityEngine;

namespace PenmonScramble.Game
{
    public partial class StageBenchmark
    {
        // 画面の高さに対する文字の大きさ。スマホでも読めるように大きめ
        private const float FontHeightRatio = 1f / 30f;
        private const float Margin = 12f;
        // ブラウザ・スマホの標準の文字には日本語が無いことがあるので、表示は英語にする
        private const string Title = "PenmonScramble benchmark";

        private GUIStyle _style;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = false };
            _style.fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height * FontHeightRatio));
            var text = BuildReport();
            var size = _style.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(Margin, Margin, size.x, size.y), text, _style);
        }

        private string BuildReport()
        {
            var text = new StringBuilder();
            text.AppendLine(Title);
            text.AppendLine(DeviceLine());
            for (var i = 0; i < _scenarios.Length && i <= _index; i++)
            {
                AppendScenario(text, _scenarios[i], running: i == _index && !IsFinished);
            }

            text.Append(IsFinished ? "Done." : string.Empty);
            return text.ToString().TrimEnd();
        }

        private void AppendScenario(StringBuilder text, Scenario scenario, bool running)
        {
            if (running)
            {
                var phase = _time < WarmupSeconds ? "warming up" : $"{_time - WarmupSeconds:0}/{MeasureSeconds:0}s";
                text.AppendLine($"> {scenario.Name}  [{phase}]  now {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):0} fps");
                return;
            }

            text.AppendLine(Summary(scenario));
        }

        private static string DeviceLine()
        {
            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            return $"{SystemInfo.deviceModel} / {SystemInfo.graphicsDeviceName} / {Screen.width}x{Screen.height} / quality {quality}";
        }

        private static string Summary(Scenario s)
        {
            return $"{s.Name}: avg {s.Frames.AverageFps:0.0} fps, 1% low {s.Frames.LowFps:0.0} fps, worst {s.Frames.WorstMs:0} ms (at {s.WorstAtSeconds:0.0}s, {s.WorstAtUnits} units), gc {s.GcCollections}, " +
                   $"units {s.PeakUnits}, batches {Counter(s.Batches)}, setpass {Counter(s.SetPass)}, draws {Counter(s.DrawCalls)}, tris {Counter(s.Triangles)}";
        }

        private static string Counter(CounterStat stat) => stat.HasValue ? $"{stat.Average}(max {stat.Max})" : "-";

        /// <summary>ログにも出す（Windows は Player.log、ブラウザは開発者ツール、Android は logcat で読める）</summary>
        private static void Log(Scenario scenario)
        {
            Debug.Log($"[Benchmark] {DeviceLine()} | {Summary(scenario)}");
        }
    }
}
