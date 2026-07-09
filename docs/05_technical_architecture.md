# 技術設計

## 目的

ゲーム本体を作る前に、どこをコードにし、どこをデータにするかを決める。

この資料はUnity、Godot、Unreal、自作エンジンのどれでも使えるように、エンジン非依存で書く。

## 推奨アーキテクチャ

```text
Presentation Layer
  画面表示、アニメーション、入力

Game Application Layer
  戦闘開始、カード使用、報酬選択、マップ遷移

Domain Layer
  ターン進行、ダメージ計算、カード効果、敵AI

Data Layer
  JSONロード、セーブ、ログ、ローカライズ
```

## 中心となる状態

BattleState:

- player
- enemies
- drawPile
- hand
- discardPile
- exhaustPile
- turnNumber
- energy
- pendingActions
- randomSeed
- combatLog

RunState:

- characterId
- deck
- relics
- gold
- currentHp
- maxHp
- map
- currentNode
- actNumber
- randomSeed
- runLog

## コマンドとして処理する操作

プレイヤー入力は直接状態を書き換えず、コマンドにする。

| コマンド | 内容 |
| --- | --- |
| StartBattle | 戦闘を開始する |
| PlayCard | カードを使う |
| EndTurn | ターンを終える |
| ChooseReward | 報酬を選ぶ |
| SkipReward | 報酬をスキップする |
| UpgradeCard | カードを強化する |
| RemoveCard | カードを削除する |

利点:

- ログを残しやすい
- バグ再現がしやすい
- 将来リプレイ機能に発展できる
- UIとゲームルールを分離できる

## ランダムの扱い

ラン開始時に `runSeed` を決める。

用途ごとに乱数系列を分ける。

- mapRng
- combatRng
- rewardRng
- eventRng

同じseedなら同じ結果になるようにする。これにより、バグ報告の再現性が上がる。

## カード効果処理

カードは `effects` の配列を上から順に解決する。

例:

```json
{
  "effects": [
    { "op": "deal_damage", "amount": 8 },
    { "op": "gain_time", "amount": 1 }
  ]
}
```

実装側では `op` ごとにハンドラを用意する。

```text
deal_damage -> DamageSystem
gain_block  -> BlockSystem
draw_cards  -> DeckSystem
apply_status -> StatusSystem
```

## ログ設計

最低限のログ:

```json
{
  "runId": "20260706-001",
  "seed": "clock-1234",
  "characterId": "warden",
  "result": "defeat",
  "floorReached": 9,
  "battles": [
    {
      "floor": 1,
      "enemyGroup": "act1_rust_pair",
      "turns": 4,
      "damageTaken": 6,
      "cardsPlayed": ["warden_strike_gear", "warden_guard_plate"]
    }
  ]
}
```

## セーブデータ

保存対象:

- RunState
- 現在の画面種別
- 現在の選択待ち情報
- seed

保存しないもの:

- アニメーション途中状態
- 一時的なUI hover
- 表示用に再計算できる値

## 実装順

1. データロード
2. BattleState
3. カード領域操作
4. ダメージとブロック
5. カード使用
6. 敵行動
7. 戦闘終了
8. 報酬選択
9. マップ遷移
10. ログ出力

## 危険な設計

避けたいもの:

- UIボタンの中にダメージ計算を書く
- カード効果を巨大なif文だけで処理する
- カード名を条件分岐に使う
- ランダムseedを残さない
- 仕様書とデータのどちらが正しいか分からない

## 初心者向けの判断基準

最初から完璧な拡張性を狙わない。

ただし、次の3つだけは最初に守る。

1. カード定義をデータで持つ
2. ゲーム状態とUI表示を分ける
3. ランダムseedとログを残す
