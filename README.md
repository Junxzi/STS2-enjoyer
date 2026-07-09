# ローグライク・ターン制ゲーム制作モック資料

これは、デッキ構築型ローグライクを自作するための「開発資料のひな形」です。

## Party Race mod download

Slay the Spire 2 の同一シード・チームレース検証modは、GitHub Releasesからダウンロードできます。

- [party_race_windows_mod.zip](https://github.com/junxzi/STS2-enjoyer/releases/latest/download/party_race_windows_mod.zip)
- 実装と導入手順: [PartyRace/README.md](PartyRace/README.md)

Slay the Spire 2 のような商用作品を参考にする場合でも、ここでは実データ・固有名詞・カード内容・画像・音声・内部実装をコピーせず、次の観点だけを抽象化して使います。

- ランごとに構造が変わるマップ
- ターン制バトル
- カード、遺物、敵、イベントをデータ駆動で管理する作り
- プレイテストとバランス調整を前提にした制作フロー

このリポジトリ内の資料は、架空ゲーム「Clockwork Descent」を題材にしたオリジナルのモックです。ゲーム本体は含みません。

重要: 学習のために既存作品を横に置いて観察するのは有効ですが、カード名、効果文、敵名、イベント本文、数値テーブル、画像、音声、内部データをそのまま仕様書やJSONに写すことは避けます。この資料は「同じもの」ではなく、「同じような完成度で設計を学べる置き換え教材」です。

## まず読む順番

1. [既存作品を参考にする範囲](docs/00_reference_policy.md)
2. [開発フロー](docs/01_development_flow.md)
3. [ゲーム仕様書](docs/02_game_design_spec.md)
4. [データ設計](docs/03_content_data_model.md)
5. [バランス設計](docs/04_balance_design.md)
6. [技術設計](docs/05_technical_architecture.md)
7. [QA・プレイテスト計画](docs/06_qa_playtest_plan.md)
8. [STS2型ゲームの観察マップ](docs/07_sts2_style_learning_map.md)

## フォルダ構成

```text
docs/
  00_reference_policy.md       既存作品を参考にする際の境界線
  01_development_flow.md       開発手順、マイルストーン、制作サイクル
  02_game_design_spec.md       ゲームの仕様書本体
  03_content_data_model.md     カード・敵・遺物などの情報の持ち方
  04_balance_design.md         数値設計と調整方針
  05_technical_architecture.md 技術構成と実装境界
  06_qa_playtest_plan.md       テスト観点、ログ、バグ票
  07_sts2_style_learning_map.md STS2型の設計を観察するための対応表
data/
  schemas/                     JSON Schemaの例
  cards/                       カード定義のサンプル
  relics/                      遺物定義のサンプル
templates/                     実制作で使う記入テンプレート
```

## 初心者向けの進め方

最初から巨大なゲームを作ろうとせず、次の順で小さく完成させます。

1. 1キャラ、20カード、5敵、3遺物だけでバトルが回る状態を作る
2. 報酬選択、休憩、ショップ、マップ移動を追加する
3. 1ステージだけ通しで遊べる縦切り版を作る
4. ログを見ながら勝率、カード採用率、被ダメージ量を調整する
5. コンテンツを増やす

「面白さ」は仕様書だけでは確定しません。仕様を小さく作り、遊び、ログを取り、直すための資料セットとして使ってください。
