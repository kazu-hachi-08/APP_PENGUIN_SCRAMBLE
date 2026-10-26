using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PenguinScramble.Common.Input
{
    /// <summary>
    /// モバイル・タッチ操作向けのUGUI仮想アクションボタン
    /// </summary>
    public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsDown { get; private set; }
        public bool IsHeld { get; private set; }
        public bool IsUp { get; private set; }

        private bool _wasPressedThisFrame = false;
        private bool _wasReleasedThisFrame = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _wasPressedThisFrame = true;
            IsHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _wasReleasedThisFrame = true;
            IsHeld = false;
        }

        private void LateUpdate()
        {
            // フレーム単位のDown / Upのフラグ更新
            IsDown = _wasPressedThisFrame;
            IsUp = _wasReleasedThisFrame;

            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }

        private void OnDisable()
        {
            ResetButton();
        }

        public void ResetButton()
        {
            IsDown = false;
            IsHeld = false;
            IsUp = false;
            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }
    }
}
