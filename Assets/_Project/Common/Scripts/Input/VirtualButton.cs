using UnityEngine;
using UnityEngine.EventSystems;

namespace PenmonScramble.Common.Input
{
    /// <summary>
    /// モバイル・タッチ操作向けのUGUI仮想アクションボタン
    /// </summary>
    public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsDown { get; private set; }
        public bool IsHeld { get; private set; }

        private bool _wasPressedThisFrame = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _wasPressedThisFrame = true;
            IsHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
        }

        private void LateUpdate()
        {
            // フレーム単位のDownのフラグ更新
            IsDown = _wasPressedThisFrame;

            _wasPressedThisFrame = false;
        }

        private void OnDisable()
        {
            ResetButton();
        }

        public void ResetButton()
        {
            IsDown = false;
            IsHeld = false;
            _wasPressedThisFrame = false;
        }
    }
}
