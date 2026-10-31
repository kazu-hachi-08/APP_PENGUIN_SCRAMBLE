using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Tests
{
    /// <summary>3D のペンギンの見た目の定義表と、そこから組み立てた形</summary>
    public class PenguinLooksTests
    {
        // 頭の寝ぐせの部品の数
        private const int TuftParts = 3;
        // 目の光の部品の数（眠そうな目には入れない）
        private const int EyeShineParts = 2;

        [Test]
        public void Table_HasOneLookPerSpecies()
        {
            Assert.That(PenguinLooks.Count, Is.EqualTo(SpeciesTable.Count));
            for (var no = 1; no <= SpeciesTable.Count; no++)
            {
                Assert.That(PenguinLooks.Has(no), Is.True, $"No.{no}");
                Assert.That(PenguinLooks.Get(no), Is.Not.SameAs(PenguinLook.Default), $"No.{no}");
            }
        }

        [Test]
        public void OutOfRangeNumbers_UseDefaultLook()
        {
            // 大ボス（101〜）や通信で壊れた番号でも落ちない
            Assert.That(PenguinLooks.Get(0), Is.SameAs(PenguinLook.Default));
            Assert.That(PenguinLooks.Get(SpeciesTable.Count + 1), Is.SameAs(PenguinLook.Default));
            Assert.That(PenguinLooks.Get(BigBossTable.SkuaNo), Is.SameAs(PenguinLook.Default));
        }

        [Test]
        public void EverySpecies_HasOrnaments_AndNoTwoLookTheSame()
        {
            var signatures = new HashSet<string>();
            for (var no = 1; no <= SpeciesTable.Count; no++)
            {
                var look = PenguinLooks.Get(no);
                Assert.That(look.Ornaments.Length, Is.GreaterThan(0), $"No.{no} に飾りが無い");
                Assert.That(signatures.Add(Signature(look)), Is.True, $"No.{no} がほかの種類と同じ見た目");
            }
        }

        [Test]
        public void EverySpecies_CanBeBuilt_WithItsOrnaments()
        {
            var materials = new ToonMaterials(Shader.Find("PenmonScramble/Toon"), Shader.Find("PenmonScramble/BlobShadow"));
            var parent = new GameObject("PenguinLooksTests");
            try
            {
                var plain = PenguinModelBuilder.Build(PenguinLook.Default, materials, parent.transform).GetComponentsInChildren<Renderer>().Length;
                for (var no = 1; no <= SpeciesTable.Count; no++)
                {
                    var look = PenguinLooks.Get(no);
                    var model = PenguinModelBuilder.Build(look, materials, parent.transform);
                    Assert.That(model.GetComponentsInChildren<Collider>(), Is.Empty, $"No.{no}: ゲームの当たりは Core が持つ");
                    // 飾りは1つにつき部品が1つ以上ある
                    var expected = plain - (look.Tuft ? 0 : TuftParts) - (look.Eyes == EyeStyle.Sleepy ? EyeShineParts : 0) + look.Ornaments.Length;
                    Assert.That(model.GetComponentsInChildren<Renderer>().Length, Is.GreaterThanOrEqualTo(expected), $"No.{no}: 飾りが組み立てられていない");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
                materials.DestroyAll();
            }
        }

        private static string Signature(PenguinLook look)
        {
            var parts = new List<string> { look.Back.ToString(), look.EyeRing.ToString(), look.FlipperColor.ToString(), look.Eyes.ToString() };
            foreach (var ornament in look.Ornaments)
            {
                parts.Add($"{ornament.Kind}:{ornament.Color}");
            }

            return string.Join("|", parts);
        }
    }
}
