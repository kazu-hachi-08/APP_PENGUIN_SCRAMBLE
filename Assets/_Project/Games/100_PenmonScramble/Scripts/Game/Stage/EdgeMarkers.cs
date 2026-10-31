using System;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 画面の外にある大ボス・仲間・大ボスの予告を、画面の端の印で指す（闘技場は画面より広いので、見えない所から来る攻撃に気づけるように）。
    /// 印は SceneBuilder が決まった数だけ作っておき、要るだけ出す。多すぎるときは大ボス・仲間・予告の順に出す
    /// </summary>
    // カメラ（StageCamera）がこのフレームの位置へ動いた後に置く
    [DefaultExecutionOrder(200)]
    public class EdgeMarkers : MonoBehaviour
    {
        // 予告の帯は長いので、端から端までの何点かのどれかが見えていれば、印は出さない
        private const int WarningSamples = 5;
        private const string WarningSign = "！";

        [SerializeField] private RectTransform _area;
        [SerializeField] private EdgeMarkerView[] _markers;
        [Tooltip("画面の端からこれより内側（画素）に見えていれば、印は出さない")]
        [SerializeField] private float _screenInset = 24f;
        [Tooltip("印を並べる枠の、_area の縁からの余白（左・下・右・上）。上は HUD のプレート、下はスマホのボタンを避ける")]
        [SerializeField] private Vector4 _padding = new Vector4(70f, 150f, 70f, 270f);
        [SerializeField] private Color _bossColor = new Color(0.9f, 0.18f, 0.18f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private float _warningPulseSpeed = 14f;
        [SerializeField] private float _warningPulseScale = 0.12f;

        private StageSnapshot _snapshot;
        private int _localSeat;
        private Func<int, Color> _seatColor;
        private int _used;

        /// <summary>毎フレーム、最新のスナップショットを渡す（置くのはカメラが動いた後の LateUpdate）</summary>
        public void Set(StageSnapshot snapshot, int localSeat, Func<int, Color> seatColor)
        {
            _snapshot = snapshot;
            _localSeat = localSeat;
            _seatColor = seatColor;
        }

        private void OnDisable()
        {
            foreach (var marker in _markers)
            {
                marker.Hide();
            }
        }

        private void LateUpdate()
        {
            _used = 0;
            var camera = Camera.main;
            if (_snapshot != null && camera != null && _snapshot.Result == StageResult.Playing)
            {
                PlaceBoss(camera);
                PlaceAllies(camera);
                PlaceWarnings(camera);
            }

            for (var i = _used; i < _markers.Length; i++)
            {
                _markers[i].Hide();
            }
        }

        /// <summary>もぐっている間は居場所が分からない（出てくる所は予告で分かる）ので出さない</summary>
        private void PlaceBoss(Camera camera)
        {
            if (_snapshot.TryGetBoss(out var boss) && !boss.Has(UnitFlags.Submerged) && TryPlace(camera, boss.Position, out var marker))
            {
                marker.ShowFace(boss.SpeciesNo, _bossColor, string.Empty);
            }
        }

        /// <summary>ダウンした仲間も出す（そばへ行けば起こせるので、居場所が要る）</summary>
        private void PlaceAllies(Camera camera)
        {
            foreach (var unit in _snapshot.Units)
            {
                if (unit.Team == Team.Player && unit.Seat != _localSeat && TryPlace(camera, unit.Position, out var marker))
                {
                    marker.ShowFace(unit.SpeciesNo, _seatColor(unit.Seat), StageView.SeatLabel(unit.Seat));
                }
            }
        }

        private void PlaceWarnings(Camera camera)
        {
            var pulse = 1f + _warningPulseScale * Mathf.Sin(Time.time * _warningPulseSpeed);
            foreach (var warning in _snapshot.Warnings)
            {
                if (_used >= _markers.Length || IsAnyOnScreen(camera, warning))
                {
                    continue;
                }

                var middle = (warning.From + warning.To) * 0.5f;
                if (TryPlace(camera, middle, out var marker, pulse))
                {
                    marker.ShowSign(WarningSign, _warningColor);
                }
            }
        }

        private bool IsAnyOnScreen(Camera camera, WarningState warning)
        {
            var screen = new Vector2(Screen.width, Screen.height);
            for (var i = 0; i < WarningSamples; i++)
            {
                var point = warning.From + (warning.To - warning.From) * (i / (WarningSamples - 1f));
                if (EdgeMarkerPlacement.IsOnScreen(camera.WorldToScreenPoint(point.ToFloor()), screen, _screenInset))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>floor の点が画面の外なら、次の印を枠の縁に置いて返す</summary>
        private bool TryPlace(Camera camera, Vec2 floor, out EdgeMarkerView marker, float scale = 1f)
        {
            marker = null;
            var screen = new Vector2(Screen.width, Screen.height);
            var point = camera.WorldToScreenPoint(floor.ToFloor());
            if (_used >= _markers.Length || EdgeMarkerPlacement.IsOnScreen(point, screen, _screenInset))
            {
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, EdgeMarkerPlacement.FacingPoint(point, screen), null, out var local);
            var rect = _area.rect;
            var box = Rect.MinMaxRect(rect.xMin + _padding.x, rect.yMin + _padding.y, rect.xMax - _padding.z, rect.yMax - _padding.w);
            var (position, angle) = EdgeMarkerPlacement.PlaceOnEdge(box, local);
            marker = _markers[_used++];
            marker.Place(position, angle, scale);
            return true;
        }
    }
}
