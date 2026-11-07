using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 投げた球の通り道: マウンドから曲げられる区間の終わりまでの、少し明るい土の帯と、終わりの白い点線
    /// （打つ人が「ここから先はまっすぐ来る」と読めるように。曲げられる間は点線が光る）。
    /// ホームの上のストライクのゾーンの枠（ストライクで黄色く光る・ボールで灰色になって、少しして戻る）
    /// </summary>
    public partial class BaseballView
    {
        private const float LaneLighten = 0.18f;
        private const int DashCount = 9;
        private const float DashFill = 0.6f;
        private const float DashWidth = 0.07f;
        private const float ZoneLineWidth = 0.07f;
        private const float ZoneBack = -0.32f;
        private const float ZoneFront = 0.36f;
        private const float ZoneFlashSeconds = 0.9f;
        private static readonly Color DashColor = new Color(1f, 1f, 1f);
        private static readonly Color DashLitColor = new Color(1f, 0.9f, 0.35f);
        private static readonly Color ZoneColor = new Color(0.98f, 0.95f, 0.80f);
        private static readonly Color ZoneStrikeColor = new Color(1f, 0.85f, 0.15f);
        private static readonly Color ZoneBallColor = new Color(0.45f, 0.45f, 0.48f);

        private MeshRenderer[] _dashes;
        private MeshRenderer[] _zoneLines;
        private float _zoneFlash;
        private bool _dashLit;

        private void BuildLane(BaseballFieldDef field)
        {
            var lift = LayerLift * 7f;
            var width = BaseballBalance.PitchMaxOffset * 2f;
            var bendEnd = field.Mound.Y - (field.Mound.Y - BaseballFieldDef.PlateY) * BaseballBalance.BendableFraction;
            var length = field.Mound.Y - bendEnd;
            var lane = Color.Lerp(DirtColor, Color.white, LaneLighten);
            AddPart("Lane", ToyMeshes.FloorQuad, _root, new Vector3(field.Mound.X, lift, bendEnd + length * 0.5f), Quaternion.identity,
                new Vector3(width, 1f, length), Materials.Floor(Texture2D.whiteTexture, lane));
            BuildDashes(field.Mound.X, bendEnd, width, lift);
            BuildZone(field, lift);
        }

        /// <summary>曲げられる区間の終わりの、横に並んだ点線</summary>
        private void BuildDashes(float centerX, float y, float width, float lift)
        {
            _dashes = new MeshRenderer[DashCount];
            var step = width / DashCount;
            for (var i = 0; i < DashCount; i++)
            {
                var x = centerX - width * 0.5f + step * (i + 0.5f);
                var dash = AddPart("BendDash", ToyMeshes.FloorQuad, _root, new Vector3(x, lift * 1.5f, y), Quaternion.identity,
                    new Vector3(step * DashFill, 1f, DashWidth), Materials.Floor(Texture2D.whiteTexture, DashColor));
                _dashes[i] = dash.GetComponent<MeshRenderer>();
            }
        }

        /// <summary>ゾーンの幅の、ホームを囲む四角い枠</summary>
        private void BuildZone(BaseballFieldDef field, float lift)
        {
            var half = field.ZoneHalfWidth;
            var corners = new[] { new Vec2(-half, ZoneBack), new Vec2(half, ZoneBack), new Vec2(half, ZoneFront), new Vec2(-half, ZoneFront) };
            _zoneLines = new MeshRenderer[corners.Length];
            for (var i = 0; i < corners.Length; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % corners.Length];
                var along = BaseballSpace.Direction(b - a);
                var line = AddPart("ZoneLine", ToyMeshes.FloorQuad, _root, BaseballSpace.ToWorld((a + b) * 0.5f, lift * 2f), Quaternion.LookRotation(along),
                    new Vector3(ZoneLineWidth, 1f, along.magnitude + ZoneLineWidth), Materials.Floor(Texture2D.whiteTexture, ZoneColor));
                _zoneLines[i] = line.GetComponent<MeshRenderer>();
            }
        }

        private void FlashZone(bool strike)
        {
            _zoneFlash = ZoneFlashSeconds;
            SetZoneColor(strike ? ZoneStrikeColor : ZoneBallColor);
        }

        private void TickLane(BaseballBoard board, float deltaTime)
        {
            var lit = board.Phase == BaseballBoardPhase.Pitching && board.Pitch.CanBend;
            if (lit != _dashLit)
            {
                _dashLit = lit;
                var material = Materials.Floor(Texture2D.whiteTexture, lit ? DashLitColor : DashColor);
                foreach (var dash in _dashes)
                {
                    dash.sharedMaterial = material;
                }
            }

            if (_zoneFlash <= 0f)
            {
                return;
            }

            _zoneFlash = Mathf.Max(0f, _zoneFlash - deltaTime);
            if (_zoneFlash <= 0f)
            {
                SetZoneColor(ZoneColor);
            }
        }

        private void ResetLane()
        {
            _zoneFlash = 0f;
            SetZoneColor(ZoneColor);
        }

        private void SetZoneColor(Color color)
        {
            var material = Materials.Floor(Texture2D.whiteTexture, color);
            foreach (var line in _zoneLines)
            {
                line.sharedMaterial = material;
            }
        }
    }
}
