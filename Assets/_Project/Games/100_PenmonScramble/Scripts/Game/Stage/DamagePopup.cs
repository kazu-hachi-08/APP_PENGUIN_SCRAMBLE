using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 当たった位置から浮かんで消えるダメージ数字。カメラの方を向き、ほかのペンギンに隠れない（BillboardText）
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private const float LifeSeconds = 0.6f;
        private const float RiseSpeed = 1.5f;
        private const float CharacterSize = 0.1f;
        // 当たった所（床）から、ペンギンの頭のあたりの高さに出す
        private const float StartHeight = 1.1f;

        private TextMesh _text;
        private Color _color;
        private float _age;

        public static void Spawn(Transform parent, StageMaterials materials, Vector2 position, int damage, Color color)
        {
            var text = BillboardText.Create(parent, "DamagePopup", materials, damage.ToString(), color, CharacterSize);
            text.transform.position = position.ToFloor(StartHeight);
            var popup = text.gameObject.AddComponent<DamagePopup>();
            popup._color = color;
            popup._text = text;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            var t = _age / LifeSeconds;
            _text.color = new Color(_color.r, _color.g, _color.b, 1f - t);
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
