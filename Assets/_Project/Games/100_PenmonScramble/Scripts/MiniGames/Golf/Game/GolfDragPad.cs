using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 画面のボタンの無い所のドラッグ（画面いっぱいの透明な板。HUD のボタンより奥に置く）。
    /// 1本指（マウスの左）の横のドラッグは狙う向き、2本指（マウスの右）のドラッグはカメラの見回し。
    /// 量は画面の高さで割った割合（端末の解像度によらず同じ手ごたえにする）。読んだら消える
    /// </summary>
    public class GolfDragPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private readonly HashSet<int> _pointers = new HashSet<int>();
        private float _aim;
        private Vector2 _look;

        /// <summary>狙う向きのドラッグ（右へ動かすと正）。読んだら 0 に戻る</summary>
        public float TakeAim()
        {
            var aim = _aim;
            _aim = 0f;
            return aim;
        }

        /// <summary>見回しのドラッグ。読んだら 0 に戻る</summary>
        public Vector2 TakeLook()
        {
            var look = _look;
            _look = Vector2.zero;
            return look;
        }

        public void OnPointerDown(PointerEventData eventData) => _pointers.Add(eventData.pointerId);

        public void OnPointerUp(PointerEventData eventData) => _pointers.Remove(eventData.pointerId);

        public void OnDrag(PointerEventData eventData)
        {
            var delta = eventData.delta / Mathf.Max(1f, Screen.height);
            if (eventData.button == PointerEventData.InputButton.Right || _pointers.Count >= 2)
            {
                // 2本指は両方の指から届くので、半分ずつ足して1本分の量にする
                _look += _pointers.Count >= 2 ? delta / _pointers.Count : delta;
                return;
            }

            _aim += delta.x;
        }

        private void OnDisable()
        {
            _pointers.Clear();
            _aim = 0f;
            _look = Vector2.zero;
        }
    }
}
