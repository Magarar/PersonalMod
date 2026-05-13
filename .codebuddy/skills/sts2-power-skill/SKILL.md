---
name: sts2-power-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 能力 (Power) 提供全面的参考与自动检查。
  涵盖能力定义 (ModPowerTemplate)、动态变量 (DynamicVar)、能力生命周期 Hook 回调 (AfterCardDrawn/BeforeCombatStart 等)、
  数值修改器 (ModifyDamageAdditive/ModifyBlockAdditive/ModifyDamageMultiplicative 等)、
  资源配置 (PowerAssetProfile)、注册方式 ([RegisterPower])、本地化文本 (powers.json)、
  PowerType/PowerStackType 枚举速查、PowerCmd 使用、以及完整的代码模板与审查清单。
  当用户要求创建新能力、修改已有能力逻辑、或排查能力相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 能力编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 能力 (Power)，核心步骤：
1. 创建能力类，继承 `ModPowerTemplate`
2. 用 `[RegisterPower]` 属性注册
3. 重写 `Type` 属性（必须，Buff 或 Debuff）
4. 重写 `StackType` 属性（必须，Counter / Intensity / Duration）
5. 重写 `AssetProfile` 配置图标路径
6. 按需重写能力生命周期方法（Hook 回调 / Modify 修改器等）
7. 编写本地化 JSON（title + smartDescription）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-05-add-power/

---

## 2. Model ID 规则

RitsuLib 注册的能力 ID 格式：

```
<MODID>_POWER_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestPower` | `{{MODID_UPPER}}_POWER_TEST_POWER` |
| `StrengthPower` | `{{MODID_UPPER}}_POWER_STRENGTH_POWER` |
| `VulnerablePower` | `{{MODID_UPPER}}_POWER_VULNERABLE_POWER` |

本地化键必须使用此 ID：

```json
{
  "{{MODID_UPPER}}_POWER_TEST_POWER.title": "邪火",
  "{{MODID_UPPER}}_POWER_TEST_POWER.description": "每次抽牌时，获得一点[gold]力量[/gold]。",
  "{{MODID_UPPER}}_POWER_TEST_POWER.smartDescription": "每次抽牌时，获得[blue]{Amount}[/blue]点[gold]力量[/gold]。"
}
```

---

## 3. 基类: ModPowerTemplate

继承链: `ModPowerTemplate` → `PowerModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `Type` | `abstract PowerType` | 能力类型 (Buff / Debuff) |
| `StackType` | `abstract PowerStackType` | 叠加类型 (Counter / Intensity / Duration) |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `PowerAssetProfile` | — | 图标路径配置 |
| `CanonicalVars` | `IEnumerable<DynamicVar>` | 空列表 | 能力数值变量 |
| `AllowNegative` | `bool` | `false` | 是否允许负值 |

### 3.3 重写必要的 Hook 回调

根据能力效果选择对应的 Hook 方法重写。

---

## 4. 枚举速查

### 4.1 PowerType

```csharp
PowerType.Buff    // 正面效果（绿色边框）
PowerType.Debuff  // 负面效果（红色边框）
```

### 4.2 PowerStackType

```csharp
PowerStackType.Counter    // 数值叠加（如 Strength, Vulnerable）—— 层数可增减，最常用
PowerStackType.Intensity  // 强度叠加（如 Metallicize）—— 层数固定，每回合变化
PowerStackType.Duration   // 持续回合（如 Buffer, Dexterity）—— 层数每回合递减
```

### 4.3 叠加类型行为解释

| 类型 | Amount 含义 | 回合变化 | 典型例子 |
|------|------------|---------|---------|
| `Counter` | 当前层数 | 不变 | 力量(Strength)、易伤(Vulnerable)、虚弱(Weak) |
| `Intensity` | 强度值 | 不变 | 金属化(Metallicize)、残影(Blur) |
| `Duration` | 剩余回合数 | 每回合 -1 | 缓冲(Buffer)、人工制品(Artifact) |

---

## 5. PowerModel 完整属性

从基类 `PowerModel` 继承的属性和方法：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Amount` | `int` | 能力层数/数值 |
| `AmountOnTurnStart` | `int` | 回合开始时的数值 |
| `Owner` | `Creature` | 拥有此能力的生物 |
| `Target` | `Creature` | 目标生物（用于指向性能力） |
| `Applier` | `Creature` | 施加者 |
| `CombatState` | `ICombatState` | 当前战斗状态 |
| `Title` | `LocString` | 标题本地化 (`powers/{Entry}.title`) |
| `Description` | `LocString` | 描述本地化 (`powers/{Entry}.description`) |
| `DynamicVars` | `DynamicVarSet` | 动态变量集合 |
| `IsMutable` | `bool` | 是否为可变副本 |
| `IsCanonical` | `bool` | 是否为规范实例 |

---

## 6. 注册方式

### 6.1 属性注册（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

[RegisterPower]
public class TestPower : ModPowerTemplate
{
    // ...
}
```

前提：在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

### 6.2 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Power<TestPower>()
    .Apply();
```

### 6.3 Manifest 注册

```csharp
new PowerRegistrationEntry<TestPower>()
```

---

## 7. 动态变量 (DynamicVar)

能力通过 `CanonicalVars` 定义数值变量，用于智能描述中的占位符。

### 7.1 定义变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => [
    new MagicNumberVar(3),   // 通用数值
];
```

### 7.2 能力专用变量

能力场景下，原版 `PowerModel` 的智能描述使用 `{Amount}` 占位符直接显示当前层数，见本地化章节。

### 7.3 运行时读取 Amount

```csharp
// Amount 是能力层数，可直接在 Hook 回调中使用
int currentStacks = Amount;  // 当前能力层数
```

---

## 8. Hook 回调方法 — 事件通知

能力通过重写基类 `AbstractModel` 中的虚方法实现。所有 Hook 返回 `Task`，可使用 `async/await`。

> **重要**: 以下签名来自 `AbstractModel` 基类，能力直接 override 这些方法即可。方法签名必须与基类**完全一致**。

### 8.1 生命周期与回合

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterActEntered` | `Task AfterActEntered()` | 进入新幕后 |
| `BeforeCombatStart` | `Task BeforeCombatStart()` | 战斗开始前 |
| `AfterCombatEnd` | `Task AfterCombatEnd(CombatRoom room)` | 战斗结束后（无论胜负） |
| `AfterCombatVictory` | `Task AfterCombatVictory(CombatRoom room)` | 战斗胜利后 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, ICombatState state)` | 任意方回合开始前 |
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide side, ICombatState state)` | 任意方回合开始后 |
| `AfterPlayerTurnStart` | `Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)` | 玩家回合开始后 |
| `AfterPlayerTurnStartLate` | `Task AfterPlayerTurnStartLate(PlayerChoiceContext ctx, Player player)` | 玩家回合开始后（晚阶段） |
| `BeforeTurnEnd` | `Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)` | 回合结束前 |
| `AfterTurnEnd` | `Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)` | 回合结束后 |

### 8.2 卡牌相关

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterCardDrawn` | `Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)` | 卡牌被抽到后 |
| `BeforeCardPlayed` | `Task BeforeCardPlayed(CardPlay cardPlay)` | 卡牌打出前 |
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)` | 卡牌打出后 |
| `AfterCardDiscarded` | `Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)` | 卡牌被丢弃后 |
| `AfterCardExhausted` | `Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)` | 卡牌被消耗后 |
| `AfterCardChangedPiles` | `Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)` | 卡牌切换牌堆后 |
| `AfterCardEnteredCombat` | `Task AfterCardEnteredCombat(CardModel card)` | 卡牌进入战斗后 |
| `AfterCardGeneratedForCombat` | `Task AfterCardGeneratedForCombat(CardModel card, Player? creator)` | 卡牌为战斗生成后 |

### 8.3 伤害与攻击

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeAttack` | `Task BeforeAttack(AttackCommand command)` | 攻击前 |
| `AfterAttack` | `Task AfterAttack(PlayerChoiceContext ctx, AttackCommand command)` | 攻击后 |
| `AfterDamageGiven` | `Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)` | 造成伤害后 |
| `BeforeDamageReceived` | `Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 受到伤害前 |
| `AfterDamageReceived` | `Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)` | 受到伤害后 |

### 8.4 应用/移除回调（能力专属）

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeApplied` | `Task BeforeApplied()` | 能力被应用前 |
| `AfterApplied` | `Task AfterApplied()` | 能力被应用后 |
| `AfterRemoved` | `Task AfterRemoved()` | 能力被移除后 |

### 8.5 其他常用

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterOrbChanneled` | `Task AfterOrbChanneled(PlayerChoiceContext ctx, Player player, OrbModel orb)` | 法球被引导后 |
| `AfterOrbEvoked` | `Task AfterOrbEvoked(PlayerChoiceContext ctx, OrbModel orb, IEnumerable<Creature> targets)` | 法球被激发后 |
| `AfterShuffle` | `Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)` | 洗牌后 |
| `AfterGoldGained` | `Task AfterGoldGained(Player player)` | 获得金币后 |
| `AfterPowerAmountChanged` | `Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)` | 力量值改变后 |

---

## 9. Modify 修改器方法 — 数值修改

能力通过这些方法**返回修改后的值**来影响游戏数值。默认返回 `0m`（加法）或 `1m`（乘法）。

### 9.1 伤害修改器（能力核心用途）

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `0m` | 伤害加法修正（如 StrengthPower 返回 `Amount`） |
| `ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `1m` | 伤害乘法修正（如 Vulnerable 返回 `1.5m`） |
| `ModifyDamageCap(Creature target, ValueProp props, Creature dealer, CardModel cardSource)` | `decimal.MaxValue` | 伤害上限 |

### 9.2 格挡修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyBlockAdditive(Creature? target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay)` | `0m` | 格挡加法修正（如 DexterityPower 返回 `Amount`） |
| `ModifyBlockMultiplicative(Creature? target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay)` | `1m` | 格挡乘法修正 |

### 9.3 HP 相关修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyHpLostBeforeOsty(Creature? target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `amount` | Osty 触发前 HP 损失修改 |
| `ModifyHpLostAfterOsty(...)` | `amount` | Osty 触发后 HP 损失修改 |

### 9.4 抽牌/能量/球体修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyHandDraw(Player player, decimal count)` | `count` | 抽牌数修改 |
| `ModifyEnergyGain(Player player, decimal amount)` | `amount` | 能量获取修改 |
| `ModifyMaxEnergy(Player player, decimal amount)` | `amount` | 最大能量修改 |
| `ModifyOrbValue(OrbModel orb, decimal value)` | `value` | 球体数值修改 |
| `ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount)` | `triggerCount` | 球体被动触发次数 |

### 9.5 卡牌相关修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyCardPlayCount(CardModel card, Creature? target, int playCount)` | `playCount` | 卡牌打出次数修改 |
| `ModifyXValue(CardModel card, int originalValue)` | `originalValue` | 修改 X 卡牌的 X 值 |

### 9.6 关于 Owner 检查

Modify 方法中必须主动检查 `Owner`，因为方法是全局调用的：

```csharp
public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
{
    if (Owner != dealer) return 0m;           // 确保只影响自己的持有者
    if (!props.IsPoweredAttack()) return 0m;  // 检查是否为力量攻击
    return Amount;                             // 返回能力层数作为伤害加成
}
```

---

## 10. PowerCmd — 能力命令

命名空间: `MegaCrit.Sts2.Core.Commands`

### 10.1 施加能力

```csharp
// 给 Owner 施加能力（RitsuLib 框架）
await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);

// 带 choiceContext 的重载（测试版 API）
await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
```

参数说明：
- 泛型 `<TPower>`：能力类型
- `owner`：拥有者（被施加者）
- `amount`：层数
- `applier`：施加者（通常是 Owner 或卡牌打出者）
- `cardSource`：卡牌来源（可选，null 表示非卡牌来源）

### 10.2 检查能力存在

```csharp
// 检查生物是否拥有某能力
bool hasStrength = creature.Powers.Any(p => p is StrengthPower);

// 获取能力
var strength = creature.Powers.OfType<StrengthPower>().FirstOrDefault();
if (strength != null)
{
    int strengthAmount = strength.Amount;
}
```

### 10.3 移除能力

```csharp
// 通过 PowerModel 本身的 AfterRemoved 回调或 Hook 处理移除逻辑
```

---

## 11. 生命周期回调: BeforeApplied / AfterApplied / AfterRemoved

能力特有的生命周期回调，在能力被应用或移除时触发：

```csharp
[RegisterPower]
public class RitualPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterApplied()
    {
        // 能力被施加后执行初始化逻辑
        // 如播放特效、设置状态等
    }

    public override async Task AfterRemoved()
    {
        // 能力被移除时的清理逻辑
        // 如清除特效等
    }
}
```

---

## 12. 资源配置 (PowerAssetProfile)

### 12.1 基本配置

```csharp
public override PowerAssetProfile AssetProfile => new(
    IconPath: "res://{{MODID}}/images/powers/test_power.png",     // 小图标
    BigIconPath: "res://{{MODID}}/images/powers/test_power.png"   // 大图标
);
```

### 12.2 图片尺寸参考

| 路径 | 推荐尺寸 | 说明 |
|------|---------|------|
| `IconPath` | 64x64 | 小图标（战斗中能力栏显示） |
| `BigIconPath` | 256x256 | 大图标（详情/悬浮显示） |

原版游戏：小图 64x64，大图 256x256。1:1 比例即可。

### 12.3 图片文件位置

```
{{MODID}}/{{MODID}}/images/powers/
├── test_power.png              # 图标（可复用为两种图标）
├── big/
│   └── test_power.png          # 大图标（推荐分开）
```

### 12.4 原版能力资源路径约定

原版能力资源路径（Mod 不需要遵循，但可参考）：

| 资源 | 路径 |
|------|------|
| 图集 | `atlases/power_atlas.sprites/{entry}.tres` |
| 大图标 | `powers/{entry}.png` |

`entry` = `Entry.ToLowerInvariant()`

---

## 13. 本地化

### 13.1 文件位置

```
{{MODID}}/{{MODID}}/localization/eng/powers.json
{{MODID}}/{{MODID}}/localization/zhs/powers.json
```

### 13.2 格式

```json
{
    "{{MODID_UPPER}}_POWER_TEST_POWER.title": "邪火",
    "{{MODID_UPPER}}_POWER_TEST_POWER.description": "每次抽牌时，获得一点[gold]力量[/gold]。",
    "{{MODID_UPPER}}_POWER_TEST_POWER.smartDescription": "每次抽牌时，获得[blue]{Amount}[/blue]点[gold]力量[/gold]。"
}
```

### 13.3 能力本地化三个字段

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 能力名称 | 是 |
| `description` | 效果描述（静态文本） | 推荐 |
| `smartDescription` | 智能描述（支持 `{Amount}` 动态显示层数） | 推荐 |

### 13.4 smartDescription 占位符

| 占位符 | 说明 |
|--------|------|
| `{Amount}` | 显示当前能力层数（最常用） |
| `{Amount:diff()}` | 显示能力变化带来的数值差异 |

### 13.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮（用于关键词） |
| `[blue]文字[/blue]` | 蓝色（用于数值） |
| `[b]文字[/b]` | 加粗 |
| `[purple]文字[/purple]` | 紫色 |

---

## 14. 完整代码模板

### 14.1 回合结束触发效果（Hook 模式）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Powers;

[RegisterPower]
public class EndOfTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/powers/end_of_turn_power.png",
        BigIconPath: "res://{{MODID}}/images/powers/end_of_turn_power.png"
    );

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;               // 只关心玩家回合
        if (choiceContext.CurrentActor != Owner) return;     // 只影响能力的持有者

        await BlockCmd.GainBlock(Amount).FromPower(this).Execute(choiceContext);
    }
}
```

### 14.2 抽牌触发效果（最简模板）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Powers;

[RegisterPower]
public class TestPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/powers/test_power.png",
        BigIconPath: "res://{{MODID}}/images/powers/test_power.png"
    );

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
    }
}
```

### 14.3 力量修改器能力（Modify 模式）

```csharp
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Powers;

/// <summary>
/// 自定义力量能力 — 每层 +X 伤害
/// </summary>
[RegisterPower]
public class CustomStrengthPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/powers/custom_strength.png",
        BigIconPath: "res://{{MODID}}/images/powers/custom_strength.png"
    );

    public override decimal ModifyDamageAdditive(
        Creature target, decimal amount, ValueProp props,
        Creature dealer, CardModel cardSource)
    {
        if (Owner != dealer) return 0m;
        if (!props.IsPoweredAttack()) return 0m;
        return Amount; // 每层 Amont 点伤害加成
    }
}
```

### 14.4 格挡修改器能力（Modify 模式）

```csharp
[RegisterPower]
public class CustomDexterityPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override decimal ModifyBlockAdditive(
        Creature? target, decimal block, ValueProp props,
        CardModel cardSource, CardPlay cardPlay)
    {
        if (Owner != target) return 0m;
        return Amount; // 每层 Amount 点格挡加成
    }
}
```

### 14.5 伤害倍率能力（Modify 乘法模式）

```csharp
[RegisterPower]
public class VulnerablePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature target, decimal amount, ValueProp props,
        Creature dealer, CardModel cardSource)
    {
        if (Owner != target) return 1m;  // 检查是否是自己受到伤害
        return 1.5m;                      // 易伤：受到 1.5 倍伤害
    }
}
```

### 14.6 回合开始层数递减（Duration 模式）

```csharp
[RegisterPower]
public class BufferPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Duration;

    // Duration 类型的能力，层数每回合战斗结束时自动 -1
    // 无需额外写代码，框架自动处理递减
}
```

### 14.7 使用抽象基类统一管理（推荐）

```csharp
[RegisterPower]
public abstract class {{MODID}}PowerModel : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new(
    IconPath: $"res://{{MODID}}/images/powers/{GetType().Name}.png",
    BigIconPath: $"res://{{MODID}}/images/powers/{GetType().Name}.png"
    );
}

// 子类只需关注逻辑
[RegisterPower]
public class DrawStrengthPower : {{MODID}}PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        await PowerCmd.Apply<StrengthPower>(Owner, 1, Owner, null);
    }
}
```

### 14.8 最简能力模板（快速起步）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Powers;

[RegisterPower]
public class MyPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
```

> 注意：最简模板缺少图标配置和效果回调，仅用于快速验证注册是否成功。正式能力需补充 `AssetProfile` 和至少一个 Hook/Modify 方法。

---

## 15. 能力效果实现模式总结

### 15.1 Hook 模式 — 事件触发后执行逻辑

适用场景：抽牌后、回合结束时、伤害后等"某事后做某事"。

```csharp
// 例：抽牌后获得力量
public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
{
    await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
}
```

### 15.2 Modify 模式 — 修改数值

适用场景：增加伤害、增加格挡等"被动数值加成"。

```csharp
// 例：力量加成
public override decimal ModifyDamageAdditive(...)
{
    if (Owner != dealer) return 0m;
    return Amount;
}
```

### 15.3 Duration 模式 — 持续回合数

适用场景：回合数限制的效果，能力层数每回合自动 -1。

```csharp
// StackType = Duration 时，层数每回合递减由框架自动处理
```

### 15.4 综合模式 — 多种效果组合

能力可以同时实现 Hook 和 Modify，组合多种效果。

---

## 16. 控制台调试命令

在游戏中按 `~` 打开控制台：

```
power {{MODID_UPPER}}_POWER_TEST_POWER 5 0
```

参数说明：
- `power <能力ID> <层数> <目标>` — 给予玩家指定能力
- `<能力ID>` 为完整的 ModelId.Entry（如 `{{MODID_UPPER}}_POWER_TEST_POWER`）
- `<层数>` 为能力层数（如 `5`）
- `<目标>` 为 0（玩家）/ 1（第一个敌人）等

快速检查能力是否注册成功：在控制台尝试给予该能力。

---

## 17. 文件组织

```
{{MODID}}/{{MODID}}Code/Powers/
├── {{MODID}}PowerModel.cs           # 抽象基类（可选）
├── TestPower.cs                    # 抽牌触发力量
├── EndOfTurnPower.cs               # 回合结束格挡
└── CustomStrengthPower.cs          # 自定义力量

{{MODID}}/{{MODID}}/
├── images/
│   └── powers/
│       ├── test_power.png          # 图标
│       ├── end_of_turn_power.png
│       └── big/                    # 大图标（可选）
│           ├── test_power.png
│           └── end_of_turn_power.png
└── localization/
    ├── eng/
    │   └── powers.json             # 英文本地化
    └── zhs/
        └── powers.json             # 中文本地化
```

---

## 18. 参考已有能力实现

需要查找类似功能的能力时，在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 力量加成 | `{{STS2_SOURCE_ROOT}}\Models\Powers\` | `StrengthPower` |
| 敏捷加成 | 同上 | `DexterityPower` |
| 易伤 | 同上 | `VulnerablePower` |
| 虚弱 | 同上 | `WeakPower` |
| 格挡加层 | 同上 | `MetallicizePower` |
| 回合触发 | 同上 | 搜索 `AfterTurnEnd` |
| 抽牌触发 | 同上 | 搜索 `AfterCardDrawn` |
| 伤害倍率 | 同上 | 搜索 `ModifyDamageMultiplicative` |
| 伤害减免 | 同上 | 搜索 `ModifyDamageAdditive` |
| 持续回合 | 同上 | 搜索 `Duration` |
| 残影(Blur) | 同上 | `BlurPower` |
| 人工制品 | 同上 | `ArtifactPower` |

**源码位置**: `{{STS2_SOURCE_ROOT}}\Models\Powers\` (约 260 个能力文件)

---

## 19. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 能力图标显示为空白 | 图标路径错误或缺失 | 检查 `AssetProfile` 中的路径和文件是否存在 |
| 描述显示原始键名 | 本地化 JSON 缺少对应条目 | 检查 powers.json 中键名是否为 `{MODID}_POWER_{CLASSNAME}.xxx` |
| 能力效果不触发 | Hook 方法签名不匹配 | 确认方法签名与基类完全一致（参数类型、返回类型、大小写） |
| 能力不在游戏中出现 | 未注册 | 确认 `[RegisterPower]` 存在且 `RegisterModAssembly` 已调用 |
| 能力被应用后无效 | Modify 方法未检查 `Owner` | 在 Modify 方法中检查 `Owner != dealer` 时返回默认值（`0m`/`1m`） |
| 能力影响双方 | Modify 方法未限制目标 | 在 Modify 方法中只对 `Owner` 生效 |
| 层数不递减 | 未设置 `Duration` | Duration 类型自动递减，Counter/Intensity 不会 |
| `{Amount}` 显示为 0 | 未设置层数 | 检查 `PowerCmd.Apply<>` 中的 amount 参数是否正确 |
| 编译错误：找不到类型 | 缺少 using 引用 | 确认引用了 `STS2RitsuLib.Scaffolding.Content` 等命名空间 |
| StackType 行为不符合预期 | 对叠加类型理解有误 | 确认 Counter / Intensity / Duration 的区别 |

---

## 20. 编写审查清单

### 20.1 基础检查

- [ ] 是否继承了 `ModPowerTemplate`？
- [ ] 是否重写了 `Type` 属性（Buff / Debuff）？
- [ ] 是否重写了 `StackType` 属性（Counter / Intensity / Duration）？
- [ ] 是否添加了 `[RegisterPower]` 属性？
- [ ] 命名空间是否正确？（`{{MODID}}.{{MODID}}Code.Powers`）

### 20.2 数值检查

- [ ] `AllowNegative` 是否设置正确？
- [ ] 数值是否合理平衡？
- [ ] Duration 类型的能力，层数是否代表剩余回合数？

### 20.3 逻辑检查

- [ ] Hook 回调方法签名是否与基类虚方法**完全一致**？
- [ ] 是否正确使用 `async/await`？
- [ ] Hook 方法中是否检查 `Owner`（只处理属于自己的触发）？
- [ ] Modify 方法中是否检查 `Owner` 和 `ValueProp`？
- [ ] 是否使用了正确的 `PowerCmd.Apply` 重载？

### 20.4 资源检查

- [ ] `AssetProfile` 中的图标路径是否正确？
- [ ] 图标 PNG 文件是否存在于对应位置？
- [ ] 文件名大小写是否与类名一致？
- [ ] 图标尺寸是否符合要求（64x64 / 256x256）？

### 20.5 本地化检查

- [ ] `powers.json` 中是否添加了 `{MODID}_POWER_{CLASSNAME}.title`？
- [ ] `powers.json` 中是否添加了 `{MODID}_POWER_{CLASSNAME}.smartDescription`？
- [ ] `smartDescription` 中是否使用了 `{Amount}` 占位符？
- [ ] 描述中的 BBCode 标签是否正确闭合？

### 20.6 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 是否在 `Entry.Init()` 中调用？

---

## 21. Hook 系统速查（能力相关）

能力中的生命周期方法本质上是对 `AbstractModel` 虚方法的重写。直接 override 即可。

在非能力场景下需要监听事件时，使用 `Hook` 静态类：

```csharp
// 在其他组件中使用 Hook
Hook.AfterCardPlayed += OnCardPlayed;
```

高级用法可参考 `sts2-core-ref` Skill 的 `references/hooks-reference.md`。

---

## 22. 关于原版 PowerModel（非 RitsuLib）

如果直接继承原版 `PowerModel`（而非 `ModPowerTemplate`），需要手动处理：
- 自行实现 `ModelId` 和注册逻辑
- 自行管理本地化键
- 自行配置资源路径

**始终推荐继承 `ModPowerTemplate`**，RitsuLib 自动处理上述细节。

---

*最后更新：2026-05-12*
