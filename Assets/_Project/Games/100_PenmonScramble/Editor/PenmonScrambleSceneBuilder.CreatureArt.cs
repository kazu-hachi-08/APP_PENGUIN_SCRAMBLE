using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// UI の顔とワールドの地面の絵（§9）を CreatureArt に入れる。
    /// 顔は3Dのモデルを撮った絵（PenmonScrambleSceneBuilder.Portraits.cs）、地面は Art/Stage/ の絵（Tools/Art/gen.py）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string ArtFolder = "Assets/_Project/Games/100_PenmonScramble/Art/";
        // 画面の中では大きくても高さの 1/5 くらい。これ以上はブラウザ版のダウンロードが重くなるだけ
        private const int CreatureArtMaxSize = 512;
        private const int GroundArtMaxSize = 2048;

        private static void CreateCreatureArt()
        {
            var art = new GameObject("CreatureArt").AddComponent<CreatureArt>();
            var so = new SerializedObject(art);
            FillSprites(so.FindProperty("_penguins"), SpeciesTable.Count, i => PenguinPortraitPath(i + 1), CreatureArtMaxSize);
            FillSprites(so.FindProperty("_bigBosses"), Balance.WorldCount, i => BossPortraitPath(BigBossTable.SkuaNo + i), CreatureArtMaxSize);
            FillSprites(so.FindProperty("_grounds"), Balance.WorldCount, i => $"{ArtFolder}Stage/World{i + 1}.png", GroundArtMaxSize);
            so.ApplyModifiedProperties();
        }

        private static void FillSprites(SerializedProperty array, int count, System.Func<int, string> pathOf, int maxSize)
        {
            array.arraySize = count;
            var found = 0;
            for (var i = 0; i < count; i++)
            {
                var sprite = LoadArtSprite(pathOf(i), maxSize);
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                found += sprite != null ? 1 : 0;
            }

            Debug.Log($"[PenmonScrambleSceneBuilder] {array.name}: 絵 {found} / {count} 枚");
        }
    }
}
