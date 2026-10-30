// ブラウザ版だけで使う名前入力。C# 側は Common/Scripts/Profile/WebNamePrompt.cs。
mergeInto(LibraryManager.library, {
  // Unity の InputField はブラウザ版で日本語変換できないため、ブラウザ標準の入力ダイアログを使う
  PenmonScramble_PromptText: function (messagePtr, defaultPtr) {
    var result = window.prompt(UTF8ToString(messagePtr), UTF8ToString(defaultPtr));
    if (result === null) return 0;
    // C# の string で受け取るため Unity のヒープに確保して返す（解放は C# 側のマーシャラが行う）
    var size = lengthBytesUTF8(result) + 1;
    var buffer = _malloc(size);
    stringToUTF8(result, buffer, size);
    return buffer;
  }
});
