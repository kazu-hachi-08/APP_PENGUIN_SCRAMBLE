using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>大ボスの動かす関節。どの大ボスも同じ名前で持ち、その大ボスに無い関節は null</summary>
    public enum BigBossPart
    {
        /// <summary>胴（前の半分）。頭・ヒレ・後ろの半分はこの子</summary>
        Body,
        Head,

        /// <summary>下あご。開くと口の中と歯が見える</summary>
        Jaw,

        /// <summary>翼（カモメ）・前ヒレ（アザラシ）・胸ビレ（シャチ）の付け根</summary>
        LimbLeft,
        LimbRight,

        /// <summary>翼の先（カモメ）。付け根から遅れて動かし、羽ばたきをしなやかに見せる</summary>
        TipLeft,
        TipRight,

        /// <summary>胴の後ろの半分（アザラシの腰・シャチの尾の付け根）</summary>
        Hips,

        /// <summary>尾羽（カモメ）・後ろヒレ（アザラシ）・尾びれ（シャチ）</summary>
        Tail,
        FootLeft,
        FootRight,
    }

    /// <summary>
    /// 組み立てた3Dの大ボスの関節への参照。組み立てるのは BigBossModelBuilder、動かすのは BigBossMotion。
    /// 寸法は当たりの半径を 1 とした値で、全体の大きさは呼び出し側が根元の scale で決める
    /// </summary>
    public sealed class BigBossModel : ToyModel
    {
        private readonly Transform[] _joints = new Transform[JointCount];

        public const int JointCount = (int)BigBossPart.FootRight + 1;

        /// <summary>大ボスの番号（BigBossTable の 101〜）。動きの型を選ぶのに使う</summary>
        public int BossNo { get; private set; }

        /// <summary>目（白目・ひとみ・光をまとめた軸）。まばたきで縦につぶす</summary>
        public IReadOnlyList<Transform> Eyes { get; private set; }

        /// <summary>胴の中心の高さ（飛んでいるカモメは浮いている高さ）。被弾でのけぞるときの軸</summary>
        public float CenterHeight { get; private set; }

        public Transform this[BigBossPart part] => _joints[(int)part];

        internal void Assign(int bossNo, IReadOnlyList<Transform> eyes, float centerHeight)
        {
            BossNo = bossNo;
            Eyes = eyes;
            CenterHeight = centerHeight;
        }

        internal void SetJoint(BigBossPart part, Transform joint) => _joints[(int)part] = joint;
    }
}
