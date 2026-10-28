using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>大ボスの攻撃の種類（§6.2）。どれも出る前に地面へ赤い範囲を出して知らせる</summary>
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
    }

    /// <summary>大ボスの攻撃1つ分。ダメージ・吹っ飛びは Skill の値を使う（技と同じ当て方で済ませるため）</summary>
    public sealed class BossAttackDef
    {
        public BossAttackDef(BossAttackKind kind, float telegraphSeconds, SkillDef skill, bool crossScreen = false)
        {
            Kind = kind;
            TelegraphSeconds = telegraphSeconds;
            Skill = skill;
            CrossScreen = crossScreen;
        }

        public BossAttackKind Kind { get; }

        /// <summary>赤い範囲を出してから攻撃が出るまで（秒）。よけられるようにするため</summary>
        public float TelegraphSeconds { get; }

        /// <summary>
        /// 突進: Range（距離）・DashSpeed・AreaRadius（体より広く当たる分）/ 雨: AreaRadius（円の半径）・Count（ばらばらに落とす数）/
        /// 飛び出し: AreaRadius / 波: 使うのは Power・Knockback だけ
        /// </summary>
        public SkillDef Skill { get; }

        /// <summary>突進: プレイヤーの方ではなく、横一直線にエリアの端から端まで突っ込む（シャチ）</summary>
        public bool CrossScreen { get; }
    }

    /// <summary>大ボス1体分（§6.2）。ペンギンの天敵なので、ペンギンの種類表とは分けて持つ</summary>
    public sealed class BigBossDef
    {
        public BigBossDef(int no, string name, Element element, float hpRate, float radius, float moveSpeed, float restSeconds, params BossAttackDef[] attacks)
        {
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
    }
}
