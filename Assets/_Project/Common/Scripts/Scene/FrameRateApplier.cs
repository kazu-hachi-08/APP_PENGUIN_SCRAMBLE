using UnityEngine;

namespace PenmonScramble.Common.Scene
{
    /// <summary>
    /// スマホの描く回数を毎秒60回にする。Unity はスマホだと何も決めないと毎秒30回に抑えるので、動きのあるゲームではカクついて見える。
    /// PC は画質の設定の垂直同期、ブラウザは画面の書き換え（requestAnimationFrame）に合わせるので触らない
    /// </summary>
    public static class FrameRateApplier
    {
        private const int MobileFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Application.targetFrameRate = MobileFrameRate;
            }
        }
    }
}
