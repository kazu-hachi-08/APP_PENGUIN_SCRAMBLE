using System.Text;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>試しのホールの仮の HUD（打順・打数・持ち時間・ゲージ・起きたこと）。本番の HUD はフェーズ 3</summary>
    public class GolfDevHud : MonoBehaviour
    {
        [SerializeField] private Text _status;
        [SerializeField] private Text _message;
        [SerializeField] private GameObject _gauge;
        [SerializeField] private RectTransform _gaugeFill;
        [SerializeField] private float _messageSeconds = 2.2f;

        private readonly StringBuilder _builder = new StringBuilder();
        private float _messageLeft;

        public void Show(GolfHoleWorld world, GolfShotControl control)
        {
            _status.text = StatusText(world);
            _gauge.SetActive(world.Phase == GolfHolePhase.Aiming);
            _gaugeFill.localScale = new Vector3(1f, control.Power, 1f);

            if (_messageLeft > 0f)
            {
                _messageLeft -= Time.deltaTime;
                _message.enabled = _messageLeft > 0f;
            }
        }

        /// <summary>起きたことを少しの間、大きく出す（カップイン・池など）</summary>
        public void Flash(string message)
        {
            _message.text = message;
            _message.enabled = true;
            _messageLeft = _messageSeconds;
        }

        private string StatusText(GolfHoleWorld world)
        {
            var hole = world.Hole;
            _builder.Clear();
            _builder.Append(hole.Name).Append("  パー ").Append(hole.Par).Append("（上限 ").Append(hole.StrokeLimit).Append(" 打）\n");
            if (world.Phase != GolfHolePhase.Finished)
            {
                _builder.Append("P").Append(world.CurrentPlayer + 1).Append(" の番");
                if (world.Phase == GolfHolePhase.Aiming)
                {
                    _builder.Append("  のこり ").Append(Mathf.CeilToInt(world.ShotTimeLeft)).Append(" 秒");
                }

                _builder.Append('\n');
            }

            for (var player = 0; player < world.PlayerCount; player++)
            {
                _builder.Append("P").Append(player + 1).Append(": ").Append(world.Strokes(player)).Append(" 打");
                if (world.HoledOut(player))
                {
                    _builder.Append("  カップイン");
                }
                else if (world.IsDone(player))
                {
                    _builder.Append("  上限");
                }

                _builder.Append('\n');
            }

            return _builder.ToString();
        }
    }
}
