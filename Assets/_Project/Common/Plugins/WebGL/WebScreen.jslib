// ブラウザ版の表示枠の縦横比を変える。実体は WebGLTemplates/PenguinScramble/index.html の miniGameSetAspect。
// 別のテンプレートでビルドしたときに止まらないよう、関数が無ければ何もしない。
mergeInto(LibraryManager.library, {
  PenguinScramble_SetAspect: function (width, height) {
    if (typeof window.miniGameSetAspect === 'function') window.miniGameSetAspect(width, height);
  }
});
