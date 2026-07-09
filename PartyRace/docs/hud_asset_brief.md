# Party Race HUD Asset Brief

この仕様は、Party RaceのHUDをSlay the Spire 2の画面に馴染ませるためのオリジナルUIアセット作成依頼用です。公式ゲーム素材の抽出、模写、ロゴ流用はしないでください。方向性は「暗いファンタジーUI、羊皮紙、金属縁、インク、控えめな魔法光」程度に留め、Party Race独自の見た目として成立させます。

## 目的

Party Raceは、同じCustomロビーから各PCで別々のローカルrunを開始し、チームごとの進捗を比較するmodです。HUDは常時表示される小型パネルと、クリックで開く詳細パネルの2段階にします。

最初に表示したい情報:

- Race状態: Lobby / Armed / Running / Finished
- Seed
- Teamごとの順位、チーム名、Act、Floor、部屋種別、経過時間、結果状態
- 状態警告: disconnected / dnf / disqualified / dead / finished
- Open / Close / Ready / Start Raceなどの操作ボタン

## 現在の実装サイズ

現行のGodot UIはこの座標・サイズで動いています。アセットはこのサイズに合わせてください。

- 画面基準: 1920x1080
- 小型HUD: 左上 `x=32 y=32 w=360 h=92`
- 小型HUD内Openボタン: `w=84 h=36`
- 詳細パネル: `x=64 y=56 w=680 h=500`
- 詳細パネル内ボタン: `w=220 h=36`
- 詳細パネル内テキスト領域: 最大幅およそ620px

将来、1280x720でも破綻しないように、主要装飾は9-sliceで伸縮できる前提にしてください。

## 必須アセット

すべて透明PNGで納品してください。テキストは原則として画像に焼き込まないでください。

1. `hud_compact_frame.png`
   - サイズ: 360x92
   - 9-slice推奨余白: left 18, top 18, right 18, bottom 18
   - 左側にテキスト3行、右側にOpenボタンが載る
   - 背景が明るい/暗い/派手でも読める濃度

2. `hud_detail_frame.png`
   - サイズ: 680x500
   - 9-slice推奨余白: left 28, top 28, right 28, bottom 28
   - 詳細情報とボタンを載せるメインパネル
   - 内側はテキストを邪魔しない低コントラスト

3. `hud_progress_row.png`
   - サイズ: 620x44
   - 9-slice推奨余白: left 16, top 12, right 16, bottom 12
   - チームごとの順位行に使う
   - 1位、通常、警告の3状態を作る場合は下記名にする:
     - `hud_progress_row_normal.png`
     - `hud_progress_row_leader.png`
     - `hud_progress_row_warning.png`

4. Button states
   - `button_primary_normal.png` 220x36
   - `button_primary_hover.png` 220x36
   - `button_primary_pressed.png` 220x36
   - `button_primary_disabled.png` 220x36
   - `button_small_normal.png` 84x36
   - `button_small_hover.png` 84x36
   - `button_small_pressed.png` 84x36
   - `button_small_disabled.png` 84x36
   - テキストは焼き込まない

5. Status badges
   - サイズ: 72x24
   - `badge_lobby.png`
   - `badge_ready.png`
   - `badge_running.png`
   - `badge_finished.png`
   - `badge_warning.png`
   - `badge_dead.png`
   - `badge_disconnected.png`
   - バッジ内テキストは焼き込まない。色と形で状態差がわかること

6. Icons
   - サイズ: 24x24、透明PNG
   - `icon_rank.png`
   - `icon_seed.png`
   - `icon_timer.png`
   - `icon_act.png`
   - `icon_floor.png`
   - `icon_hp.png`
   - `icon_team.png`
   - `icon_host.png`
   - `icon_client.png`
   - `icon_ready.png`
   - `icon_victory.png`
   - `icon_death.png`
   - `icon_disconnect.png`
   - `icon_room_combat.png`
   - `icon_room_elite.png`
   - `icon_room_boss.png`
   - `icon_room_event.png`
   - `icon_room_shop.png`
   - `icon_room_rest.png`
   - `icon_room_treasure.png`
   - `icon_room_unknown.png`

## 任意アセット

- `hud_corner_flourish.png` 64x64
- `divider_horizontal.png` 620x8
- `rank_medal_1.png`, `rank_medal_2.png`, `rank_medal_3.png` 28x28
- `panel_shadow.png` 720x540
- 24x24アイコンをまとめた `hud_icons_atlas.png` と `hud_icons_atlas.json`

## 見た目の方向性

- 暗い画面でも明るい画面でも読める、半透明の濃い土台
- 羊皮紙や金属の縁取りは使ってよいが、画面を占有しすぎない
- 金、赤、青緑、灰の差し色を使い、紫一色や青一色に寄せない
- 重要状態は色だけに依存しない。形、アイコン、縁取りでも区別する
- 小型HUDはゲームプレイの邪魔をしない。装飾は細く、視線誘導は控えめ
- 詳細パネルは情報量が多くても読みやすい。背景テクスチャは低コントラスト

## 禁止事項

- 公式STS2画像、ロゴ、カード枠、アイコン、フォントのコピー
- 画像内に固定テキストを焼き込むこと
- 透明PNGの端に不要な白/黒フリンジが出ること
- HUD全体を強い発光、濃いグラデーション、派手な装飾で覆うこと
- 小型HUDの可読領域を装飾で狭くすること

## 納品形式

納品先想定:

```text
PartyRace/assets/hud/
```

必須ファイル:

- PNG一式
- `asset_manifest.json`
- 元データ: Figma、PSD、SVG、または高解像度PNG
- `preview_1920x1080.png`
- `preview_1280x720.png`

`asset_manifest.json` 例:

```json
{
  "scale": 1,
  "nineSlice": {
    "hud_compact_frame.png": { "left": 18, "top": 18, "right": 18, "bottom": 18 },
    "hud_detail_frame.png": { "left": 28, "top": 28, "right": 28, "bottom": 28 },
    "hud_progress_row.png": { "left": 16, "top": 12, "right": 16, "bottom": 12 }
  },
  "fontRecommendation": "Use game/system UI font; do not bundle an unlicensed font.",
  "notes": "Original assets only. No official STS2 art copied."
}
```

## 受け入れ条件

- 小型HUD 360x92に、3行テキストと84x36ボタンを載せても破綻しない
- 詳細パネル 680x500に、4チーム分の進捗行、状態、イベント数行、操作ボタンが収まる
- 1920x1080と1280x720のプレビューで、背景に対して文字が読める
- 14px相当の文字が、背景テクスチャに埋もれない
- 9-slice指定の中央領域を伸ばしても角や縁が歪まない
- アイコンは24x24でも意味が判別できる
- 色覚差があっても ready/running/warning/finished/dead/disconnected が区別できる
- PNGは透明背景で、不要な余白や縁の汚れがない
- 画像に英語/日本語などの固定テキストが含まれていない
- すべてオリジナルアセットで、第三者素材のライセンス問題がない

## 他AIエージェントへの依頼文

```text
Party RaceというSlay the Spire 2向けmodのHUD用アセットを作ってください。
公式STS2素材はコピーせず、暗いファンタジーUI、羊皮紙、金属縁、インク、控えめな魔法光を方向性にしたオリジナルUIにしてください。

作るものは、透明PNGの小型HUDフレーム 360x92、詳細パネルフレーム 680x500、進捗行 620x44、ボタン状態、状態バッジ、24x24アイコン一式です。
テキストは画像に焼き込まず、Godot側でLabel/Buttonとして載せます。
9-sliceで伸縮できるよう、角と縁が崩れないデザインにしてください。

HUDはゲーム画面左上に常駐し、Seed、Race状態、チーム進捗を表示します。
詳細パネルでは4チーム分の順位、Act、Floor、RoomType、経過時間、結果状態を表示します。
1920x1080と1280x720のプレビューも出してください。

受け入れ条件:
- 14px相当の文字が読める
- 装飾がゲームプレイを邪魔しない
- 24x24アイコンでも意味が判別できる
- ready/running/warning/finished/dead/disconnected が色だけでなく形でも区別できる
- 公式素材、ロゴ、固定テキストを含まない
- asset_manifest.jsonに9-slice余白を記載する
```
