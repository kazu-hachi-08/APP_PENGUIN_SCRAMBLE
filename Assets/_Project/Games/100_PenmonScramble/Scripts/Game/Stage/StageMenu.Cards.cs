using System;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// メニューの札を選ぶ・押す。選んでいる札は1枚だけで、マウスを乗せる・方向キー・スティックの上下で替わり、決定・クリック・タップで押す。
    /// 「やり直す」を出さない人（協力のゲスト）は、残りの札を詰めて並べる。
    /// 選んでいる場所（札・音量の板・確認の窓）で、上下左右と決定・Esc の意味が変わる
    /// </summary>
    public partial class StageMenu
    {
        // _cards の並び（上から）
        private const int ResumeCard = 0;
        private const int RestartCard = 1;
        private const int HelpCard = 2;
        private const int SettingsCard = 3;
        private const int TitleCard = 4;

        [Tooltip("上から つづける・やり直す・遊び方・設定・タイトルへ")]
        [SerializeField] private StageMenuCard[] _cards;
        [Tooltip("一番上の札の場所と、1枚下へのずれ（下ほど少し右へずらして、斜めの札の並びにする）")]
        [SerializeField] private Vector2 _firstCardPosition = new Vector2(360f, 272f);
        [SerializeField] private Vector2 _cardStep = new Vector2(28f, -136f);
        [Tooltip("開くときに1枚ずつずらす秒")]
        [SerializeField] private float _cardInStagger = 0.045f;
        [SerializeField] private float _cardOutStagger = 0.02f;

        private int _selected;

        private void AwakeCards()
        {
            foreach (var card in _cards)
            {
                card.Hovered += OnCardHovered;
                card.Pressed += OnCardPressed;
            }
        }

        /// <summary>出す札を決めて上から詰めて並べる</summary>
        private void BindCards()
        {
            var row = 0;
            for (var i = 0; i < _cards.Length; i++)
            {
                var shown = IsCardShown(i);
                _cards[i].gameObject.SetActive(shown);
                if (shown)
                {
                    _cards[i].Place(_firstCardPosition + _cardStep * row);
                    row++;
                }
            }
        }

        private bool IsCardShown(int index) => index != RestartCard || _canRestart;

        private void PlayCardsIn()
        {
            if (!IsCardShown(_selected))
            {
                _selected = ResumeCard;
            }

            SelectCard(_selected);
            var row = 0;
            for (var i = 0; i < _cards.Length; i++)
            {
                if (!IsCardShown(i))
                {
                    continue;
                }

                _cards[i].SnapSelection();
                _cards[i].PlayIn(row * _cardInStagger);
                row++;
            }
        }

        /// <summary>上の札から順に抜ける。全部抜け終わるまでの秒を返す</summary>
        private float PlayCardsOut()
        {
            var longest = 0f;
            var row = 0;
            for (var i = 0; i < _cards.Length; i++)
            {
                if (!IsCardShown(i))
                {
                    continue;
                }

                var delay = row * _cardOutStagger;
                _cards[i].PlayOut(delay);
                longest = Mathf.Max(longest, _cards[i].OutDuration(delay));
                row++;
            }

            return longest;
        }

        /// <summary>確認の窓 → 音量の板 → 札 の順に、手前の物が入力を受ける</summary>
        private void UpdateFocus(bool pause)
        {
            var step = _navigator.Step;
            if (_confirm.IsOpen)
            {
                if (pause) _confirm.Cancel();
                else if (_navigator.Submit) _confirm.Submit();
                else _confirm.Move(step);
                return;
            }

            if (_volume.IsOpen && (pause || _navigator.Submit))
            {
                // 板を閉じて札へ戻る（メニューまでは閉じない）
                _volume.Close();
                return;
            }

            if (pause)
            {
                BeginClose();
                return;
            }

            UpdateCardKeys(step);
        }

        private void UpdateCardKeys(Vector2Int step)
        {
            if (_volume.IsOpen && step != Vector2Int.zero)
            {
                _volume.Move(step);
                return;
            }

            if (step.y != 0)
            {
                SelectCard(NextShownCard(_selected, -step.y));
                GameSounds.Play(GameSound.Click);
            }
            else if (step.x > 0 && _selected == SettingsCard)
            {
                // 板は札の右に出るので、右で開けるようにする
                PressCard(SettingsCard, keyFocus: true);
            }
            else if (_navigator.Submit)
            {
                PressCard(_selected, keyFocus: true);
            }
        }

        /// <summary>出している札の中で、上下へ1つ進む（端では止まる）</summary>
        private int NextShownCard(int from, int direction)
        {
            for (var i = from + direction; i >= 0 && i < _cards.Length; i += direction)
            {
                if (IsCardShown(i))
                {
                    return i;
                }
            }

            return from;
        }

        private void SelectCard(int index)
        {
            if (index != SettingsCard)
            {
                // 板は「設定」を選んでいる間だけ出す
                _volume.Close();
            }

            _selected = index;
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetSelected(i == index);
            }
        }

        private void OnCardHovered(StageMenuCard card)
        {
            // 確認の窓が出ている間・閉じる途中は、後ろの札を選び直さない
            if (!_shown || IsClosing || _confirm.IsOpen)
            {
                return;
            }

            var index = Array.IndexOf(_cards, card);
            if (index >= 0 && index != _selected)
            {
                SelectCard(index);
            }
        }

        /// <summary>クリック・タップ</summary>
        private void OnCardPressed(StageMenuCard card)
        {
            if (!_shown || IsClosing || _confirm.IsOpen)
            {
                return;
            }

            var index = Array.IndexOf(_cards, card);
            if (index >= 0)
            {
                SelectCard(index);
                PressCard(index, keyFocus: false);
            }

            ClearSelection();
        }

        /// <param name="keyFocus">キーボード・コントローラーで押した（音量の板の行を光らせる）</param>
        private void PressCard(int index, bool keyFocus)
        {
            GameSounds.Play(GameSound.Click);
            switch (index)
            {
                case ResumeCard:
                    BeginClose();
                    break;
                case RestartCard:
                    ConfirmRestart();
                    break;
                case HelpCard:
                    OpenHowToPlay();
                    break;
                case SettingsCard:
                    ToggleVolume(keyFocus);
                    break;
                case TitleCard:
                    ConfirmTitle();
                    break;
            }
        }

        private void ToggleVolume(bool keyFocus)
        {
            if (_volume.IsOpen)
            {
                _volume.Close();
                return;
            }

            _volume.Open(keyFocus);
        }

        private void ConfirmRestart()
        {
            var message = _solo ? "はじめからやり直します。\nいまの進み具合は残りません" : "全員ではじめからやり直します";
            _confirm.Show("やり直す", message, "やり直す", OnRestartConfirmed, null);
        }

        private void ConfirmTitle()
        {
            _confirm.Show("タイトルへ", TitleMessage(), "タイトルへ", OnTitleConfirmed, null);
        }

        /// <summary>協力では、抜けると何が起きるかを伝える（ゲストは部屋から抜け、ホストが抜けると全員が戻る）</summary>
        private string TitleMessage()
        {
            if (_solo)
            {
                return "ステージをやめてタイトルに戻ります。\nいまの進み具合は残りません";
            }

            return _canRestart
                ? "部屋を閉じてタイトルに戻ります。\nほかの人も全員タイトルに戻ります"
                : "部屋から抜けてタイトルに戻ります";
        }

        private void OnRestartConfirmed()
        {
            CloseNow();
            RestartRequested?.Invoke();
        }

        private void OnTitleConfirmed()
        {
            CloseNow();
            TitleRequested?.Invoke();
        }
    }
}
