using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// UI の顔（ペンギン・大ボス）とワールドの地面の絵。SceneBuilder が Art/ にある PNG をここへ入れる。
    /// 顔は、Rebuild のときに3Dのモデルを撮って作る（UI の顔がゲーム中の見た目とそろうように）
    /// </summary>
    // 画面の部品が Start で絵を引くより先に、居場所を知らせておく
    [DefaultExecutionOrder(-100)]
    public class CreatureArt : MonoBehaviour
    {
        /// <summary>添字は種類の番号-1</summary>
        [SerializeField] private Sprite[] _penguins = new Sprite[SpeciesTable.Count];
        /// <summary>添字は大ボスの番号-101</summary>
        [SerializeField] private Sprite[] _bigBosses = new Sprite[Balance.WorldCount];
        /// <summary>添字はワールド-1。1エリア分（16×9マス）に引き伸ばして敷く</summary>
        [SerializeField] private Sprite[] _grounds = new Sprite[Balance.WorldCount];

        private static CreatureArt _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>ペンギン（1〜20）か大ボス（101〜）の絵。無ければ null</summary>
        public static Sprite Find(int speciesNo)
        {
            if (_instance == null)
            {
                return null;
            }

            return At(_instance._penguins, speciesNo - 1) ?? At(_instance._bigBosses, speciesNo - BigBossTable.SkuaNo);
        }

        public static Sprite Ground(int world) => _instance != null ? At(_instance._grounds, world - 1) : null;

        /// <summary>UI の顔（カード・ガチャ・カットイン）。絵の無い種類（新しい版で増えた子など）は出さない</summary>
        public static void ShowFace(Image image, int speciesNo)
        {
            var art = Find(speciesNo);
            image.sprite = art;
            image.color = Color.white;
            image.preserveAspect = true;
            image.enabled = art != null;
        }

        // 配列の外や、まだ絵の無い所は null（Unity の「消えたオブジェクト」も null にそろえる）
        private static Sprite At(Sprite[] sprites, int index)
        {
            return index >= 0 && index < sprites.Length && sprites[index] != null ? sprites[index] : null;
        }
    }
}
