using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニットの体以外の目印: 足元の席の色の輪・頭の上の名前（無ければ「P1」）「BOSS」・ダウン中の復活の範囲とたまり具合・
    /// 協力でメニューを開いている人の「MENU」
    /// </summary>
    public partial class UnitView
    {
        private const float SeatRingRatio = 1.25f;
        private const float ReviveRingAlpha = 0.6f;
        private const float ReviveFillAlpha = 0.35f;
        // 頭のてっぺんから文字までのすき間（マス）
        private const float LabelGap = 0.3f;
        private const float LabelCharacterSize = 0.09f;
        // 名前（6文字まで）が長いと頭の上で横に広がって隣の子にかぶるので、この幅（全角の字数）を超える分は小さくする
        private const float LabelMaxEms = 3.2f;
        // 半角の英数字は全角の字のおよそこの幅
        private const float HalfWidthEm = 0.6f;
        private static readonly Color BossLabelColor = new Color(0.9f, 0.15f, 0.15f);
        // 名前のあとに付けるので、短く目立つ英語にする
        private const string InMenuSuffix = " MENU";

        private SpriteRenderer _seatRing;
        private TextMesh _label;
        private string _labelText;
        private SpriteRenderer _reviveRing;
        private SpriteRenderer _reviveFill;
        private float _revive;

        private void CreateMarks(Color? seatColor, string label)
        {
            if (seatColor.HasValue)
            {
                // 種類の見た目だけだと協力で誰のか分からないので、席の色の輪を足元に出す
                _seatRing = CreateDecal("SeatRing", DecalSprites.Ring, seatColor.Value, FloorDecal.SeatRingHeight, _radius * 2f * SeatRingRatio);
                CreateLabel(label, seatColor.Value);
                CreateReviveParts(seatColor.Value);
            }
            else if (IsBoss)
            {
                CreateLabel("BOSS", BossLabelColor);
            }

            RenderMarks();
        }

        /// <summary>頭の上に浮かべる。ほかのペンギンの後ろに隠れて誰か分からなくならないよう、いつも手前に出す（BillboardText）</summary>
        private void CreateLabel(string label, Color color)
        {
            _labelText = label;
            _label = BillboardText.Create(transform, "Label", _materials, label, color, LabelSize(label));
            PlaceLabel();
        }

        private static float LabelSize(string label)
        {
            var ems = 0f;
            foreach (var c in label)
            {
                ems += c < 0x80 ? HalfWidthEm : 1f;
            }

            return ems > LabelMaxEms ? LabelCharacterSize * LabelMaxEms / ems : LabelCharacterSize;
        }

        /// <summary>種類で背の高さが違うので、組み直すたびに置き直す</summary>
        private void PlaceLabel()
        {
            if (_label != null)
            {
                var top = _model.TopHeight * _model.transform.localScale.y;
                _label.transform.localPosition = Vector3.up * (top + LabelGap);
            }
        }

        /// <summary>復活の範囲（輪）と、たまった分だけ広がる円。ダウンの間だけ出す</summary>
        private void CreateReviveParts(Color seatColor)
        {
            var diameter = Balance.ReviveRadius * 2f;
            _reviveRing = CreateDecal("ReviveRing", DecalSprites.Ring, WithAlpha(seatColor, ReviveRingAlpha), FloorDecal.ReviveHeight, diameter);
            _reviveFill = CreateDecal("ReviveFill", DecalSprites.Circle, WithAlpha(seatColor, ReviveFillAlpha), FloorDecal.ReviveHeight, diameter);
        }

        private SpriteRenderer CreateDecal(string partName, Sprite sprite, Color color, float height, float diameter)
        {
            var decal = FloorDecal.Create(transform, partName, sprite, _materials.Decal, color, transform.position, Vector2.one * diameter);
            decal.transform.localPosition = Vector3.up * height;
            return decal;
        }

        private void RenderMarks()
        {
            if (_label != null)
            {
                // もぐっている・空から降りてくる間は、体が床の上に無いので名前も出さない
                _label.gameObject.SetActive(!IsSubmerged && !IsDroppingIn);
                // メニューを開いている間は止まっているので、仲間が「やられそう」と心配しないよう分かるようにする
                var text = IsInMenu ? _labelText + InMenuSuffix : _labelText;
                if (_label.text != text)
                {
                    _label.text = text;
                }
            }

            if (_reviveRing == null)
            {
                return;
            }

            _reviveRing.enabled = IsDown;
            _reviveFill.enabled = IsDown && _revive > 0f;
            FloorDecal.SetSize(_reviveFill, Vector2.one * (Balance.ReviveRadius * 2f * _revive));
        }

        private void HideMarks()
        {
            if (_seatRing != null)
            {
                _seatRing.enabled = false;
            }

            if (_reviveRing != null)
            {
                _reviveRing.enabled = false;
                _reviveFill.enabled = false;
            }

            if (_label != null)
            {
                _label.gameObject.SetActive(false);
            }
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
