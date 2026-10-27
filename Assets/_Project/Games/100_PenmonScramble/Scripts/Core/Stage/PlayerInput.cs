namespace PenmonScramble.Core
{
    /// <summary>
    /// 1人分の入力。オンラインではゲストがこれだけをホストへ送る（§8）ので、小さく保つ
    /// </summary>
    public readonly struct PlayerInput
    {
        /// <summary>移動の向き。長さは 0〜1（スティックの倒し具合）</summary>
        public readonly Vec2 Move;

        /// <summary>押している間、待ち時間が明けるたびに技を出す（技1の連打をスマホでも楽にするため）</summary>
        public readonly bool Skill1;
        public readonly bool Skill2;

        public PlayerInput(Vec2 move, bool skill1, bool skill2)
        {
            Move = move;
            Skill1 = skill1;
            Skill2 = skill2;
        }

        public bool IsSkillHeld(int slot) => slot == 0 ? Skill1 : Skill2;
    }
}
