# コンテンツデータ設計

## 目的

カード、敵、遺物、イベント、報酬をコードから分離する。

初心者開発では、最初はコードに直書きしたくなる。しかしカードを増やすたびにコードを触る設計にすると、バグが増え、バランス調整も遅くなる。ゲームルールの処理はコード、コンテンツの数値と説明はデータで持つ。

## Source of Truth

| 情報 | 管理場所 | 理由 |
| --- | --- | --- |
| カードID、コスト、効果 | `data/cards/*.json` | 実装と調整で共有する |
| 遺物ID、発動条件、効果 | `data/relics/*.json` | 条件と効果を機械的に検証する |
| 敵ステータス、行動表 | 将来 `data/enemies/*.json` | 行動パターンを調整しやすくする |
| イベント選択肢 | 将来 `data/events/*.json` | テキストと結果を分離する |
| 大きな仕様判断 | `docs/*.md` | なぜそうしたかを残す |

## 命名規則

IDは英小文字、数字、アンダースコアのみ。

例:

- `warden_strike_gear`
- `relic_spring_loaded_core`
- `enemy_rust_rat`

表示名は後からローカライズできるように、IDとは分ける。

## カードデータ

カードは次の情報を持つ。

| フィールド | 型 | 説明 |
| --- | --- | --- |
| id | string | 一意ID |
| displayName | string | 表示名 |
| character | string | 使用キャラ、または `neutral` |
| rarity | string | `starter`, `common`, `uncommon`, `rare`, `special` |
| type | string | `attack`, `skill`, `gear`, `status`, `curse` |
| cost | number または string | 通常は数値。Xコストは `X` |
| target | string | `enemy`, `all_enemies`, `self`, `none` |
| tags | string[] | 検索・シナジー用 |
| rulesText | string | プレイヤー向け説明 |
| effects | object[] | 実処理用の効果列 |
| upgrade | object | 強化差分 |
| balance | object | 調整メモ |

## 効果データの考え方

効果は「小さな命令の配列」として持つ。

例:

```json
[
  { "op": "deal_damage", "amount": 6 },
  { "op": "gain_block", "amount": 3 }
]
```

複雑なカードも、可能な限り小さな命令を組み合わせる。どうしても特殊な挙動が必要な場合だけ、コード側に専用ハンドラを追加する。

## 実装側の責務

データ:

- 何というカードか
- コストはいくつか
- どんな効果を何回行うか
- 強化後に何が変わるか

コード:

- ダメージ計算
- ブロック計算
- 状態異常の処理
- ターン進行
- 対象選択
- ランダム処理
- セーブとロード

## バリデーション

データ追加時に最低限チェックする。

- IDが重複していない
- 必須フィールドがある
- コストが範囲内
- 説明文と効果が大きく矛盾していない
- upgradeで存在しないフィールドを変更していない
- 未実装の `op` を使っていない

## データ変更の記録

バランス調整は理由が重要。

悪い例:

```text
damage 8 -> 7
```

良い例:

```text
damage 8 -> 7
Act 1通常敵の平均撃破ターンが2.1で短すぎたため。初期デッキで強すぎる。
```

## 将来の拡張

Act 1縦切り版が安定したら、次を追加する。

- `data/enemies/`
- `data/events/`
- `data/rewards/`
- `data/localization/ja.json`
- データ検証スクリプト
- プレイログ集計スクリプト
