# 全観点共通のルール

## haiku に必ず渡すルール

解析・修正どちらのプロンプトにも、次のブロックをそのまま貼る。

```
## このプロジェクトで守ること
- 作業ディレクトリは C:\MyApp\APP_PENGUIN_SCRAMBLE。パスはここからの相対で書く。
- セーブデータを壊さない。次に関わる変更は提案も修正もしない（見つけたら「対象外」と書くだけ）:
  - 保存用のデータクラス（JSON にして PlayerPrefs に置くもの）と、その中のクラスの項目名・型・クラス名
  - PlayerPrefs のキー文字列（"PenguinScramble." で始まるもの）
  - 記録に名前（文字列 ID）で入るものの名前（未使用に見えても消さない・名前を変えない）
  - セーブデータのバージョン番号
- 生成されたアセット（Assets/_Project/Games/100_PenguinScramble/Data 以下の .asset、Scenes 以下の .unity）は編集しない。数値やシーンはコード（定義表・SceneBuilder）から作られている。
- フォルダ名 Games/100_PenguinScramble と名前空間 PenguinScramble.* は変えない。
- .cs ファイルを消すときは、同じ名前の .meta も一緒に消す。
- Tests/ 以下は編集しない（読むのは自由）。
- public な型・メソッドの名前は変えない（SceneBuilder やテストが文字列や参照で使っている可能性がある）。
- コメントは「何をしているか」より「なぜそうしているか」を残す方針。
```

## メインが承認前に「対象外」に回すもの

- 上のセーブデータ関連に触れる指摘すべて
- `[SerializeField]` のフィールドを「未使用」とする指摘で、SceneBuilder（`Editor/`）から `SerializedObject` や文字列で設定されているもの。コンパイラの CS0649 警告（「割り当てられません」）は SceneBuilder が値を入れているので誤検出。
- 仕様書を変えるのではなく **コードの挙動を変える** 必要がある指摘（リファクタリングの範囲外）。報告に「挙動の不一致」として残し、直すかは別途ユーザーに聞く。

## Unity 特有の「使っていないように見えて使われている」もの

haiku の指摘をメインが確認するときの目安。

- `Awake` `Start` `Update` `OnEnable` `OnDisable` `OnDestroy` `OnValidate` `OnTriggerEnter` などの Unity メッセージ
- `[MenuItem]` `[InitializeOnLoad]` `[RuntimeInitializeOnLoadMethod]` `[ContextMenu]` が付いたメソッド
- Netcode の `[ServerRpc]` `[ClientRpc]` `[Rpc]`、`NetworkVariable`、`OnNetworkSpawn`
- `UnityEvent` / `Button.onClick` に SceneBuilder から `AddListener` や `UnityEventTools.AddPersistentListener` で登録されているメソッド
- `nameof(...)` や文字列でメソッド名・フィールド名を参照している箇所（`SerializedObject.FindProperty("_xxx")` など）
- `partial class` の別ファイルから呼ばれているもの
