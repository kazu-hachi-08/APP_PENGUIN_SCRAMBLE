using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 画面の端の印1つ: 外を指すとがった丸（色で何の印か分かる）と、丸の中の顔か記号、下の名前。
    /// とがった丸だけを回し、顔と文字は回さない（読めるように）
    /// </summary>
    public class EdgeMarkerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _pointerPivot;
        [SerializeField] private Image _pointer;
        [SerializeField] private GameObject _faceGroup;
        [SerializeField] private Image _face;
        [SerializeField] private Text _sign;
        [SerializeField] private Text _label;

        private int _faceSpecies = -1;

        /// <summary>顔の印（大ボス・仲間）。label は丸の下に出す名前（大ボスは出さない）</summary>
        public void ShowFace(int speciesNo, Color color, string label)
        {
            if (speciesNo != _faceSpecies)
            {
                _faceSpecies = speciesNo;
                CreatureArt.ShowFace(_face, speciesNo);
            }

            _faceGroup.SetActive(true);
            _sign.enabled = false;
            Show(color, label);
        }

        /// <summary>記号の印（大ボスの予告の「！」）</summary>
        public void ShowSign(string sign, Color color)
        {
            _faceGroup.SetActive(false);
            _sign.enabled = true;
            _sign.text = sign;
            Show(color, string.Empty);
        }

        /// <param name="angle">外を指す向き（度。右が 0、反時計回り）</param>
        public void Place(Vector2 position, float angle, float scale)
        {
            _root.anchoredPosition = position;
            _root.localScale = Vector3.one * scale;
            _pointerPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void Show(Color color, string label)
        {
            _pointer.color = color;
            _label.text = label;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
