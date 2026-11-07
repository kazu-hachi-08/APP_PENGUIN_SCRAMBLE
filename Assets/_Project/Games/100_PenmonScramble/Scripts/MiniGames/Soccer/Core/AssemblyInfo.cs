using System.Runtime.CompilerServices;

// テストでボールの状態（浮いた高さなど）を直接置けるようにする（ゲーム側からは SoccerWorld だけが書き換える）
[assembly: InternalsVisibleTo("PenmonScramble.Tests")]
