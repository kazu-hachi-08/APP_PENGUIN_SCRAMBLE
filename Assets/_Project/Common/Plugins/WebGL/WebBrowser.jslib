// ブラウザ版だけで使うコピー。C# 側は Common/Scripts/Online/WebBrowser.cs。
mergeInto(LibraryManager.library, {
  // GUIUtility.systemCopyBuffer はブラウザ版だとPCのクリップボードに入らない
  PenmonScramble_CopyText: function (textPtr) {
    var text = UTF8ToString(textPtr);
    var manual = function () { window.prompt('このコードをコピーしてください', text); };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(manual);
    } else {
      manual();
    }
  }
});
