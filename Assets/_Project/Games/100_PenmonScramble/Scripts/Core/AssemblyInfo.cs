using System.Runtime.CompilerServices;

// テストで位置や向きを直接置けるようにする（ゲーム側からは StageWorld だけが書き換える）
[assembly: InternalsVisibleTo("PenmonScramble.Tests")]
