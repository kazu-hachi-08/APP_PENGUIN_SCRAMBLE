using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 仮想スティック・ボタンは、タッチできる端末でだけ出す（PC では画面が狭くなるだけなので）
    /// </summary>
    public class TouchControlsVisibility : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(Application.isMobilePlatform || Touchscreen.current != null);
        }
    }
}
