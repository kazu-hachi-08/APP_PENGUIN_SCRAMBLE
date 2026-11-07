using System.Runtime.CompilerServices;

// テストで打球や投げた球の状態（位置・速さ）を直接置けるようにする（ゲーム側からは BaseballBoard だけが書き換える）
[assembly: InternalsVisibleTo("PenmonScramble.Tests")]
