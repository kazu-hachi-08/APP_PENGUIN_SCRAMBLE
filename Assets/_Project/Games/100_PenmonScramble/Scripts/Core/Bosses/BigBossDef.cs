using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>大ボスの攻撃の種類。どれも出る前に地面へ赤い範囲を出して知らせる</summary>
    public enum BossAttackKind : byte
    {
        /// <summary>赤い帯の上をまっすぐ突っ込む（急降下・体当たり・画面を横切る突進）</summary>
        Charge,

        /// <summary>プレイヤーの足元とばらばらの場所に赤い円 → そこへ降ってくる（羽を飛ばす）</summary>
        Rain,

        /// <summary>もぐって消え、一番近いプレイヤーの足元の赤い円から飛び出す（氷の下から飛び出す）</summary>
        Ambush,

        /// <summary>エリアの上半分か下半分を赤い帯で知らせ、波で押し流す（波を起こす）</summary>
        Wave,

        /// <summary>闘技場の何か所かに赤い円 → 竜巻が立ち、しばらく近くのプレイヤーを中へ吸い込む（巨大ペンモンの風）</summary>
        Vortex,

        /// <summary>一番近いプレイヤーへ向けた赤い帯 → ボスは動かず、帯に稲妻が走る（巨大ペンモンの雷）</summary>
        Beam,

        /// <summary>ボスから扇形の赤い範囲 → 強い風で押し流す。押されて柵に当たると痛い（オオフルマカモメの突風）</summary>
        Gust,

        /// <summary>もぐって背びれだけが床を走る。走る道を赤い線で見せ、何度か向きを変えて走る。もぐっている間は攻撃が効かない（サメの背びれ）</summary>
        Fin,
    }

    /// <summary>
    /// 本気の大ボスが HP 半分を切った後に増える攻撃。Kind の攻撃を出すときだけ、Extra 回ぶん多く出す。
    /// Simultaneous なら同時に（波が2本）、そうでなければ休まずに続けて（羽の雨が2回）
    /// </summary>
    public sealed class BossRage
    {
        public BossRage(BossAttackKind kind, int extra, bool simultaneous)
        {
            Kind = kind;
            Extra = extra;
            Simultaneous = simultaneous;
        }

        public BossAttackKind Kind { get; }
        public int Extra { get; }
        public bool Simultaneous { get; }
    }

    /// <summary>大ボスの攻撃1つ分。ダメージ・吹っ飛びは Skill の値を使う（技と同じ当て方で済ませるため）</summary>
    public sealed class BossAttackDef
    {
        public BossAttackDef(BossAttackKind kind, float telegraphSeconds, SkillDef skill, bool crossScreen = false, SkillDef leftover = null)
        {
            Kind = kind;
            TelegraphSeconds = telegraphSeconds;
            Skill = skill;
            CrossScreen = crossScreen;
            Leftover = leftover;
        }

        public BossAttackKind Kind { get; }

        /// <summary>赤い範囲を出してから攻撃が出るまで（秒）。よけられるようにするため</summary>
        public float TelegraphSeconds { get; }

        /// <summary>
        /// 突進: Range（距離）・DashSpeed・AreaRadius（体より広く当たる分）/ 雨: AreaRadius（円の半径）・Count（ばらばらに落とす数）/
        /// 飛び出し: AreaRadius / 波: 使うのは Power・Knockback だけ /
        /// 竜巻: AreaRadius（吸い込む円の半径）・Count（竜巻の数）・Duration・Interval（当たる間隔）・Pull（吸い込む速さ）/
        /// 稲妻: AreaRadius（帯の幅の半分）・Range（帯の長さ。0 なら柵まで）/
        /// 突風: Range（扇の半径）・HalfAngleDegrees・DashSpeed（押し流す速さ）・Duration（押す秒数）・Power と Knockback は柵に当たったとき /
        /// 背びれ: Range（1回に走る距離）・DashSpeed・AreaRadius（体の半径に足す分。背びれだけ当たるので負）・Count（向きを変えて走り直す回数）/
        /// 竜巻: ChaseSpeed があれば、一番近いプレイヤーの方へゆっくり動く
        /// </summary>
        public SkillDef Skill { get; }

        /// <summary>突進: プレイヤーの方ではなく、横一直線に柵まで突っ込む（シャチ）。Range は使わない</summary>
        public bool CrossScreen { get; }

        /// <summary>
        /// 攻撃の後に床に残す範囲（設置と同じに、しばらく中の相手を削る）。Count か所を、攻撃の場所のまわりに置く。
        /// サメの食いつきの溶岩のしぶき。残さなければ null
        /// </summary>
        public SkillDef Leftover { get; }
    }

    /// <summary>大ボス1体分。ペンギンの天敵なので、ペンギンの種類表とは分けて持つ</summary>
    public sealed class BigBossDef
    {
        public BigBossDef(int no, string name, Element element, float hpRate, float radius, float moveSpeed, float restSeconds, BossRage rage, params BossAttackDef[] attacks)
        {
            Rage = rage;
            No = no;
            Name = name;
            Element = element;
            HpRate = hpRate;
            Radius = radius;
            MoveSpeed = moveSpeed;
            RestSeconds = restSeconds;
            Attacks = attacks;
        }

        /// <summary>種類の番号。ペンギン（1〜20）と重ならない 101〜。通信でもこの番号で送る</summary>
        public int No { get; }
        public string Name { get; }

        /// <summary>はじける演出の色にだけ使う</summary>
        public Element Element { get; }
        public float HpRate { get; }
        public float Radius { get; }
        public float MoveSpeed { get; }

        /// <summary>攻撃が終わってから次の予告を出すまで（秒）。この間が反撃のチャンス</summary>
        public float RestSeconds { get; }

        /// <summary>この順に繰り返す</summary>
        public IReadOnlyList<BossAttackDef> Attacks { get; }

        /// <summary>本気（ボスラッシュの3戦目）で HP 半分を切った後に増える攻撃</summary>
        public BossRage Rage { get; }
    }
}
