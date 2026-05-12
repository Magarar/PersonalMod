---
name: sts2-potion-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 药水 (Potion) 提供全面的参考与自动检查。
  涵盖药水定义 (ModPotionTemplate)、动态变量 (DynamicVar)、药水资源路径 (PotionAssetProfile)、
  药水使用逻辑 (OnUse)、目标选择 (TargetType)、注册方式 ([RegisterPotion])、药水池 (PotionPoolModel)、
  本地化文本 (potions.json)、PotionRarity/PotionUsage 枚举速查、Hook 回调继承、
  药水相关命令 (CreatureCmd/PowerCmd/BlockCmd 等)、以及完整的代码模板与审查清单。
  当用户要求创建新药水、修改已有药水逻辑、或排查药水相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 药水编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 药水 (Potion)，核心步骤：
1. 创建药水类，继承 `ModPotionTemplate`
2. 用 `[RegisterPotion(typeof(XxxPool))]` 注册到药水池
3. 重写 `Rarity` 属性（必须，常见/稀有等）
4. 重写 `Usage` 属性（必须，CombatOnly / AnyTime / Automatic）
5. 重写 `TargetType` 属性（必须，Self / SingleEnemy / AnyPlayer 等）
6. 重写 `AssetProfile` 配置图片路径
7. 重写 `OnUse` 方法编写使用逻辑
8. 编写本地化 JSON（title + description）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-06-add-potion/

---

## 2. Model ID 规则

RitsuLib 注册的药水 ID 格式：

```
<MODID>_POTION_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestPotion` | `{{MODID_UPPER}}_POTION_TEST_POTION` |
| `StrengthPotion` | `PERSONALMOD_POTION_STRENGTH_POTION` |
| `HealingPotion` | `PERSONALMOD_POTION_HEALING_POTION` |

本地化键必须使用此 ID：

```json
{
  "PERSONALMOD_POTION_TEST_POTION.title": "测试药水",
  "PERSONALMOD_POTION_TEST_POTION.description": "获得[blue]{Block}[/blue]点[gold]格挡[/gold]。"
}
```

---

## 3. 基类: ModPotionTemplate

继承链: `ModPotionTemplate` → `PotionModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `Rarity` | `abstract PotionRarity` | 药水稀有度 |
| `Usage` | `abstract PotionUsage` | 使用时机（战斗中/随时/自动） |
| `TargetType` | `abstract TargetType` | 目标类型 |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `PotionAssetProfile` | — | 图片路径配置 |
| `CanonicalVars` | `protected virtual IEnumerable<DynamicVar>` | 空列表 | 动态变量（描述中的数值占位符） |
| `ExtraHoverTips` | `public virtual IEnumerable<IHoverTip>` | 空列表 | 额外悬停提示（如预览卡牌/能力） |
| `CanBeGeneratedInCombat` | `virtual bool` | `true` | 是否可在战斗中生成 |
| `PassesCustomUsabilityCheck` | `virtual bool` | `true` | 自定义可用性检查 |

### 3.3 PotionModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题本地化 (`potions/{Entry}.title`) |
| `DynamicDescription` | `LocString` | 带动态变量替换的描述 |
| `SelectionScreenPrompt` | `LocString` | 选择界面提示 (`potions/{Entry}.selectionScreenPrompt`) |
| `ImagePath` | `string` | 药水图片路径 |
| `OutlinePath` | `string?` | 药水轮廓路径（可能为 null） |
| `Image` | `Texture2D` | 药水图片纹理 |
| `Outline` | `Texture2D?` | 药水轮廓纹理 |
| `Owner` | `Player` | 药水所属玩家 |
| `DynamicVars` | `DynamicVarSet` | 动态变量集合 |
| `Pool` | `PotionPoolModel` | 所属药水池 |
| `HoverTips` | `IEnumerable<IHoverTip>` | 所有悬停提示（含默认和额外） |
| `IsQueued` | `bool` | 是否已入队等待使用 |
| `HasBeenRemovedFromState` | `bool` | 是否已从状态中移除 |

### 3.4 PotionModel 方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `OnUse` | `protected virtual Task OnUse(PlayerChoiceContext choiceContext, Creature? target)` | **核心方法**：药水使用时的效果逻辑 |
| `EnqueueManualUse` | `void EnqueueManualUse(Creature? target)` | 手动入队药水使用 |
| `Discard` | `void Discard()` | 丢弃药水 |
| `RemoveBeforeUse` | `void RemoveBeforeUse()` | 使用前移除药水 |
| `AfterUsageCanceled` | `void AfterUsageCanceled()` | 取消使用后回调 |
| `CanThrowAtAlly` | `bool CanThrowAtAlly()` | 是否可丢给队友（多玩家模式） |

### 3.5 Hook 回调继承

`PotionModel` 继承自 `AbstractModel`，因此也支持完整的 Hook 回调系统。药水可通过 override 以下方法在特定事件触发时执行逻辑：

- 药水继承 `PotionModel`，而 `PotionModel` 的 `ShouldReceiveCombatHooks` 返回 `true`
- 因此药水可以在战斗中 override 各类 Hook 回调（如 `AfterDamageReceived`、`AfterTurnEnd` 等）
- 典型例子：`FairyInABottle` 药水 override `ShouldDie` 和 `AfterPreventingDeath` 来实现自动救命效果

**完整 Hook 回调列表** 参见 `sts2-relic-skill` 或 `sts2-core-ref` 的 `references/hooks-reference.md`，药水适用相同的回调签名。

---

## 4. 枚举速查

### 4.1 PotionRarity

```csharp
PotionRarity.None       // 无稀有度（默认）
PotionRarity.Common      // 常见
PotionRarity.Uncommon    // 罕见
PotionRarity.Rare        // 稀有
PotionRarity.Event       // 事件药水
PotionRarity.Token       // 衍生物药水
```

### 4.2 PotionUsage

```csharp
PotionUsage.None        // 无（默认，不可手动使用）
PotionUsage.CombatOnly   // 仅限战斗中使用（最常用）
PotionUsage.AnyTime      // 随时可以使用（包括非战斗状态）
PotionUsage.Automatic    // 自动触发使用（如 FairyInABottle 自动救命）
```

### 4.3 TargetType

```csharp
TargetType.Self          // 自身
TargetType.AnyEnemy      // 一个敌人（玩家选择）
TargetType.AllEnemies    // 全体敌人
TargetType.AnyPlayer     // 任意玩家（含自己，可用于多玩家模式投掷药水）
TargetType.AnyAlly       // 任意友方
```

---

## 5. 动态变量 (DynamicVar)

药水通过 `CanonicalVars` 定义数值变量，用于本地化描述中的占位符。

### 5.1 常用变量类型

| 变量类型 | 用途 | 本地化占位符 |
|---------|------|-------------|
| `BlockVar` | 格挡值 | `{Block}` |
| `DamageVar` | 伤害值 | `{Damage}` |
| `HealVar` | 治疗量 | `{Heal}` |
| `CardsVar` | 卡牌数 | `{Cards}` |
| `MagicNumberVar` | 通用数值 | `{MagicNumber}` |
| `EnergyVar` | 能量值 | `{Energy:energyIcons()}` |
| `PowerVar<TPower>` | 能力层数 | 自动获取能力名称和图标 |

### 5.2 定义变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => [
    new BlockVar(12, ValueProp.Unpowered),       // 12 点格挡
    new HealVar(10),                              // 10 点治疗
    new CardsVar(3),                              // 3 张牌
    new MagicNumberVar(5),                        // 通用数值 5
    new PowerVar<StrengthPower>(2),               // 2 层力量（带能力预览）
];
```

### 5.3 运行时读取变量

```csharp
// 在 OnUse 或其他方法中读取变量值
int blockAmount = DynamicVars.Block.IntValue;
int healAmount = DynamicVars.Heal.IntValue;
int drawCount = DynamicVars.Cards.IntValue;
decimal magicValue = DynamicVars.MagicNumber.BaseValue;
```

### 5.4 ExtraHoverTips — 额外悬停提示

药水可以显示额外的悬停提示，例如预览生成的卡牌或展示施加的能力。

```csharp
// 预览一张卡牌灵魂（卡牌悬浮显示）
protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [HoverTipFactory.FromCard<Soul>()];

// 预览一个能力（能力悬浮显示）
protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [HoverTipFactory.FromPower<StrengthPower>()];

// 静态提示（如格挡描述）
protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [HoverTipFactory.Static(StaticHoverTip.Block, Array.Empty<DynamicVar>())];
```

---

## 6. 资源路径配置 (PotionAssetProfile)

### 6.1 基本配置

```csharp
public override PotionAssetProfile AssetProfile => new(
    ImagePath: "res://PersonalMod/images/potions/test_potion.png",   // 药水本体图片
    OutlinePath: "res://PersonalMod/images/potions/test_potion.png"  // 药水轮廓图片
);
```

### 6.2 图片说明

| 路径 | 说明 |
|------|------|
| `ImagePath` | 药水本体图片（显示在药水槽中） |
| `OutlinePath` | 药水轮廓图片 |

> 只要最终能被 Godot 当作 `Texture2D` 读取的格式均可，不限定为 png。

### 6.3 原版药水资源路径约定

原版药水资源路径（Mod 不需要遵循，但可参考）：

| 资源 | 路径 |
|------|------|
| 图集 | `atlases/potion_atlas.sprites/{entry}.tres` |
| 轮廓图集 | `atlases/potion_outline_atlas.sprites/{entry}.tres` |

`entry` = `Entry.ToLowerInvariant()`

---

## 7. 注册方式

### 7.1 属性注册（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

[RegisterPotion(typeof(SharedPotionPool))]
public class TestPotion : ModPotionTemplate
{
    // ...
}
```

前提：在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

### 7.2 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Potion<SharedPotionPool, TestPotion>()  // 注册到共享池
    .Potion<MyCharacterPotionPool, MyPotion>()  // 注册到角色专属池
    .Apply();
```

### 7.3 Manifest 注册

```csharp
new PotionRegistrationEntry<SharedPotionPool, TestPotion>()
```

### 7.4 可用药水池

| 药水池 | 说明 |
|--------|------|
| `SharedPotionPool` | 共享药水池（所有角色可用） |
| `IroncladPotionPool` | 铁甲战士专属药水池 |
| `SilentPotionPool` | 静默猎手专属药水池 |
| `DefectPotionPool` | 缺陷专属药水池 |
| `NecrobinderPotionPool` | 死灵法师专属药水池 |
| `RegentPotionPool` | 摄政王专属药水池 |
| `TokenPotionPool` | 衍生物药水池 |
| `EventPotionPool` | 事件药水池 |

---

## 8. 药水使用逻辑 (OnUse)

### 8.1 方法签名

```csharp
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
```

- `choiceContext` — 玩家选择上下文
- `target` — 目标生物（根据 `TargetType` 可能是 null，如 TargetType.Self 时 target 为 null）

### 8.2 常用命令

在 `OnUse` 中常用的战斗命令：

| 命令 | 说明 | 示例 |
|------|------|------|
| `BlockCmd.GainBlock(amount)` | 获得格挡 | `await BlockCmd.GainBlock(amount).Execute(choiceContext)` |
| `PowerCmd.Apply<TPower>(target, amount, applier, cardSource)` | 施加能力 | `await PowerCmd.Apply<StrengthPower>(Owner, 2, Owner, null)` |
| `DamageCmd.Attack(amount)` | 造成伤害 | `await DamageCmd.Attack(amount).FromCard(this).Targeting(target).Execute(choiceContext)` |
| `CreatureCmd.Heal(target, amount)` | 治疗 | `await CreatureCmd.Heal(Owner, 10).Execute(choiceContext)` |
| `CardPileCmd.Draw(ctx, count, player)` | 抽牌 | `await CardPileCmd.Draw(choiceContext, 2, Owner)` |
| `CardCmd.Upgrade(card, style)` | 升级卡牌 | `CardCmd.Upgrade(card, CardPreviewStyle.HorizontalLayout)` |

### 8.3 药水使用流程

1. 玩家选中药水并确认目标
2. `BeforeUse` 事件触发
3. 药水入队 (`EnqueueManualUse`)
4. 执行 `UsePotionAction`
5. 调用 `OnUse` 执行效果逻辑
6. 使用后药水从药水槽移除

---

## 9. 本地化

### 9.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/potions.json
PersonalMod/PersonalMod/localization/zhs/potions.json
```

### 9.2 格式

```json
{
    "PERSONALMOD_POTION_TEST_POTION.title": "测试药水",
    "PERSONALMOD_POTION_TEST_POTION.description": "获得[blue]{Block}[/blue]点[gold]格挡[/gold]。"
}
```

### 9.3 可选字段

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 药水名称 | 是 |
| `description` | 效果描述，支持动态变量占位符 | 是 |
| `selectionScreenPrompt` | 选择界面提示文字 | 可选 |

### 9.4 描述占位符

| 占位符 | 对应变量 | 说明 |
|--------|---------|------|
| `{Block}` | `BlockVar` | 格挡值 |
| `{Damage}` | `DamageVar` | 伤害值 |
| `{Heal}` | `HealVar` | 治疗量 |
| `{Cards}` | `CardsVar` | 抽牌/数值 |
| `{MagicNumber}` | `MagicNumberVar` | 通用数值 |
| `{Energy:energyIcons()}` | 能量 | 渲染能量图标 |

### 9.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮（用于关键词） |
| `[blue]文字[/blue]` | 蓝色（用于数值） |
| `[b]文字[/b]` | 加粗 |
| `[purple]文字[/purple]` | 紫色 |

---

## 10. Hook 回调 — 药水继承自 AbstractModel 的完整事件

药水继承自 `PotionModel`，而 `PotionModel` 的 `ShouldReceiveCombatHooks` 返回 `true`，因此药水可以 override 所有基类的 Hook 回调方法。

**典型应用场景**：
- `FairyInABottle`：结合 `Usage.Automatic` + `ShouldDie` + `AfterPreventingDeath` 实现自动救命
- 自定义药水可在 `AfterTurnEnd` 中检查条件并触发生效

### 10.1 战斗生命周期

| 方法 | 说明 |
|------|------|
| `BeforeCombatStart()` | 战斗开始前 |
| `AfterCombatEnd(CombatRoom room)` | 战斗结束后 |
| `AfterPlayerTurnStart(ctx, player)` | 玩家回合开始后 |
| `BeforeTurnEnd(ctx, side)` | 回合结束前 |
| `AfterTurnEnd(ctx, side)` | 回合结束后 |

### 10.2 伤害与格挡

| 方法 | 说明 |
|------|------|
| `BeforeDamageReceived(ctx, target, amount, props, dealer, cardSource)` | 受到伤害前 |
| `AfterDamageReceived(ctx, target, result, props, dealer, cardSource)` | 受到伤害后 |
| `BeforeBlockGained(creature, amount, props, cardSource)` | 获得格挡前 |
| `AfterBlockGained(creature, amount, props, cardSource)` | 获得格挡后 |

### 10.3 死亡相关

| 方法 | 说明 |
|------|------|
| `ShouldDie(Creature creature)` | 是否允许死亡 |
| `BeforeDeath(Creature creature)` | 死亡前 |
| `AfterDeath(ctx, creature, wasRemovalPrevented, deathAnimLength)` | 死亡后 |
| `AfterPreventingDeath(Creature creature)` | 阻止死亡后 |

### 10.4 完整的 Hook 回调列表

完整列表参见 `sts2-relic-skill` SKILL.md 的"6. Hook 回调方法"章节或 `sts2-core-ref` 的 `references/hooks-reference.md`，药水适用相同的回调签名。

---

## 11. 完整代码模板

### 11.1 获得格挡药水（最简模板）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Potions;

[RegisterPotion(typeof(SharedPotionPool))]
public class BlockPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(12, ValueProp.Unpowered)
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await BlockCmd.GainBlock(DynamicVars.Block.IntValue)
            .FromPotion(this)
            .Execute(choiceContext);
    }
}
```

### 11.2 施加能力药水（含 ExtraHoverTips）

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public class StrengthPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(2)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // target 可以是自己或任意玩家 (TargetType.AnyPlayer)
        Creature actualTarget = target ?? Owner.Creature;
        await PowerCmd.Apply<StrengthPower>(actualTarget, DynamicVars.StrengthPower.IntValue, Owner, null);
    }
}
```

### 11.3 治疗药水（HealVar）

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public class HealingPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.AnyTime;   // 非战斗也可使用
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(10)
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CreatureCmd.Heal(Owner, DynamicVars.Heal.IntValue)
            .Execute(choiceContext);
    }
}
```

### 11.4 造成伤害药水（DamageCmd）

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public class FirePotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(15)
    ];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target); // 确保目标不为 null
        await DamageCmd.Attack(DynamicVars.Damage.IntValue)
            .FromPotion(this)
            .Targeting(target!)
            .Execute(choiceContext);
    }
}
```

### 11.5 自动救命药水（Hook 模式 + Automatic 用法）

参考 `FairyInABottle` 实现：

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public class AutoSavePotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.Automatic;  // 自动触发
    public override TargetType TargetType => TargetType.Self;

    public override bool CanBeGeneratedInCombat => false; // 不在战斗中掉落

    // 阻止持有者死亡
    public override bool ShouldDie(Creature creature)
    {
        return creature != Owner.Creature;
    }

    // 阻止死亡后自动使用药水（治疗等效果）
    public override async Task AfterPreventingDeath(Creature creature)
    {
        await CreatureCmd.Heal(Owner, 10).Execute(default);
    }
}
```

### 11.6 生成卡牌到手牌药水

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public class SoulPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    // 显示生成的卡牌预览
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Soul>()];

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await Soul.CreateInHand(Owner, DynamicVars.Cards.IntValue, Owner.Creature.CombatState!);
    }
}
```

### 11.7 使用抽象基类统一管理（推荐）

```csharp
[RegisterPotion(typeof(SharedPotionPool))]
public abstract class PersonalModPotionModel : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png",
        OutlinePath: $"res://{{MODID}}/images/potions/{GetType().Name}.png"
    );
}

// 子类只需关注逻辑
[RegisterPotion(typeof(SharedPotionPool))]
public class MyHealPotion : PersonalModPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(8)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CreatureCmd.Heal(Owner, DynamicVars.Heal.IntValue)
            .Execute(choiceContext);
    }
}
```

### 11.8 最简药水模板（快速起步）

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Potions;

[RegisterPotion(typeof(SharedPotionPool))]
public class MyPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
}
```

> 最简模板缺少图片配置和效果逻辑，仅用于快速验证注册是否成功。正式药水需补充 `AssetProfile` 和 `OnUse` 方法。

---

## 12. 药水效果实现模式总结

### 12.1 基础模式 — OnUse 执行逻辑

适用场景：所有需要通过使用即时生效的药水。

```csharp
protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
{
    // 使用 BlockCmd / PowerCmd / DamageCmd / CreatureCmd 等执行效果
    await BlockCmd.GainBlock(DynamicVars.Block.IntValue)
        .FromPotion(this)
        .Execute(ctx);
}
```

### 12.2 目标选择模式

| TargetType | target 参数 | 示例 |
|-----------|------------|------|
| `TargetType.Self` | null | 增益自身效果 |
| `TargetType.AnyEnemy` | 敌人 | 对敌人造成伤害 |
| `TargetType.AnyPlayer` | 玩家或 null | 可对自己或队友使用 |
| `TargetType.AllEnemies` | null | 全体伤害（无需选择目标） |

### 12.3 使用时机模式

| PotionUsage | 说明 | 举例 |
|------------|------|------|
| `CombatOnly` | 只能在战斗中使用（最常用） | 力量药水、格挡药水 |
| `AnyTime` | 非战斗也可使用 | 治疗药水 |
| `Automatic` | 自动触发，不需要玩家使用 | 妖精之水 |

### 12.4 Hook 模式 — 事件触发（药水作为 HookListener）

适用场景：药水在战斗中监听事件并反应（如自动救命）。

```csharp
public override bool ShouldDie(Creature creature)
{
    return creature != Owner.Creature; // 阻止自己死亡
}

public override async Task AfterPreventingDeath(Creature creature)
{
    // 阻止死亡后执行治疗
}
```

---

## 13. 控制台调试命令

在游戏中按 `~` 打开控制台：

```
potion PERSONALMOD_POTION_TEST_POTION
```

快速检查药水是否注册成功：在控制台尝试获取该药水（输入 `potion` 加药水 ID）。

---

## 14. 文件组织

```
{{MODID}}/{{MODID}}Code/Potions/
├── PersonalModPotionModel.cs        # 抽象基类（可选）
├── BlockPotion.cs                   # 格挡药水
├── HealingPotion.cs                 # 治疗药水
└── StrengthPotion.cs                # 力量药水

{{MODID}}/{{MODID}}/
├── images/
│   └── potions/
│       ├── BlockPotion.png          # 药水本体图片
│       ├── BlockPotion_outline.png  # 轮廓图片（可选）
│       ├── HealingPotion.png
│       └── StrengthPotion.png
└── localization/
    ├── eng/
    │   └── potions.json             # 英文本地化
    └── zhs/
        └── potions.json             # 中文本地化
```

---

## 15. 参考已有药水实现

需要查找类似功能的药水时，在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 格挡药水 | `Models/Potions/` | `BlockPotion` |
| 攻击药水 | `Models/Potions/` | `AttackPotion` |
| 力量药水 | `Models/Potions/` | `StrengthPotion` |
| 敏捷药水 | `Models/Potions/` | `DexterityPotion` |
| 自动触发 | `Models/Potions/` | `FairyInABottle` (Usage.Automatic) |
| 治疗药水 | `Models/Potions/` | `BloodPotion` |
| 抽牌药水 | `Models/Potions/` | `SwiftPotion` |
| 能量药水 | `Models/Potions/` | `EnergyPotion` |
| 升级卡牌 | `Models/Potions/` | `BlessingOfTheForge` |
| 获得能力 | `Models/Potions/` | `StrengthPotion`, `DexterityPotion` |
| 全体伤害 | `Models/Potions/` | `FirePotion` |
| 易伤/虚弱 | `Models/Potions/` | `VulnerablePotion`, `WeakPotion` |
| Token 药水 | `Models/Potions/` | `PotionShapedRock` (Rarity: Token) |
| 衍生物池 | `PotionPools/` | `TokenPotionPool` |
| 事件药水池 | `PotionPools/` | `EventPotionPool` |

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Potions\` (约 45 个药水文件)

---

## 16. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 药水图片显示为空白 | 图片路径错误或缺失 | 检查 `AssetProfile` 中 `ImagePath` 和 `OutlinePath` 是否正确 |
| 描述显示原始键名 | 本地化 JSON 缺少对应条目 | 检查 `potions.json` 中键名是否为 `{MODID}_POTION_{CLASSNAME}.xxx` |
| 药水使用无效果 | `OnUse` 方法未正确重写 | 确认方法签名完全一致 `Task OnUse(PlayerChoiceContext, Creature?)` |
| 药水不在游戏中出现 | 未注册或注册失败 | 确认 `[RegisterPotion]` 存在且 `RegisterModAssembly` 已调用 |
| 药水可在非战斗中使用 | `Usage` 设置错误 | 将 `Usage` 改为 `PotionUsage.CombatOnly` |
| 药水不能选择目标 | `TargetType` 设置错误 | 根据需求设置正确的 `TargetType` |
| 目标为 null 时崩溃 | 未检查 target 参数 | `TargetType.Self` 时 target 为 null，需使用 `Owner.Creature` |
| `{Block}` 显示为 0 | `CanonicalVars` 中未定义对应变量 | 在 `CanonicalVars` 中添加 `BlockVar` |
| ExtraHoverTips 不显示 | 未重写 `ExtraHoverTips` 属性 | 添加 `HoverTipFactory.FromCard<T>()` 或 `FromPower<T>()` |
| 编译错误：找不到类型 | 缺少 using 引用 | 确认引用了 `STS2RitsuLib.Scaffolding.Content` 等命名空间 |
| 注册了正确的池却不出现 | 药水可能被 Epoch 锁定 | 检查药水池是否有 `GetUnlockedPotions` 限制 |

---

## 17. 调试技巧

### 17.1 控制台命令

在游戏中按 `~` 打开控制台：
- `potion <POTION_ID>` — 获取指定药水
- `potion list` — 查看所有可用药水（如果控制台支持）

### 17.2 常见检查步骤

1. 确认 Mod 编译成功且没有运行时错误
2. 确认 `Entry.Init()` 中调用了 `RegisterModAssembly`
3. 确认药水类有 `[RegisterPotion(typeof(XxxPool))]` 属性
4. 确认 `potions.json` 存在且格式正确
5. 确认药水图片文件存在于 `AssetProfile` 指定的路径

---

## 18. 编写审查清单

### 18.1 基础检查

- [ ] 是否继承了 `ModPotionTemplate`？
- [ ] 是否重写了 `Rarity` 属性？
- [ ] 是否重写了 `Usage` 属性？
- [ ] 是否重写了 `TargetType` 属性？
- [ ] 是否添加了 `[RegisterPotion(typeof(XxxPool))]` 属性？
- [ ] 命名空间是否正确？（`PersonalMod.PersonalModCode.Potions`）

### 18.2 数值检查

- [ ] `CanonicalVars` 中是否定义了所有需要的变量？
- [ ] 描述中的占位符名是否与变量名匹配？
- [ ] 数值是否合理平衡？

### 18.3 逻辑检查

- [ ] 是否重写了 `OnUse` 方法且使用了 `async Task`？
- [ ] 方法签名是否与基类完全一致？
- [ ] `TargetType.Self` 时是否正确处理了 `target` 为 null 的情况？
- [ ] `TargetType.AnyEnemy` 时是否调用了 `AssertValidForTargetedPotion`？
- [ ] 是否使用了正确的命令（`BlockCmd` / `PowerCmd` / `DamageCmd` / `CreatureCmd` 等）？
- [ ] Hook 回调中是否检查了 `Owner`？

### 18.4 资源检查

- [ ] `AssetProfile` 中的图片路径是否正确？
- [ ] 药水本体图片 PNG 文件是否存在于对应位置？
- [ ] 轮廓图片是否也存在？
- [ ] 文件名大小写是否与类名一致？

### 18.5 本地化检查

- [ ] `potions.json` 中是否添加了 `{MODID}_POTION_{CLASSNAME}.title`？
- [ ] `potions.json` 中是否添加了 `{MODID}_POTION_{CLASSNAME}.description`？
- [ ] 描述中的 BBCode 标签是否正确闭合？
- [ ] 描述中的动态变量占位符是否与 `CanonicalVars` 定义一致？

### 18.6 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 是否在 `Entry.Init()` 中调用？
- [ ] 药水池类型是否正确（`SharedPotionPool` 或角色专属池）？

---

## 19. 关于原版 PotionModel（非 RitsuLib）

如果直接继承原版 `PotionModel`（而非 `ModPotionTemplate`），需要手动处理：
- 自行实现 `ModelId` 和注册逻辑
- 自行管理本地化键
- 自行配置资源路径
- 自行实现药水在场景中的显示

**始终推荐继承 `ModPotionTemplate`**，RitsuLib 自动处理上述细节。

---

*最后更新：2026-05-12*
