using UnityEngine;
using UnityEngine.UI;

namespace PenguinScramble.Game
{
    /// <summary>
    /// バージョンを画面の隅に出す。配った版と手元の版の食い違いを、遊んでいる人の画面だけで確かめられるようにする
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class VersionLabel : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Text>().text = "v" + Application.version;
        }
    }
}
