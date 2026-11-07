using System.Collections.Generic;
using PenmonScramble.Common.Profile;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// みんなでのとき、ほかの人が動かしている子の頭の上に、その人の名前を席の色で出す（誰がどの子かを見分ける）。
    /// 自分の子には出さない（頭の上の黄色い印 SoccerControlMarker で分かり、名前と重なるため）。
    /// HUD の上の文字を、子の頭の位置へ毎フレーム置き直す（3D の文字にしないのは、HUD と同じ字で読みやすくするため）
    /// </summary>
    public class SoccerNameTags : MonoBehaviour
    {
        [Tooltip("名前の見本（隠しておく）。席の数だけ写して使う")]
        [SerializeField] private Text _template;
        [Tooltip("足元から名前までの高さ（マス）")]
        [SerializeField] private float _headHeight = 1.5f;

        private readonly List<Text> _tags = new List<Text>();
        private RectTransform _area;

        private void Awake()
        {
            _area = (RectTransform)transform;
            _template.gameObject.SetActive(false);
        }

        public void Clear()
        {
            foreach (var tag in _tags)
            {
                tag.gameObject.SetActive(false);
            }
        }

        /// <summary>seatShown は席ごとの、名前を出す子（−1 なら出さない）。localSeat（この端末の人）の名前は出さない</summary>
        public void Sync(IReadOnlyList<int> seatShown, int localSeat, SoccerView view)
        {
            var camera = Camera.main;
            for (var seat = 0; seat < seatShown.Count; seat++)
            {
                var tag = TagOf(seat);
                var feet = view.PlayerPosition(seatShown[seat]);
                var shown = seat != localSeat && camera != null && feet.HasValue && Place(tag.rectTransform, camera, feet.Value + Vector3.up * _headHeight);
                tag.gameObject.SetActive(shown);
            }

            for (var i = seatShown.Count; i < _tags.Count; i++)
            {
                _tags[i].gameObject.SetActive(false);
            }
        }

        private Text TagOf(int seat)
        {
            while (_tags.Count <= seat)
            {
                var tag = Instantiate(_template, _area);
                var index = _tags.Count;
                tag.name = $"Name{index}";
                tag.text = SeatNames.Get(index);
                tag.color = MiniGameSeats.ColorOf(index);
                _tags.Add(tag);
            }

            return _tags[seat];
        }

        /// <summary>頭の上の点を画面に写して置く。カメラの後ろなら置かない</summary>
        private bool Place(RectTransform tag, Camera camera, Vector3 head)
        {
            var screen = camera.WorldToScreenPoint(head);
            if (screen.z <= 0f)
            {
                return false;
            }

            // HUD の Canvas は画面に重ねる形なので、写す先のカメラは無し
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screen, null, out var local);
            tag.anchoredPosition = local;
            return true;
        }
    }
}
