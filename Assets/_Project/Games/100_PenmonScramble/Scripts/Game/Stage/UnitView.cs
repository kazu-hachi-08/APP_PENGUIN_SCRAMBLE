using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニット1体の仮の見た目（種類の色の丸＋向きの三角。プレイヤーは席の色の輪と、頭の上に「P1」など。ボスは「BOSS」）。
    /// 位置は更新（1/30秒）ごとに受け取り、その間を補間して描く（オンラインのゲストも同じ描き方にするため）
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        private const float FlashSeconds = 0.1f;
        private const float SquashSeconds = 0.15f;
        private const float SquashAmount = 0.25f;
        private const float DefeatSeconds = 0.5f;
        private const float DefeatSpinDegreesPerSecond = 900f;
        private const float DefeatRiseSpeed = 3f;
        private const float FacingDistanceRatio = 0.85f;
        private const float FacingSizeRatio = 0.5f;
        private const float SeatRingRatio = 1.25f;
        private const float LabelHeightRatio = 1.6f;
        private const float LabelCharacterSize = 0.05f;
        private const int LabelFontSize = 64;

        // 状態ごとの見た目
        private static readonly Color FrozenTint = new Color(0.75f, 0.95f, 1f);
        private const float FrozenTintAmount = 0.65f;
        private const float InvincibleBlinkPerSecond = 12f;
        private const float AfterimageInterval = 0.04f;
        private const float AfterimageSeconds = 0.2f;
        private const float AfterimageAlpha = 0.45f;
        private const float SwitchPoofSeconds = 0.3f;

        // ダウン中は灰色でつぶれて見せ、足元に復活の範囲と進み具合を出す（§4.5）
        private static readonly Color DownTint = new Color(0.55f, 0.55f, 0.6f);
        private const float DownTintAmount = 0.6f;
        private static readonly Vector2 DownScale = new Vector2(1.3f, 0.55f);
        private const float ReviveRingAlpha = 0.6f;
        private const float ReviveFillAlpha = 0.35f;
        private static readonly Color BossLabelColor = new Color(0.9f, 0.15f, 0.15f);

        private SpriteRenderer _body;
        private SpriteRenderer _facing;
        private SpriteRenderer _seatRing;
        private GameObject _label;
        private SpriteRenderer _reviveRing;
        private SpriteRenderer _reviveFill;
        private float _revive;
        private Color _color;
        private float _radius;
        private Vector2 _from;
        private Vector2 _to;
        private Vector2 _facingDirection;
        private UnitFlags _flags;
        private float _flashTimer;
        private float _squashTimer;
        private float _afterimageTimer;
        private float _defeatTimer = -1f;

        public Team Team { get; private set; }
        public int SpeciesNo { get; private set; }
        public bool IsBoss => (_flags & UnitFlags.Boss) != 0;
        private bool IsDown => (_flags & UnitFlags.Down) != 0;

        /// <param name="seatColor">プレイヤーの席の色。null なら輪と文字を出さない（敵）</param>
        public static UnitView Create(Transform parent, UnitState unit, Color? seatColor, string label)
        {
            var view = new GameObject($"Unit{unit.Id}_{unit.Team}").AddComponent<UnitView>();
            view.transform.SetParent(parent, false);
            view.Setup(unit, seatColor);
            if (seatColor.HasValue)
            {
                view.CreateLabel(label, seatColor.Value);
                view.CreateReviveParts(seatColor.Value);
            }
            else if (view.IsBoss)
            {
                view.CreateLabel("BOSS", BossLabelColor);
            }

            return view;
        }

        private void Setup(UnitState unit, Color? seatColor)
        {
            Team = unit.Team;
            SpeciesNo = unit.SpeciesNo;
            _color = PenguinPalette.BodyColor(unit);
            _radius = unit.Radius;
            if (seatColor.HasValue)
            {
                // 種類の色だけだと協力で誰のか分からないので、席の色の輪を足元に出す
                _seatRing = CreatePart("SeatRing", PlaceholderSprites.Ring, seatColor.Value);
                _seatRing.transform.localScale = Vector3.one * (_radius * 2f * SeatRingRatio);
            }

            _body = CreatePart("Body", PlaceholderSprites.Circle, _color);
            _body.transform.localScale = Vector3.one * (_radius * 2f);
            _facing = CreatePart("Facing", PlaceholderSprites.Triangle, Color.Lerp(_color, Color.black, 0.5f));
            _facing.transform.localScale = Vector3.one * (_radius * FacingSizeRatio);
            _from = _to = unit.Position.ToVector2();
            _facingDirection = unit.Facing.ToVector2();
            _flags = unit.Flags;
            _revive = unit.Revive;
            Render(1f);
        }

        /// <summary>英数字だけなので、ダメージ数字と同じく日本語フォントの要らない TextMesh で描く</summary>
        private void CreateLabel(string label, Color color)
        {
            var obj = new GameObject("Label");
            _label = obj;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * (_radius * LabelHeightRatio);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = obj.AddComponent<TextMesh>();
            text.font = font;
            text.text = label;
            text.fontSize = LabelFontSize;
            text.characterSize = LabelCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            // ほかのペンギンの後ろに隠れて誰か分からなくならないよう、いつも手前に出す
            renderer.sortingOrder = SortingDepth.Popup - 1;
        }

        /// <summary>復活の範囲（輪）と、たまった分だけ広がる円。ダウンの間だけ出す</summary>
        private void CreateReviveParts(Color seatColor)
        {
            _reviveRing = CreatePart("ReviveRing", PlaceholderSprites.Ring, new Color(seatColor.r, seatColor.g, seatColor.b, ReviveRingAlpha));
            _reviveRing.transform.localScale = Vector3.one * (Balance.ReviveRadius * 2f);
            _reviveRing.sortingOrder = SortingDepth.Ground;
            _reviveFill = CreatePart("ReviveFill", PlaceholderSprites.Circle, new Color(seatColor.r, seatColor.g, seatColor.b, ReviveFillAlpha));
            _reviveFill.sortingOrder = SortingDepth.Ground;
            _reviveRing.enabled = false;
            _reviveFill.enabled = false;
        }

        private SpriteRenderer CreatePart(string partName, Sprite sprite, Color color)
        {
            var obj = new GameObject(partName);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        public void PushState(UnitState unit)
        {
            _from = _to;
            _to = unit.Position.ToVector2();
            _facingDirection = unit.Facing.ToVector2();
            var wasDown = IsDown;
            _flags = unit.Flags;
            _revive = unit.Revive;
            if (unit.SpeciesNo != SpeciesNo)
            {
                ChangeSpecies(unit);
            }
            else if (wasDown != IsDown)
            {
                // ダウンした・起き上がった瞬間を見せる（起き上がりは先頭の子なので、種類が変わればそちらの演出で出る）
                Poof();
            }
        }

        /// <summary>交代した（自分で替えた・倒れて次の子が出た）。色を替えて、ぽんと出てきたように見せる</summary>
        private void ChangeSpecies(UnitState unit)
        {
            SpeciesNo = unit.SpeciesNo;
            _color = PenguinPalette.BodyColor(unit);
            _facing.color = Color.Lerp(_color, Color.black, 0.5f);
            Poof();
        }

        private void Poof()
        {
            BurstFx.Spawn(transform.parent, PlaceholderSprites.Ring, Color.white, _to, _radius, _radius * 2f, SwitchPoofSeconds, SortingDepth.Popup - 2);
            Squash();
        }

        /// <summary>alpha は前回の更新から次の更新までの進み具合（0〜1）</summary>
        public void Render(float alpha)
        {
            if (_defeatTimer >= 0f)
            {
                return;
            }

            var position = Vector2.Lerp(_from, _to, alpha);
            transform.position = position;
            var order = SortingDepth.ForY(position.y);
            if (_seatRing != null)
            {
                _seatRing.sortingOrder = order;
            }

            _body.sortingOrder = order + 1;
            _facing.sortingOrder = order + 2;
            _facing.enabled = !IsDown;
            RenderRevive();
            _facing.transform.localPosition = _facingDirection * (_radius * FacingDistanceRatio);
            _facing.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_facingDirection.y, _facingDirection.x) * Mathf.Rad2Deg);
        }

        private void RenderRevive()
        {
            if (_reviveRing == null)
            {
                return;
            }

            _reviveRing.enabled = IsDown;
            _reviveFill.enabled = IsDown && _revive > 0f;
            _reviveFill.transform.localScale = Vector3.one * (Balance.ReviveRadius * 2f * _revive);
        }

        public void Flash() => _flashTimer = FlashSeconds;

        public void Squash() => _squashTimer = SquashSeconds;

        /// <summary>倒れたら回りながら飛んで消える（§9）。Core からは消えているので、ここからは見た目だけで動く</summary>
        public void PlayDefeat()
        {
            _defeatTimer = 0f;
            _facing.enabled = false;
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
                _label.SetActive(false);
            }
        }

        private void Update()
        {
            _flashTimer -= Time.deltaTime;
            _body.color = CurrentBodyColor();
            UpdateSquash();
            UpdateAfterimage();
            UpdateDefeat();
        }

        private Color CurrentBodyColor()
        {
            if (_flashTimer > 0f)
            {
                return Color.white;
            }

            if (IsDown)
            {
                return Color.Lerp(_color, DownTint, DownTintAmount);
            }

            var color = (_flags & UnitFlags.Frozen) != 0 ? Color.Lerp(_color, FrozenTint, FrozenTintAmount) : _color;
            // 無敵の間は点滅させて、攻撃が効かないことを分かるようにする
            if ((_flags & UnitFlags.Invincible) != 0 && Mathf.Repeat(Time.time * InvincibleBlinkPerSecond, 1f) < 0.5f)
            {
                color.a = 0.4f;
            }

            return color;
        }

        private void UpdateSquash()
        {
            _squashTimer -= Time.deltaTime;
            var amount = _squashTimer > 0f ? SquashAmount * (_squashTimer / SquashSeconds) : 0f;
            var scale = IsDown ? DownScale : Vector2.one;
            _body.transform.localScale = new Vector3(scale.x + amount, scale.y - amount, 1f) * (_radius * 2f);
        }

        /// <summary>突進中は残像を残して、すべっているのが分かるようにする</summary>
        private void UpdateAfterimage()
        {
            if ((_flags & UnitFlags.Dashing) == 0 || _defeatTimer >= 0f)
            {
                return;
            }

            _afterimageTimer -= Time.deltaTime;
            if (_afterimageTimer > 0f)
            {
                return;
            }

            _afterimageTimer = AfterimageInterval;
            var color = new Color(_color.r, _color.g, _color.b, AfterimageAlpha);
            BurstFx.Spawn(transform.parent, PlaceholderSprites.Circle, color, transform.position, _radius, _radius * 0.7f, AfterimageSeconds, _body.sortingOrder - 1);
        }

        private void UpdateDefeat()
        {
            if (_defeatTimer < 0f)
            {
                return;
            }

            _defeatTimer += Time.deltaTime;
            var t = _defeatTimer / DefeatSeconds;
            transform.Rotate(0f, 0f, DefeatSpinDegreesPerSecond * Time.deltaTime);
            transform.position += Vector3.up * (DefeatRiseSpeed * Time.deltaTime);
            transform.localScale = Vector3.one * Mathf.Max(0f, 1f - t);
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
