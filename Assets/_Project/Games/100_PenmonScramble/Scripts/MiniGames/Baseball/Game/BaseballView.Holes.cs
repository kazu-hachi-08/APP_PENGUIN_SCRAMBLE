using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 盤の穴: 黒い穴と、種類が分かる色の縁（ヒットは明るい色・アウトは暗い色）。穴の手前（ホームの側）の盤に小さな札（「1B」「アウト」など）を描く
    /// （守備の子は穴の後ろに立つので、札は子に隠れない手前に置く）。球が入った穴は縁が白く光って膨らむ
    /// </summary>
    public partial class BaseballView
    {
        private const float HoleRimWiden = 0.14f;
        private const float HoleRimHeight = 0.5f;
        private const float HoleRimGloss = 0.5f;
        private const float HoleFlashSeconds = 0.7f;
        private const float HoleFlashScale = 0.35f;
        private const float LabelGap = 0.3f;
        private const float LabelCharacterSize = 0.045f;
        private const int LabelFontSize = 64;
        private static readonly Color HoleColor = new Color(0.07f, 0.06f, 0.06f);
        private static readonly Color HoleFlashColor = new Color(1f, 1f, 0.85f);

        private readonly List<HolePart> _holes = new List<HolePart>();

        private void BuildHoles(BaseballFieldDef field)
        {
            var dark = Materials.Floor(Texture2D.whiteTexture, HoleColor);
            for (var i = 0; i < field.Holes.Count; i++)
            {
                var hole = field.Holes[i];
                var center = BaseballSpace.ToWorld(hole.Center);
                var diameter = hole.Radius * 2f;
                AddPart($"Hole{i}", ToyMeshes.FloorDisc, _root, center + Vector3.up * (LayerLift * 10f), Quaternion.identity, new Vector3(diameter, 1f, diameter), dark);
                var rimScale = new Vector3(diameter + HoleRimWiden, HoleRimHeight, diameter + HoleRimWiden);
                var rim = AddPart($"HoleRim{i}", ToyMeshes.Torus, _root, center + Vector3.up * (LayerLift * 11f), Quaternion.identity, rimScale, RimMaterial(hole.Result));
                BuildLabel(hole, i);
                _holes.Add(new HolePart(rim, rimScale, RimMaterial(hole.Result)));
            }
        }

        /// <summary>札は盤に寝かせた字。カメラ（ホームの後ろ）から読める向きで、穴の手前に置く</summary>
        private void BuildLabel(BaseballHole hole, int index)
        {
            var obj = new GameObject($"HoleLabel{index}");
            obj.transform.SetParent(_root, false);
            var toward = -hole.Center.Normalized;
            var at = hole.Center + toward * (hole.Radius + LabelGap);
            obj.transform.localPosition = BaseballSpace.ToWorld(at, LayerLift * 12f);
            obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = obj.AddComponent<TextMesh>();
            var font = GameFontApplier.Font;
            text.font = font;
            text.text = Label(hole.Result);
            text.fontSize = LabelFontSize;
            text.characterSize = LabelCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = LabelColor(hole.Result);
            obj.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        private Material RimMaterial(BaseballHitResult result) => Materials.Get(RimColor(result), HoleRimGloss);

        /// <summary>縁の色: ヒットは明るい色（単打は黄・二塁打はだいだい・三塁打は桃色）、アウトは暗い色</summary>
        public static Color RimColor(BaseballHitResult result)
        {
            return result switch
            {
                BaseballHitResult.Single => new Color(1f, 0.88f, 0.25f),
                BaseballHitResult.Double => new Color(1f, 0.58f, 0.18f),
                BaseballHitResult.Triple => new Color(1f, 0.42f, 0.72f),
                BaseballHitResult.GroundOut => new Color(0.26f, 0.28f, 0.36f),
                BaseballHitResult.DoublePlay => new Color(0.50f, 0.14f, 0.18f),
                BaseballHitResult.FlyOut => new Color(0.18f, 0.24f, 0.46f),
                _ => Color.gray,
            };
        }

        /// <summary>札の字（短く。アウトの種類はゴロ・フライ・ゲッツーで言い分ける）</summary>
        private static string Label(BaseballHitResult result)
        {
            return result switch
            {
                BaseballHitResult.Single => "1B",
                BaseballHitResult.Double => "2B",
                BaseballHitResult.Triple => "3B",
                BaseballHitResult.GroundOut => "アウト",
                BaseballHitResult.DoublePlay => "ゲッツー",
                BaseballHitResult.FlyOut => "フライ",
                _ => string.Empty,
            };
        }

        /// <summary>ヒットは縁と同じ明るい色、アウトは白（暗い縁の色のままだと盤の上で読めないため）</summary>
        private static Color LabelColor(BaseballHitResult result) => BaseballResults.IsOut(result) ? Color.white : RimColor(result);

        private void FlashHole(int index)
        {
            if (index >= 0 && index < _holes.Count)
            {
                _holes[index].Flash = HoleFlashSeconds;
            }
        }

        private void TickHoles(float deltaTime)
        {
            foreach (var hole in _holes)
            {
                if (hole.Flash <= 0f)
                {
                    continue;
                }

                hole.Flash = Mathf.Max(0f, hole.Flash - deltaTime);
                var t = hole.Flash / HoleFlashSeconds;
                hole.Rim.localScale = hole.BaseScale * (1f + Mathf.Sin(t * Mathf.PI) * HoleFlashScale);
                hole.Renderer.sharedMaterial = hole.Flash > 0f ? Materials.Get(HoleFlashColor, HoleRimGloss) : hole.Normal;
            }
        }

        private sealed class HolePart
        {
            public HolePart(Transform rim, Vector3 baseScale, Material normal)
            {
                Rim = rim;
                BaseScale = baseScale;
                Normal = normal;
                Renderer = rim.GetComponent<MeshRenderer>();
            }

            public Transform Rim { get; }
            public Vector3 BaseScale { get; }
            public Material Normal { get; }
            public MeshRenderer Renderer { get; }
            public float Flash { get; set; }
        }
    }
}
