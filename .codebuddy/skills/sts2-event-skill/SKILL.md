---
name: sts2-event-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 事件 (Event) 提供全面的参考与自动检查。
  涵盖事件定义 (ModEventTemplate)、多阶段事件流转 (SetEventState / SetEventFinished)、
  选项系统 (EventOption / InitialOptionKey / ModOptionKey)、
  动态变量 (DynamicVar / DamageVar / GoldVar)、
  出现条件 (IsAllowed)、生命周期回调 (BeforeEventStarted / OnEventFinished)、
  背景图/场景配置 (EventAssetProfile)、奖励命令 (RewardsCmd.OfferCustom)、
  注册方式 ([RegisterActEvent] / [RegisterSharedEvent])、
  本地化文本 (events.json / 多页面键格式)、
  以及完整的代码模板与审查清单。
  当用户要求创建新事件、修改事件逻辑、或排查事件相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 事件编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 事件 (Event)，核心步骤：
1. 创建事件类，继承 `ModEventTemplate`
2. 用 `[RegisterActEvent<TAct>()]` 或 `[RegisterSharedEvent]` 注册
3. 重写 `AssetProfile` 配置背景图
4. 重写 `CanonicalVars` 定义数值变量（可选）
5. 重写 `IsAllowed` 设置出现条件（可选）
6. 重写 `GenerateInitialOptions()` 定义选项
7. 在选项回调中实现多阶段流转（`SetEventState` → 新选项 → `SetEventFinished`）
8. 编写本地化 JSON（events.json）

**当前项目 ModId**: `PersonalMod`

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-12-add-event/

---

## 2. Model ID 规则

RitsuLib 注册的事件 ID 格式：

```
<MODID>_EVENT_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestEvent` | `PERSONALMOD_EVENT_TEST_EVENT` |
| `AbyssalBaths` | `PERSONALMOD_EVENT_ABYSSAL_BATHS` |
| `MysteriousSpring` | `PERSONALMOD_EVENT_MYSTERIOUS_SPRING` |

本地化键必须使用此 ID：

```json
{
  "PERSONALMOD_EVENT_TEST_EVENT.title": "与戈多相遇",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.description": "描述文本..."
}
```

---

## 3. 基类: ModEventTemplate

继承链: `ModEventTemplate` → `EventModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `GenerateInitialOptions()` | `abstract IReadOnlyList<EventOption>` | 生成事件初始选项列表 |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `EventAssetProfile` | — | 背景图/场景路径配置 |
| `CanonicalVars` | `protected virtual IEnumerable<DynamicVar>` | 空数组 | 动态变量（描述中的占位符） |
| `IsAllowed(IRunState)` | `virtual bool` | `true` | 出现条件（如金币超过阈值） |
| `ButtonColor` | `virtual Color` | `Color(1,1,1,0.9)` | 选项按钮颜色 |
| `LayoutType` | `virtual EventLayoutType` | `Default` | 布局类型 |
| `L10NLookup(string)` | — | — | 本地化文本查找 |

### 3.3 EventModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题（`events/{Entry}.title`） |
| `InitialDescription` | `LocString` | 初始描述（`events/{Entry}.pages.INITIAL.description`） |
| `Owner` | `Player?` | 事件所属玩家 |
| `Rng` | `Rng` | 随机数生成器 |
| `DynamicVars` | `DynamicVarSet` | 动态变量集合 |
| `LocTable` | `string` | 本地化表名（`events`） |
| `IsFinished` | `bool` | 事件是否结束 |
| `CurrentOptions` | `IReadOnlyList<EventOption>` | 当前可用选项 |
| `IsShared` | `bool` | 是否为共享事件 |
| `Node` | `Control?` | 事件场景节点 |
| `CanonicalEncounter` | `EncounterModel?` | 关联遭遇（战斗事件） |

### 3.4 核心方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `GenerateInitialOptions()` | `abstract IReadOnlyList<EventOption>` | **必须重写**：初始选项列表 |
| `SetEventState(LocString, IReadOnlyList<EventOption>)` | `void` | **切换事件阶段**：更新描述和选项 |
| `SetEventFinished(LocString?)` | `void` | **结束事件**：显示结束描述 |
| `BeforeEventStarted(bool isPreFinished)` | `Task` | 事件开始前回调 |
| `OnEventFinished()` | `void` | 事件结束后回调 |
| `L10NLookup(string key)` | `LocString` | 本地化文本查找 |
| `IsAllowed(IRunState)` | `bool` | 出现条件判断 |

### 3.5 选项构建辅助方法

| 方法 | 说明 |
|------|------|
| `InitialOptionKey(string option)` | 构建 INITIAL 页面的选项键 |
| `ModOptionKey(string page, string option)` | 构建指定页面的选项键 |

---

## 4. 事件生命周期与多阶段流转

### 4.1 完整生命周期

```
玩家进入事件房间
  └─ BeforeEventStarted(isPreFinished)
  └─ GenerateInitialOptions() → 显示初始选项
       └─ 玩家选择选项 → 选项回调
            ├─ 再次调用 SetEventState() → 进入下一阶段 → 显示新选项
            └─ 调用 SetEventFinished() → 结束事件
  └─ OnEventFinished()
  └─ 玩家离开事件房间
```

### 4.2 多阶段事件流转

事件通过 `SetEventState` 实现多阶段流转，每个阶段可以有不同的描述和选项：

```csharp
// 阶段 1: 初始选项
protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
[
    new EventOption(this, TakeDamage, InitialOptionKey("TAKE_DAMAGE")),
    new EventOption(this, LoseGold, InitialOptionKey("LOSE_GOLD")),
];

// 阶段 1 选项 A 的回调：切换到阶段 2
private async Task TakeDamage()
{
    // 执行效果 ...
    ChooseRewardTypePage(); // 进入下一阶段
}

// 阶段 2: 选择奖励
private void ChooseRewardTypePage()
{
    SetEventState(
        L10NLookup($"{Id.Entry}.pages.CHOOSE_TYPE.description"),
        [
            new EventOption(this, ChoosePotions, ModOptionKey("CHOOSE_TYPE", "CHOOSE_POTIONS")),
            new EventOption(this, ChooseCards, ModOptionKey("CHOOSE_TYPE", "CHOOSE_CARDS")),
        ]
    );
}

// 阶段 2 选项的回调：结束事件
private async Task ChoosePotions()
{
    await RewardsCmd.OfferCustom(Owner!, [new PotionReward(Owner!)]);
    SetEventFinished(L10NLookup($"{Id.Entry}.pages.POTIONS_CHOSEN.description"));
}
```

### 4.3 回调选项键命名

选项键决定本地化 JSON 中的键名，有两种方式：

1. **方法名自动生成**: `new EventOption(this, TakeDamage, ...)` 中如果不传键名参数，系统会从方法名自动 slugify 生成键名。但**推荐始终显式指定键名**。

2. **InitialOptionKey**: 生成 `{Entry}.pages.INITIAL.options.{KEY}` 格式的键。

3. **ModOptionKey**: 生成 `{Entry}.pages.{PAGE}.options.{KEY}` 格式的键。

```csharp
// 生成: {Entry}.pages.INITIAL.options.TAKE_DAMAGE.title
InitialOptionKey("TAKE_DAMAGE")

// 生成: {Entry}.pages.CHOOSE_TYPE.options.CHOOSE_POTIONS.title
ModOptionKey("CHOOSE_TYPE", "CHOOSE_POTIONS")
```

---

## 5. 事件选项 (EventOption)

### 5.1 构造函数

```csharp
new EventOption(
    EventModel ownerEvent,     // 所属事件实例 (this)
    Func<Task> onChosen,       // 选择后的回调
    string textKey,            // 选项文本键
    params IHoverTip[] extraHoverTips  // 额外悬停提示
);
```

### 5.2 完整参数

```csharp
new EventOption(
    this,
    OnHealChosen,
    InitialOptionKey("HEAL"),
    false,           // alwaysEnabled: 是否始终可用
    true,            // isProceed: 是否为"继续"按钮
    new IHoverTip[] { ... }  // 额外悬停提示
);
```

### 5.3 常用选项模式

```csharp
// 简单选项
new EventOption(this, OnDamage, InitialOptionKey("TAKE_DAMAGE"));

// 带描述的选项（在本地化 JSON 中加 .description 键）
// events.json:
// {Entry}.pages.INITIAL.options.TAKE_DAMAGE.title = "标题"
// {Entry}.pages.INITIAL.options.TAKE_DAMAGE.description = "描述"
```

---

## 6. 动态变量 (DynamicVar)

事件中的动态变量用于描述文本中的数值占位符。

### 6.1 常用变量类型

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars =>
[
    new DamageVar(10, ValueProp.Unblockable | ValueProp.Unpowered),  // 伤害
    new GoldVar(60),                                                  // 金币
    new HealVar(15),                                                  // 治疗
    new BlockVar(12, ValueProp.Unpowered),                            // 格挡
    new MagicNumberVar(5),                                            // 通用数值
];
```

### 6.2 运行时读取

```csharp
decimal damage = DynamicVars.Damage.BaseValue;
decimal goldCost = DynamicVars.Gold.BaseValue;
```

---

## 7. 资源配置 (EventAssetProfile)

### 7.1 基本配置

```csharp
public override EventAssetProfile AssetProfile => new(
    InitialPortraitPath: "res://PersonalMod/images/events/test_event.png"  // 事件背景图
);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `InitialPortraitPath` | `string` | 事件背景插图路径（PNG 或任何 Godot 可读格式） |
| `BackgroundScenePath` | `string?` | 可选的自定义背景场景路径 |
| `CustomPortraitPath` | `string?` | 可选的自定义肖像路径 |
| `CustomLayoutScenePath` | `string?` | 可选的自定义布局场景路径 |
| `CustomVfxScenePath` | `string?` | 可选的自定义特效场景路径 |

### 7.2 原版资源路径约定

| 资源 | 路径 |
|------|------|
| 事件插图 | `images/events/{entry}.png` |
| 恐惧模式插图 | `images/events/{entry}_phobia_mode.png` |
| 背景场景 | `scenes/events/background_scenes/{entry}.tscn` |
| 特效场景 | `scenes/vfx/events/{entry}_vfx.tscn` |

---

## 8. 出现条件 (IsAllowed)

通过重写 `IsAllowed` 控制事件何时出现：

```csharp
// 条件：所有玩家金币 ≥ 60
public override bool IsAllowed(IRunState runState) =>
    runState.Players.All(p => p.Gold >= DynamicVars.Gold.BaseValue);

// 条件：当前幕索引
public override bool IsAllowed(IRunState runState) =>
    runState.CurrentActIndex == 1;  // 只在第二幕出现
```

---

## 9. 生命周期回调

### 9.1 BeforeEventStarted — 事件开始前

```csharp
// 事件开始前执行，如锁定 UI 状态
protected override Task BeforeEventStarted(bool isPreFinished)
{
    Owner!.CanRemovePotions = false;  // 禁止移除药水
    return Task.CompletedTask;
}
```

### 9.2 OnEventFinished — 事件结束后

```csharp
// 事件结束后清理
protected override void OnEventFinished()
{
    Owner!.CanRemovePotions = true;  // 恢复药水操作
}
```

---

## 10. 注册方式

### 10.1 注册到指定幕

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterActEvent(typeof(Glory))]
public class TestEvent : ModEventTemplate { ... }
```

### 10.2 注册为共享事件

```csharp
[RegisterSharedEvent]
public class TestEvent : ModEventTemplate { ... }
```

使用 `[RegisterSharedEvent]` 时，通常需要配合 `IsAllowed` 自定义出现条件。

### 10.3 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("PersonalMod")
    .SharedEvent<TestEvent>()
    .ActEvent<Glory, TestEvent>()
    .Apply();
```

---

## 11. 常用命令

在事件选项回调中常用的命令：

| 命令 | 说明 | 示例 |
|------|------|------|
| `CreatureCmd.Damage(ctx, creature, amount, props, source)` | 造成伤害 | `await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars.Damage, null, null)` |
| `PlayerCmd.LoseGold(amount, player, lossType)` | 失去金币 | `await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, Owner!, GoldLossType.Stolen)` |
| `CreatureCmd.Heal(creature, amount)` | 治疗 | `await CreatureCmd.Heal(Owner, 15).Execute(default)` |
| `BlockCmd.GainBlock(amount)` | 获得格挡 | `await BlockCmd.GainBlock(10).Execute(choiceContext)` |
| `PowerCmd.Apply<TPower>(target, amount, applier, source)` | 施加能力 | `await PowerCmd.Apply<StrengthPower>(Owner, 2, Owner, null)` |
| `CardPileCmd.Draw(ctx, count, player)` | 抽牌 | `await CardPileCmd.Draw(ctx, 2, Owner)` |
| `RewardsCmd.OfferCustom(player, rewards)` | 提供自定义奖励 | `await RewardsCmd.OfferCustom(Owner!, [new PotionReward(Owner!)])` |
| `PlayerCmd.GainGold(amount, player)` | 获得金币 | `await PlayerCmd.GainGold(50, Owner!)` |

### 11.1 Reward 类型

| 奖励类型 | 说明 |
|---------|------|
| `new PotionReward(Player)` | 药水奖励 |
| `new CardReward(CardCreationOptions, count, Player)` | 卡牌奖励 |
| `new GoldReward(decimal)` | 金币奖励 |
| `new RelicReward(RelicModel)` | 遗物奖励 |

### 11.2 CardCreationOptions

```csharp
// 从角色卡池生成卡牌
CardCreationOptions.ForNonCombatWithDefaultOdds([Owner!.Character.CardPool])

// 从多个卡池生成
CardCreationOptions.ForNonCombatWithDefaultOdds([pool1, pool2])
```

---

## 12. 本地化

### 12.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/events.json
PersonalMod/PersonalMod/localization/zhs/events.json
```

### 12.2 格式

```json
{
  "PERSONALMOD_EVENT_TEST_EVENT.title": "与戈多相遇",

  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.description": "岔路口的长椅……",

  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.options.TAKE_DAMAGE.title": "用疼痛记住这一刻",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.options.TAKE_DAMAGE.description": "受到[red]{Damage}[/red]点伤害。",

  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.options.LOSE_GOLD.title": "留下过路费",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.INITIAL.options.LOSE_GOLD.description": "失去[gold]{Gold}[/gold]枚金币。",

  "PERSONALMOD_EVENT_TEST_EVENT.pages.CHOOSE_TYPE.description": "戈多从长椅底下摸出一个布包……",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.CHOOSE_TYPE.options.CHOOSE_POTIONS.title": "接过一瓶药水",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.CHOOSE_TYPE.options.CHOOSE_POTIONS.description": "领取药水奖励。",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.CHOOSE_TYPE.options.CHOOSE_CARDS.title": "领张牌再走",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.CHOOSE_TYPE.options.CHOOSE_CARDS.description": "领取卡牌奖励。",

  "PERSONALMOD_EVENT_TEST_EVENT.pages.POTIONS_CHOSEN.description": "液体在瓶里轻轻晃荡……",
  "PERSONALMOD_EVENT_TEST_EVENT.pages.CARDS_CHOSEN.description": "纸牌边缘划过指缝……"
}
```

### 12.3 键格式

| 字段 | 格式 | 必需 |
|------|------|------|
| `title` | `{Entry}.title` | 是 |
| `pages.INITIAL.description` | `{Entry}.pages.INITIAL.description` | 是 |
| `pages.INITIAL.options.<KEY>.title` | `{Entry}.pages.INITIAL.options.<KEY>.title` | 是（每个选项） |
| `pages.INITIAL.options.<KEY>.description` | `{Entry}.pages.INITIAL.options.<KEY>.description` | 推荐 |
| `pages.<PAGE>.description` | `{Entry}.pages.<PAGE>.description` | 自定义页面 |
| `pages.<PAGE>.options.<KEY>.title` | `{Entry}.pages.<PAGE>.options.<KEY>.title` | 自定义页面选项 |
| `pages.<PAGE>.description` | `{Entry}.pages.<PAGE>.description` | 结束页面 |

### 12.4 动态变量占位符

| 占位符 | 对应变量 | 说明 |
|--------|---------|------|
| `{Damage}` | `DamageVar` | 伤害值 |
| `{Gold}` | `GoldVar` | 金币数量 |
| `{Heal}` | `HealVar` | 治疗量 |
| `{Block}` | `BlockVar` | 格挡值 |
| `{MagicNumber}` | `MagicNumberVar` | 通用数值 |

### 12.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色 |
| `[red]文字[/red]` | 红色 |
| `[blue]文字[/blue]` | 蓝色 |
| `[green]文字[/green]` | 绿色 |
| `[purple]文字[/purple]` | 紫色 |
| `[sine]文字[/sine]` | 正弦波动效果（叙事常用） |
| `[b]文字[/b]` | 加粗 |

---

## 13. EventLayoutType

```csharp
EventLayoutType.Default    // 默认事件布局 (default_event_layout.tscn)
EventLayoutType.Combat     // 战斗事件布局 (combat_event_layout.tscn)
EventLayoutType.Ancient    // Ancient 事件布局 (ancient_event_layout.tscn)
EventLayoutType.Custom     // 自定义布局
```

---

## 14. 完整代码模板

### 14.1 多阶段事件（完整）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Events;

[RegisterActEvent(typeof(Glory))]
public sealed class TestEvent : ModEventTemplate
{
    // 背景图
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://PersonalMod/images/events/test_event.png"
    );

    // 数值变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Unblockable | ValueProp.Unpowered),
        new GoldVar(60)
    ];

    // 出现条件：所有玩家金币 ≥ 60
    public override bool IsAllowed(IRunState runState) =>
        runState.Players.All(p => p.Gold >= DynamicVars.Gold.BaseValue);

    // 事件开始前禁止移除药水
    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        Owner!.CanRemovePotions = false;
        return Task.CompletedTask;
    }

    // 事件结束后恢复
    protected override void OnEventFinished()
    {
        Owner!.CanRemovePotions = true;
    }

    // 阶段 1: 初始选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, TakeDamage, InitialOptionKey("TAKE_DAMAGE")),
        new EventOption(this, LoseGold, InitialOptionKey("LOSE_GOLD")),
    ];

    // 选项：失去生命
    private async Task TakeDamage()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            DynamicVars.Damage,
            null,
            null
        );
        ChooseRewardTypePage();
    }

    // 选项：失去金币
    private async Task LoseGold()
    {
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, Owner!, GoldLossType.Stolen);
        ChooseRewardTypePage();
    }

    // 阶段 2: 选择奖励类型
    private void ChooseRewardTypePage()
    {
        SetEventState(
            L10NLookup($"{Id.Entry}.pages.CHOOSE_TYPE.description"),
            [
                new EventOption(this, ChoosePotions,
                    ModOptionKey("CHOOSE_TYPE", "CHOOSE_POTIONS")),
                new EventOption(this, ChooseCards,
                    ModOptionKey("CHOOSE_TYPE", "CHOOSE_CARDS")),
            ]
        );
    }

    // 选择药水奖励并结束
    private async Task ChoosePotions()
    {
        await RewardsCmd.OfferCustom(Owner!, [new PotionReward(Owner!)]);
        SetEventFinished(
            L10NLookup($"{Id.Entry}.pages.POTIONS_CHOSEN.description"));
    }

    // 选择卡牌奖励并结束
    private async Task ChooseCards()
    {
        await RewardsCmd.OfferCustom(Owner!, [
            new CardReward(
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    [Owner!.Character.CardPool]),
                3,
                Owner)
        ]);
        SetEventFinished(
            L10NLookup($"{Id.Entry}.pages.CARDS_CHOSEN.description"));
    }
}
```

### 14.2 单阶段事件（最简）

```csharp
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Events;

[RegisterActEvent(typeof(Glory))]
public sealed class SimpleEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://PersonalMod/images/events/simple_event.png"
    );

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex == 1;  // 只在第二幕出现

    // 单阶段：一个选项直接结束
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, OnAccept, InitialOptionKey("ACCEPT")),
        new EventOption(this, OnLeave,  InitialOptionKey("LEAVE")),
    ];

    private Task OnAccept()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPTED.description"));
        return Task.CompletedTask;
    }

    private Task OnLeave()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEFT.description"));
        return Task.CompletedTask;
    }
}
```

### 14.3 使用抽象基类（推荐）

```csharp
[RegisterActEvent(typeof(Glory))]
public abstract class PersonalModEventModel : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://PersonalMod/images/events/{GetType().Name}.png"
    );
}

// 子类只需关注逻辑
[RegisterActEvent(typeof(Glory))]
public sealed class MyEvent : PersonalModEventModel
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, OnHeal, InitialOptionKey("HEAL")),
    ];

    private async Task OnHeal()
    {
        await CreatureCmd.Heal(Owner, 15).Execute(default);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }
}
```

---

## 15. 文件组织

```
PersonalMod/PersonalModCode/Events/
├── PersonalModEventModel.cs           # 抽象基类（可选）
├── TestEvent.cs                       # 多阶段事件
└── SimpleEvent.cs                     # 单阶段事件

PersonalMod/PersonalMod/
├── images/
│   └── events/
│       ├── test_event.png             # 事件背景插画
│       └── simple_event.png
└── localization/
    ├── eng/
    │   └── events.json                # 英文本地化
    └── zhs/
        └── events.json                # 中文本地化
```

---

## 16. 参考已有事件实现

在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 多阶段事件 | `Models/Events/` | `AbyssalBaths`, `ColossalFlower` |
| 单阶段事件 | `Models/Events/` | `AromaOfChaos` |
| 奖励选择 | `Models/Events/` | `Amalgamator` |
| 战斗事件 | `Models/Events/` | `BattlewornDummy` |
| IsAllowed 条件 | `Models/Events/` | 搜索 `IsAllowed` |
| BeforeEventStarted | `Models/Events/` | 搜索 `BeforeEventStarted` |

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Events\` (约 68 个事件文件)

---

## 17. 调试

### 17.1 控制台命令

在游戏中按 `~` 打开控制台：

```
event PERSONALMOD_EVENT_TEST_EVENT
```

强制触发指定事件。

---

## 18. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 事件不在游戏中出现 | 未正确注册或 `IsAllowed` 返回 false | 检查 `[RegisterActEvent]` 和 `IsAllowed` 条件 |
| 事件背景显示空白 | 背景图路径错误 | 检查 `AssetProfile.InitialPortraitPath` |
| 选项显示原始键名 | 选项键格式错误 | 使用 `InitialOptionKey()` 或 `ModOptionKey()` 构建键名 |
| 多阶段事件卡住 | 选项回调中未调用 `SetEventState` 或 `SetEventFinished` | 确保每个选项回调最终调用其中一个 |
| 动态变量不显示 | `CanonicalVars` 未定义对应变量 | 添加 `DamageVar` / `GoldVar` 等定义 |
| 事件描述显示原始键名 | 本地化 JSON 缺少条目 | 检查 events.json 中对应键 |
| BeforeEventStarted 不执行 | 未正确重写 | 方法签名: `Task BeforeEventStarted(bool)` |
| 奖励不出现 | `RewardsCmd.OfferCustom` 参数错误 | 确保 `Player` 不为 null 且 `Reward` 类型正确 |
| `{Damage}` 显示为 0 | CanonicalVars 中未定义 | 在 `CanonicalVars` 中添加 `DamageVar` |
| 选项按钮颜色不对 | 未重写 `ButtonColor` | 重写 `ButtonColor` 属性 |

---

## 19. 编写审查清单

### 19.1 基础检查

- [ ] 是否继承了 `ModEventTemplate`？
- [ ] 是否添加了 `[RegisterActEvent]` 或 `[RegisterSharedEvent]` 属性？
- [ ] 是否重写了 `GenerateInitialOptions()`？
- [ ] 所有选项回调中是否最终调用了 `SetEventState` 或 `SetEventFinished`？
- [ ] 命名空间是否正确？

### 19.2 资源检查

- [ ] `AssetProfile.InitialPortraitPath` 路径是否正确？
- [ ] 背景插图 PNG 文件是否存在？

### 19.3 逻辑检查

- [ ] 是否需要 `IsAllowed` 条件？
- [ ] 多阶段事件中每个阶段的描述和选项是否正确设置？
- [ ] `BeforeEventStarted` / `OnEventFinished` 是否成对使用？
- [ ] `CanonicalVars` 是否定义了所有必需的变量？

### 19.4 本地化检查

- [ ] `events.json` 中是否添加了 `title`？
- [ ] 是否添加了 `pages.INITIAL.description`？
- [ ] 每个选项是否有 `title`（和可选的 `description`）？
- [ ] 多阶段的额外页面是否有对应的 `description` 和 `options`？
- [ ] BBCode 标签是否正确闭合？

### 19.5 注册检查

- [ ] `RegisterModAssembly` 在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
