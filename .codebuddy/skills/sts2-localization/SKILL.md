---
name: sts2-localization
description: >-
  该 Skill 为杀戮尖塔2 (Slay the Spire 2) Mod 本地化编写提供全面的参考与自动审查。
  涵盖本地化 JSON 文件格式、键命名规则 (Model ID)、BBCode 标签 (Godot 原生 + 游戏自定义)、
  占位变量 (DynamicVar) 及其对应的 Var 类、Formatter 语法 (diff/energyIcons/starIcons/cond/choose 等)、
  卡牌独有上下文变量、SmartFormat 内置格式化器、各内容类型 (卡牌/遗物/能力/药水/事件等) 的
  本地化字段要求、文件组织结构、多语言支持，以及完整的代码模板与审查清单。
  当用户要求创建或修改 Mod 本地化文本、编写描述、排查本地化显示问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 本地化编写 Skill

## 1. 概述

STS2 的本地化基于 Godot 的 `RichTextLabel` + `SmartFormat` 库，使用 JSON 文件存储键值对。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/05-variable-and-description/

**SmartFormat 文档**: https://github.com/axuno/SmartFormat/wiki

**当前项目 ModId**: `PersonalMod`

---

## 2. 本地化文件格式

### 2.1 文件位置

```
{modId}/localization/{Language}/{category}.json
```

示例：

```
PersonalMod/PersonalMod/localization/
├── eng/                    # 英文（源语言，必须提供）
│   ├── cards.json          # 卡牌
│   ├── powers.json         # 能力
│   ├── relics.json         # 遗物
│   ├── potions.json        # 药水
│   ├── events.json         # 事件
│   ├── ancients.json       # 先古之民对话
│   ├── monsters.json       # 怪物
│   ├── card_keywords.json  # 卡牌关键词（悬停提示）
│   ├── static_hover_tips.json  # 静态悬停提示
│   ├── encounters.json     # 遭遇战
│   ├── enchantments.json   # 附魔
│   ├── epochs.json         # 纪元
│   ├── characters.json     # 角色
│   ├── main_menu_ui.json   # 主菜单 UI
│   ├── settings_ui.json    # 设置界面
│   └── ...
├── zhs/                    # 简体中文
│   ├── cards.json
│   ├── powers.json
│   └── ...
└── jpn/                    # 日语
    └── ...
```

### 2.2 支持的语言代码

| 代码 | 语言 | 说明 |
|------|------|------|
| `eng` | English | 源语言（必须） |
| `zhs` | 简体中文 | 常用 |
| `jpn` | 日语 | — |
| `kor` | 韩语 | — |
| `rus` | 俄语 | — |
| `fra` | 法语 | — |
| `spa` | 西班牙语（拉美） | — |
| `esp` | 西班牙语（西班牙） | — |
| `tur` | 土耳其语 | — |
| `ita` | 意大利语 | — |
| `ptb` | 葡萄牙语（巴西） | — |
| `pol` | 波兰语 | — |
| `deu` | 德语 | — |
| `tha` | 泰语 | 未完成 |

### 2.3 JSON 格式

```json
{
  "ENTRY_ID.field": "文本内容"
}
```

- **键格式**: `{ENTRY_ID}.{field}`（大写字母 + 下划线）
- **换行**: 使用 `\n`
- **文件编码**: UTF-8
- 所有键值对为扁平结构（无嵌套对象）

---

## 3. Model ID 规则 (键名)

### 3.1 原版内容

原版内容直接使用 `Entry`（全大写+下划线）作为键前缀：

```json
{
  "STRIKE_IRONCLAD.title": "Strike",
  "STRIKE_IRONCLAD.description": "Deal {Damage:diff()} damage.",
  "BURNING_BLOOD.title": "Burning Blood",
  "BURNING_BLOOD.description": "Heal 6 HP at the end of each combat.",
  "BURNING_BLOOD.flavor": "The heart pumps anew."
}
```

### 3.2 RitsuLib Mod 内容

通过 RitsuLib 注册的内容，ID 格式为：

```
<MODID>_<CATEGORY>_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。

| 内容类型 | CATEGORY | 示例 |
|---------|----------|------|
| 卡牌 | `CARD` | `PERSONALMOD_CARD_TEST_CARD` |
| 遗物 | `RELIC` | `PERSONALMOD_RELIC_TEST_RELIC` |
| 能力 | `POWER` | `PERSONALMOD_POWER_TEST_POWER` |
| 药水 | `POTION` | `PERSONALMOD_POTION_TEST_POTION` |

**C# 类型名到 ID 的转换规则**: PascalCase → UPPER_SNAKE_CASE

| C# 类型名 | Model ID |
|---------|----------|
| `TestCard` | `PERSONALMOD_CARD_TEST_CARD` |
| `HeavySlash` | `PERSONALMOD_CARD_HEAVY_SLASH` |
| `BurningBlood` | `PERSONALMOD_RELIC_BURNING_BLOOD` |
| `MyCoolRelic` | `PERSONALMOD_RELIC_MY_COOL_RELIC` |

### 3.3 自定义变量 Tooltip

自定义 DynamicVar 的悬停提示，键名无需 MODID 前缀：


```json
// static_hover_tips.json
{
    "TEST_LEECH.description": "Steal life equal to damage dealt.",
    "TEST_LEECH.title": "Leech"
}
```

对应 C# 中 `.WithSharedTooltip("TEST_LEECH")` 的设置。

---

## 4. 各内容类型的本地化字段

### 4.1 卡牌 (cards.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.title` | 是 | 卡牌名称 |
| `{ID}.description` | 是 | 卡牌效果描述 |

```json
{
    "PERSONALMOD_CARD_TEST_CARD.title": "Test Card",
    "PERSONALMOD_CARD_TEST_CARD.description": "Deal {Damage:diff()} damage."
}
```

### 4.2 遗物 (relics.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.title` | 是 | 遗物名称 |
| `{ID}.description` | 是 | 遗物效果描述（战斗 tooltip） |
| `{ID}.flavor` | 推荐 | 风味文本（斜体展示） |
| `{ID}.eventDescription` | 可选 | 事件选择界面的动态描述 |

```json
{
    "PERSONALMOD_RELIC_TEST_RELIC.title": "Test Relic",
    "PERSONALMOD_RELIC_TEST_RELIC.description": "At the start of your turn, draw [blue]{Cards}[/blue] cards.",
    "PERSONALMOD_RELIC_TEST_RELIC.flavor": "Looks familiar?",
    "PERSONALMOD_RELIC_TEST_RELIC.eventDescription": "Draw [blue]{Cards}[/blue] cards at the start of each combat."
}
```

### 4.3 能力 (powers.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.title` | 是 | 能力名称 |
| `{ID}.description` | 是 | 简短描述（固定文本） |
| `{ID}.smartDescription` | 推荐 | 带变量替换的动态描述（tooltip 用） |

```json
{
    "PERSONALMOD_POWER_TEST_POWER.title": "Test Power",
    "PERSONALMOD_POWER_TEST_POWER.description": "Gain Strength at the start of your turn.",
    "PERSONALMOD_POWER_TEST_POWER.smartDescription": "At the start of your turn, gain [blue]{Amount}[/blue] [gold]Strength[/gold]."
}
```

### 4.4 药水 (potions.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.title` | 是 | 药水名称 |
| `{ID}.description` | 是 | 药水效果描述 |

### 4.5 事件 (events.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.title` | 是 | 事件标题 |
| `{ID}.description` | 是 | 事件描述文本 |

### 4.6 先古之民 (ancients.json)

| 字段 | 必需 | 说明 |
|------|------|------|
| `{ID}.name` | 是 | 先古角色名 |
| `{ID}.dialogue_*` | 推荐 | 对话文本行 |

### 4.7 关键词 (card_keywords.json)

卡牌描述中高亮关键词的悬停提示：

```json
{
    "EXHAUST.title": "Exhaust",
    "EXHAUST.description": "Permanently remove this card from your deck for the rest of the run."
}
```

### 4.8 静态悬停提示 (static_hover_tips.json)

自定义变量的悬停提示（对应 `WithSharedTooltip`）：

```json
{
    "TEST_LEECH.title": "Leech",
    "TEST_LEECH.description": "Steal life equal to damage dealt."
}
```

---

## 5. BBCode 标签

STS2 使用 Godot 的 `RichTextLabel` 渲染文本，支持 Godot 原生 BBCode 和游戏自定义标签。

**Godot BBCode 文档**: https://docs.godotengine.org/zh-cn/4.x/tutorials/ui/bbcode_in_richtextlabel.html

### 5.1 Godot 原生 BBCode

| 标签 | 说明 | 示例 |
|------|------|------|
| `[b]...[/b]` | 粗体 | `[b]bold[/b]` |
| `[i]...[/i]` | 斜体 | `[i]italic[/i]` |
| `[u]...[/u]` | 下划线 | `[u]underline[/u]` |
| `[color=...]...[/color]` | 文字颜色 | `[color=red]red text[/color]` |
| `[font=...]...[/font]` | 字体 | `[font=Arial]Arial text[/font]` |
| `[size=...]...[/size]` | 字号 | `[size=24]large text[/size]` |

### 5.2 游戏自定义标签

| 标签 | 效果 | 典型用途 |
|------|------|---------|
| `[gold]...[/gold]` | 金色文字 | 关键词高亮（力量、敏捷、格挡、弃牌堆等） |
| `[blue]...[/blue]` | 蓝色文字 | 数值高亮 |
| `[red]...[/red]` | 红色文字 | 负面效果、损失 |
| `[green]...[/green]` | 绿色文字 | 正面效果 |
| `[purple]...[/purple]` | 紫色文字 | — |
| `[orange]...[/orange]` | 橙色文字 | — |
| `[pink]...[/pink]` | 粉色文字 | — |
| `[aqua]...[/aqua]` | 水绿色文字 | — |
| `[b]...[/b]` | 粗体 | — |
| `[i]...[/i]` | 斜体 | — |
| `[jitter]...[/jitter]` | 抖动动画 | — |
| `[sine]...[/sine]` | 正弦波动动画 | — |
| `[fade_in]...[/fade_in]` | 渐显动画 | — |
| `[fly_in]...[/fly_in]` | 飞入动画 | — |
| `[thinky_dots]...[/thinky_dots]` | 思考点点动画 | — |
| `[ancient_banner]...[/ancient_banner]` | 古代横幅风格 | 先古卡牌 |

### 5.3 标签使用规范

- **关键词**使用 `[gold]` 高亮: `[gold]Strength[/gold]`, `[gold]Block[/gold]`, `[gold]Draw Pile[/gold]`, `[gold]Discard Pile[/gold]`, `[gold]Hand[/gold]`, `[gold]Deck[/gold]`, `[gold]Exhaust[/gold]`, `[gold]Upgrade[/gold]`, `[gold]Transform[/gold]`
- **数值**使用 `[blue]` 高亮: `[blue]6[/blue]`, `[blue]{Cards}[/blue]`
- **负面/损失**使用 `[red]` 高亮: `[red]Lose[/red] HP`
- **BBCode 标签必须正确闭合**，否则会导致渲染错误

---

## 6. 占位变量 (DynamicVar)

占位变量在描述文本中使用 `{VariableName}` 格式，会被对应 DynamicVar 的数值替换。

### 6.1 内置数值变量

| 占位符 | 对应类 | 说明 | 描述示例 |
|--------|--------|------|---------|
| `{Damage}` | `DamageVar` | 伤害 | `Deal {Damage:diff()} damage.` |
| `{Block}` | `BlockVar` | 格挡 | `Gain {Block:diff()} Block.` |
| `{Cards}` | `CardsVar` | 卡牌数量 | `Draw {Cards:diff()} cards.` |
| `{Energy}` | `EnergyVar` | 能量（动态值） | `Gain {Energy:energyIcons()}.` |
| `{energyPrefix}` | — | 能量（固定数值） | `Gain {energyPrefix:energyIcons(1)}.` |
| `{Repeat}` | `RepeatVar` | 重复次数 | `Deal {Damage:diff()} damage {Repeat:diff()} times.` |
| `{Heal}` | `HealVar` | 治疗 | `Heal {Heal:diff()} HP.` |
| `{HpLoss}` | `HpLossVar` | 失去生命 | `Lose {HpLoss:diff()} HP.` |
| `{MaxHp}` | `MaxHpVar` | 最大生命 | `Gain {MaxHp:diff()} Max HP.` |
| `{Gold}` | `GoldVar` | 金币 | `Gain {Gold:diff()} Gold.` |
| `{Summon}` | `SummonVar` | 召唤 | `Summon {Summon:diff()}.` |
| `{Forge}` | `ForgeVar` | 铸造 | `Forge {Forge:diff()}.` |
| `{Stars}` | `StarsVar` | 辉星 | `Gain {Stars:starIcons()}.` |
| `{CalculatedDamage}` | `CalculatedDamageVar` | 计算出的伤害量 | `(Deals {CalculatedDamage:diff()} damage)` |
| `{CalculatedBlock}` | `CalculatedBlockVar` | 计算出的格挡值 | `(Gains {CalculatedBlock:diff()} Block)` |

### 6.2 内置能力变量

| 占位符 | 对应类 | 说明 | 描述示例 |
|--------|--------|------|---------|
| `{StrengthPower}` | `PowerVar<StrengthPower>` | 力量 | `Gain {StrengthPower:diff()} Strength.` |
| `{DexterityPower}` | `PowerVar<DexterityPower>` | 敏捷 | `Gain {DexterityPower:diff()} Dexterity.` |
| `{WeakPower}` | `PowerVar<WeakPower>` | 虚弱 | `Apply {WeakPower:diff()} Weak.` |
| `{VulnerablePower}` | `PowerVar<VulnerablePower>` | 易伤 | `Apply {VulnerablePower:diff()} Vulnerable.` |
| `{PoisonPower}` | `PowerVar<PoisonPower>` | 中毒 | `Apply {PoisonPower:diff()} Poison.` |
| `{DoomPower}` | `PowerVar<DoomPower>` | 灾厄 | `Apply {DoomPower:diff()} Doom.` |
| `{ThornsPower}` | `PowerVar<ThornsPower>` | 荆棘 | `Gain {ThornsPower:diff()} Thorns.` |
| `{VigorPower}` | `PowerVar<VigorPower>` | 活力 | `Gain {VigorPower:diff()} Vigor.` |
| `{RitualPower}` | `PowerVar<RitualPower>` | 仪式 | `Gain {RitualPower:diff()} Ritual.` |
| `{IntangiblePower}` | `PowerVar<IntangiblePower>` | 无实体 | `Gain {IntangiblePower:diff()} Intangible.` |
| `{ArtifactPower}` | `PowerVar<ArtifactPower>` | 人工制品 | `Gain {ArtifactPower:diff()} Artifact.` |
| `{AngerPower}` | `PowerVar<AngerPower>` | 愤怒 | — |
| `{AfterimagePower}` | `PowerVar<AfterimagePower>` | 残影 | — |
| `{AccuracyPower}` | `PowerVar<AccuracyPower>` | 精准 | — |

> **注意**: 更多能力变量参见原版 `powers.json` 中的用法。

### 6.3 自定义变量

```csharp
// C# 中定义自定义变量
private static readonly DynamicVar _charges =
    ModCardVars.Int("Leech", amount: 3)
        .WithSharedTooltip("TEST_LEECH");

protected override IEnumerable<DynamicVar> CanonicalVars => [
    new DamageVar(12, ValueProp.Move),
    _charges
];
```

描述中使用自定义变量名：

```json
{
    "PERSONALMOD_CARD_TEST_CARD.description": "[gold]Leech[/gold]{Leech:diff()}.\nDeal {Damage:diff()} damage."
}
```

---

## 7. Formatter 语法

Formatter 用于格式化变量的显示形式，语法为 `{Variable:formatter()}`。基于 `SmartFormat` 库。

### 7.1 游戏自定义 Formatter

| Formatter | 说明 | 示例 |
|-----------|------|------|
| `diff()` | 高于基础值变绿，低于基础值变红。用于升级预览和战斗数值变化 | `{Damage:diff()}` |
| `inverseDiff()` | 高于基础值变红，低于基础值变绿（与 diff 相反） | `{HpLoss:inverseDiff()}` |
| `energyIcons()` | 将数值渲染为能量图标 | `{Energy:energyIcons()}` |
| `energyIcons(n)` | 渲染 n 个固定能量图标 | `{energyPrefix:energyIcons(1)}` |
| `starIcons()` | 将数值渲染为辉星图标 | `{Stars:starIcons()}` |
| `IfUpgraded:show` | 根据升级状态显示不同文本 | `{IfUpgraded:show:Upgraded text\|Normal text}` |
| `abs()` | 绝对值 | `{Damage:abs()}` |
| `percentMore()` | 将乘数转为增加百分比（1.25 → 25%） | `{Boost:percentMore()}` |
| `percentLess()` | 将乘数转为减少百分比（0.75 → 25%） | `{Reduction:percentLess()}` |

### 7.2 SmartFormat 内置 Formatter

| Formatter | 说明 | 示例 |
|-----------|------|------|
| `cond` | 条件分支 | `{X:cond:>0?Active\|Inactive}` |
| `choose` | 按索引或值选择分支 | `{X:choose(1\|2\|3):one\|two\|three\|other}` |
| `plural` | 复数形式 | `Draw {Cards:diff()} {Cards:plural:card\|cards}.` |
| `list` | 列表拼接 | — |

> **更多 SmartFormat Formatter**: https://github.com/axuno/SmartFormat/wiki

### 7.3 Formatter 组合示例

```
// 条件分支：数值 > 0 时显示额外效果，否则不显示
{FanOfKnivesAmount:cond:>0? to ALL enemies|} dealing {Damage:diff()} damage.

// 升级切换文本
{IfUpgraded:show:ALL cards|1 card} in your [gold]Hand[/gold].

// 复数
Draw {Cards:diff()} {Cards:plural:card|cards}.

// 固定能量图标
Add a 0{energyPrefix:energyIcons(1)} copy of this card.

// 卡牌上下文：仅在战斗中显示额外行
{InCombat:\n(Hits {CalculatedHits:diff()} times.)|}
```

---

## 8. 卡牌独有上下文变量

卡牌描述中可用的额外上下文变量：

| 变量名 | 含义 | 用法示例 |
|--------|------|---------|
| `singleStarIcon` | 星星图标 | `Whenever you gain {singleStarIcon}` |
| `InCombat` | 是否处于战斗 | `{InCombat:\n(Hits {CalculatedHits:diff()} times.)\|}` |
| `IsTargeting` | 当前是否有目标 | `{IsTargeting:\n(Deals {CalculatedDamage:diff()})\|}` |
| `OnTable` | 牌是否在手牌或出牌区 | `{OnTable:cond:true?Active\|Inactive}` |
| `IfUpgraded` | 是否已升级 | `{IfUpgraded:show:ALL cards\|1 card}` |
| `CalculatedHits` | 计算出的命中次数 | `{InCombat:\n(Hits {CalculatedHits:diff()} times.)\|}` |
| `CalculatedDamage` | 计算出的伤害 | `{IsTargeting:\n(Deals {CalculatedDamage:diff()})\|}` |

---

## 9. 完整描述示例

### 9.1 卡牌描述示例

```json
{
    "PERSONALMOD_CARD_HEAVY_SLASH.title": "Heavy Slash",
    "PERSONALMOD_CARD_HEAVY_SLASH.description": "Deal {Damage:diff()} damage."
}
```

升级后伤害 +4，`diff()` 会将增加的数值显示为绿色。

```json
{
    "PERSONALMOD_CARD_ADAPTIVE_STRIKE.title": "Adaptive Strike",
    "PERSONALMOD_CARD_ADAPTIVE_STRIKE.description": "Deal {Damage:diff()} damage.\nAdd a 0{energyPrefix:energyIcons(1)} copy of this card into your [gold]Discard Pile[/gold]."
}
```

```json
{
    "PERSONALMOD_CARD_ACROBATICS.title": "Acrobatics",
    "PERSONALMOD_CARD_ACROBATICS.description": "Draw {Cards:diff()} cards.\nDiscard 1 card."
}
```

多段攻击：

```json
{
    "PERSONALMOD_CARD_MULTI_STRIKE.title": "Multi Strike",
    "PERSONALMOD_CARD_MULTI_STRIKE.description": "Deal {Damage:diff()} damage {Repeat:diff()} times."
}
```

条件显示：

```json
{
    "PERSONALMOD_CARD_FAN_OF_KNIVES.title": "Fan of Knives",
    "PERSONALMOD_CARD_FAN_OF_KNIVES.description": "Deal {Damage:diff()} damage to ALL enemies.{FanOfKnivesAmount:cond:>0? Deals damage for each card in your [gold]Discard Pile[/gold].|}"
}
```

### 9.2 遗物描述示例

```json
{
    "PERSONALMOD_RELIC_DRAW_RELIC.title": "Ornamental Fan",
    "PERSONALMOD_RELIC_DRAW_RELIC.description": "At the start of each combat, draw [blue]{Cards}[/blue] additional cards.",
    "PERSONALMOD_RELIC_DRAW_RELIC.flavor": "A pretty fan that holds the breeze of battle."
}
```

```json
{
    "PERSONALMOD_RELIC_DOUBLE_DAMAGE.title": "Pen Nib",
    "PERSONALMOD_RELIC_DOUBLE_DAMAGE.description": "Every [blue]{AttackCount}[/blue]th time you play an Attack card, deal double damage.",
    "PERSONALMOD_RELIC_DOUBLE_DAMAGE.flavor": "The nib glows with intent."
}
```

### 9.3 能力描述示例

```json
{
    "PERSONALMOD_POWER_STRENGTH.title": "Strength",
    "PERSONALMOD_POWER_STRENGTH.description": "Gain Strength.",
    "PERSONALMOD_POWER_STRENGTH.smartDescription": "[gold]Strength[/gold] increases attack damage dealt by [blue]{Amount}[/blue]."
}
```

---

## 10. 描述编写规范

### 10.1 用词约定

| 中文 | 英文 | BBCode |
|------|------|--------|
| 力量 | Strength | `[gold]Strength[/gold]` |
| 敏捷 | Dexterity | `[gold]Dexterity[/gold]` |
| 格挡 | Block | `[gold]Block[/gold]` |
| 虚弱 | Weak | `[gold]Weak[/gold]` |
| 易伤 | Vulnerable | `[gold]Vulnerable[/gold]` |
| 中毒 | Poison | `[gold]Poison[/gold]` |
| 抽牌堆 | Draw Pile | `[gold]Draw Pile[/gold]` |
| 弃牌堆 | Discard Pile | `[gold]Discard Pile[/gold]` |
| 手牌 | Hand | `[gold]Hand[/gold]` |
| 牌组 | Deck | `[gold]Deck[/gold]` |
| 消耗 | Exhaust | `[gold]Exhaust[/gold]` |
| 升级 | Upgrade | `[gold]Upgrade[/gold]` |
| 转化 | Transform | `[gold]Transform[/gold]` |
| 召唤 | Summon | `[gold]Summon[/gold]` |

### 10.2 编写规则

1. **必须使用 `[gold]` 高亮关键词**: `[gold]Block[/gold]`, `[gold]Strength[/gold]` 等
2. **必须使用 `:diff()` 格式化数值**: `{Damage:diff()}`, `{Block:diff()}` 等
3. **多行描述使用 `\n`**: `Deal {Damage:diff()} damage.\nGain {Block:diff()} Block.`
4. **能量图标使用 `:energyIcons()`**: `{Energy:energyIcons()}` 而非纯数字
5. **BBCode 标签必须正确闭合**: 禁止出现未闭合的标签
6. **数值变量名必须与 C# 中 CanonicalVars 定义的名称一致**

---

## 11. 审查清单

### 11.1 键名检查

- [ ] 键名格式是否为 `{MODID}_{CATEGORY}_{CLASSNAME}.field`？
- [ ] 键名是否全部 UPPER_SNAKE_CASE？
- [ ] C# 类型名与键名转换是否正确（PascalCase → UPPER_SNAKE_CASE）？

### 11.2 字段完整性检查

- [ ] 卡牌是否有 `title` + `description`？
- [ ] 遗物是否有 `title` + `description` + `flavor`？
- [ ] 能力是否有 `title` + `description` + `smartDescription`？
- [ ] 事件是否有 `title` + `description`？

### 11.3 占位符检查

- [ ] 占位符名称是否与 C# 中 `CanonicalVars` 定义的变量名一致？
- [ ] 数值占位符是否使用了 `:diff()` formatter？
- [ ] 能量占位符是否使用了 `:energyIcons()` formatter？
- [ ] 自定义变量 Tooltip 是否在 `static_hover_tips.json` 中定义了对应的 `title` + `description`？

### 11.4 BBCode 检查

- [ ] 所有 `[gold]`, `[blue]` 等标签是否正确闭合？
- [ ] 关键词是否使用了 `[gold]` 高亮？
- [ ] 数值是否使用了 `[blue]` 高亮？
- [ ] 换行是否使用 `\n` 而非其他方式？

### 11.5 文件检查

- [ ] JSON 文件是否为合法的 JSON 格式（无语法错误）？
- [ ] 文件编码是否为 UTF-8？
- [ ] 英文 (`eng`) 本地化文件是否已创建？

---

## 12. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 描述显示原始键名（如 `PERSONALMOD_CARD_TEST_CARD.title`） | 本地化 JSON 缺少对应条目 | 检查 JSON 文件中是否包含正确的键 |
| 数值显示为 0 或空白 | `CanonicalVars` 中未定义对应变量 | 在 C# 的 `CanonicalVars` 中添加对应 Var |
| 占位符不被替换 | 占位符名称与变量名大小写不一致 | 确保 `{Damage}` 与 `DamageVar` 的名称完全匹配 |
| BBCode 标签显示为原始文本 | 标签未正确闭合 | 检查 `[gold]` 是否有对应的 `[/gold]` |
| 升级预览无颜色变化 | 未使用 `:diff()` formatter | 将 `{Damage}` 改为 `{Damage:diff()}` |
| 能量显示为数字而非图标 | 未使用 `:energyIcons()` | 将 `{Energy}` 改为 `{Energy:energyIcons()}` |
| JSON 加载失败 | JSON 语法错误（多余逗号、缺少引号等） | 使用 JSON 校验工具检查格式 |

---

## 13. 参考资源

### 13.1 原版本地化文件

| 文件 | 路径 |
|------|------|
| 卡牌 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\cards.json` |
| 能力 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\powers.json` |
| 遗物 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\relics.json` |
| 药水 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\potions.json` |
| 事件 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\events.json` |
| 关键词 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\card_keywords.json` |
| 悬停提示 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\static_hover_tips.json` |
| 先古对话 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\ancients.json` |
| 怪物 | `D:\杀戮尖塔2Mod\sts2\steam\localization\eng\monsters.json` |

### 13.2 外部文档

| 文档 | 链接 |
|------|------|
| Godot BBCode | https://docs.godotengine.org/zh-cn/4.x/tutorials/ui/bbcode_in_richtextlabel.html |
| SmartFormat Wiki | https://github.com/axuno/SmartFormat/wiki |
| RitsuLib 变量与描述 | https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/05-variable-and-description/ |

---

*最后更新：2026-05-12*
