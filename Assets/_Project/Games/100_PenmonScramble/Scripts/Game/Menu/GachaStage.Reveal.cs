using System;
using System.Collections;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 引く流れ:レバーが回る → カプセルが転がり出て台に止まる（カメラが寄る）→ 揺れる →（★3 は部屋が暗くなり金の光の柱）
    /// → ★の色に光って★が1つずつ出る → 割れて中の子が飛び出し、くるっと回ってポーズ。
    /// 流れは GachaScreen のコルーチンの中で回すので、とばすときは GachaScreen が止めてから Finish を呼ぶ
    /// </summary>
    public sealed partial class GachaStage
    {
        private const float BackToWideSeconds = 0.45f;

        private const float LeverSeconds = 0.9f;
        private const int LeverClicks = 4;

        private const float RollSeconds = 1.1f;
        // 取り出し口から飛び出して弧を描き、だんだん低くはねて止まる。|sin| の山の数（1/3・2/3・1 で床に当たる）
        private const int RollBounces = 3;
        private const float RollHop = 0.9f;
        private const float CapsuleSize = 0.8f;
        private static readonly Color CapsuleTopColor = new Color(0.62f, 0.88f, 1f);
        private static readonly Color CapsuleBottomColor = new Color(1f, 0.62f, 0.22f);
        // 止まったときは少し傾ける（アイコンの絵と同じ、斜めの合わせ目に見せる）
        private const float RestTilt = 18f;

        private static readonly float[] WobbleAngles = { 12f, 18f, 26f };
        private const float WobbleSeconds = 0.28f;
        private const float WobblePause = 0.18f;
        private const float WobbleHop = 0.25f;

        private const float RareFadeSeconds = 0.3f;
        private const float RareHoldSeconds = 0.55f;
        private static readonly Vector2 PillarSize = new Vector2(1.5f, 10f);
        private const float PillarHeight = 4f;
        // 光は子の少し奥に立てる（手前に重なると、足して光る分だけ子の色が白く飛ぶ）
        private const float GlowBehind = 0.6f;

        private const float CapsuleTintAmount = 0.6f;
        private const float StarSeconds = 0.3f;
        private const float HaloHeight = 0.5f;
        private const float HaloBaseSize = 1.6f;
        private const float HaloSizePerStar = 0.5f;
        private const float HaloRestAlpha = 0.45f;
        private const float HaloPulse = 0.08f;

        private const float OpenSeconds = 0.5f;
        private const float ShellFlyHeight = 1.4f;
        private const float ShellSpin = 260f;
        private const int SparkleCoinsPerStar = 3;
        private const float SparkleSpread = 1.2f;

        // 出てきた子: 台の上で少し大きく見せる。小さく出て弾み、1回転してから正面を向く
        private const float RevealModelScale = 1.25f;
        private const float PopSeconds = 0.5f;
        private const float PopStart = 0.3f;
        // 正面を向いてから、技のポーズを順に見せる間隔
        private const float PoseInterval = 2.4f;

        private Capsule? _capsule;
        private SpriteRenderer _pillar;
        private SpriteRenderer _halo;
        private Color _haloColor;
        private int _haloStars;
        private PenguinModel _penguin;
        private PenguinMotion _motion;
        private SkillShape[] _shapes;
        private int _nextShape;
        private float _popAge;
        private float _poseTimer;

        private static float ModelScale => PenguinModelBuilder.ScaleForRadius(Balance.PenguinRadius) * RevealModelScale;

        private static Vector3 RestPoint => RevealSpot + Vector3.up * PedestalTop;

        /// <summary>1回分を最初から最後まで見せる。onStar は★が1つ出るたびに、その数（1〜）で呼ぶ</summary>
        public IEnumerator Play(int speciesNo, int stars, Action<int> onStar)
        {
            RemovePenguin(withSmoke: true);
            yield return MoveShot(Wide, BackToWideSeconds);
            yield return TurnLever();
            yield return RollOut();
            yield return Wobble();
            var color = PenguinLabels.StarColor(stars);
            if (stars >= Balance.MaxStars)
            {
                yield return RarePillar(color);
            }

            yield return LightUp(color, stars, onStar);
            yield return Open(speciesNo, stars);
        }

        /// <summary>とばしたとき: 途中の物を片付けて、出てきた子が台に立った所にする</summary>
        public void Finish(int speciesNo, int stars)
        {
            DestroyCapsule();
            DestroyPillar();
            SetRareDark(0f);
            _stir = 0f;
            LeverAngle = 0f;
            if (_penguin == null)
            {
                SpawnPenguin(speciesNo);
            }

            _popAge = PopSeconds;
            ShowHalo(PenguinLabels.StarColor(stars), stars);
            SetHaloAlpha(HaloRestAlpha);
            SetShot(Close);
        }

        /// <summary>画面を開いたとき: 前に引いた子や途中の物を消して、マシンの前に構える</summary>
        private void ResetReveal()
        {
            RemovePenguin(withSmoke: false);
            DestroyCapsule();
            DestroyPillar();
            if (_halo != null) _halo.gameObject.SetActive(false);
            _stir = 0f;
            LeverAngle = 0f;
            SetRareDark(0f);
        }

        /// <summary>部屋ごと消したので、持っていた参照を捨てる</summary>
        private void ForgetReveal()
        {
            _capsule = null;
            _pillar = null;
            _halo = null;
            _penguin = null;
            _motion = null;
        }

        private IEnumerator MoveShot(Shot to, float seconds)
        {
            var from = _shot;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                SetShot(Shot.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }

            SetShot(to);
        }

        /// <summary>レバーを1回転させる。1/4 回るごとにカチッと鳴らし、窓の中のカプセルをかき混ぜる</summary>
        private IEnumerator TurnLever()
        {
            var clicks = 0;
            for (var t = 0f; t < LeverSeconds; t += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, t / LeverSeconds);
                LeverAngle = progress * 360f;
                _stir = Mathf.Sin(Mathf.PI * t / LeverSeconds);
                if (progress * LeverClicks >= clicks + 1)
                {
                    clicks++;
                    GameSounds.Play(GameSound.GachaLever);
                }

                yield return null;
            }

            LeverAngle = 0f;
            _stir = 0f;
        }

        /// <summary>取り出し口から飛び出したカプセルが、はねながら転がって台の上に止まる。その間にカメラが台へ寄る</summary>
        private IEnumerator RollOut()
        {
            var capsule = BuildCapsule(_world, "Capsule", CapsuleSize, CapsuleTopColor, CapsuleBottomColor, withSeam: true);
            _capsule = capsule;
            var start = ChuteMouth - Vector3.up * (CapsuleSize * 0.5f);
            var rest = Quaternion.Euler(0f, 0f, RestTilt);
            var previous = start;
            var impacts = 1;
            capsule.Root.localPosition = start;
            GameSounds.Play(GameSound.GachaDrop);
            for (var t = 0f; t < RollSeconds; t += Time.deltaTime)
            {
                var u = t / RollSeconds;
                var flat = Vector3.Lerp(new Vector3(start.x, 0f, start.z), new Vector3(RestPoint.x, 0f, RestPoint.z), 1f - (1f - u) * (1f - u));
                var ground = Mathf.Lerp(start.y, RestPoint.y, Mathf.SmoothStep(0f, 1f, u));
                var hop = RollHop * (1f - u) * (1f - u) * Mathf.Abs(Mathf.Sin(Mathf.PI * RollBounces * u));
                var position = flat + Vector3.up * (ground + hop);
                Roll(capsule, position - previous, u, rest);
                capsule.Root.localPosition = position;
                previous = position;
                // はねるたびにコトンと鳴らす（sin が 0 になる所が床に当たった所）
                if (u * RollBounces >= impacts && impacts < RollBounces)
                {
                    impacts++;
                    GameSounds.Play(GameSound.GachaDrop);
                }

                SetShot(Shot.Lerp(Wide, Close, Mathf.SmoothStep(0f, 1f, u)));
                yield return null;
            }

            capsule.Root.localPosition = RestPoint;
            capsule.Pivot.localRotation = rest;
            SetShot(Close);
        }

        /// <summary>進んだ分だけ転がす。終わりに近づいたら、止まったときの傾きへなめらかに寄せる</summary>
        private static void Roll(Capsule capsule, Vector3 step, float progress, Quaternion rest)
        {
            var flat = new Vector3(step.x, 0f, step.z);
            if (flat.sqrMagnitude > 0f)
            {
                var axis = Vector3.Cross(Vector3.up, flat.normalized);
                var degrees = flat.magnitude / (CapsuleSize * 0.5f) * Mathf.Rad2Deg;
                capsule.Pivot.Rotate(axis, degrees, Space.World);
            }

            const float settleFrom = 0.7f;
            if (progress > settleFrom)
            {
                var settle = (progress - settleFrom) / (1f - settleFrom);
                capsule.Pivot.localRotation = Quaternion.Slerp(capsule.Pivot.localRotation, rest, settle);
            }
        }

        /// <summary>だんだん大きく3回揺れる。最後は少し跳ねる</summary>
        private IEnumerator Wobble()
        {
            var root = _capsule.Value.Root;
            for (var i = 0; i < WobbleAngles.Length; i++)
            {
                GameSounds.Play(GameSound.GachaDrop);
                var last = i == WobbleAngles.Length - 1;
                for (var t = 0f; t < WobbleSeconds; t += Time.deltaTime)
                {
                    var u = t / WobbleSeconds;
                    var angle = Mathf.Sin(u * Mathf.PI * 2f) * (1f - u) * WobbleAngles[i] * (i % 2 == 0 ? 1f : -1f);
                    root.localRotation = Quaternion.Euler(0f, 0f, angle);
                    var hop = last ? Mathf.Sin(u * Mathf.PI) * WobbleHop : 0f;
                    root.localPosition = RestPoint + Vector3.up * hop;
                    yield return null;
                }

                root.localRotation = Quaternion.identity;
                root.localPosition = RestPoint;
                yield return new WaitForSeconds(WobblePause);
            }
        }

        /// <summary>★3: 部屋が暗くなって、台から金の光の柱が立つ（ここで「当たり」と分かる）</summary>
        private IEnumerator RarePillar(Color color)
        {
            GameSounds.Play(GameSound.GachaRare);
            _pillar = AddGlow("Pillar", color, RevealSpot + new Vector3(0f, PillarHeight, GlowBehind), PillarSize);
            for (var t = 0f; t < RareFadeSeconds; t += Time.deltaTime)
            {
                var u = t / RareFadeSeconds;
                SetRareDark(u);
                FloorDecal.SetSize(_pillar, new Vector2(PillarSize.x * u, PillarSize.y));
                yield return null;
            }

            SetRareDark(1f);
            FloorDecal.SetSize(_pillar, PillarSize);
            yield return new WaitForSeconds(RareHoldSeconds);
        }

        /// <summary>カプセルが★の色に光り、★が1つ出るたびに後ろの光が大きくなる</summary>
        private IEnumerator LightUp(Color color, int stars, Action<int> onStar)
        {
            var capsule = _capsule.Value;
            capsule.Top.GetComponent<Renderer>().sharedMaterial = _materials.Toon.Get(Color.Lerp(CapsuleTopColor, color, CapsuleTintAmount), 1f);
            capsule.Bottom.GetComponent<Renderer>().sharedMaterial = _materials.Toon.Get(Color.Lerp(CapsuleBottomColor, color, CapsuleTintAmount), 1f);
            for (var i = 1; i <= stars; i++)
            {
                ShowHalo(color, i);
                onStar?.Invoke(i);
                GameSounds.Play(GameSound.Star);
                for (var t = 0f; t < StarSeconds; t += Time.deltaTime)
                {
                    capsule.Pivot.localScale = Vector3.one * ToyEasing.Pop(t / StarSeconds, 1.15f);
                    yield return null;
                }

                capsule.Pivot.localScale = Vector3.one;
            }
        }

        /// <summary>殻が上と下へ割れて飛び、煙とコイン・星がはじけて、中の子が飛び出す</summary>
        private IEnumerator Open(int speciesNo, int stars)
        {
            GameSounds.Play(GameSound.GachaOpen);
            var capsule = _capsule.Value;
            var floor = Floor(World(RevealSpot));
            SmokePuff.Spawn(_world, _materials, floor, Balance.PenguinRadius * RevealModelScale, CapsuleSize);
            DefeatSparkle.Spawn(_world, _materials, floor, stars * SparkleCoinsPerStar, SparkleSpread);
            SpawnPenguin(speciesNo);
            var topFrom = capsule.Top.localPosition;
            var bottomFrom = capsule.Bottom.localPosition;
            for (var t = 0f; t < OpenSeconds; t += Time.deltaTime)
            {
                var u = t / OpenSeconds;
                var shrink = Vector3.one * (CapsuleSize * (1f - u * u));
                capsule.Top.localPosition = topFrom + new Vector3(0.3f, ShellFlyHeight, 0f) * u;
                capsule.Top.localRotation = Quaternion.Euler(-ShellSpin * u, 0f, 0f);
                capsule.Top.localScale = shrink;
                capsule.Bottom.localPosition = bottomFrom + new Vector3(-0.2f, -0.3f, -0.3f) * u;
                capsule.Bottom.localRotation = Quaternion.Euler(180f + ShellSpin * 0.5f * u, 0f, 0f);
                capsule.Bottom.localScale = shrink;
                if (_pillar != null)
                {
                    SetRareDark(1f - u);
                    _pillar.color = new Color(_pillar.color.r, _pillar.color.g, _pillar.color.b, 1f - u);
                }

                SetHaloAlpha(Mathf.Lerp(1f, HaloRestAlpha, u));
                yield return null;
            }

            DestroyCapsule();
            DestroyPillar();
            SetRareDark(0f);
            SetHaloAlpha(HaloRestAlpha);
            while (_popAge < PopSeconds)
            {
                yield return null;
            }
        }

        private void SpawnPenguin(int speciesNo)
        {
            _penguin = PenguinModelBuilder.Build(PenguinLooks.Get(speciesNo), _materials.Toon, _world);
            _penguin.transform.localPosition = RestPoint;
            _penguin.transform.localScale = Vector3.one * (ModelScale * PopStart);
            _penguin.SetRealShadows(false);
            _motion = _penguin.gameObject.AddComponent<PenguinMotion>();
            // 回り終えるまでは向きを決めない（根元を回して1回転を見せるので、姿勢の向きが打ち消さないように）
            _motion.SnapFacing(Vector3.back);
            _motion.Facing = Vector3.zero;
            _motion.PlaySpecial();
            var species = SpeciesTable.Get(speciesNo);
            _shapes = new[] { StandingShape(species.Skill1.Shape), StandingShape(species.Skill2.Shape) };
            _nextShape = 0;
            _popAge = 0f;
            _poseTimer = PoseInterval * 0.5f;
        }

        /// <summary>突進はその場ですべっても前に出ないので、近接の振りで見せる（タイトルのジオラマと同じ）</summary>
        private static SkillShape StandingShape(SkillShape shape) => shape == SkillShape.Dash ? SkillShape.Melee : shape;

        private void RemovePenguin(bool withSmoke)
        {
            if (_penguin == null)
            {
                return;
            }

            if (withSmoke)
            {
                SmokePuff.Spawn(_world, _materials, Floor(World(RevealSpot)), Balance.PenguinRadius * RevealModelScale, _penguin.TopHeight * ModelScale);
            }

            Destroy(_penguin.gameObject);
            _penguin = null;
            _motion = null;
            if (_halo != null) _halo.gameObject.SetActive(false);
        }

        /// <summary>出てきた子: 弾みながら1回転して正面を向き、そのあとは技のポーズを順に見せる</summary>
        private void TickRevealIdle(float dt)
        {
            TickHalo();
            if (_penguin == null)
            {
                return;
            }

            if (_popAge < PopSeconds)
            {
                _popAge += dt;
                var u = Mathf.Clamp01(_popAge / PopSeconds);
                _penguin.transform.localScale = Vector3.one * (ModelScale * ToyEasing.Pop(u, PopStart));
                _penguin.transform.localRotation = Quaternion.Euler(0f, 360f * (1f - Mathf.SmoothStep(0f, 1f, u)), 0f);
                if (_popAge >= PopSeconds)
                {
                    _motion.Facing = Vector3.back;
                }

                return;
            }

            _penguin.transform.localScale = Vector3.one * ModelScale;
            _penguin.transform.localRotation = Quaternion.identity;
            _motion.Facing = Vector3.back;
            _poseTimer -= dt;
            if (_poseTimer <= 0f)
            {
                _poseTimer = PoseInterval;
                _motion.PlayAttack(_shapes[_nextShape]);
                _nextShape = (_nextShape + 1) % _shapes.Length;
            }
        }

        private void ShowHalo(Color color, int stars)
        {
            if (_halo == null)
            {
                _halo = AddGlow("Halo", color, RevealSpot + new Vector3(0f, HaloHeight, GlowBehind), Vector2.one * HaloBaseSize);
            }

            _halo.gameObject.SetActive(true);
            _haloColor = color;
            _haloStars = stars;
            _halo.color = color;
            FloorDecal.SetSize(_halo, Vector2.one * HaloSize(stars));
        }

        private static float HaloSize(int stars) => HaloBaseSize + HaloSizePerStar * stars;

        private void SetHaloAlpha(float alpha)
        {
            if (_halo != null)
            {
                _halo.color = new Color(_haloColor.r, _haloColor.g, _haloColor.b, alpha);
            }
        }

        /// <summary>出てきた子の後ろの光をゆっくり息づかせる</summary>
        private void TickHalo()
        {
            if (_halo == null || !_halo.gameObject.activeSelf || _penguin == null || _popAge < PopSeconds)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(_time * 2f) * HaloPulse;
            FloorDecal.SetSize(_halo, Vector2.one * (HaloSize(_haloStars) * pulse));
        }

        private void DestroyCapsule()
        {
            if (_capsule.HasValue && _capsule.Value.Root != null)
            {
                Destroy(_capsule.Value.Root.gameObject);
            }

            _capsule = null;
        }

        private void DestroyPillar()
        {
            if (_pillar != null)
            {
                _glows.Remove(_pillar.transform);
                Destroy(_pillar.gameObject);
            }

            _pillar = null;
        }
    }
}
