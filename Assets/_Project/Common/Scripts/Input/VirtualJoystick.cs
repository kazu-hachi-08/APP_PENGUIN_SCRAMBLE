using UnityEngine;
using UnityEngine.EventSystems;

namespace PenmonScramble.Common.Input
{
    /// <summary>
    /// モバイル・タッチ操作向けのUGUI仮想ジョイスティック
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;

        [Header("Settings")]
        [SerializeField] private float _handleRange = 60f;
        [SerializeField] private float _deadZone = 0.1f;

        private Vector2 _inputVector = Vector2.zero;
        private Canvas _canvas;
        private Camera _cam;

        public Vector2 Direction => _inputVector;

        private void Awake()
        {
            if (_background == null) _background = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _cam = (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera) ? _canvas.worldCamera : null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_background == null || _handle == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                _cam,
                out Vector2 position))
            {
                // バックグラウンドサイズに応じた正規化
                Vector2 radius = _background.sizeDelta * 0.5f;
                float maxDistance = _handleRange > 0 ? _handleRange : radius.x;

                Vector2 clampedPos = Vector2.ClampMagnitude(position, maxDistance);
                _handle.anchoredPosition = clampedPos;

                Vector2 rawInput = clampedPos / maxDistance;
                if (rawInput.magnitude < _deadZone)
                {
                    _inputVector = Vector2.zero;
                }
                else
                {
                    _inputVector = rawInput;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }

        private void OnDisable()
        {
            ResetInput();
        }

        public void ResetInput()
        {
            _inputVector = Vector2.zero;
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
