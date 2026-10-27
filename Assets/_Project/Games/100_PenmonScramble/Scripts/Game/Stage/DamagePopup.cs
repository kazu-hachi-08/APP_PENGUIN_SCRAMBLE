using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 当たった位置から浮かんで消えるダメージ数字。
    /// 数字だけなので、日本語フォントの要らない標準フォントの TextMesh で描く
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private const float LifeSeconds = 0.6f;
        private const float RiseSpeed = 1.5f;
        private const float CharacterSize = 0.06f;
        private const int FontSize = 64;
        private const float StartHeight = 0.5f;

        private TextMesh _text;
        private Color _color;
        private float _age;

        public static void Spawn(Transform parent, Vector2 position, int damage, Color color)
        {
            var popup = new GameObject("DamagePopup").AddComponent<DamagePopup>();
            popup.transform.SetParent(parent, false);
            popup.transform.position = position + Vector2.up * StartHeight;
            popup._color = color;
            popup._text = CreateText(popup.gameObject, damage.ToString(), color);
        }

        private static TextMesh CreateText(GameObject obj, string value, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = obj.AddComponent<TextMesh>();
            text.font = font;
            text.text = value;
            text.fontSize = FontSize;
            text.characterSize = CharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = SortingDepth.Popup;
            return text;
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
