using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 野球のカメラ: キャッチャーの後ろの高めから盤のほぼ全体を見る（投げる人も打つ人も同じ画面）。
    /// 打球が出たら見る所を球の方へ寄せて少し近づき、ホームランはフェンスの向こうへ飛んでいく球まで追う。結果が決まって次の球になったら戻る。
    /// 向きは変えずに位置だけ動かす（盤の傾きが変わらないので、打球の向きが読みやすい）
    /// </summary>
    public class BaseballCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [Tooltip("いつもの位置（キャッチャーの後ろの高め）")]
        [SerializeField] private Vector3 _homePosition = new Vector3(0f, 13f, -8.5f);
        [Tooltip("いつもの向き（下へ傾ける角度）")]
        [SerializeField] private Vector3 _homeAngles = new Vector3(48f, 0f, 0f);
        [Tooltip("打球を追うとき、見る所をいつもの所から球の方へ寄せる割合")]
        [SerializeField] private float _followShare = 0.55f;
        [Tooltip("打球を追うときに近づく割合（いつもの距離に掛ける）")]
        [SerializeField] private float _followZoom = 0.8f;
        [Tooltip("浮いた球の高さを見る所に足す割合（高く上がった球が画面の上へ抜けないように）")]
        [SerializeField] private float _heightShare = 0.5f;
        [Tooltip("球を追うときの追いつく速さ（秒。小さいほどすぐ追う）")]
        [SerializeField] private float _followSeconds = 0.3f;
        [Tooltip("いつもの所へ戻るときの速さ（秒）")]
        [SerializeField] private float _returnSeconds = 0.5f;

        private Vector3 _homeFocus;
        private float _homeDistance;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _distance;
        private float _distanceVelocity;
        private Vector3? _target;

        private void Awake()
        {
            var rotation = Quaternion.Euler(_homeAngles);
            var forward = rotation * Vector3.forward;
            // いつもの位置から見る向きの線が盤（高さ 0）に当たる所を、いつもの見る所にする
            _homeDistance = _homePosition.y / Mathf.Max(0.01f, -forward.y);
            _homeFocus = _homePosition + forward * _homeDistance;
            Snap();
        }

        /// <summary>追う球（盤の上の位置）。null ならいつもの所へ戻る。毎フレーム呼ぶ</summary>
        public void Follow(Vector3? ball) => _target = ball;

        /// <summary>なめらかに動かさず、すぐいつもの所へ置く</summary>
        public void Snap()
        {
            _target = null;
            _focus = _homeFocus;
            _distance = _homeDistance;
            _focusVelocity = Vector3.zero;
            _distanceVelocity = 0f;
            Apply();
        }

        private void LateUpdate()
        {
            var following = _target.HasValue;
            var focus = _homeFocus;
            var distance = _homeDistance;
            if (following)
            {
                var ball = _target.Value;
                var ground = new Vector3(ball.x, ball.y * _heightShare, ball.z);
                focus = Vector3.Lerp(_homeFocus, ground, _followShare);
                distance = _homeDistance * _followZoom;
            }

            var seconds = following ? _followSeconds : _returnSeconds;
            _focus = Vector3.SmoothDamp(_focus, focus, ref _focusVelocity, seconds);
            _distance = Mathf.SmoothDamp(_distance, distance, ref _distanceVelocity, seconds);
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(_homeAngles);
            _camera.transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
