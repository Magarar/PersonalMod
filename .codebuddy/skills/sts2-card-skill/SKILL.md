---
name: sts2-card-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 卡牌提供全面的参考与自动检查。
  涵盖卡牌定义 (ModCardTemplate)、动态变量 (DynamicVar)、卡牌打出逻辑 (OnPlay)、升级逻辑 (OnUpgrade)、
  资源配置 (CardAssetProfile)、卡池注册 ([RegisterCard])、本地化文本、枚举速查、常用命令 (DamageCmd)、
  以及完整的代码模板与审查清单。
  当用户要求创建新卡牌、修改已有卡牌逻辑、或排查卡牌相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 卡牌编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 卡牌，核心步骤：
1. 创建卡牌类，继承 `ModCardTemplate`
2. 用 `[RegisterCard(typeof(XxxPool))]` 注册到卡池
3. 定义 `CanonicalVars`（伤害/格挡等数值）
4. 实现 `OnPlay`（打出逻辑）和 `OnUpgrade`（升级逻辑）
5. 配置 `AssetProfile`（卡图路径）
6. 编写本地化 JSON

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

---

## 2. Model ID 规则

RitsuLib 注册的卡牌 ID 格式：

```
<MODID>_CARD_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|---------|---------------|
| `TestCard` | `{{MODID_UPPER}}_CARD_TEST_CARD` |
| `StrikeIronclad` | `{{MODID_UPPER}}_CARD_STRIKE_IRONCLAD` |
| `HeavySlash` | `{{MODID_UPPER}}_CARD_HEAVY_SLASH` |

本地化键必须使用此 ID：

```json
{
  "{{MODID_UPPER}}_CARD_TEST_CARD.title": "Test Card",
  "{{MODID_UPPER}}_CARD_TEST_CARD.description": "Deal {Damage:diff()} damage.\nDraw {Cards:diff()} cards.\nGain {Block:diff()} Block."
}
```

---

## 3. 基类: ModCardTemplate

继承链: `ModCardTemplate` → `CardModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

构造函数:

```csharp
public ModCardTemplate(
    int energyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool showInCardLibrary = true)
```

### 3.1 必须实现

| 成员 | 类型 | 说明 |
|------|------|------|
| `OnPlay(PlayerChoiceContext, CardPlay)` | `async Task` | 卡牌打出时的核心逻辑 |
| `OnUpgrade()` | `void` | 升级时修改数值 |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CanonicalVars` | `IEnumerable<DynamicVar>` | 空列表 | 基础数值定义 |
| `AssetProfile` | `CardAssetProfile` | 空 | 卡图/卡框等资源 |
| `CanonicalTags` | `HashSet<CardTag>` | 空集合 | Strike / Defend 等标签 |
| `GainsBlock` | `bool` | `false` | 是否获得格挡 |

### 3.3 重要属性

| 属性 | 说明 |
|------|------|
| `DynamicVars.Damage` | 伤害变量（需在 CanonicalVars 中定义 DamageVar） |
| `DynamicVars.Block` | 格挡变量（需在 CanonicalVars 中定义 BlockVar） |
| `DynamicVars.MagicNumber` | 通用变量 |
| `IsUpgraded` | 当前是否已升级 |
| `CurrentUpgradeLevel` / `MaxUpgradeLevel` | 升级等级 |

---

## 4. 枚举速查

### 4.1 CardType

```csharp
CardType.Attack   // 攻击牌
CardType.Skill    // 技能牌
CardType.Power    // 能力牌
CardType.Status   // 状态牌
CardType.Curse    // 诅咒牌
```

### 4.2 CardRarity

```csharp
CardRarity.Basic      // 基础牌（起始卡组）
CardRarity.Common     // 普通
CardRarity.Uncommon   // 罕见
CardRarity.Rare       // 稀有
CardRarity.Special    // 特殊
CardRarity.Ancient    // 先古
```

### 4.3 TargetType

```csharp
TargetType.None                 // 无目标
TargetType.Self                 // 自身
TargetType.AnyEnemy             // 一个敌人（玩家选择）
TargetType.AllEnemies           // 所有敌人
TargetType.RandomEnemy          // 随机一个敌人
TargetType.AnyPlayer            // 任意玩家（含自己）
TargetType.AnyAlly              // 任意友方
TargetType.AllAllies            // 所有友方
TargetType.TargetedNoCreature   // 目标选择，非生物
TargetType.Osty                 // 选择牺牲品（Osty）
```

### 4.4 CardTag

```csharp
CardTag.Strike   // 打击类
CardTag.Defend   // 防御类
```

### 4.5 ValueProp (bitflag)

```csharp
ValueProp.Move           // 受力量/敏捷等修正（卡牌造成）
ValueProp.Unpowered      // 不受力量修正
ValueProp.Unblockable    // 不可格挡
ValueProp.SkipHurtAnim   // 跳过受伤动画
// 可组合: ValueProp.Unblockable | ValueProp.Unpowered
```

---

## 5. 动态变量 (DynamicVar)

### 5.1 内置变量类型

| 变量类型 | 命名空间 | 用途 | 本地化占位符 |
|---------|---------|------|-------------|
| `DamageVar` | `STS2RitsuLib.Cards.DynamicVars` | 伤害 | `{Damage:diff()}` |
| `BlockVar` | `STS2RitsuLib.Cards.DynamicVars` | 格挡 | `{Block:diff()}` |
| `CardsVar` | `MegaCrit.Sts2.Core.Localization.DynamicVars` | 抽牌数 | `{Cards:diff()}` |
| `MagicNumberVar` | `MegaCrit.Sts2.Core.Localization.DynamicVars` | 通用数值 | `{MagicNumber:diff()}` |
| `HealVar` | — | 治疗 | `{Heal:diff()}` |
| `EnergyVar` | — | 能量 | `{Energy:energyIcons()}` |

### 5.2 定义变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => [
    new DamageVar(12, ValueProp.Move),    // 12点伤害，受修正
    new BlockVar(5, ValueProp.Move),      // 5点格挡
];
```

### 5.3 升级修改变量

```csharp
protected override void OnUpgrade()
{
    DynamicVars.Damage.UpgradeValueBy(4);  // 伤害 +4
    DynamicVars.Block.UpgradeValueBy(3);   // 格挡 +3
}
```

### 5.4 自定义变量 (高级)

```csharp
private static readonly DynamicVar _charges =
    ModCardVars.Int("charges", amount: 3)
        .WithSharedTooltip("my_mod_charges");

public override DynamicVarSet CreateDynamicVars() =>
    new DynamicVarSet().Add(_charges);
```

---

## 6. 卡牌打出逻辑 (OnPlay)

### 6.1 基本结构

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 参照 PommelStrike 反编译模式
    ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

    // 1. 抽牌（用 CardsVar 定义数量，不写死）
    _ = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

    // 2. 造成伤害（带 VFX 和音效）
    _ = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
        .FromCard(this)
        .Targeting(cardPlay.Target)
        .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
        .Execute(choiceContext);

    // 3. 获得格挡（需显式调用 CreatureCmd.GainBlock）
    _ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
}
```

| 关键点 | 说明 |
|--------|------|
| `CardsVar(n)` | 在 `CanonicalVars` 中定义抽牌数量，不要硬编码数字 |
| `CardPileCmd.Draw` | 返回 `IEnumerable<CardModel>`，用 `_` 丢弃或变量接收 |
| `GainsBlock` | 必须设为 `true`，用于 UI 预览显示格挡值 |
| `CreatureCmd.GainBlock` | 在 `OnPlay` 中显式调用才能真正获得格挡 |
| `Owner.Creature` | 目标生物（卡牌持有者的生物实体） |
| `DynamicVars.Block` | 直接传入 `BlockVar` 实例（不是 `.BaseValue`） |
| `DynamicVars.Cards.BaseValue` | 传入 `CardsVar` 的基础值（不应硬编码数字） |
| `ArgumentNullException.ThrowIfNull` | 对有目标选择的卡牌做 null 检查（参照原版模式） |
| `.WithHitFx(...)` | 攻击链附加特效路径和音效（参照 PommelStrike 模式） |

### 6.2 常用命令 (Commands)

命名空间: `MegaCrit.Sts2.Core.Commands`

| 命令 | 说明 | 示例 |
|------|------|------|
| `DamageCmd.Attack(amount)` | 造成伤害 | `.Targeting(target).Execute(ctx)` |
| `CreatureCmd.GainBlock(creature, blockVar, cardPlay)` | 获得格挡 | 传 `Owner.Creature` + `DynamicVars.Block` + `cardPlay` |
| `PowerCmd.ApplyPower(power, target)` | 施加能力 | `.Execute(ctx)` |
| `CreatureCmd` | 生物相关命令 | — |
| `VfxCmd` | 特效命令 | — |
| `CardCmd` / `CardPileCmd` | 卡牌相关命令 | 抽牌/弃牌等 |

### 6.3 完整命令链示例

```csharp
// 目标 null 检查
ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

// 抽牌（用 CardsVar 管理数值）
_ = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

// 造成伤害（带特效和音效）
_ = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this)                             // 标记来源卡牌
    .Targeting(cardPlay.Target)                 // 指定目标
    .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")  // VFX 特效
    .Execute(choiceContext);                    // 执行

// 获得格挡（参照 DefendIronclad 的反编译代码）
_ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
```

### 6.4 async/await 说明

- STS2 使用 `async/await` 控制效果顺序执行（类似 STS1 的 Action 队列）
- `await` 会等待当前效果动画播放完毕后再继续
- 所有 `Execute()` 和类似的命令方法都返回 `Task`

---

## 7. 资源配置 (CardAssetProfile)

### 7.1 基本配置

```csharp
public override CardAssetProfile AssetProfile => new(
    PortraitPath: $"res://{{MODID}}/images/card_portraits/{GetType().Name}.png"
);
```

- 卡图任意尺寸，官方尺寸: 普通卡 250x190，先古卡 250x351
- 路径中 `res://PersonalMod/` 对应项目的 modid 资源文件夹

### 7.2 完整配置

```csharp
public override CardAssetProfile AssetProfile => new(
    PortraitPath: $"res://{{MODID}}/images/card_portraits/{GetType().Name}.png",
    FramePath: "",                    // 卡牌背景
    PortraitBorderPath: "",           // 边框（如感染状态牌）
    BannerTexturePath: ""             // 横幅（稀有度等）
);
```

### 7.3 卡图文件位置

```
PersonalMod/PersonalMod/images/card_portraits/
├── TestCard.png          # 对应 res://PersonalMod/images/card_portraits/TestCard.png
└── colorless/            # 可按卡池分子目录
    └── MyColorlessCard.png
```

---

## 8. 卡池注册

### 8.1 注册到已有卡池

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models.CardPools;

// 注册到无色卡池
[RegisterCard(typeof(ColorlessCardPool))]
public class TestCard : ModCardTemplate { ... }

// 注册到铁甲战士卡池
[RegisterCard(typeof(IroncladCardPool))]
public class IroncladStrike : ModCardTemplate { ... }
```

### 8.2 注册到自定义卡池

```csharp
// 1. 定义自定义卡池
[RegisterSharedCardPool]  // 或在 ContentPack 中注册
public class MyCardPool : TypeListCardPoolModel
{
    public override string Title => "My Pool";
    public override string EnergyColorName => "orange";
    public override string CardFrameMaterialPath => "card_frame_orange";
    public override Color DeckEntryCardColor => new("d2a15a");
    public override bool IsColorless => false;
}

// 2. 使用抽象基类统一注册（Inherit = true 会自动注册所有子类）
[RegisterCard(typeof(MyCardPool), Inherit = true)]
public abstract class MyCardModel : ModCardTemplate { ... }
```

### 8.3 注册为角色起始卡

```csharp
[RegisterCard(typeof(ColorlessCardPool))]
[RegisterCharacterStarterCard(typeof(MyCharacter), 5)]  // 5张
public class StarterCard : ModCardTemplate { ... }
```

### 8.4 三种注册方式

```csharp
// 方式1: 属性注册（需 ModTypeDiscoveryHub.RegisterModAssembly）
[RegisterCard(typeof(ColorlessCardPool))]
public class MyCard : ModCardTemplate { ... }

// 方式2: 流式构建器
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Card<MyCardPool, MyCard>()
    .Apply();

// 方式3: Manifest 注册
new CardRegistrationEntry<MyCardPool, MyCard>()
```

---

## 9. 本地化

### 9.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/cards.json
PersonalMod/PersonalMod/localization/zhs/cards.json
```

### 9.2 格式

```json
{
    "PERSONALMOD_CARD_TEST_CARD.title": "Test Card",
    "PERSONALMOD_CARD_TEST_CARD.description": "Deal {Damage:diff()} damage.\nDraw {Cards:diff()} cards.\nGain {Block:diff()} Block."
}
```

### 9.3 描述占位符

| 占位符 | 对应 | 说明 |
|--------|------|------|
| `{Damage:diff()}` | `DamageVar` | 伤害数值（含升级差异） |
| `{Block:diff()}` | `BlockVar` | 格挡数值 |
| `{Cards:diff()}` | `CardsVar` | 抽牌/卡牌数量 |
| `{MagicNumber:diff()}` | `MagicNumberVar` | 通用数值 |
| `{Energy:energyIcons()}` | 能量 | 渲染能量图标 |

### 9.4 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮 |
| `[b]文字[/b]` | 加粗 |
| `[purple]文字[/purple]` | 紫色 |

### 9.5 关键词（可选）

在 `localization/eng/card_keywords.json` 中添加自定义关键词解释。

---

## 10. 完整代码模板

### 10.1 攻击卡模板

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class MyAttackCard : ModCardTemplate
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.AnyEnemy;

    public MyAttackCard()
        : base(EnergyCost, Type, Rarity, Target, showInCardLibrary: true)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://{{MODID}}/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
```

### 10.2 防御卡模板

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class MyDefendCard : ModCardTemplate
{
    private const int EnergyCost = 1;

    public MyDefendCard()
        : base(EnergyCost, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
```

### 10.3 技能卡（抽牌+格挡）模板

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class MySkillCard : ModCardTemplate
{
    private const int EnergyCost = 1;

    public MySkillCard()
        : base(EnergyCost, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8, ValueProp.Move),
        new CardsVar(2),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        _ = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
```

### 10.4 能力卡模板

```csharp
[RegisterCard(typeof(ColorlessCardPool))]
public class MyPowerCard : ModCardTemplate
{
    private const int EnergyCost = 2;

    public MyPowerCard()
        : base(EnergyCost, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MagicNumberVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加一个能力
        // await PowerCmd.ApplyPower(new MyCustomPower(), target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.MagicNumber.UpgradeValueBy(1);
    }
}
```

### 10.5 使用抽象基类统一管理（推荐）

```csharp
[RegisterCard(typeof(MyCardPool), Inherit = true)]
public abstract class PersonalModCardModel : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://{{MODID}}/images/card_portraits/{GetType().Name}.png"
    );

    protected PersonalModCardModel(
        int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool showInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, showInCardLibrary)
    {
    }
}

// 子类只需关注逻辑
[RegisterCard(typeof(MyCardPool))]  // 子类也需要 RegisterCard（如果 Inherit=true 则不需要）
public class MyCard : PersonalModCardModel
{
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(play.Target!)
            .Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
```

---

## 11. 文件组织

```
{{MODID}}/{{MODID}}Code/Cards/
├── PersonalModCardModel.cs       # 抽象基类（可选）
├── MyAttackCard.cs               # 攻击卡
├── MyDefendCard.cs               # 防御卡
└── MyPowerCard.cs                # 能力卡

{{MODID}}/{{MODID}}/
├── images/
│   └── card_portraits/
│       ├── MyAttackCard.png      # 卡图
│       ├── MyDefendCard.png
│       └── MyPowerCard.png
└── localization/
    ├── eng/
    │   └── cards.json            # 英文本地化
    └── zhs/
        └── cards.json            # 中文本地化
```

---

## 12. 参考已有卡牌实现

需要查找类似功能的卡牌时，在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 单体攻击 | `Models/Cards/ironclad/` | `Strike`, `PommelStrike` |
| AOE攻击 | `Models/Cards/` | `Cleave`, `Whirlwind` |
| 获得格挡 | `Models/Cards/` | `Defend`, `ShrugItOff` |
| 抽牌 | `Models/Cards/` | `BattleTrance`, `PommelStrike` |
| 施加能力 | `Models/Cards/` | `Flex`, `Warcry`, `Inflame` |
| 能力牌 | `Models/Cards/` | `DemonForm`, `Barricade` |
| 多段攻击 | `Models/Cards/` | `SwordBoomerang`, `RiddleWithHoles` |

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Cards\`

---

## 13. 调试

- 战斗中按 `~` 打开控制台
- 输入 `card PERSONALMOD_CARD_TEST_CARD` 获取指定卡牌
- 只能在战斗中使用命令获得卡牌
- 图鉴中显示 `???` 是正常的，需要先在游戏中遇到该卡

---

## 14. 编写审查清单

### 14.1 基础检查

- [ ] 是否继承了 `ModCardTemplate`？
- [ ] 构造函数参数是否正确传递 `energyCost, type, rarity, targetType`？
- [ ] 是否添加了 `[RegisterCard(typeof(XxxPool))]` 属性？
- [ ] 命名空间是否正确？

### 14.2 数值检查

- [ ] `CanonicalVars` 中是否定义了所有需要的变量（DamageVar/BlockVar 等）？
- [ ] `ValueProp` 是否设置正确（受修正 vs 不受修正）？
- [ ] `OnUpgrade` 中是否正确修改了对应的变量？

### 14.3 逻辑检查

- [ ] `OnPlay` 是否标记为 `async Task`？
- [ ] 命令链是否完整（`.FromCard()` → `.Targeting()` → `.Execute()`）？
- [ ] 是否正确使用了 `await`？
- [ ] 多个效果是否有正确的执行顺序？

### 14.4 资源检查

- [ ] `AssetProfile` 中的卡图路径是否正确？
- [ ] 卡图 PNG 文件是否存在于对应位置？
- [ ] 文件名大小写是否与类名一致？

### 14.5 本地化检查

- [ ] `cards.json` 中是否添加了 `{MODID}_CARD_{CLASSNAME}.title`？
- [ ] `cards.json` 中是否添加了 `{MODID}_CARD_{CLASSNAME}.description`？
- [ ] 描述中的占位符是否与 `CanonicalVars` 中的变量名匹配？

---

## 15. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 卡牌在左上角显示为空白 | 卡图路径错误或缺失 | 检查 `AssetProfile.PortraitPath` 和文件是否存在 |
| 描述显示原始键名 | 本地化 JSON 中缺少对应条目 | 检查 cards.json 中键名是否为 `{MODID_UPPER}_CARD_{CLASSNAME}.xxx` |
| 打出卡牌没有效果 | `OnPlay` 中命令未正确执行 | 检查命令链是否完整，`cardPlay.Target` 是否为 null |
| 升级后数值没变 | `OnUpgrade` 中修改了错误的变量 | 确认 `DynamicVars.Damage` 等名称与 CanonicalVars 中一致 |
| 注册失败 | `[RegisterCard]` 的卡池类型不匹配 | 确认卡池类名和命名空间正确 |
| 卡牌不在卡池中出现 | 未注册或注册到错误的池 | 确认 `RegisterModAssembly` 已在 Entry.Init() 中调用 |

---

## 16. Hook 系统速查（卡牌相关）

在卡牌或能力中监听战斗事件：

| Hook | 用途 |
|------|------|
| `Hook.AfterCardPlayed` | 任意卡牌打出后 |
| `Hook.BeforeCardPlayed` | 卡牌打出前 |
| `Hook.AfterCardDrawn` | 抽牌后 |
| `Hook.AfterCardDiscarded` | 弃牌后 |
| `Hook.AfterCardExhausted` | 消耗后 |
| `Hook.AfterDamageGiven` | 造成伤害后 |
| `Hook.AfterBlockGained` | 获得格挡后 |

详细 Hook 参考见 `sts2-core-ref` Skill 的 `references/hooks-reference.md`。
