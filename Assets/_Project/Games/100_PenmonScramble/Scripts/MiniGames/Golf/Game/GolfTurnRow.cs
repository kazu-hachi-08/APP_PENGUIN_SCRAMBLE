using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>打順の表の1行: 席の色の丸・名前・打数。いまの番は ▶ と明るい板、カップに入った人は「カップイン」、上限で終わった人は「上限」</summary>
    public class GolfTurnRow : MonoBehaviour
    {
        private static readonly Color CurrentPlate = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color OtherPlate = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color DoneColor = new Color(1f, 0.88f, 0.35f);

        [SerializeField] private Image _plate;
        [SerializeField] private Image _seat;
        [SerializeField] private Text _name;
        [SerializeField] private Text _strokes;
        [SerializeField] private GameObject _marker;

        public void Show(string label, Color seatColor, int strokes, bool current, bool holedOut, bool gaveUp)
        {
            gameObject.SetActive(true);
            _plate.color = current ? CurrentPlate : OtherPlate;
            _seat.color = seatColor;
            _name.text = label;
            _marker.SetActive(current);
            if (holedOut || gaveUp)
            {
                _strokes.text = $"{strokes} 打  {(holedOut ? "カップイン" : "上限")}";
                _strokes.color = DoneColor;
            }
            else
            {
                _strokes.text = $"{strokes} 打";
                _strokes.color = Color.white;
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
