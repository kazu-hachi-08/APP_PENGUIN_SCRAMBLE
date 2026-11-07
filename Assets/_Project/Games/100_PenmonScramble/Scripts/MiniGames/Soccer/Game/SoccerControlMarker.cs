using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// この端末の人が動かしている子の頭の上の印: 先を下へ向けたしずく（▼）を、上下に弾ませて浮かべる。
    /// 足元の輪は斜め上のカメラでは小さく、体やほかの子に隠れやすいので、隠れない頭の上に一番の手がかりを置く。
    /// 色はチームの色（赤・青）とぶつからない黄色。切り替わった瞬間はポンと大きくなって戻る
    /// </summary>
    public sealed class SoccerControlMarker
    {
        // 足元からしずくの真ん中までの高さ（マス）。頭の上の飾り（とさか）に触れない所
        private const float Height = 1.75f;
        private static readonly Vector3 Size = new Vector3(0.34f, 0.46f, 0.34f);
        private const float Gloss = 0.5f;
        private static readonly Color MarkerColor = new Color(1f, 0.86f, 0.15f);
        // 上下の弾み（ゆっくり浮き沈みさせて、止まった飾りより目に入るように）
        private const float BobHeight = 0.12f;
        private const float BobRate = 3.2f;
        // 切り替わったときのポン（大きくしてから戻す）
        private const float PopScale = 1.7f;
        private const float PopSeconds = 0.3f;

        private readonly Transform _root;
        private float _time;
        private float _pop;

        public SoccerControlMarker(ToonMaterials materials, Transform parent)
        {
            var obj = new GameObject("ControlMarker");
            obj.transform.SetParent(parent, false);
            // しずくは丸い底・とがった上なので、ひっくり返して先を下（子の頭）へ向ける
            obj.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            obj.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.Drop;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.Get(MarkerColor, Gloss);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _root = obj.transform;
            _root.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible, bool pop)
        {
            _root.gameObject.SetActive(visible);
            if (visible && pop)
            {
                _pop = PopSeconds;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_root.gameObject.activeSelf)
            {
                return;
            }

            _time += deltaTime;
            _pop = Mathf.Max(0f, _pop - deltaTime);
            var bob = Mathf.Abs(Mathf.Sin(_time * BobRate)) * BobHeight;
            _root.localPosition = Vector3.up * (Height + bob);
            _root.localScale = Size * SoccerPop.Scale(_pop, PopSeconds, PopScale);
        }
    }

    /// <summary>切り替わった瞬間のポン（大きく膨らんで、少し縮んでから元へ）。印と足元の輪で同じ動きにする</summary>
    public static class SoccerPop
    {
        /// <param name="left">ポンの残りの秒（0 で元の大きさ）</param>
        public static float Scale(float left, float seconds, float peak)
        {
            if (left <= 0f)
            {
                return 1f;
            }

            // 0 → 1 で進み、出だしが一番大きく、最後は元の大きさへ収まる
            var t = 1f - left / seconds;
            return 1f + (peak - 1f) * (1f - t) * (1f - t);
        }
    }
}
