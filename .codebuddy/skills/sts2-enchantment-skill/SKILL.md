---
name: sts2-enchantment-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 附魔 (Enchantment) 提供全面的参考与自动检查。
  涵盖附魔定义 (ModEnchantmentTemplate)、附魔数值修改器 (EnchantDamageAdditive / EnchantBlockAdditive / EnchantPlayCount)、
  附魔生命周期回调 (OnPlay / OnEnchant / AfterCardPlayed / RecalculateValues)、
  附魔状态 (EnchantmentStatus / ShowAmount / DisplayAmount)、
  可附魔性判断 (CanEnchant / CanEnchantCardType)、
  资源配置 (图标路径约定)、注册方式 ([RegisterEnchantment])、
  本地化文本 (enchantments.json / title / description / extraCardText)、
  以及完整的代码模板与审查清单。
  当用户要求创建新附魔、修改附魔效果逻辑、或排查附魔相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 附魔 (Enchantment) 编写 Skill (RitsuLib)

## 1. 概述

附魔 (Enchantment) 是杀戮尖塔2 中附着在卡牌上的额外效果，可以修改卡牌的伤害、格挡、打出次数等数值，或在卡牌打出时触发额外逻辑。

在 RitsuLib 框架中编写 STS2 Mod 附魔，核心步骤：
1. 创建附魔类，继承 `ModEnchantmentTemplate`
2. 用 `[RegisterEnchantment]` 属性注册
3. 按需重写数值修改器（`EnchantDamageAdditive`、`EnchantBlockAdditive`、`EnchantPlayCount` 等）
4. 按需重写生命周期回调（`OnPlay`、`OnEnchant`、`AfterCardPlayed`、`RecalculateValues` 等）
5. 重写 `CanonicalVars` 定义动态变量
6. 重写 `CanEnchantCardType` 或 `CanEnchant` 限制可附魔的卡牌类型（可选）
7. 编写本地化 JSON（enchantments.json）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

---

## 2. Model ID 规则

RitsuLib 注册的附魔 ID 格式：

```
<MODID>_ENCHANTMENT_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `AdroitEnchant` | `{{MODID_UPPER}}_ENCHANTMENT_ADROIT_ENCHANT` |
| `SharpEnchant` | `{{MODID_UPPER}}_ENCHANTMENT_SHARP_ENCHANT` |

本地化键必须使用此 ID：

```json
{
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.title": "灵巧",
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.description": "获得 {Block} 点格挡。",
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.extraCardText": "获得 {Amount} 点格挡。"
}
```

---

## 3. 基类: ModEnchantmentTemplate

继承链: `ModEnchantmentTemplate` → `EnchantmentModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 推荐重写的属性

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CanonicalVars` | `protected virtual IEnumerable<DynamicVar>` | 空数组 | 动态变量 |
| `ShowAmount` | `virtual bool` | `false` | 是否在附魔图标上显示数值 |
| `DisplayAmount` | `virtual int` | `Amount` | 图标上显示的数值 |
| `HasExtraCardText` | `virtual bool` | `false` | 是否在卡牌描述中添加额外文本 |
| `IsStackable` | `virtual bool` | `false` | 是否可堆叠在已有附魔上 |
| `ShouldGlowGold` | `virtual bool` | `false` | 是否金色发光 |
| `ShouldGlowRed` | `virtual bool` | `false` | 是否红色发光 |
| `ShouldStartAtBottomOfDrawPile` | `virtual bool` | `false` | 附魔卡牌是否在抽牌堆底部 |
| `ExtraHoverTips` | `protected virtual IEnumerable<IHoverTip>` | 空数组 | 额外悬停提示 |

### 3.2 EnchantmentModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题（`enchantments/{Entry}.title`） |
| `Amount` | `int` | 附魔数值 |
| `Status` | `EnchantmentStatus` | 附魔状态（`Normal` / `Disabled`） |
| `Card` | `CardModel` | 附魔所在的卡牌 |
| `HasCard` | `bool` | 是否有绑定的卡牌 |
| `DynamicVars` | `DynamicVarSet` | 动态变量集合 |
| `Icon` | `CompressedTexture2D` | 附魔图标 |
| `IconPath` | `string` | 图标路径（自动回退） |
| `ShowAmount` | `bool` | 是否显示数值 |
| `DisplayAmount` | `int` | 显示数值 |
| `HasExtraCardText` | `bool` | 是否有额外卡牌文本 |
| `IsStackable` | `bool` | 是否可堆叠 |
| `HoverTip` | `HoverTip` | 悬停提示 |
| `StatusChanged` | `event Action` | 状态变更事件 |

### 3.3 核心方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `OnPlay` | `Task OnPlay(PlayerChoiceContext, CardPlay?)` | **卡牌打出时触发**（核心效果回调） |
| `OnEnchant` | `virtual void OnEnchant()` | **附魔被施加时调用**（可用于添加关键词等） |
| `RecalculateValues` | `virtual void RecalculateValues()` | **数值重算**（当卡牌数值变化时同步附魔数值） |
| `CanEnchant(CardModel)` | `virtual bool` | 判断是否能附魔到指定卡牌 |
| `CanEnchantCardType(CardType)` | `virtual bool` | 判断是否能附魔到指定类型的卡牌 |

---

## 4. 数值修改器方法

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `EnchantDamageAdditive(decimal originalDamage, ValueProp props)` | `0m` | 伤害加法修正 |
| `EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)` | `1m` | 伤害乘法修正 |
| `EnchantBlockAdditive(decimal originalBlock, ValueProp props)` | `0m` | 格挡加法修正 |
| `EnchantBlockMultiplicative(decimal originalBlock, ValueProp props)` | `1m` | 格挡乘法修正 |
| `EnchantPlayCount(int originalPlayCount)` | `originalPlayCount` | 打出次数修正（如 Glam 附魔增加打出次数） |

### 4.1 使用示例

```csharp
// 伤害 +X（力量攻击）
public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
{
    if (!props.IsPoweredAttack()) return 0m;
    return Amount; // 附魔层数作为伤害加成
}

// 格挡 +Amount（受到力量卡或怪物格挡影响）
public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
{
    if (!props.IsPoweredCardOrMonsterMoveBlock()) return 0m;
    return Amount - 1;
}

// 增加打出次数
public override int EnchantPlayCount(int originalPlayCount)
{
    return originalPlayCount + DynamicVars["Times"].IntValue;
}
```

---

## 5. 生命周期回调

### 5.1 OnPlay — 卡牌打出时触发

当附魔卡牌被打出时触发。这是附魔最常用的效果回调。

```csharp
public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
{
    // 卡牌打出时额外效果
    await BlockCmd.GainBlock(Amount)
        .FromCard(Card)
        .Execute(choiceContext);
}
```

### 5.2 OnEnchant — 附魔施加时触发

附魔被施加到卡牌上时调用，通常用于添加关键词。

```csharp
protected override void OnEnchant()
{
    Card.AddKeyword(CardKeyword.Exhaust);  // 附魔后使卡牌获得"消耗"
}
```

### 5.3 AfterCardPlayed — 卡牌打出后触发

继承自 `AbstractModel` 的 Hook 回调，可以监听任意卡牌的打出。

```csharp
public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
{
    if (cardPlay.Card != Card) return;  // 只关心自己的卡牌
    Amount++;                            // 每打出一次 +1
}
```

### 5.4 RecalculateValues — 数值重算

当卡牌升级或附魔时重新计算动态变量的实际值。

```csharp
public override void RecalculateValues()
{
    DynamicVars.Block.BaseValue = Amount;  // 同步 BlockVar 与 Amount
}
```

---

## 6. 附魔状态 (EnchantmentStatus)

```csharp
EnchantmentStatus.Normal     // 正常
EnchantmentStatus.Disabled   // 禁用（附魔效果不生效）
```

通过修改 `Status` 属性可以启用/禁用附魔效果：

```csharp
// 禁用附魔（如 Glam 附魔使用一次后禁用）
Status = EnchantmentStatus.Disabled;
```

---

## 7. 可附魔性判断

### 7.1 CanEnchantCardType — 按卡牌类型过滤

```csharp
// 只允许附魔到攻击牌
public override bool CanEnchantCardType(CardType cardType)
{
    return cardType == CardType.Attack;
}
```

### 7.2 CanEnchant — 完整判断

```csharp
// 只附魔到带 Defend 标签的卡牌
public override bool CanEnchant(CardModel card)
{
    return base.CanEnchant(card) && card.Tags.Contains(CardTag.Defend);
}
```

默认 `CanEnchant` 逻辑：
- 不能附魔到 `Status`/`Curse`/`Quest` 类型
- 不能附魔到不满足 `CanEnchantCardType` 的类型
- 不能附魔到牌组中不可打出的卡牌
- 卡牌已有附魔时，除非 `IsStackable` 为 true 且类型相同，否则不能重复附魔

---

## 8. 图标资源

### 8.1 图标路径

附魔图标通过资源查找自动定位，优先级：

```
enchantments/{entry}.png           # 首选图标
enchantments/beta/{entry}.png      # Beta 图标（回退）
enchantments/missing_enchantment.png  # 缺失图标（最终回退）
```

Mod 附魔的图标应放入：

```
PersonalMod/PersonalMod/images/enchantments/{entry}.png
```

### 8.2 原版附魔图标参考

原版附魔图标位于 `D:\杀戮尖塔2Mod\sts2\steam\images\enchantments\`：
adroit, clone, corrupted, favored, glam, goopy, imbued, inky, instinct, momentum, nimble, perfect_fit, royally_approved, sharp, slither, slumbering_essence, souls_power, sown, spiral, steady, swift, tezcataras_ember, vigorous 等 24 个。

---

## 9. 注册方式

### 9.1 属性注册（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

[RegisterEnchantment]
public class SharpEnchant : ModEnchantmentTemplate { ... }
```

前提：在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

### 9.2 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Enchantment<SharpEnchant>()
    .Apply();
```

---

## 10. 动态变量

附魔中的动态变量与卡牌类似，支持以下常用类型：

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars =>
[
    new BlockVar(0, ValueProp.Move),       // 格挡值
    new DamageVar(1, ValueProp.Move),      // 伤害值
    new PowerVar<WeakPower>(1),            // 能力层数
    new DynamicVar("Times", 1),            // 自定义变量
];
```

### 10.1 与 Amount 同步

附魔的 `Amount` 常在附魔施加时由外部决定。在 `RecalculateValues` 中将其同步到动态变量：

```csharp
public override void RecalculateValues()
{
    DynamicVars.Block.BaseValue = Amount;  // BlockVar 的值跟随 Amount
}
```

---

## 11. 本地化

### 11.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/enchantments.json
PersonalMod/PersonalMod/localization/zhs/enchantments.json
```

### 11.2 格式

```json
{
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.title": "灵巧",
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.description": "获得 {Block} 点格挡。",
  "PERSONAL_MOD_ENCHANTMENT_ADROIT_ENCHANT.extraCardText": "获得 {Amount} 点格挡。"
}
```

### 11.3 字段说明

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 附魔名称 | 是 |
| `description` | 在悬停/附魔选择界面显示的描述 | 推荐 |
| `extraCardText` | 在卡牌描述末尾追加的文本（需 `HasExtraCardText = true`） | 可选 |

### 11.4 描述占位符

| 占位符 | 说明 |
|--------|------|
| `{Amount}` | 附魔数值（在卡牌上显示时自动替换为当前值） |
| `{Block}` | BlockVar 值 |
| `{Damage}` | DamageVar 值 |

### 11.5 extraCardText 特殊占位符

`extraCardText` 中除了 `{Amount}` 外，还支持 `{TargetType}` 用于显示当前卡牌的目标类型（仅在运行时有效）。

---

## 12. 完整代码模板

### 12.1 增加格挡附魔（Adroit 模式）

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Enchantments;

[RegisterEnchantment]
public class AdroitEnchant : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;  // 在卡牌描述中追加文本
    public override bool ShowAmount => true;          // 图标上显示数值

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(0, ValueProp.Move)];

    // 卡牌打出时获得格挡
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await BlockCmd.GainBlock(Amount)
            .FromCard(Card)
            .Execute(choiceContext);
    }

    // 同步 Amount 到 BlockVar
    public override void RecalculateValues()
    {
        DynamicVars.Block.BaseValue = Amount;
    }
}
```

### 12.2 增加伤害 + 施加减益附魔（Inky 模式）

```csharp
[RegisterEnchantment]
public class InkyEnchant : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1, ValueProp.Move),
        new PowerVar<WeakPower>(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    // 伤害 +1
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack()) return 0m;
        return DynamicVars.Damage.BaseValue;
    }

    // 卡牌打出时施加虚弱
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(
                cardPlay.Target,
                (int)DynamicVars.WeakPower.BaseValue,
                cardPlay.Card?.Owner.Creature,
                Card
            );
        }
    }
}
```

### 12.3 增加打出次数附魔（Glam 模式）

```csharp
[RegisterEnchantment]
public class GlamEnchant : ModEnchantmentTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Times", 1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayDynamic,
            [DynamicVars["Times"]])];

    private bool _usedThisCombat;

    // 增加打出次数
    public override int EnchantPlayCount(int originalPlayCount)
    {
        if (_usedThisCombat) return originalPlayCount;
        return originalPlayCount + DynamicVars["Times"].IntValue;
    }

    // 打出后禁用
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (_usedThisCombat) return;
        if (cardPlay.Card != Card) return;
        _usedThisCombat = true;
        Status = EnchantmentStatus.Disabled;
    }
}
```

### 12.4 添加关键词附魔（Goopy 模式）

```csharp
[RegisterEnchantment]
public class GoopyEnchant : ModEnchantmentTemplate
{
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    // 只附魔到带 Defend 标签的牌
    public override bool CanEnchant(CardModel card) =>
        base.CanEnchant(card) && card.Tags.Contains(CardTag.Defend);

    // 附魔时添加"消耗"关键词
    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Exhaust);
    }

    // 每打出一次 +1 格挡
    public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
    {
        if (!props.IsPoweredCardOrMonsterMoveBlock()) return 0m;
        return Amount - 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card) return;
        Amount++;
        if (Card.DeckVersion?.Enchantment != null)
            Card.DeckVersion.Enchantment.Amount = Amount;
    }
}
```

### 12.5 最简附魔模板

```csharp
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Enchantments;

[RegisterEnchantment]
public class MyEnchant : ModEnchantmentTemplate
{
    // 卡牌打出时额外抽牌
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 1, Card.Owner);
    }
}
```

---

## 13. 文件组织

```
{{MODID}}/{{MODID}}Code/Enchantments/
├── AdroitEnchant.cs                  # 格挡附魔
├── InkyEnchant.cs                    # 伤害 + 减益附魔
└── MyEnchant.cs                      # 自定义附魔

{{MODID}}/{{MODID}}/
├── images/
│   └── enchantments/
│       └── adroit_enchant.png        # 附魔图标
└── localization/
    ├── eng/
    │   └── enchantments.json         # 英文本地化
    └── zhs/
        └── enchantments.json         # 中文本地化
```

---

## 14. 参考已有附魔实现

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 增加格挡 | `Models/Enchantments/` | `Adroit` |
| 增加伤害 | `Models/Enchantments/` | `Inky` |
| 增加打出次数 | `Models/Enchantments/` | `Glam` |
| 添加关键词 | `Models/Enchantments/` | `Goopy`（添加 Exhaust） |
| 限制卡牌类型 | `Models/Enchantments/` | `Corrupted`（限制攻击牌） |
| 卡牌复制 | `Models/Enchantments/` | `Clone` |
| 附魔后修改卡牌 | `Models/Enchantments/` | `Imbued`、`Momentum` |
| 附魔后每回合效果 | `Models/Enchantments/` | `Instinct` |

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Enchantments\` (约 30 个附魔文件)

---

## 15. 调试

在游戏中按 `~` 打开控制台。附魔通常通过事件、遗物或卡牌效果获得，可配合以下方式测试：
- 使用卡牌升级/附魔相关事件测试附魔效果
- 通过控制台添加测试用遗物或药水来触发附魔获得

---

## 16. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 附魔效果不触发 | `OnPlay` 未重写或方法签名不匹配 | 确认 `OnPlay` 方法签名正确 |
| 附魔数值为 0 | `CanonicalVars` 中未定义变量或未同步 `Amount` | 在 `RecalculateValues` 中同步 `Amount` |
| 附魔图标显示为空白 | 图标文件不存在 | 添加 `images/enchantments/{entry}.png` |
| 附魔描述显示原始键名 | 本地化 JSON 缺少条目 | 检查 enchantments.json |
| 附魔不显示在附魔池 | 未注册 | 确认有 `[RegisterEnchantment]` 属性 |
| 无法附魔到卡牌 | `CanEnchant` 返回 false | 检查 `CanEnchantCardType` 和 `CanEnchant` 的逻辑 |
| extraCardText 不显示 | `HasExtraCardText` 未设为 true | 设置 `HasExtraCardText => true` |
| 数值不更新 | `RecalculateValues` 未正确实现 | 确保在 `RecalculateValues` 中同步动态变量 |
| 附魔影响所有卡牌 | `AfterCardPlayed` 中未检查 `cardPlay.Card != Card` | 在 Hook 回调中检查是否为绑定的卡牌 |

---

## 17. 编写审查清单

### 17.1 基础检查

- [ ] 是否继承了 `ModEnchantmentTemplate`？
- [ ] 是否添加了 `[RegisterEnchantment]` 属性？
- [ ] 命名空间是否正确？

### 17.2 逻辑检查

- [ ] 卡牌打出效果是否在 `OnPlay` 中实现？
- [ ] 数值修改是否使用了 `EnchantDamageAdditive` / `EnchantBlockAdditive` / `EnchantPlayCount` 等方法？
- [ ] 是否需要重写 `OnEnchant` 添加关键词？
- [ ] 动态变量是否需要同步 `Amount`（`RecalculateValues`）？
- [ ] 是否需要限制可附魔的卡牌类型（`CanEnchant` / `CanEnchantCardType`）？
- [ ] Hook 回调中是否检查了 `cardPlay.Card != Card`？

### 17.3 资源检查

- [ ] 附魔图标 PNG 文件是否存在于 `images/enchantments/`？
- [ ] 图标命名是否与 ModelId Entry 一致（小写）？

### 17.4 本地化检查

- [ ] `enchantments.json` 中是否添加了 `title`？
- [ ] 是否添加了 `description`？
- [ ] 如果 `HasExtraCardText` 为 true，是否添加了 `extraCardText`？

### 17.5 注册检查

- [ ] `RegisterModAssembly` 在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
