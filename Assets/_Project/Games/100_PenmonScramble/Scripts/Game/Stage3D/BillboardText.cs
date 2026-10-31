using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 宙に浮かべる文字（頭の上の名前・「BOSS」・ダメージ数字）。いつもカメラの方を向き、ほかの物に隠れない。
    /// 名前は日本語もあるので、フォントは StageMaterials の丸ゴシック
    /// </summary>
    public static class BillboardText
    {
        private const int FontSize = 64;

        public static TextMesh Create(Transform parent, string objName, StageMaterials materials, string value, Color color, float characterSize)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMesh>();
            text.font = materials.Font;
            text.text = value;
            text.fontSize = FontSize;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.OverlayText;
            renderer.sortingOrder = SortingDepth.Popup;
            obj.AddComponent<FaceCamera>();
            return text;
        }
    }
}
