using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 両ヒレで物を持って振る動き（ミニゲームのゴルフのクラブなど）と、しくじって頭を抱える動き。
    /// 持つ物そのものは呼び出し側が胴（Body）の子に付けて、HoldSwing と同じ角度で回す（ここでは体とヒレだけ）
    /// </summary>
    public sealed partial class PenguinMotion
    {
        private const float HoldBlendRate = 8f;
        private const float DespairBlendRate = 6f;

        // 持つ: 少し前かがみになって、ヒレを体の前へ伸ばして内へ寄せる（先が胸の前で合う）。頭は下の物を見る
        private const float HoldLean = 10f;
        private const float HoldReach = 70f;
        private const float HoldClose = 40f;
        private const float HoldHeadDown = 14f;
        // 振る: 体は振る角度の一部だけひねり、ヒレは残りを受け持つ。頭は逆へ回して、振っている間も前の物を見続ける
        private const float HoldTwist = 0.25f;
        private const float HoldFlipperFollow = 0.5f;
        private const float HoldHeadCounter = 0.2f;

        // 頭を抱える: ヒレを横から頭の上まで上げて前へ寄せ、うなだれて首を横に振る
        private const float DespairRaise = 150f;
        private const float DespairForward = 35f;
        private const float DespairLean = 12f;
        private const float DespairHeadDown = 18f;
        private const float DespairShake = 12f;
        private const float DespairShakeRate = 2.5f;
        private const float DespairSlump = 0.05f;

        /// <summary>両ヒレで物を体の前に持つ。止めると立った姿勢へなめらかに戻る</summary>
        public bool Holding { get; set; }

        /// <summary>持った物を振る角度（度）。正で右へ振り上げる、負で左へ振り抜く。持っていないときは効かない</summary>
        public float HoldSwing { get; set; }

        /// <summary>頭を抱える。止めると立った姿勢へなめらかに戻る</summary>
        public bool Despairing { get; set; }

        private void TickHold(float dt)
        {
            _pose.Hold = Mathf.MoveTowards(_pose.Hold, Holding && !Down ? 1f : 0f, HoldBlendRate * dt);
            _pose.HoldSwing = HoldSwing;
            _pose.Despair = Mathf.MoveTowards(_pose.Despair, Despairing && !Down ? 1f : 0f, DespairBlendRate * dt);
        }

        private static void HoldParts(PenguinPose pose, float standing, ref PartPose p)
        {
            var w = pose.Hold * standing;
            if (w <= 0f)
            {
                return;
            }

            var swing = pose.HoldSwing * w;
            p.Body.Euler += new Vector3(HoldLean * w, swing * HoldTwist, 0f);
            p.Head.Euler += new Vector3(HoldHeadDown * w, -swing * HoldHeadCounter, 0f);
            p.SwingFlippers(-HoldReach * w);
            p.OpenFlippers(-HoldClose * w);
            // 両ヒレを同じ向きへ回す（左右で逆に開く OpenFlippers とは違い、どちらも右へ振れる）
            p.FlipperLeft.Euler.z += swing * HoldFlipperFollow;
            p.FlipperRight.Euler.z += swing * HoldFlipperFollow;
        }

        private static void DespairParts(PenguinPose pose, float standing, ref PartPose p)
        {
            var w = pose.Despair * standing;
            if (w <= 0f)
            {
                return;
            }

            var shake = Mathf.Sin(pose.Time * DespairShakeRate * Mathf.PI * 2f) * DespairShake;
            p.Squash(DespairSlump * w);
            p.Body.Euler.x += DespairLean * w;
            p.Head.Euler += new Vector3(DespairHeadDown * w, shake * w, 0f);
            p.OpenFlippers(DespairRaise * w);
            p.SwingFlippers(-DespairForward * w);
        }
    }
}
