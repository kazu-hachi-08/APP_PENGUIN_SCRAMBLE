using System;
using PenmonScramble.MiniGames.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>ミニゲーム画面のカード1枚（絵・名前・一言の説明）。押すとそのミニゲームのコース選びへ</summary>
    public class MiniGameCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _art;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;

        public void Show(MiniGameDef game, Sprite art, Action<MiniGameDef> onClick)
        {
            _nameText.text = game.Name;
            _descriptionText.text = game.Description;
            _art.sprite = art;
            _art.enabled = art != null;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(game));
        }
    }
}
