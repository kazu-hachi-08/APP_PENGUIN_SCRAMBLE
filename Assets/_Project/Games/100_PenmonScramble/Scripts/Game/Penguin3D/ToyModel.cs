using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 球などを組み合わせた3Dのおもちゃ（ペンギン・大ボス）の共通部分: 体全体の土台・足元の丸い影・色の変え方・影の落とし方。
    /// 前は +Z、足元が原点。部品の名前は PenguinModel / BigBossModel が持つ
    /// </summary>
    public abstract class ToyModel : MonoBehaviour
    {
        private readonly List<Part> _parts = new List<Part>();
        private ToonMaterials _materials;

        /// <summary>体全体。向き・転がる・倒れる・跳ぶときに丸ごと動かす。足元の丸い影は入れない（床に残すため）</summary>
        public Transform Figure { get; private set; }

        public Renderer BlobShadow { get; private set; }

        /// <summary>頭のてっぺん（いちばん高い所）の高さ（飾りは含めない）。頭の上の文字・氷の塊の高さに使う</summary>
        public float TopHeight { get; private set; }

        /// <summary>今の色の変え方（SetTint）</summary>
        public PenguinTint Tint { get; private set; }

        internal void AssignFrame(Transform figure, Renderer blobShadow, float topHeight)
        {
            Figure = figure;
            BlobShadow = blobShadow;
            TopHeight = topHeight;
        }

        internal void SetMaterials(ToonMaterials materials) => _materials = materials;

        internal void AddPart(Renderer renderer, Color color, float gloss) => _parts.Add(new Part(renderer, color, gloss));

        /// <summary>部品の色をまとめて変える（敵の暗さ・凍る・ダウン・被弾の光）。同じ変え方なら何もしないので、毎フレーム呼んでよい</summary>
        public void SetTint(PenguinTint tint)
        {
            if (tint.Equals(Tint))
            {
                return;
            }

            Tint = tint;
            foreach (var part in _parts)
            {
                part.Renderer.sharedMaterial = _materials.Get(tint.Apply(part.Color), part.Gloss);
            }
        }

        /// <summary>本物の影を落とすか、足元の丸い影にするか。両方出すと影が二重になるのでどちらか一方</summary>
        public void SetRealShadows(bool real)
        {
            foreach (var part in _parts)
            {
                part.Renderer.shadowCastingMode = real ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            BlobShadow.enabled = !real;
        }

        /// <summary>組み立てたときの色。色を変えるときは、いつもこの色から計算し直す</summary>
        private readonly struct Part
        {
            public readonly Renderer Renderer;
            public readonly Color Color;
            public readonly float Gloss;

            public Part(Renderer renderer, Color color, float gloss)
            {
                Renderer = renderer;
                Color = color;
                Gloss = gloss;
            }
        }
    }
}
