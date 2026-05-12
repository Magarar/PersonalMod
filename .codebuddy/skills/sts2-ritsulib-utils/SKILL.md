---
name: sts2-ritsulib-utils
description: >-
  该 Skill 提供杀戮尖塔2 (Slay the Spire 2) Mod 开发中常用小工具功能的参考与实现。
  涵盖手牌上限修改 (IMaxHandSizeModifier)、手牌泛光 (CardHandOutline / ShouldGlowGoldInternal / ShouldGlowRedInternal)、
  血条覆盖 (IHealthBarForecastSource / HealthBarForecasts)、数据持久化 (SavedAttachedState)、
  以及其他不属于专用 Skill 的各种实用工具。
  当其它 Skill 需要对应的通用功能时（如修改手牌上限、添加血条覆盖层等），可先在此处查找。
  此 Skill 预留了扩展空间，新的实用功能可以按相同的章节模式添加。
auto_trigger: false
trigger_priority: 5
---

# STS2 RitsuLib 实用工具 Skill

## 1. 概述

本 Skill 收录了杀戮尖塔2 Mod 开发中常用的但规模较小、不足以独立成篇的实用功能。当需要实现以下功能，且没有对应的专用 Skill 时，可在此处查找：

| 功能 | 说明 | 章节 |
|------|------|------|
| 手牌上限修改 | 修改玩家的最大手牌数 | §2 |
| 手牌泛光 | 给手牌添加金色/红色/自定义颜色发光 | §3 |
| 血条覆盖 | 为能力添加血条覆盖层（类似中毒/灾厄效果） | §4 |
| 局内数据保存 | 保存卡牌/遗物等对象的持久化状态 | §5 |
| ... (预留扩展) | ... | ... |

**当前项目 ModId**: `PersonalMod`

---

## 2. 手牌上限修改

### 2.1 概述

通过实现 `IMaxHandSizeModifier` 接口来修改玩家的最大手牌上限。该接口可以在任何 `AbstractModel` 子类（如能力、遗物）上实现。

### 2.2 接口方法

```csharp
public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
```

还有后执行版本（Late），适合需要覆盖其他修改的场景：

```csharp
public int ModifyMaxHandSizeLate(Player player, int currentMaxHandSize)
```

### 2.3 使用示例（能力）

```csharp
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Powers;

[RegisterPower]
public class TestPower : ModPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png"
    );

    // 实现 IMaxHandSizeModifier
    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player != Owner.Player)
            return currentMaxHandSize;
        return currentMaxHandSize + 2;  // 手牌上限 +2
    }
}
```

### 2.4 使用示例（遗物）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class HandSizeRelic : ModRelicTemplate, IMaxHandSizeModifier
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player != Owner)
            return currentMaxHandSize;
        return currentMaxHandSize + 1;
    }
}
```

### 2.5 获取玩家的手牌上限

```csharp
// 不要硬编码 10，使用此方法获取
int maxHandSize = RitsuLibFramework.GetMaxHandSize(player);
```

### 2.6 注意事项

- 返回的值是**修改后的手牌上限**
- 如果你想设置为一个固定值，建议使用 `ModifyMaxHandSizeLate`
- 最终值不会少于 0（系统会兜底处理）
- 注意 Hook 的顺序（每日特效和单例在最后触发）

---

## 3. 手牌泛光

### 3.1 原版金/红光

如果只需要金色和红色发光，直接在卡牌类中重写以下属性：

```csharp
// 何时发金色光
protected override bool ShouldGlowGoldInternal =>
    Owner.Creature.GetPowerAmount<TestPower>() > 5;

// 何时发红色光
protected override bool ShouldGlowRedInternal =>
    !Owner.Creature.HasPower<TestPower>();
```

### 3.2 任意颜色发光的注册

RitsuLib 提供 `CardHandOutline` 内容包方法，可在 `Entry.Init()` 中注册：

```csharp
using Godot;
using STS2RitsuLib;

public static void Init()
{
    var ctx = RitsuLibFramework.CreateContentPack(ModId)
        .CardHandOutline<TestCard>(new ModCardHandOutlineRule(
            card => card.Owner.Creature.CurrentHp <= 10,  // 发光条件
            Colors.Purple,                                 // 发光颜色
            0,                                             // (可选) 优先级，更高的才会展示
            false                                          // (可选) 不可打出时隐藏边框
        ))
        .Apply();
}
```

### 3.3 ModCardHandOutlineRule 参数

| 参数 | 类型 | 说明 |
|------|------|------|
| 条件 | `Func<CardModel, bool>` | 判断卡牌是否应该发光 |
| 颜色 | `Color` | 发光颜色 |
| 优先级 | `int` (可选) | 优先级，更高的才会展示，默认 0 |
| 隐藏边框 | `bool` (可选) | 不可打出时是否隐藏边框，默认 false |

### 3.4 批量设置泛光

可以为基类设置泛光规则，所有子类自动生效：

```csharp
.CardHandOutline<CardModel>(new ModCardHandOutlineRule(
    card => card.HasModKeyword("my_keyword"),
    Colors.Cyan
))
```

---

## 4. 血条覆盖

### 4.1 概述

通过实现 `IHealthBarForecastSource` 接口，可以在生物血条上添加覆盖层（类似 `中毒`、`灾厄` 的效果），展示即将受到的伤害或治疗量。

### 4.2 接口方法

```csharp
using STS2RitsuLib.Combat.HealthBars;

public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
    HealthBarForecastContext context)
```

### 4.3 使用示例（能力）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Powers;

[RegisterPower]
public class TestPower2 : ModPowerTemplate, IHealthBarForecastSource
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Weakness", 1.25m)
    ];

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || Owner.CurrentHp > Amount)
            return 1m;
        return DynamicVars["Weakness"].BaseValue;
    }

    // 实现 IHealthBarForecastSource 接口
    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
        HealthBarForecastContext context)
    {
        return HealthBarForecasts.Single(
            context.Creature.GetPowerAmount<TestPower2>(),   // 展示的数量
            new Color(0.4f, 0.1f, 0.1f),                     // 颜色
            HealthBarForecastGrowthDirection.FromLeft,         // 延伸方向
            0,                                                  // (可选) 顺序，越大越远离血条边缘
            null                                                // (可选) 自定义材质
        );
    }
}
```

### 4.4 HealthBarForecasts 辅助方法

| 方法 | 说明 |
|------|------|
| `HealthBarForecasts.Single(amount, color, direction, order, material)` | 单个覆盖段 |
| 其他辅助 | ... |

### 4.5 HealthBarForecastGrowthDirection

```csharp
HealthBarForecastGrowthDirection.FromLeft   // 从血条左边开始延伸
HealthBarForecastGrowthDirection.FromRight  // 从血条右边开始延伸
```

---

## 5. 局内数据保存 (SavedAttachedState)

### 5.1 概述

`SavedAttachedState<TOwner, TValue>` 是 RitsuLib 提供的工具，用于给卡牌、遗物等对象添加跟随存档保存的状态数据。

命名空间: `STS2RitsuLib.Utils`

### 5.2 基本用法

```csharp
using STS2RitsuLib.Utils;

// 在类的静态字段中定义
public static readonly SavedAttachedState<TestRelic, int> GameTurns
    = new("GameTurns", _ => 0);
```

| 参数 | 说明 |
|------|------|
| 泛型 TOwner | 所属对象的类型（如 `TestRelic`） |
| 泛型 TValue | 要保存的值的类型（如 `int`） |
| 第1个参数 | 保存用的状态名，同一对象类型内不要重复 |
| 第2个参数 | 默认值构造器（存档中没有该值时使用） |

### 5.3 读取与修改

```csharp
// 读取
int currentValue = GameTurns[this];

// 修改
GameTurns[this]++;

// 注意：修改后需要更新 DynamicVar 才能在描述中反映
DynamicVars["GameTurns"].BaseValue = GameTurns[this];
```

### 5.4 在遗物中使用的完整示例

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace PersonalMod.PersonalModCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TestRelic : ModRelicTemplate
{
    // 定义可保存状态
    public static readonly SavedAttachedState<TestRelic, int> GameTurns
        = new("GameTurns", _ => 0);

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1),
        new DynamicVar("GameTurns", GameTurns[this])
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://PersonalMod/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://PersonalMod/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://PersonalMod/images/relics/{GetType().Name}.png"
    );

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player)
    {
        // 每回合 +1，并更新描述
        GameTurns[this]++;
        DynamicVars["GameTurns"].BaseValue = GameTurns[this];
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}
```

### 5.5 本地化

```json
{
  "PERSONALMOD_RELIC_TEST_RELIC.title": "测试遗物",
  "PERSONALMOD_RELIC_TEST_RELIC.description": "每回合开始时，抽[blue]{Cards}[/blue]张牌。\n已经历过[blue]{GameTurns}[/blue]回合了。",
  "PERSONALMOD_RELIC_TEST_RELIC.flavor": "觉得很眼熟？"
}
```

### 5.6 在卡牌上的使用

```csharp
public static readonly SavedAttachedState<TestCard, bool> WasUpgradedThisCombat
    = new("WasUpgradedThisCombat", _ => false);

// 读取
bool upgraded = WasUpgradedThisCombat[this];
```

### 5.7 注意事项

- `SavedAttachedState` 自动处理存档序列化，无需额外写读档逻辑
- 同一个对象类型内的状态名不能重复
- 如果需要在描述中显示，需要同时添加对应的 `DynamicVar`

---

## 6. 功能索引

| 功能 | API / 接口 | 所在命名空间 | 章节 |
|------|-----------|-------------|------|
| 修改手牌上限 | `IMaxHandSizeModifier` | `STS2RitsuLib.Combat.HandSize` | §2 |
| 获取手牌上限 | `RitsuLibFramework.GetMaxHandSize(Player)` | `STS2RitsuLib` | §2.5 |
| 卡牌金色发光 | `ShouldGlowGoldInternal` (override) | `CardModel` | §3.1 |
| 卡牌红色发光 | `ShouldGlowRedInternal` (override) | `CardModel` | §3.1 |
| 任意颜色泛光 | `.CardHandOutline<TCard>()` | `STS2RitsuLib` | §3.2 |
| 血条覆盖层 | `IHealthBarForecastSource` | `STS2RitsuLib.Combat.HealthBars` | §4 |
| 局内数据保存 | `SavedAttachedState<TOwner, TValue>` | `STS2RitsuLib.Utils` | §5 |
| 数据持久化 | `RitsuLibFramework.GetDataStore()` + `ModDataStore` | `STS2RitsuLib` | 参见 `sts2-singleton-model-skill` |
| (预留) | ... | ... | ... |

---

## 7. 扩展指南

本 Skill 设计为可扩展。添加新功能时按以下模式：

1. 在本文件中新增一个章节（如 §6、§7...），标题格式为 `## N. 功能名称`
2. 在文件头部的概述表中新增一行
3. 在 §6 "功能索引" 表中新增一行
4. 遵循已有章节的格式：概述 → API 说明 → 代码示例 → 注意事项

---

*最后更新：2026-05-12*
