using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 盤の上の子1体（本編の3Dのペンモン。おもちゃの盤に合わせて小さく）。CPU のチームの子は本編の雑魚と同じく暗い色にする。
    /// 出てきたとき・別の所へ移ったときは、ぽんと膨らんで出る（ゴルフの打つ子と同じ）
    /// </summary>
    public sealed class BaseballPenguin
    {
        /// <summary>子の体の半径（マス）。盤の穴（半径 0.4〜0.5）の後ろに立って、穴が隠れない大きさ</summary>
        public const float Radius = 0.26f;

        private const float PopSeconds = 0.28f;
        private const float PopFrom = 0.3f;
        private const float PopOvershoot = 0.15f;

        private readonly float _scale;
        private float _pop = 1f;

        private BaseballPenguin(Transform root, PenguinMotion motion, int species)
        {
            Root = root;
            Motion = motion;
            Species = species;
            _scale = root.localScale.x;
        }

        public Transform Root { get; }
        public PenguinMotion Motion { get; }
        public int Species { get; }

        public static BaseballPenguin Build(string name, int species, bool wild, ToonMaterials materials, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * PenguinModelBuilder.ScaleForRadius(Radius);
            var look = PenguinLooks.Has(species) ? PenguinLooks.Get(species) : PenguinLooks.Get(1);
            var model = PenguinModelBuilder.Build(look, materials, root);
            // 太陽の影は切っているので、足元の丸い影にする（本編のステージと同じ）
            model.SetRealShadows(false);
            model.SetTint(wild ? PenguinTint.Dark(UnitView.EnemyDarkness) : PenguinTint.None);
            var motion = model.gameObject.AddComponent<PenguinMotion>();
            return new BaseballPenguin(root, motion, species);
        }

        /// <summary>立つ所へ置く。前の所から離れていたら、ぽんと出す</summary>
        public void PlaceAt(Vector3 position, Vector3 facing, bool pop)
        {
            Root.localPosition = position;
            Motion.SnapFacing(facing);
            Motion.Facing = facing;
            if (pop)
            {
                _pop = 0f;
            }
        }

        public void SetActive(bool active) => Root.gameObject.SetActive(active);

        public void Tick(float deltaTime)
        {
            if (_pop >= 1f)
            {
                return;
            }

            _pop = Mathf.Min(1f, _pop + deltaTime / PopSeconds);
            var eased = 1f - (1f - _pop) * (1f - _pop);
            var scale = Mathf.LerpUnclamped(PopFrom, 1f, eased) + Mathf.Sin(_pop * Mathf.PI) * PopOvershoot;
            Root.localScale = Vector3.one * (_scale * scale);
        }

        /// <summary>球の方へ顔を向ける（床の上の向きだけ。真上・足元の球では向きを変えない）</summary>
        public void Face(Vector3 target)
        {
            var toward = target - Root.position;
            toward.y = 0f;
            if (toward.sqrMagnitude > 0.01f)
            {
                Motion.Facing = toward;
            }
        }

        /// <summary>喜ぶ・悔しがる・どちらもしない（立った姿勢へ戻る）</summary>
        public void React(bool cheer, bool despair)
        {
            Motion.Cheering = cheer;
            Motion.Despairing = despair;
        }
    }
}
