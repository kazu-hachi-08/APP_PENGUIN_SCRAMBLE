using System.Runtime.CompilerServices;

// テストで球の状態（カップに入った等）を直接置けるようにする（ゲーム側からは GolfHoleWorld だけが書き換える）
[assembly: InternalsVisibleTo("PenmonScramble.Tests")]
