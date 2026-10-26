using PenguinScramble.Common.UI;
using UnityEngine;

namespace PenguinScramble.Common.AppUpdate
{
    /// <summary>
    /// Windows 版でタイトルを開いたとき、新しい版が出ていれば「更新する / あとで」を出す。
    /// 毎回 zip を落として展開し直す手間をなくすため。入れ替えは WindowsUpdater。タイトル画面の CommonDialog と一緒に置く
    /// </summary>
    public class UpdateNotice : MonoBehaviour
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/kazu-hachi-08/APP_PENGUIN_SCRAMBLE/releases/latest";
        private const string Title = "新しい版が あります";
        private const string ConfirmText = "更新する";
        private const string CancelText = "あとで";

        [SerializeField] private CommonDialog _dialog;

        // タイトルに戻るたびに聞くとうるさいので、起動してから1回だけ。
        // 確かめている間にタイトルを離れると止まるので、答えが返ってから済みにする
        private static bool _checked;

        private void OnEnable()
        {
            if (_checked || !WindowsUpdater.CanUpdate)
            {
                return;
            }
            StartCoroutine(WindowsUpdater.FetchNewVersion(LatestReleaseApi, Offer));
        }

        private void Offer(string newVersion)
        {
            _checked = true;
            if (newVersion == null)
            {
                return;
            }
            string message = $"{newVersion} に 更新できます\n閉じて 入れ替え、開き直します\n（記録は そのまま 残ります）";
            _dialog.Show(Title, message, onConfirm: WindowsUpdater.InstallAndQuit, confirmText: ConfirmText, cancelText: CancelText);
        }
    }
}
