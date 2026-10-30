using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 文字・光をいつも画面と平行に向ける。カメラの位置ではなく向きにそろえるので、画面の端でも斜めにゆがまない
    /// </summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        private static Camera _camera;

        // 出た最初のフレームから向いているようにする
        private void OnEnable() => LateUpdate();

        private void LateUpdate()
        {
            if (_camera == null)
            {
                // Camera.main は毎回探すと重いので、見つけたら覚えておく（シーンを作り直したら探し直す）
                _camera = Camera.main;
                if (_camera == null) return;
            }

            transform.rotation = _camera.transform.rotation;
        }
    }
}
