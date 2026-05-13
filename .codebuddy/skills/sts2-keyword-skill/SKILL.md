---
name: sts2-keyword-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 卡牌属性提供全面参考。
  涵盖卡牌关键词 (CardKeyword) 的注册与使用、自定义 Tag (CardTag) 的定义与判断、
  动态变量 (DynamicVar) 的自定义与本地化、卡牌提示文本 (HoverTip) 的添加、
  卡牌描述中的 BBCode 与动态占位符语法、以及底层 CardModel 属性速查 (CanonicalKeywords/CanonicalTags/CanonicalVars/ExtraHoverTips)。
  当用户要求创建自定义卡牌属性、添加卡牌关键词、添加自定义 Tag、添加卡牌提示文本、或排查卡牌属性相关问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 卡牌属性编写 Skill (RitsuLib)

## 1. 概述

"卡牌属性"指卡牌上的关键词（`消耗`、`虚无`）、动态变量（伤害、格挡数值）、Tag（`打击`、`防御`）和提示文本（tooltip 悬浮框）。

RitsuLib 提供了一整套属性注册与管理 API，覆盖以下四种类型：

| 类型 | 说明 | 核心 API |
|------|------|---------|
| **卡牌关键词 (Keyword)** | 如 `消耗`、`虚无` 等固定属性，显示在卡牌描述区域 | `[RegisterOwnedCardKeyword]` / `RegisteredKeywordIds` / `HasModKeyword()` |
| **卡牌 Tag (CardTag)** | 如 `打击`、`防御` 等分类标签，用于遗物/能力的条件判断 | `[RegisterOwnedCardTag]` / `RegisteredCardTagIds` / `HasModCardTag()` |
| **动态变量 (DynamicVar)** | 卡牌数值（伤害、格挡、自定义值），支持本地化 tooltip 和差值显示 | `CanonicalVars` / `ModCardVars.Int()` / `.WithSharedTooltip()` |
| **提示文本 (HoverTip)** | 卡牌悬浮时显示的解释方框，与 [gold]BBCode[/gold] 染色配合 | `AdditionalHoverTips` / `HoverTipFactory` |

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-04-card-properties/

---

## 2. Model ID 规则

所有 RitsuLib 注册的卡牌属性使用固定的标识符格式。

### 2.1 关键词 (Keyword) ID

```
<MODID>_KEYWORD_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名（关键词） | Keyword ID |
|--------------------|------------|
| `Unique` | `{{MODID_UPPER}}_KEYWORD_UNIQUE` |
| `Brew` | `{{MODID_UPPER}}_KEYWORD_BREW` |
| `Ward` | `{{MODID_UPPER}}_KEYWORD_WARD` |

### 2.2 Tag ID

```
<MODID>_TAG_<TYPENAME>
```

| C# 类型名（Tag） | Tag ID |
|-----------------|--------|
| `Heavy` | `{{MODID_UPPER}}_TAG_HEAVY` |
| `Piercing` | `{{MODID_UPPER}}_TAG_PIERCING` |

### 2.3 动态变量名称

动态变量的名称使用 camelCase 字符串，如 `"leech"`、`"charges"`、`"thorns"`。

---

## 3. 卡牌关键词 (CardKeyword)

关键词是卡牌的固定属性，如 `消耗` (Exhaust)、`虚无` (Ethereal)、`固有` (Innate) 等。

### 3.1 注册关键词

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterOwnedCardKeyword(
    nameof(Unique),
    IconPath = "res://{{MODID}}/images/keywords/unique.svg",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
// [RegisterOwnedCardKeyword(nameof(Unique2), IconPath = "res://{{MODID}}/icon.svg")] // 添加更多关键词
public class MyKeywords
{
    public static readonly string Unique = ModContentRegistry.GetQualifiedCardKeywordId(Entry.ModId, nameof(Unique));

    // 还可以添加更多...
}
```

**参数说明**：

| 参数 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `nameof(Xxx)` | `string` | 是 | 关键词 C# 名称（在类中作为 static readonly string 的字段名） |
| `IconPath` | `string` | 否 | 关键词图标的资源路径，如 `"res://icon.svg"` |
| `CardDescriptionPlacement` | `ModKeywordCardDescriptionPlacement` | 否 | 关键词描述在卡牌上的显示位置 |

**`ModKeywordCardDescriptionPlacement` 枚举**：

| 值 | 说明 |
|----|------|
| (不设置) | 默认不显示在卡牌描述区域 |
| `BeforeCardDescription` | 显示在卡牌描述之前 |

### 3.2 关键词本地化

在 `card_keywords.json` 中添加：

```json
{
    "{{MODID_UPPER}}_KEYWORD_UNIQUE.description": "卡组中只能有一张同名牌。",
    "{{MODID_UPPER}}_KEYWORD_UNIQUE.title": "唯一"
}
```

### 3.3 在卡牌上使用已注册的关键词

在卡牌类中覆写 `RegisteredKeywordIds`：

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

[RegisterCard(typeof(TestCardPool))]
public class TestCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Unique];
    // 多个关键词：=> [MyKeywords.Unique, MyKeywords.Brew];
}
```

### 3.4 运行时判断关键词

在能力/遗物/Hook 回调中判断卡牌是否有关键词：

```csharp
// cardPlay.Card 是 CardModel 类型
if (cardPlay.Card.HasModKeyword(MyKeywords.Unique))
{
    // 执行逻辑
}

// 或在其他上下文中通过 CardModel 实例判断
if (cardModel.HasModKeyword(MyKeywords.Unique))
{
    // ...
}
```

### 3.5 运行时添加/移除关键词 (底层 CardModel API)

底层 `CardModel` 支持运行时修改关键词集合：

```csharp
// 运行时添加
card.AddKeyword(keyword);

// 运行时移除
card.RemoveKeyword(keyword);

// 关键词变更事件
card.KeywordsChanged += OnKeywordsChanged;
```

---

## 4. 卡牌 Tag (CardTag)

Tag 是卡牌的分类标签，如 `打击` (Strike)、`防御` (Defend) 等。原版 Tag 如 `CardTag.Strike`、`CardTag.Defend` 等可直接使用。自定义 Tag 需注册。

### 4.1 注册自定义 Tag

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterOwnedCardTag(nameof(Heavy))]
// [RegisterOwnedCardTag(nameof(Heavy2))] // 添加更多 Tag
public class MyTags
{
    public static readonly string Heavy = ModContentRegistry.GetQualifiedCardTagId(Entry.ModId, nameof(Heavy));

    // 自定义 Tag 常量（用于在 Mod 内部判断）
}
```

> 注意：`ModContentRegistry` 和 `Entry` 来自 RitsuLib 框架。`"{{MODID}}"` 指向 Mod 自身的 ID。

### 4.2 在卡牌上添加 Tag

在卡牌类中覆写 `RegisteredCardTagIds`：

```csharp
[RegisterCard(typeof(TestCardPool))]
public class TestCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<string> RegisteredCardTagIds => [MyTags.Heavy];
    // 多个 Tag：=> [MyTags.Heavy, CardTag.Strike];
}
```

### 4.3 运行时判断 Tag

```csharp
if (cardPlay.Card.HasModCardTag(MyTags.Heavy))
{
    // 执行逻辑
}

// 也可以判断原版 Tag
if (cardPlay.Card.HasTag(CardTag.Strike))
{
    // 被打击木偶加成
}
```

### 4.4 底层 CardModel API

```csharp
// 获取所有 Tag 属性
// card.Tags 返回 IEnumerable<CardTag>
// card.CanonicalTags 返回 HashSet<CardTag>（可覆写）
```

---

## 5. 动态变量 (DynamicVar)

动态变量指卡牌描述中的动态数值，如 `造成{Damage}点伤害` 中的 `{Damage}`。

### 5.1 内置变量类型

| 变量类型 | 用途 | 本地化占位符 |
|---------|------|-------------|
| `DamageVar(decimal value, ValueProp props)` | 伤害值 | `{Damage:diff()}` |
| `BlockVar(decimal value, ValueProp props)` | 格挡值 | `{Block:diff()}` |
| `MagicNumberVar(decimal value)` | 通用数值 | `{MagicNumber:diff()}` |
| `CardsVar(decimal value)` | 抽牌数/卡牌数 | `{Cards:diff()}` |
| `HealVar(decimal value)` | 治疗量 | `{Heal:diff()}` |
| `EnergyVar(decimal value)` | 能量 | `{Energy:energyIcons()}` |

### 5.2 定义内置变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars =>
[
    new DamageVar(12, ValueProp.Move),
    new BlockVar(8)
];
```

### 5.3 自定义变量 (ModCardVars)

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars =>
[
    new DamageVar(12, ValueProp.Move),
    ModCardVars.Int("Leech", 3)
        // .WithSharedTooltip("TEST_LEECH") // 如果要加 tooltip 本地化
];
```

**`ModCardVars` 方法**：

| 方法 | 说明 |
|------|------|
| `ModCardVars.Int(string name, int amount)` | 创建整型动态变量 |
| `ModCardVars.Float(string name, double amount)` | 创建浮点型动态变量 |

**方法链**：

| 方法 | 说明 |
|------|------|
| `.WithSharedTooltip(string key)` | 绑定 tooltip 本地化键（见 5.5 节） |
| `.WithTooltip(string titleTable, string titleKey, string? iconPath)` | 自定义 tooltip 标题 |
| `.WithTooltip(Func<DynamicVar, HoverTip> factory)` | 完全自定义 tooltip |

### 5.4 升级时修改变量

```csharp
public override void OnUpgrade()
{
    DynamicVars.Damage.BaseValue = 15;  // 伤害从 12 -> 15
    DynamicVars["Leech"].BaseValue = 5;  // 自定义变量从 3 -> 5
}
```

### 5.5 自定义变量本地化 (static_hover_tips.json)

为自定义变量添加 tooltip（可选）：

```json
{
    "{{MODID_UPPER}}_TEST_LEECH.description": "吸取等量生命。",
    "{{MODID_UPPER}}_TEST_LEECH.title": "汲取"
}
```

### 5.6 在卡牌描述中使用

CardModel 的 Description 和 DynamicDescription 中可用：

```json
{
    "{{MODID_UPPER}}_CARD_TEST_CARD.description": "[gold]汲取[/gold]{Leech:diff()}。\n造成{Damage:diff()}点伤害。"
}
```

**描述占位符语法**：

| 语法 | 效果 |
|------|------|
| `{VarName}` | 显示变量的当前值 |
| `{VarName:diff()}` | 显示差值（升级变化时变红/绿） |
| `{Energy:energyIcons()}` | 渲染能量图标 |

### 5.7 在逻辑中使用变量

```csharp
public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
{
    // 使用 DynamicVars["Leech"].BaseValue 获取数值
    // 先让敌人失去生命（不可格挡不受能力影响的伤害）
    ctx.DealDamage(user, target, DynamicVars["Leech"].BaseValue,
        ValueProp.Unblockable | ValueProp.Unpowered);

    // 再让玩家回复生命
    ctx.Heal(user, DynamicVars["Leech"].BaseValue);
}
```

### 5.8 运行时读取动态变量

```csharp
// 在遗物/能力中读取卡牌上的动态变量
int leech = cardPlay.Card.DynamicVars.GetIntOrDefault("Leech");
decimal val = cardPlay.Card.DynamicVars.GetValueOrDefault("Leech");
bool active = cardPlay.Card.DynamicVars.HasPositiveValue("Leech");
```

---

## 6. 卡牌提示文本 (HoverTip)

提示文本是卡牌悬浮时显示的解释方框，用于解释描述中通过 BBCode 染色的术语。

### 6.1 添加提示文本

在卡牌类中覆写 `AdditionalHoverTips`，通过 `HoverTipFactory` 创建提示框：

```csharp
using MegaCrit.Sts2.Core.Localization.HoverTips;

[RegisterCard(typeof(TestCardPool))]
public class TestCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 通过 HoverTipFactory 添加各种提示文本
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.KeywordTooltip("key_exhaust"),      // 消耗
        HoverTipFactory.KeywordTooltip("key_ethereal"),     // 虚无
        HoverTipFactory.KeywordTooltip("key_innate"),       // 固有
        // 或自定义提示文本
        HoverTipFactory.SimpleHoverTip("易伤", "受到的伤害增加 50%。"),
        // 或从本地化表加载
        HoverTipFactory.LocHoverTip("card_keywords", "MY_KEYWORD"),
    ];
}
```

**`HoverTipFactory` 常用方法**：

| 方法 | 说明 |
|------|------|
| `KeywordTooltip(string keywordKey)` | 从原版关键词本地化加载提示文本 |
| `SimpleHoverTip(string title, string description)` | 直接指定标题和描述 |
| `LocHoverTip(string table, string key)` | 从本地化表加载 |
| `BlankHoverTip()` | 空提示框（用作分隔） |

### 6.2 提示文本与 BBCode 配合

描述中通过 BBCode 染色后，再通过 `AdditionalHoverTips` 添加对应的提示框：

```json
{
    "{{MODID_UPPER}}_CARD_TEST_CARD.description": "造成{Damage:diff()}点伤害。给予[gold]易伤[/gold]。"
}
```

```csharp
protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
[
    HoverTipFactory.KeywordTooltip("key_vulnerable"),  // 为 [gold]易伤[/gold] 提供提示
];
```

---

## 7. 卡牌描述 BBCode 语法参考

卡牌描述中可用的 BBCode 标签：

| 标签 | 效果 | 示例 |
|------|------|------|
| `[gold]文字[/gold]` | 金色高亮（用于关键词） | `[gold]消耗[/gold]` |
| `[blue]文字[/blue]` | 蓝色（用于数值） | `[blue]{Block}[/blue]` |
| `[b]文字[/b]` | 加粗 | `[b]重要[/b]` |
| `[purple]文字[/purple]` | 紫色 | `[purple]易伤[/purple]` |
| `[jitter]文字[/jitter]` | 抖动效果 | -- |
| `[sine]文字[/sine]` | 正弦波动 | -- |
| `\n` | 换行 | `"第一行\n第二行"` |

---

## 6. 底层 CardModel 属性速查

以下为底层 `CardModel`（命名空间 `MegaCrit.Sts2.Core.Models`）中与属性相关的虚属性，RitsuLib 的 `ModCardTemplate` 已封装了对应接口：

| 虚属性 | 类型 | 默认值 | RitsuLib 封装 | 说明 |
|--------|------|--------|--------------|------|
| `CanonicalKeywords` | `virtual IEnumerable<CardKeyword>` | 空数组 | `RegisteredKeywordIds` | 卡牌固有的关键词 |
| `CanonicalTags` | `protected virtual HashSet<CardTag>` | 空集合 | `RegisteredCardTagIds` | 卡牌固有的 Tag |
| `CanonicalVars` | `protected virtual IEnumerable<DynamicVar>` | 空数组 | `CanonicalVars` (同名) | 卡牌固有的动态变量 |
| `DynamicVars` | `DynamicVarSet` (get) | 从 CanonicalVars 初始化 | `DynamicVars` | 动态变量运行时集合 |
| `ExtraHoverTips` | `protected virtual IEnumerable<IHoverTip>` | 空数组 | `AdditionalHoverTips` | 额外的提示文本 |
| `Keywords` | `IReadOnlySet<CardKeyword>` (get) | CanonicalKeywords + 运行时添加 | `Keywords` | 关键词并集（含运行时） |
| `Tags` | `virtual IEnumerable<CardTag>` (get) | CanonicalTags 副本 | `Tags` | Tag 集合 |
| `HoverTips` | `IEnumerable<IHoverTip>` (get) | 自动组合 ExtraHoverTips + 关键词等 | `HoverTips` | 完整悬浮提示列表 |
| `ShouldGlowGoldInternal` | `protected virtual bool` | `false` | -- | 卡牌是否显示金色边框 |
| `ShouldGlowRedInternal` | `protected virtual bool` | `false` | -- | 卡牌是否显示红色边框 |

---

## 7. 本地化文件组织

### 7.1 关键词本地化

```
{{MODID}}/{{MODID}}/localization/eng/card_keywords.json
```

```json
{
    "{{MODID_UPPER}}_KEYWORD_UNIQUE.description": "Only one copy of this card can be in your deck.",
    "{{MODID_UPPER}}_KEYWORD_UNIQUE.title": "Unique"
}
```

### 7.2 动态变量提示文本本地化

```
{{MODID}}/{{MODID}}/localization/eng/static_hover_tips.json
```

```json
{
    "TEST_LEECH.description": "Drain equal amount of life.",
    "TEST_LEECH.title": "Leech"
}
```

### 7.3 文件目录结构

```
{{MODID}}/{{MODID}}/localization/
├── eng/
│   ├── cards.json                 # 卡牌标题与描述
│   ├── card_keywords.json         # 卡牌关键词（title + description）
│   ├── static_hover_tips.json     # 动态变量悬浮提示（title + description）
│   └── ... (其他本地化文件)
└── zhs/
    ├── cards.json
    ├── card_keywords.json
    ├── static_hover_tips.json
    └── ...
```

---

## 8. 完整代码模板

### 8.1 关键词 + Tag 注册类

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

namespace {{MODID}}.{{MODID}}Code;

[RegisterOwnedCardKeyword(nameof(Unique), IconPath = "res://{{MODID}}/images/keywords/unique.svg")]
[RegisterOwnedCardKeyword(nameof(Brew), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardTag(nameof(Heavy))]
[RegisterOwnedCardTag(nameof(Piercing))]
public static class MyCardProperties
{
    // Keywords
    public static readonly string Unique = ModContentRegistry.GetQualifiedCardKeywordId(Entry.ModId, nameof(Unique));
    public static readonly string Brew = ModContentRegistry.GetQualifiedCardKeywordId("{{MODID}}", nameof(Brew));

    // Tags
    public static readonly string Heavy = ModContentRegistry.GetQualifiedCardTagId(Entry.ModId, nameof(Heavy));
    public static readonly string Piercing = ModContentRegistry.GetQualifiedCardTagId("{{MODID}}", nameof(Piercing));
}
```

### 8.2 使用关键词 + 自定义变量的卡牌

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.HoverTips;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Cards;

[RegisterCard(typeof(SharedCardPool))]
public class TestCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string Title => "Test Card";
    public override string Description => "[gold]汲取[/gold]{Leech:diff()}。\n造成{Damage:diff()}点伤害。";

    protected override IEnumerable<string> RegisteredKeywordIds => [MyCardProperties.Unique, MyCardProperties.Brew];
    protected override IEnumerable<string> RegisteredCardTagIds => [MyCardProperties.Heavy];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        ModCardVars.Int("Leech", 3).WithSharedTooltip("TEST_LEECH")
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.KeywordTooltip("key_vulnerable"),
    ];

    public override void OnUpgrade()
    {
        DynamicVars.Damage.BaseValue = 15;
        DynamicVars["Leech"].BaseValue = 5;
    }

    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        // 汲取伤害（不可格挡不受能力影响）
        ctx.DealDamage(user, target, DynamicVars["Leech"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered);
        // 回血
        ctx.Heal(user, DynamicVars["Leech"].BaseValue);

        // 普通伤害
        ctx.DealDamage(user, target, Damage);
    }
}
```

### 8.3 带提示文本 + Tag 判断的卡牌

```csharp
[RegisterCard(typeof(SharedCardPool))]
public class AnotherCard : ModCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string Title => "Heavy Strike";
    public override string Description => "造成{Damage:diff()}点伤害。\n给予[gold]易伤[/gold]。";

    protected override IEnumerable<string> RegisteredCardTagIds => [MyCardProperties.Heavy, CardTag.Strike];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.KeywordTooltip("key_vulnerable"),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(18)
    ];

    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        ctx.DealDamage(user, target, Damage);

        // Tag 判断示例：如果有 Heavy Tag 则额外施加易伤
        if (ctx.CardModel.HasModCardTag(MyCardProperties.Heavy))
        {
            ctx.ApplyPower(target, new VulnerablePower(), 2);
        }
    }
}
```

---

## 9. 完整属性注册流程总结

```
┌─────────────────────────────────────┐
│ 1. 注册关键词 (Keyword)              │
│    [RegisterOwnedCardKeyword]        │
│    + card_keywords.json 本地化       │
└────────────────┬────────────────────┘
                 ▼
┌─────────────────────────────────────┐
│ 2. 注册 Tag                         │
│    [RegisterOwnedCardTag]            │
│    + GetQualifiedCardTagId() 获取ID  │
└────────────────┬────────────────────┘
                 ▼
┌─────────────────────────────────────┐
│ 3. 添加到卡牌                        │
│    RegisteredKeywordIds             │
│    RegisteredCardTagIds             │
│    CanonicalVars                    │
│    AdditionalHoverTips              │
└────────────────┬────────────────────┘
                 ▼
┌─────────────────────────────────────┐
│ 4. 本地化描述与占位符                │
│    cards.json 中使用 BBCode         │
│    static_hover_tips.json (可选)    │
└────────────────┬────────────────────┘
                 ▼
┌─────────────────────────────────────┐
│ 5. 运行时判断                        │
│    HasModKeyword()                  │
│    HasModCardTag()                  │
│    DynamicVars.GetValueOrDefault()  │
└─────────────────────────────────────┘
```

---

## 10. 参考已有内容

### 10.1 查找原版关键词定义

原版关键词在源码中定义，可通过搜索了解：

```
{{STS2_SOURCE_ROOT}}\Models\CardKeyword.cs
{{STS2_SOURCE_ROOT}}\Models\CardTag.cs
```

### 10.2 查找原版卡牌实现中的关键词/Tag

```
D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Cards\
```

搜索 `CanonicalKeywords`、`CanonicalTags`、`CanonicalVars` 了解原版卡牌如何定义属性。

### 10.3 本地化参考

```
{{STS2_GAME_ROOT}}\localization\eng\card_keywords.json
{{STS2_GAME_ROOT}}\localization\eng\static_hover_tips.json
```

---

## 11. Hook 回调参考 — 卡牌属性的常见判断场景

在能力 (Power) 和遗物 (Relic) 的 Hook 回调中，经常需要根据卡牌的关键词、Tag、动态变量来判断或取数。以下是最常用的判断场景和方法签名速查。

> 完整 Hook 列表和签名参考 `sts2-relic-skill` 或 `sts2-power-skill`。

### 11.1 常用判断方法速查

| 场景 | Hook 回调 | 关键词/Tag 判断 | 动态变量读取 |
|------|----------|----------------|-------------|
| 卡牌打出后 | `AfterCardPlayed(CardPlay cardPlay)` | `cardPlay.Card.HasModKeyword(xxx)` | `cardPlay.Card.DynamicVars.GetIntOrDefault(xxx)` |
| 卡牌打出前 | `BeforeCardPlayed(CardPlay cardPlay)` | `cardPlay.Card.HasModCardTag(xxx)` | `cardPlay.Card.DynamicVars.GetValueOrDefault(xxx)` |
| 卡牌被抽到后 | `AfterCardDrawn(CardModel card, ...)` | `card.HasModKeyword(xxx)` | `card.DynamicVars.HasPositiveValue(xxx)` |
| 卡牌被丢弃后 | `AfterCardDiscarded(CardModel card, ...)` | `card.HasModKeyword(xxx)` | — |
| 卡牌被消耗后 | `AfterCardExhausted(CardModel card, ...)` | `card.HasModKeyword(xxx)` | — |
| 卡牌进入战斗 | `AfterCardEnteredCombat(CardModel card)` | `card.HasModKeyword(xxx)` | — |
| 攻击前 | `BeforeAttack(AttackCommand command)` | `command.Card?.HasModKeyword(xxx)` | — |
| 攻击后 | `AfterAttack(AttackCommand command)` | `command.Card?.HasModCardTag(xxx)` | — |
| 造成伤害后 | `AfterDamageGiven(..., CardModel? cardSource)` | `cardSource?.HasModKeyword(xxx)` | — |
| 获得格挡前 | `BeforeBlockGained(..., CardModel? cardSource)` | `cardSource?.HasModKeyword(xxx)` | — |

### 11.2 典型代码示例

```csharp
// 在能力/遗物的 AfterCardPlayed 中判断关键词
public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
{
    if (cardPlay.Card?.HasModKeyword(MyCardProperties.Unique) == true)
    {
        // 唯一关键词的卡牌被打出后，执行特殊逻辑
        await CardPileCmd.Exhaust(ctx, cardPlay.Card, Owner);
    }
}

// 在 AfterCardDrawn 中判断 Tag
public override async Task AfterCardDrawn(
    PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
{
    if (card.HasModCardTag(MyCardProperties.Heavy))
    {
        // Heavy Tag 的卡牌被抽到时，获得格挡
        await BlockCmd.GainBlock(DynamicVars.Block.IntValue)
            .FromPower(this)
            .ApplyTo(Owner)
            .Execute(ctx);
    }
}

// 在 ModifyDamageAdditive 中根据卡牌 Tag 调整数值
public override decimal ModifyDamageAdditive(
    Creature target, decimal amount, ValueProp props,
    Creature dealer, CardModel cardSource)
{
    if (dealer != Owner) return 0m;
    if (cardSource?.HasModCardTag(MyCardProperties.Heavy) == true)
    {
        return Amount; // Heavy Tag 的卡牌获得额外伤害加成
    }
    return 0m;
}

// 在 AfterCardExhausted 中判断关键词
public override async Task AfterCardExhausted(
    PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
{
    if (card.HasModKeyword(MyCardProperties.Unique))
    {
        // 唯一卡牌被消耗时触发的额外效果
    }
}
```

### 11.3 在 Hook 中读取动态变量

```csharp
// 在遗物/能力的 Hook 中读取卡牌的动态变量
public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
{
    var card = cardPlay.Card;
    if (card == null) return;

    // 读取自定义变量
    int leechValue = card.DynamicVars.GetIntOrDefault("Leech");
    if (leechValue > 0)
    {
        // 根据卡牌的 LeeCh 值执行逻辑
        await HealCmd.Heal(leechValue).FromRelic(this).Execute(ctx);
    }

    // 读取内置变量
    decimal damage = card.DynamicVars.Damage.BaseValue;
    decimal block = card.DynamicVars.Block.BaseValue;
}
```

### 11.4 BeforeCardPlayed 中阻止特定关键词卡牌

```csharp
public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
{
    // 阻止含有特定关键词的卡牌被自动打出
    if (card.HasModKeyword(MyCardProperties.Unique))
    {
        return false;
    }
    return true;
}
```

---

## 12. 调试

- 战斗中按 `~` 打开控制台
- 输入 `keyword list` 查看已注册关键词
- 输入 `tag list` 查看已注册 Tag
- 输入 `card <CARD_ID>` 查看卡牌详细信息（包括关键词和 Tag）
- 卡牌描述显示为原始键名（如 `{{MODID_UPPER}}_CARD_TEST_CARD.title`）是本地化文件缺失的信号
- 动态变量占位符显示为 `{VarName}` 表示未正确替换，检查 `CanonicalVars` 中是否定义了对应名称的变量

---

## 13. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 关键词不显示在卡牌上 | `RegisteredKeywordIds` 未覆写或 ID 不正确 | 确认卡牌类中覆写了 `RegisteredKeywordIds` |
| 关键词图标不显示 | `IconPath` 路径错误或文件缺失 | 检查 `[RegisterOwnedCardKeyword]` 中的图标路径 |
| `HasModKeyword()` 返回 false | Keyword ID 不匹配 | 确认使用了 `ModContentRegistry.GetQualifiedCardKeywordId()` 获取的完整 ID |
| 自定义变量占位符显示为 `{Leech}` | 描述中变量名与 `ModCardVars.Int("Leech")` 名称不匹配 | 检查变量名大小写是否完全一致 |
| `:diff()` 无效 | 变量未正确连接到描述 | 确认变量已包含在 `CanonicalVars` 中 |
| Tag 判断不生效 | Tag ID 未正确获取 | 确认使用了 `ModContentRegistry.GetQualifiedCardTagId()` |
| 提示文本不显示 | `AdditionalHoverTips` 未正确覆写 | 确认覆写了 IEnumberable\<IHoverTip\> 属性 |
| 编译错误：找不到类型 | 缺少 using 引用 | 添加 `STS2RitsuLib.Interop.AutoRegistration`、`STS2RitsuLib.Cards.DynamicVars` 等命名空间 |
| 关键词未注册 | `RegisterOwnedCardKeyword` 属性未添加 | 确认在关键词类上添加了属性 |

---

## 14. 编写审查清单

### 14.1 关键词检查

- [ ] 关键词类是否添加了 `[RegisterOwnedCardKeyword]` 属性？
- [ ] 关键词类的 `static readonly string` 是否通过 `ModContentRegistry.GetQualifiedCardKeywordId()` 获取？
- [ ] `card_keywords.json` 中是否添加了对应的 `.title` 和 `.description`？
- [ ] 卡牌类中是否覆写了 `RegisteredKeywordIds`？
- [ ] 运行时的 `HasModKeyword()` 调用是否使用了正确的 ID？

### 14.2 Tag 检查

- [ ] Tag 类是否添加了 `[RegisterOwnedCardTag]` 属性？
- [ ] Tag ID 是否通过 `ModContentRegistry.GetQualifiedCardTagId()` 获取？
- [ ] 卡牌类中是否覆写了 `RegisteredCardTagIds`？
- [ ] 运行时的 `HasModCardTag()` 调用是否使用了正确的 ID？

### 14.3 动态变量检查

- [ ] `CanonicalVars` 中是否定义了所有描述中使用的变量？
- [ ] `ModCardVars.Int()` 的名称是否与描述中的 `{VarName}` 完全匹配（区分大小写）？
- [ ] 升级时是否更新了 `BaseValue`？
- [ ] 使用 `.WithSharedTooltip()` 时，`static_hover_tips.json` 中是否添加了对应条目？

### 14.4 提示文本检查

- [ ] 描述中使用了 BBCode 染色的术语，是否在 `AdditionalHoverTips` 中添加了对应提示？
- [ ] `HoverTipFactory.KeywordTooltip()` 的 key 是否与原版关键词匹配？

### 14.5 本地化检查

- [ ] `card_keywords.json` 中是否添加了所有注册的关键词？
- [ ] `static_hover_tips.json` 中是否添加了所有自定义变量的 tooltip？
- [ ] BBCode 标签是否正确闭合？

---

*最后更新：2026-05-12*
