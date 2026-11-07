using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 走者の頭の上に浮かんで消える字（ホームへ還った子の「+1」、ゲッツーでアウトになった子の「アウト」）。
    /// 何点入ったか・誰がアウトになったかを、走る子の所で見せる。カメラの方を向く
    /// </summary>
    public sealed class BaseballRunnerPopup : MonoBehaviour
    {
        public static readonly Color ScoreColor = new Color(1f, 0.86f, 0.25f);
        public static readonly Color OutColor = new Color(0.62f, 0.82f, 1f);
        private const float LifeSeconds = 1.1f;
        private const float RiseSpeed = 0.7f;
        private const float CharacterSize = 0.05f;
        private const int FontSize = 64;
        // 子（半径 0.26）の頭の少し上に出す
        private const float StartHeight = 0.9f;
        // 出てすぐ少し大きく見せてから元の大きさへ戻す（ぽんと出る）
        private const float PopSeconds = 0.18f;
        private const float PopScale = 1.5f;

        private TextMesh _text;
        private Color _color;
        private float _age;

        public static void Spawn(Transform parent, Vector3 localFoot, string value, Color color)
        {
            var obj = new GameObject("RunnerPopup");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localFoot + Vector3.up * StartHeight;
            var text = obj.AddComponent<TextMesh>();
            var font = GameFontApplier.Font;
            text.font = font;
            text.text = value;
            text.fontSize = FontSize;
            text.characterSize = CharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            obj.AddComponent<FaceCamera>();
            var popup = obj.AddComponent<BaseballRunnerPopup>();
            popup._text = text;
            popup._color = color;
            popup.Update();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.localPosition += Vector3.up * (RiseSpeed * Time.deltaTime);
            var pop = Mathf.Clamp01(_age / PopSeconds);
            transform.localScale = Vector3.one * Mathf.Lerp(PopScale, 1f, pop);
            var t = _age / LifeSeconds;
            // 後半だけ薄くする（前半はしっかり読ませる）
            var alpha = Mathf.Clamp01(2f - t * 2f);
            _text.color = new Color(_color.r, _color.g, _color.b, alpha);
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
