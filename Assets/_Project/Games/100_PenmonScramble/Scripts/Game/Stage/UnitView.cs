using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニット1体の仮の見た目（色つきの丸＋向きの三角）。
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

        private SpriteRenderer _body;
        private SpriteRenderer _facing;
        private Color _color;
        private float _radius;
        private Vector2 _from;
        private Vector2 _to;
        private Vector2 _facingDirection;
        private float _flashTimer;
        private float _squashTimer;
        private float _defeatTimer = -1f;

        public static UnitView Create(Transform parent, Unit unit, Color color)
        {
            var view = new GameObject($"Unit{unit.Id}_{unit.Team}").AddComponent<UnitView>();
            view.transform.SetParent(parent, false);
            view.Setup(unit, color);
            return view;
        }

        private void Setup(Unit unit, Color color)
        {
            _color = color;
            _radius = unit.Radius;
            _body = CreatePart("Body", PlaceholderSprites.Circle, color);
            _body.transform.localScale = Vector3.one * (_radius * 2f);
            _facing = CreatePart("Facing", PlaceholderSprites.Triangle, Color.Lerp(color, Color.black, 0.5f));
            _facing.transform.localScale = Vector3.one * (_radius * FacingSizeRatio);
            _from = _to = unit.Position.ToVector2();
            _facingDirection = unit.Facing.ToVector2();
            Render(1f);
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

        public void PushState(Unit unit)
        {
            _from = _to;
            _to = unit.Position.ToVector2();
            _facingDirection = unit.Facing.ToVector2();
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
            _body.sortingOrder = order;
            _facing.sortingOrder = order + 1;
            _facing.transform.localPosition = _facingDirection * (_radius * FacingDistanceRatio);
            _facing.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_facingDirection.y, _facingDirection.x) * Mathf.Rad2Deg);
        }

        public void Flash() => _flashTimer = FlashSeconds;

        public void Squash() => _squashTimer = SquashSeconds;

        /// <summary>倒れたら回りながら飛んで消える（§9）。Core からは消えているので、ここからは見た目だけで動く</summary>
        public void PlayDefeat()
        {
            _defeatTimer = 0f;
            _facing.enabled = false;
        }

        private void Update()
        {
            _flashTimer -= Time.deltaTime;
            _body.color = _flashTimer > 0f ? Color.white : _color;
            UpdateSquash();
            UpdateDefeat();
        }

        private void UpdateSquash()
        {
            _squashTimer -= Time.deltaTime;
            var amount = _squashTimer > 0f ? SquashAmount * (_squashTimer / SquashSeconds) : 0f;
            _body.transform.localScale = new Vector3(1f + amount, 1f - amount, 1f) * (_radius * 2f);
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
