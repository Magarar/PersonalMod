---
name: sts2-epoch-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 时间线/纪元 (Epoch/Story) 系统提供全面的参考与自动检查。
  涵盖 Story 定义 (ModStoryTemplate)、Epoch 类型 (CharacterUnlockEpochTemplate / PackDeclaredCardUnlockEpochTemplate / PackDeclaredRelicUnlockEpochTemplate / PotionUnlockEpochTemplate)、
  注册属性 ([RegisterStory] / [RegisterEpoch] / [RegisterStoryEpoch] / [AutoTimelineSlot])、
  解锁属性 ([RequireEpoch] / [UnlockEpochAfterRunAs] / [UnlockEpochAfterWinAs] / [UnlockEpochAfterEliteVictories] / [UnlockEpochAfterBossVictories] / [UnlockEpochAfterAscensionOneWin])、
  内容注册 ([RegisterEpochCards] / [RegisterEpochRelicsFromPool] / [RequireAllCardsInPool])、
  EpochEra 枚举速查、EpochAssetProfile 资源路径配置、本地化文本 (epochs.json)、
  以及完整的代码模板与审查清单。
  当用户要求为自定角色添加解锁进度条/时间线/纪元/剧情、或排查 Epoch 相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 时间线/纪元 (Epoch/Story) 编写 Skill (RitsuLib)

## 1. 概述

时间线 (Timeline) 是杀戮尖塔2 用来兼顾**解锁人物内容**和**讲述故事**的系统。每个角色都有对应的"故事"(Story)，故事中按顺序排列多个"时期"(Epoch)，玩家达成特定条件后解锁后续 Epoch，逐步解锁更多内容（卡牌、遗物、药水等）。

在 RitsuLib 框架中编写 STS2 Mod 时间线，核心步骤：
1. 创建 Story 类，继承 `ModStoryTemplate`
2. 为每个时期创建 Epoch 类：
   - **角色解锁** → `CharacterUnlockEpochTemplate<TCharacter>`
   - **卡牌解锁** → `PackDeclaredCardUnlockEpochTemplate`（自动声明式）
   - **遗物解锁** → `PackDeclaredRelicUnlockEpochTemplate`
   - **药水解锁** → `PotionUnlockEpochTemplate`
3. 用 `[RegisterStory]` / `[RegisterEpoch]` / `[RegisterStoryEpoch]` 注册
4. 用 `[AutoTimelineSlot]` 指定时间线位置
5. 在人物类上用 `[RequireEpoch]` / `[UnlockEpochAfter*]` 绑定解锁条件
6. 编写本地化 JSON（epochs.json）

**当前项目 ModId**: `PersonalMod`

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-09-add-timeline/

---

## 2. Model ID 规则

### 2.1 Story ID

Story 的 `StoryKey` 是自定义字符串标识，本地化键为 `STORY_<UPPER_CASE_KEY>`：

```json
{
  "STORY_TEST": "戈多"
}
```

### 2.2 Epoch ID

Epoch 的 `Id` 属性是自定义字符串标识，用于本地化键和注册检索：

```json
{
  "TEST_CHARACTER_EPOCH.title": "等待者",
  "TEST_CHARACTER_EPOCH.description": "描述文本..."
}
```

本地化表为 `epochs`，键格式为 `{Epoch.Id}.{field}`。

### 2.3 通过特定幕的 Epoch 键

通过某一幕解锁的 Epoch 需要按固定 ID 格式检索，RitsuLib 提供辅助方法生成：

```csharp
internal static string ActEpochKey(int actNum) =>
    ModContentRegistry.GetFixedPublicEntry(Entry.ModId, typeof(TestCharacter)) + $"_{actNum + 1}_EPOCH";
```

---

## 3. 基类: ModStoryTemplate

继承链: `ModStoryTemplate` → `StoryModel`

命名空间: `STS2RitsuLib.Timeline.Scaffolding`

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `StoryKey` | `protected abstract string` | 故事唯一标识符（需确保不与其他 Mod 冲突） |

### 3.2 StoryModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 故事 ID（从 `StoryKey` 自动生成 slug） |
| `Epochs` | `EpochModel[]` | 故事中所有 Epoch 数组 |
| `StoryTitle` | `string?` | 故事标题（本地化键 `STORY_{UPPER_ID}`） |

---

## 4. Epoch 模板类型

所有 Epoch 类继承自 `EpochModel`，RitsuLib 提供以下模板：

### 4.1 CharacterUnlockEpochTemplate<TCharacter>

**用途**: 解锁指定角色成为可玩角色。

```csharp
[RegisterEpoch]
[RegisterStoryEpoch(typeof(TestStory), Order = 0)]
[AutoTimelineSlotBeforeColumn(EpochEra.Seeds0)]
[RequireAllCardsInPool(typeof(TestCardPool))]
public class TestEpoch : CharacterUnlockEpochTemplate<TestCharacter>
{
    public override string Id => "TEST_CHARACTER_EPOCH";
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://icon.svg",
        BigPortraitPath: "res://icon.svg"
    );
    protected override IEnumerable<Type> ExpansionEpochTypes => [typeof(TestCardEpoch), /*...*/];
}
```

### 4.2 PackDeclaredCardUnlockEpochTemplate

**用途**: 通过 `[RegisterEpochCards]` 声明解锁哪些卡牌。

| 成员 | 类型 | 说明 |
|------|------|------|
| `Id` | `abstract string` | 时期唯一标识符 |
| `AssetProfile` | `abstract EpochAssetProfile` | 时期图片配置 |

```csharp
[RegisterEpoch]
[RegisterStoryEpoch(typeof(TestStory), Order = 1)]
[AutoTimelineSlot(EpochEra.Seeds0)]
[RegisterEpochCards(typeof(TestCard), typeof(TestCard2), typeof(TestCard3))]
public class TestCardEpoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => "TEST_CARD_EPOCH";
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://icon.svg",
        BigPortraitPath: "res://icon.svg"
    );
}
```

### 4.3 PackDeclaredRelicUnlockEpochTemplate

**用途**: 通过 `[RegisterEpochRelicsFromPool]` 声明解锁某遗物池中的所有遗物。

```csharp
[RegisterEpoch]
[RegisterStoryEpoch(typeof(TestStory), Order = 3)]
[AutoTimelineSlot(EpochEra.Peace0)]
[RegisterEpochRelicsFromPool(typeof(TestRelicPool))]
public sealed class TestAct2Epoch : PackDeclaredRelicUnlockEpochTemplate
{
    public override string Id => TestStory.ActEpochKey(2);
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://icon.svg",
        BigPortraitPath: "res://icon.svg"
    );
}
```

### 4.4 PotionUnlockEpochTemplate

**用途**: 解锁药水。

---

## 5. EpochModel 完整属性

继承自原版 `EpochModel` 的属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `abstract string` | 时期唯一标识符 |
| `Era` | `abstract EpochEra` | 所属纪元时代 |
| `EraPosition` | `abstract int` | 纪元内位置序号 |
| `StoryId` | `string?` | 所属 Story ID（默认 null） |
| `Title` | `LocString` | 标题（`epochs/{Id}.title`） |
| `Description` | `LocString` | 描述（`epochs/{Id}.description`） |
| `UnlockInfo` | `LocString` | 解锁提示（`epochs/{Id}.unlockInfo`） |
| `UnlockText` | `LocString` | 解锁文本（`epochs/{Id}.unlockText`） |
| `Year` | `string` | 纪元年份（从 `eras` 本地化表获取） |
| `EraName` | `string` | 纪元名称 |
| `Portrait` | `Texture2D` | 小肖像（图集路径） |
| `BigPortrait` | `Texture2D` | 大肖像 |
| `StoryTitle` | `string?` | 故事标题 |
| `IsArtPlaceholder` | `bool` | 是否为占位图 |

### 5.1 推荐重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `Id` | `abstract string` | 时期 ID（用于本地化键） |
| `AssetProfile` | `abstract EpochAssetProfile` | 图片路径配置 |
| `ExpansionEpochTypes` | `protected virtual IEnumerable<Type>` | 解锁本时期后解锁的所有后续时期 |

---

## 6. EpochEra 枚举速查

`EpochEra` 定义了时间线中不同时代的位置：

```csharp
EpochEra.Prehistoria0    // = -20000  史前时代
EpochEra.Prehistoria1    // = -19999
EpochEra.Prehistoria2    // = -19998
EpochEra.Seeds0          // = 0       播种时代（起始）
EpochEra.Seeds1          // = 1
EpochEra.Seeds2          // = 2
EpochEra.Seeds3          // = 3
EpochEra.Blight0         // = 1201    枯萎时代
EpochEra.Blight1         // = 1202
EpochEra.Blight2         // = 1203
EpochEra.Flourish0       // = 1800    繁荣时代
EpochEra.Flourish1       // = 1801
EpochEra.Flourish2       // = 1802
EpochEra.Flourish3       // = 1803
EpochEra.Invitation0     // = 2733    邀请时代
EpochEra.Invitation1     // = 2734
EpochEra.Invitation2     // = 2735
EpochEra.Invitation3     // = 2736
EpochEra.Invitation4     // = 2737
EpochEra.Invitation5     // = 2738
EpochEra.Invitation6     // = 2739
EpochEra.Invitation7     // = 2740
EpochEra.Peace0          // = 3000    和平时代
EpochEra.Peace1          // = 3001
EpochEra.FarFuture0      // = 10000   遥远未来
EpochEra.FarFuture1      // = 10001
```

---

## 7. 注册属性

### 7.1 Story 注册

```csharp
[RegisterStory]
public class TestStory : ModStoryTemplate
{
    protected override string StoryKey => "test";
}
```

### 7.2 Epoch 注册

所有 Epoch 类必须同时添加 `[RegisterEpoch]` 和 `[RegisterStoryEpoch]`：

```csharp
[RegisterEpoch]                                          // 注册到全局 Epoch 系统
[RegisterStoryEpoch(typeof(TestStory), Order = 0)]       // 关联到 Story 并指定顺序
public class TestEpoch : CharacterUnlockEpochTemplate<TestCharacter> { ... }
```

`Order` 值决定了 Epoch 在 Story 中的排列顺序。数字越小越靠前。

### 7.3 AutoTimelineSlot — 时间线位置

```csharp
// 精确指定时代和位置
[AutoTimelineSlot(EpochEra.Seeds0)]

// 在指定时代之前自动分配位置
[AutoTimelineSlotBeforeColumn(EpochEra.Seeds0)]
```

### 7.4 内容注册属性

| 属性 | 说明 | 适用 Epoch 类型 |
|------|------|-----------------|
| `[RegisterEpochCards(typeof(Card1), typeof(Card2))]` | 注册该 Epoch 解锁的卡牌 | `PackDeclaredCardUnlockEpochTemplate` |
| `[RegisterEpochRelicsFromPool(typeof(Pool))]` | 注册该 Epoch 解锁的遗物池 | `PackDeclaredRelicUnlockEpochTemplate` |
| `[RequireAllCardsInPool(typeof(TestCardPool))]` | 要求卡池中所有卡牌依赖此 Epoch | `CharacterUnlockEpochTemplate` |

---

## 8. 解锁条件属性（在人物类上使用）

在角色类上用以下属性绑定 Epoch 的解锁条件：

| 属性 | 解锁条件 | 说明 |
|------|---------|------|
| `[RequireEpoch(typeof(TestEpoch))]` | 无 | 角色依赖此 Epoch（必须先解锁此 Epoch 才能选角色） |
| `[UnlockEpochAfterRunAs(typeof(TestCardEpoch))]` | 打一局 | 用该角色打一局游戏后解锁 |
| `[UnlockEpochAfterWinAs(typeof(TestVictoryEpoch))]` | 赢一局 | 用该角色赢得一局后解锁 |
| `[UnlockEpochAfterAscensionOneWin(typeof(TestAscensionOneEpoch))]` | 进阶1胜利 | 用该角色在进阶1胜利 |
| `[UnlockEpochAfterEliteVictories(typeof(TestEliteEpoch))]` | 累计杀精英 | 累计击败 15 个精英 |
| `[UnlockEpochAfterBossVictories(typeof(TestBossEpoch))]` | 累计杀Boss | 累计击败 15 个 Boss |
| `[RevealAscensionAfterEpoch(typeof(TestVictoryEpoch))]` | — | 此 Epoch 揭示后解锁进阶难度 |

### 8.1 在角色类上使用的完整示例

```csharp
[RegisterCharacter]
[RequireEpoch(typeof(TestEpoch))]
[UnlockEpochAfterRunAs(typeof(TestCardEpoch))]
[UnlockEpochAfterWinAs(typeof(TestVictoryEpoch))]
[UnlockEpochAfterEliteVictories(typeof(TestEliteEpoch))]
[UnlockEpochAfterBossVictories(typeof(TestBossEpoch))]
[UnlockEpochAfterAscensionOneWin(typeof(TestAscensionOneEpoch))]
[RevealAscensionAfterEpoch(typeof(TestVictoryEpoch))]
public class TestCharacter : ModCharacterTemplate<TestCardPool, TestRelicPool, TestPotionPool>
{
    // ...
}
```

### 8.2 解锁条件速查表

| 类型 | 解锁条件 | 解锁内容 | 需要代码 |
|------|---------|---------|---------|
| 解锁角色 | 指定角色打一把后解锁 | 角色本身 | `[RegisterEpoch]` + `[RegisterStoryEpoch]` + `CharacterUnlockEpochTemplate` |
| 打一局 | 用角色完成一局 | 卡牌/遗物/药水 | 角色上 `[UnlockEpochAfterRunAs]` |
| 赢一局 | 用角色赢一局 | 卡牌/遗物/药水 | 角色上 `[UnlockEpochAfterWinAs]` |
| 通过第一幕 | 用角色打败第一幕 Boss | 卡牌 | 通过 ActEpochKey 指定 |
| 通过第二幕 | 用角色打败第二幕 Boss | 遗物 | 通过 ActEpochKey 指定 |
| 通过第三幕 | 用角色打败第三幕 Boss | 药水/卡牌 | 通过 ActEpochKey 指定 |
| 累计击杀精英 | 累计击败 15 个精英 | 卡牌/遗物/药水 | 角色上 `[UnlockEpochAfterEliteVictories]` |
| 累计击杀 Boss | 累计击败 15 个 Boss | 卡牌/遗物/药水 | 角色上 `[UnlockEpochAfterBossVictories]` |
| 进阶1 | 进阶1 胜利 | 卡牌/遗物/药水 | 角色上 `[UnlockEpochAfterAscensionOneWin]` |

---

## 9. 资源配置 (EpochAssetProfile)

### 9.1 基本配置

```csharp
public override EpochAssetProfile AssetProfile => new(
    PackedPortraitPath: "res://PersonalMod/images/epochs/test_epoch.png",   // 小肖像
    BigPortraitPath: "res://PersonalMod/images/epochs/test_epoch_big.png"   // 大肖像
);
```

| 参数 | 说明 |
|------|------|
| `PackedPortraitPath` | 小肖像路径（时间线节点显示） |
| `BigPortraitPath` | 大肖像路径（详情界面显示） |

### 9.2 原版资源路径约定

| 资源 | 路径 |
|------|------|
| 图集埋入 | `atlases/epoch_atlas.sprites/{id}.tres` |
| 大肖像 | `timeline/epoch_portraits/{id}.png` |

---

## 10. ExpansionEpochTypes — 链式解锁

每个 Epoch 通过 `ExpansionEpochTypes` 声明其解锁后应该揭示哪些后续 Epoch：

```csharp
protected override IEnumerable<Type> ExpansionEpochTypes =>
[
    typeof(TestCardEpoch),        // 第一个后续时期
    typeof(TestAct1Epoch),        // 通过第一幕
    typeof(TestAct2Epoch),        // 通过第二幕
    typeof(TestAct3Epoch),        // 通过第三幕
    typeof(TestVictoryEpoch),      // 胜利
    typeof(TestEliteEpoch),        // 精英成就
    typeof(TestBossEpoch),         // Boss成就
    typeof(TestAscensionOneEpoch), // 进阶1
];
```

这些 Epoch 会显示在时间线上作为"待解锁"状态，玩家达成条件后逐一揭示。

---

## 11. 本地化

### 11.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/epochs.json
PersonalMod/PersonalMod/localization/zhs/epochs.json
```

### 11.2 格式

```json
{
  "STORY_TEST": "戈多",

  "TEST_CHARACTER_EPOCH.title": "等待者",
  "TEST_CHARACTER_EPOCH.description": "路旁只有一棵[green]树[/green]、一块石头，以及一只被反复擦亮的[gold]怀表[/gold]。",
  "TEST_CHARACTER_EPOCH.unlock": "[blue]戈多[/blue]终于出现在路尽头。",
  "TEST_CHARACTER_EPOCH.unlockInfo": "{IsRevealed:已经用|用}[pink]{Prerequisite}[/pink]进行一局游戏{IsRevealed:|来揭示这个历史节点}。",
  "TEST_CHARACTER_EPOCH.unlockText": "解锁[blue]戈多[/blue]成为一名可玩角色。",

  "TEST_CARD_EPOCH.title": "第一副牌",
  "TEST_CARD_EPOCH.description": "...",
  "TEST_CARD_EPOCH.unlockInfo": "{IsRevealed:已经以|以}[blue]戈多[/blue]完成一局游戏{IsRevealed:|来揭示这个历史节点}。",
  "TEST_CARD_EPOCH.unlockText": "解锁[blue]戈多[/blue]的更多卡牌。"
}
```

### 11.3 字段说明

| 字段 | 说明 | 必需 |
|------|------|------|
| `STORY_<KEY>` | Story 名称（`STORY_` 前缀 + StoryKey 大写） | 是 |
| `<Id>.title` | Epoch 标题 | 是 |
| `<Id>.description` | Epoch 描述（叙事文本，支持 BBCode） | 推荐 |
| `<Id>.unlock` | 解锁时显示的提示文本 | 可选 |
| `<Id>.unlockInfo` | 解锁条件说明（支持 `{IsRevealed}` 和 `{Prerequisite}` 占位符） | 推荐 |
| `<Id>.unlockText` | 解锁内容文本 | 推荐 |

### 11.4 unlockInfo 特殊占位符

| 占位符 | 说明 |
|--------|------|
| `{IsRevealed:已揭示|未揭示}` | 条件已达成/未达成的双态文本 |
| `{Prerequisite}` | 前置条件的名称 |

### 11.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮 |
| `[blue]文字[/blue]` | 蓝色 |
| `[green]文字[/green]` | 绿色 |
| `[red]文字[/red]` | 红色 |
| `[purple]文字[/purple]` | 紫色 |
| `[pink]文字[/pink]` | 粉色 |
| `[sine]文字[/sine]` | 正弦波动 |
| `[b]文字[/b]` | 加粗 |

---

## 12. 完整代码模板

### 12.1 角色完整时间线

```csharp
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Timeline.Scaffolding;

namespace PersonalMod.PersonalModCode.Epochs;

// ========== Story ==========
[RegisterStory]
public class PersonalModStory : ModStoryTemplate
{
    protected override string StoryKey => "personal_mod";

    // 辅助方法：生成通过第 N 幕的 Epoch ID
    internal static string ActEpochKey(int actNum) =>
        ModContentRegistry.GetFixedPublicEntry(Entry.ModId, typeof(PersonalModCharacter))
        + $"_{actNum + 1}_EPOCH";
}

// ========== Epoch 1: 解锁角色 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 0)]
[AutoTimelineSlotBeforeColumn(EpochEra.Seeds0)]
[RequireAllCardsInPool(typeof(PersonalModCardPool))]
public class PersonalModCharacterEpoch : CharacterUnlockEpochTemplate<PersonalModCharacter>
{
    public override string Id => "PERSONALMOD_CHARACTER_EPOCH";

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/character_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/character_epoch_big.png"
    );

    protected override IEnumerable<Type> ExpansionEpochTypes =>
    [
        typeof(PersonalModCardEpoch),
        typeof(PersonalModAct1Epoch),
        typeof(PersonalModAct2Epoch),
        typeof(PersonalModAct3Epoch),
        typeof(PersonalModVictoryEpoch),
    ];
}

// ========== Epoch 2: 解锁基础卡牌 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 1)]
[AutoTimelineSlot(EpochEra.Seeds0)]
[RegisterEpochCards(typeof(TestCard1), typeof(TestCard2), typeof(TestCard3))]
public class PersonalModCardEpoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => "PERSONALMOD_CARD_EPOCH";

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/card_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/card_epoch_big.png"
    );
}

// ========== Epoch 3: 通过第一幕解锁更多卡牌 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 2)]
[AutoTimelineSlot(EpochEra.Blight1)]
[RegisterEpochCards(typeof(TestCard4), typeof(TestCard5))]
public sealed class PersonalModAct1Epoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => PersonalModStory.ActEpochKey(1);

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/act1_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/act1_epoch_big.png"
    );
}

// ========== Epoch 4: 通过第二幕解锁遗物池 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 3)]
[AutoTimelineSlot(EpochEra.Peace0)]
[RegisterEpochRelicsFromPool(typeof(PersonalModRelicPool))]
public sealed class PersonalModAct2Epoch : PackDeclaredRelicUnlockEpochTemplate
{
    public override string Id => PersonalModStory.ActEpochKey(2);

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/act2_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/act2_epoch_big.png"
    );
}

// ========== Epoch 5: 通过第三幕解锁更多卡牌 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 4)]
[AutoTimelineSlot(EpochEra.Seeds2)]
[RegisterEpochCards(typeof(TestCard6), typeof(TestCard7))]
public sealed class PersonalModAct3Epoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => PersonalModStory.ActEpochKey(3);

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/act3_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/act3_epoch_big.png"
    );
}

// ========== Epoch 6: 胜利解锁更多卡牌 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 5)]
[AutoTimelineSlot(EpochEra.Blight2)]
[RegisterEpochCards(typeof(TestCard8), typeof(TestCard9))]
public sealed class PersonalModVictoryEpoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => "PERSONALMOD_VICTORY_EPOCH";

    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/victory_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/victory_epoch_big.png"
    );
}

// ========== Epoch 7: 精英成就 ==========
[RegisterEpoch]
[RegisterStoryEpoch(typeof(PersonalModStory), Order = 6)]
[AutoTimelineSlot(EpochEra.Invitation1)]
[RegisterEpochCards(typeof(TestCard10))]
public sealed class PersonalModEliteEpoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => "PERSONALMOD_ELITE_EPOCH";
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/elite_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/elite_epoch_big.png"
    );
}
```

### 12.2 角色类上的解锁绑定

在 `PersonalModCharacter` 类上绑定所有解锁条件：

```csharp
[RegisterCharacter]
[RequireEpoch(typeof(PersonalModCharacterEpoch))]          // 角色依赖此 Epoch
[UnlockEpochAfterRunAs(typeof(PersonalModCardEpoch))]       // 打一局解锁卡牌 Epoch
[UnlockEpochAfterWinAs(typeof(PersonalModVictoryEpoch))]    // 赢一局解锁胜利 Epoch
[UnlockEpochAfterEliteVictories(typeof(PersonalModEliteEpoch))]  // 累计精英解锁
[RevealAscensionAfterEpoch(typeof(PersonalModVictoryEpoch))]     // 胜利后解锁进阶
public class PersonalModCharacter : ModCharacterTemplate<PersonalModCardPool, PersonalModRelicPool, PersonalModPotionPool>
{
    // ...
}
```

### 12.3 最简时间线模板

```csharp
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Timeline.Scaffolding;

namespace PersonalMod.PersonalModCode.Epochs;

[RegisterStory]
public class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my_mod";
}

[RegisterEpoch]
[RegisterStoryEpoch(typeof(MyStory), Order = 0)]
[AutoTimelineSlot(EpochEra.Invitation0)]
public class MyEpoch : PackDeclaredCardUnlockEpochTemplate
{
    public override string Id => "MY_FIRST_EPOCH";
    public override EpochAssetProfile AssetProfile => new(
        PackedPortraitPath: "res://PersonalMod/images/epochs/my_epoch.png",
        BigPortraitPath: "res://PersonalMod/images/epochs/my_epoch_big.png"
    );
}
```

---

## 13. 文件组织

```
PersonalMod/PersonalModCode/Epochs/
├── PersonalModStory.cs               # Story + 全部 Epoch 定义（推荐放一起）
└── (或分多个文件管理)

PersonalMod/PersonalMod/
├── images/
│   └── epochs/
│       ├── character_epoch.png       # 小肖像
│       ├── character_epoch_big.png   # 大肖像
│       ├── card_epoch.png
│       ├── card_epoch_big.png
│       └── ... 
└── localization/
    ├── eng/
    │   └── epochs.json               # 英文本地化
    └── zhs/
        └── epochs.json               # 中文本地化
```

---

## 14. 参考已有 Epoch 实现

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 角色解锁 Epoch | `Timeline/Epochs/` | `Ironclad2Epoch`, `Defect1Epoch` (结构参考) |
| 卡牌解锁 Epoch | `Timeline/Epochs/` | `Colorless1Epoch` |
| 遗物解锁 Epoch | `Timeline/Epochs/` | `Relic1Epoch` |
| 药水解锁 Epoch | `Timeline/Epochs/` | `Potion1Epoch` |
| 事件解锁 Epoch | `Timeline/Epochs/` | `Event1Epoch` |
| 角色 Story | `Timeline/Stories/` | (角色 Story 文件) |

源码位置:
- 基类: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Timeline\EpochModel.cs`
- Story: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Timeline\StoryModel.cs`
- 原版 Epoch 实现: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Timeline\Epochs\`

---

## 15. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 时间线上不显示 Mod Epoch | 缺少 `[RegisterStoryEpoch]` 或 `[AutoTimelineSlot]` | 确保同时有 `[RegisterEpoch]` + `[RegisterStoryEpoch]` + `[AutoTimelineSlot]` |
| 角色不出现 | `[RequireEpoch]` 未绑定或 Epoch 未解锁 | 检查角色类上是否有 `[RequireEpoch(typeof(XxxEpoch))]` |
| 卡牌不出现 | 未注册到 Epoch 或卡池未关联 | 检查 `[RegisterEpochCards]` 或 `[RequireAllCardsInPool]` |
| 遗物不出现 | 未注册到 Epoch | 检查 `[RegisterEpochRelicsFromPool(typeof(Pool))]` |
| 解锁条件不触发 | 角色上缺少对应 `[UnlockEpochAfter*]` 属性 | 检查角色类上的解锁条件属性 |
| Epoch 顺序不对 | `Order` 值设置错误 | 检查 `[RegisterStoryEpoch(..., Order = N)]` 中的 Order |
| 通过某幕不激活 Epoch | ActEpochKey 格式错误 | 确认 Epoch ID 与辅助方法生成的键一致 |
| 本地化文本不显示 | epochs.json 键名不匹配 | 检查 Epoch.Id 与 JSON 键名是否一致 |
| Story 名称不显示 | 缺少 `STORY_<KEY>` 键 | 检查 epochs.json 中是否有 `STORY_<UPPER_STORY_KEY>` |
| Epoch 肖像显示空白 | 资源路径错误 | 检查 `AssetProfile` 中的图片路径 |

---

## 16. 编写审查清单

### 16.1 Story 检查

- [ ] `ModStoryTemplate` 是否已继承？
- [ ] 是否添加了 `[RegisterStory]` 属性？
- [ ] `StoryKey` 是否唯一且不与其他 Mod 冲突？

### 16.2 Epoch 检查

- [ ] 每个 Epoch 是否同时有 `[RegisterEpoch]` 和 `[RegisterStoryEpoch]`？
- [ ] `Order` 值是否按正确顺序排列？
- [ ] `Id` 是否唯一且与本地化键匹配？
- [ ] `AssetProfile` 是否配置了正确的图片路径？
- [ ] 第一个 Epoch 是否有 `ExpansionEpochTypes` 列出所有后续 Epoch？

### 16.3 内容注册检查

- [ ] 卡牌解锁 Epoch 是否使用了 `[RegisterEpochCards]`？
- [ ] 遗物解锁 Epoch 是否使用了 `[RegisterEpochRelicsFromPool]`？
- [ ] 角色解锁 Epoch 是否使用了 `[RequireAllCardsInPool]`？
- [ ] 通过幕的 Epoch 是否使用了 ActEpochKey 生成 ID？

### 16.4 角色解锁检查

- [ ] 角色类上是否有 `[RequireEpoch]`？
- [ ] 每个 `[UnlockEpochAfter*]` 是否对应正确的 Epoch 类型？
- [ ] 是否需要 `[RevealAscensionAfterEpoch]`？

### 16.5 本地化检查

- [ ] `epochs.json` 中是否有 `STORY_<KEY>` 键？
- [ ] 每个 Epoch 是否有 `title`、`description`、`unlockInfo`、`unlockText`？
- [ ] `unlockInfo` 中的 `{IsRevealed}` 格式是否正确？
- [ ] BBCode 标签是否正确闭合？

### 16.6 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 是否在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
