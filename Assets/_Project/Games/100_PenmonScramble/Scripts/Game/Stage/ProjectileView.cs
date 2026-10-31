using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 技の弾。属性の色の小さな球のまわりを光らせ、床に丸い影を落とす（宙のどこを飛んでいるか分かるように）。
    /// UnitView と同じく、更新の間を補間して描く
    /// </summary>
    public class ProjectileView : MonoBehaviour
    {
        // 敵の弾は暗くして、自分たちの弾と見分けられるようにする。光も弱くする
        private const float EnemyDarkness = 0.4f;
        private const float GlowAlpha = 1f;
        private const float EnemyGlowAlpha = 0.45f;
        // 当たりの円に対する、球と光の大きさ
        private const float BallSizeRatio = 1.2f;
        private const float GlowSizeRatio = 3.5f;
        private const float ShadowSizeRatio = 1.6f;
        private const float BallGloss = 1f;
        // ペンギンの胸のあたりを飛ばす（床すれすれだと影と重なって見えにくい）
        private const float FlyHeight = 0.45f;
        // 光は細かく揺らして、燃えているように見せる
        private const float FlickerSpeed = 30f;
        private const float FlickerAmount = 0.12f;

        private SpriteRenderer _glow;
        private float _glowSize;
        // 弾ごとに揺れの始まりをずらして、全部の光が同時に脈打たないようにする
        private float _flickerPhase;
        private Vector2 _from;
        private Vector2 _to;

        public static ProjectileView Create(Transform parent, StageMaterials materials, ProjectileState projectile, bool isEnemy)
        {
            var view = new GameObject($"Projectile{projectile.Id}").AddComponent<ProjectileView>();
            view.transform.SetParent(parent, false);
            var element = PenguinPalette.ElementColor(projectile.Element);
            var color = isEnemy ? Color.Lerp(element, Color.black, EnemyDarkness) : element;
            var diameter = projectile.Radius * 2f;
            view.CreateBall(materials, color, diameter * BallSizeRatio);
            view.CreateGlow(materials, element, isEnemy ? EnemyGlowAlpha : GlowAlpha, diameter * GlowSizeRatio);
            view.CreateShadow(materials, diameter * ShadowSizeRatio);
            view._from = view._to = projectile.Position.ToVector2();
            view.Render(1f);
            return view;
        }

        private void CreateBall(StageMaterials materials, Color color, float size)
        {
            var ball = new GameObject("Ball");
            ball.transform.SetParent(transform, false);
            ball.transform.localPosition = Vector3.up * FlyHeight;
            ball.transform.localScale = Vector3.one * size;
            var renderer = ToyParts.AddMesh(ball, ToyMeshes.Sphere, materials.Toon.Get(color, BallGloss));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void CreateGlow(StageMaterials materials, Color color, float alpha, float size)
        {
            var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * FlyHeight;
            glow.sprite = DecalSprites.Glow;
            glow.sharedMaterial = materials.Glow;
            glow.color = new Color(color.r, color.g, color.b, alpha);
            glow.sortingOrder = SortingDepth.Popup - 4;
            glow.gameObject.AddComponent<FaceCamera>();
            _glow = glow;
            _glowSize = size;
            _flickerPhase = Random.value * Mathf.PI * 2f;
        }

        private void CreateShadow(StageMaterials materials, float size)
        {
            ToyParts.FloorPlate("Shadow", transform, Vector3.up * FloorDecal.BurstHeight, new Vector3(size, 1f, size), materials.Toon.BlobShadow);
        }

        public void PushState(ProjectileState projectile)
        {
            _from = _to;
            _to = projectile.Position.ToVector2();
        }

        public void Render(float alpha)
        {
            transform.position = Vector2.Lerp(_from, _to, alpha).ToFloor();
        }

        private void Update()
        {
            var flicker = 1f + Mathf.Sin(Time.time * FlickerSpeed + _flickerPhase) * FlickerAmount;
            FloorDecal.SetSize(_glow, Vector2.one * (_glowSize * flicker));
        }
    }
}
