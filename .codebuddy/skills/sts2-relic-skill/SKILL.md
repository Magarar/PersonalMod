---
name: sts2-relic-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 遗物提供全面的参考与自动检查。
  涵盖遗物定义 (ModRelicTemplate)、动态变量 (DynamicVar)、遗物生命周期 Hook 回调 (OnPlay/AfterObtained/AfterCombatVictory 等)、
  数值修改器 (Modify* 方法)、行为守卫 (Should* 方法)、条件修改 (Try* 方法)、修改通知 (After*Modifying 方法)、
  资源配置 (RelicAssetProfile)、遗物池注册 ([RegisterRelic])、本地化文本、RelicRarity 枚举速查、常用命令 (CardPileCmd 等)、
  以及完整的代码模板与审查清单。
  当用户要求创建新遗物、修改已有遗物逻辑、或排查遗物相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 遗物编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 遗物，核心步骤：
1. 创建遗物类，继承 `ModRelicTemplate`
2. 用 `[RegisterRelic(typeof(XxxPool))]` 注册到遗物池
3. 重写 `Rarity` 属性（必须）
4. 重写 `AssetProfile` 配置图标路径
5. 按需重写生命周期方法（Hook 回调 / Modify 修改器 / Should 守卫等）
6. 编写本地化 JSON（title + description + flavor）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-03-add-relic/

---

## 2. Model ID 规则

RitsuLib 注册的遗物 ID 格式：

```
<MODID>_RELIC_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|---------|---------------|
| `TestRelic` | `{{MODID_UPPER}}_RELIC_TEST_RELIC` |
| `BurningBlood` | `{{MODID_UPPER}}_RELIC_BURNING_BLOOD` |
| `MyCoolRelic` | `{{MODID_UPPER}}_RELIC_MY_COOL_RELIC` |

本地化键必须使用此 ID：

```json
{
  "{{MODID_UPPER}}_RELIC_TEST_RELIC.title": "测试遗物",
  "{{MODID_UPPER}}_RELIC_TEST_RELIC.description": "每回合开始时，抽[blue]{Cards}[/blue]张牌。",
  "{{MODID_UPPER}}_RELIC_TEST_RELIC.flavor": "觉得很眼熟？"
}
```

---

## 3. 基类: ModRelicTemplate

继承链: `ModRelicTemplate` → `RelicModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `Rarity` | `abstract RelicRarity` | 遗物稀有度 |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `RelicAssetProfile` | 空 | 图标路径配置 |
| `CanonicalVars` | `IEnumerable<DynamicVar>` | 空列表 | 遗物数值变量 |
| `IsStackable` | `bool` | `false` | 是否可堆叠 |
| `ShowCounter` | `bool` | `false` | 是否显示计数器 |
| `DisplayAmount` | `int` | `0` | 计数器显示的数值 |
| `IsAllowedInShops` | `bool` | `true` | 是否允许出现在商店 |

### 3.3 RelicModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题本地化 (`relics/{Entry}.title`) |
| `Flavor` | `LocString` | 风味文本 (`relics/{Entry}.flavor`) |
| `Description` | `LocString` (private) | 遗物效果描述 |
| `DynamicDescription` | `LocString` | 带动态变量替换的描述（tooltip 用） |
| `DynamicEventDescription` | `LocString` | 事件选择界面用的动态描述 |
| `IconBaseName` | `string` | 图标基础名 (默认: `Entry.ToLowerInvariant()`) |
| `PackedIconPath` | `string` | packed 图标路径 |
| `PackedIconOutlinePath` | `string` | packed 轮廓图标路径 |
| `BigIconPath` | `string` | 大图标路径 |
| `IsAllowedInShops` | `bool` | 是否允许出现在商店（默认 `true`） |
| `IsUsedUp` | `bool` | 是否一次性消耗遗物（默认 `false`） |
| `HasUponPickupEffect` | `bool` | 拾取时是否有一次性效果（默认 `false`） |
| `SpawnsPets` | `bool` | 是否产生宠物（默认 `false`） |
| `IsStackable` | `bool` | 是否可堆叠（默认 `false`） |
| `AddsPet` | `bool` | 是否添加宠物（默认 `false`） |
| `ShowCounter` | `bool` | 是否显示计数器（默认 `false`） |
| `DisplayAmount` | `int` | 计数数值（默认 `0`） |
| `MerchantCost` | `int` | 商店价格，根据稀有度自动计算 |
| `FlashSfx` | `string` | 遗物触发时的音效路径 |
| `ShouldFlashOnPlayer` | `bool` | 触发时是否在玩家身上闪烁（默认 `true`） |
| `StackCount` | `int` | 当前堆叠层数（默认 `1`） |
| `Owner` | `Player` | 遗物所属玩家 |
| `FloorAddedToDeck` | `int` | 获取遗物的楼层 |
| `Status` | `RelicStatus` | 遗物状态 (`Normal/Active/Disabled`) |
| `DynamicVars` | `DynamicVarSet` | 动态变量集合 |
| `IsWax` | `bool` | 是否是蜡封遗物 ([SavedProperty]) |
| `IsMelted` | `bool` | 是否已融化 ([SavedProperty]) |

---

## 4. 枚举速查

### 4.1 RelicRarity

```csharp
RelicRarity.Starter    // 起始遗物（角色初始携带）
RelicRarity.Common     // 普通
RelicRarity.Uncommon   // 罕见
RelicRarity.Rare       // 稀有
RelicRarity.Boss       // Boss 遗物
RelicRarity.Special    // 特殊
RelicRarity.Shop       // 商店遗物
RelicRarity.Event      // 事件遗物
RelicRarity.Ancient    // 先古遗物
```

### 4.2 可用的遗物池

```csharp
SharedRelicPool       // 共享遗物池（所有角色可用）
// 角色专属池需要自定义
```

---

## 5. 动态变量 (DynamicVar)

遗物通过 `CanonicalVars` 定义数值变量，用于本地化描述中的占位符。

### 5.1 常用变量类型

| 变量类型 | 用途 | 本地化占位符 |
|---------|------|-------------|
| `IntVar` / `CardsVar` | 整数（如抽牌数） | `{Cards}` 或 `{VarName:diff()}` |
| `HealVar` | 治疗量 | `{Heal:diff()}` |
| `MagicNumberVar` | 通用数值 | `{MagicNumber:diff()}` |
| `DamageVar` | 伤害值 | `{Damage:diff()}` |
| `BlockVar` | 格挡值 | `{Block:diff()}` |
| `EnergyVar` | 能量值 | `{Energy:energyIcons()}` |

### 5.2 定义变量

```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => [
    new CardsVar(1),       // 抽牌数 = 1
    new HealVar(6),        // 治疗 = 6
    new MagicNumberVar(3)  // 通用数值 = 3
];
```

### 5.3 使用变量

```csharp
// 在 Hook 回调中读取变量值
int drawCount = DynamicVars.Cards.IntValue;
decimal healAmount = DynamicVars.Heal.BaseValue;

// 可堆叠遗物：StackCount 会自动累加，可用在计算中
int totalHeal = DynamicVars.Heal.IntValue * StackCount;
```

---

## 6. Hook 回调方法 — 事件通知

遗物的效果通过重写基类 `AbstractModel` 中的虚方法实现。所有 Hook 返回 `Task`，可使用 `async/await`。

> **重要**: 以下签名来自 `AbstractModel` 基类，遗物直接 override 这些方法即可。方法签名必须与基类**完全一致**。

### 6.1 生命周期与回合

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterActEntered` | `Task AfterActEntered()` | 进入新幕后 |
| `BeforeCombatStart` | `Task BeforeCombatStart()` | 战斗开始前 |
| `BeforeCombatStartLate` | `Task BeforeCombatStartLate()` | 战斗开始前（晚阶段） |
| `AfterCombatEnd` | `Task AfterCombatEnd(CombatRoom room)` | 战斗结束后（无论胜负） |
| `AfterCombatVictoryEarly` | `Task AfterCombatVictoryEarly(CombatRoom room)` | 战斗胜利后（早阶段） |
| `AfterCombatVictory` | `Task AfterCombatVictory(CombatRoom room)` | 战斗胜利后 |
| `BeforeSideTurnStart` | `Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, ICombatState state)` | 任意方回合开始前 |
| `AfterSideTurnStart` | `Task AfterSideTurnStart(CombatSide side, ICombatState state)` | 任意方回合开始后 |
| `AfterSideTurnStartLate` | `Task AfterSideTurnStartLate(CombatSide side, ICombatState state)` | 任意方回合开始后（晚阶段） |
| `AfterPlayerTurnStartEarly` | `Task AfterPlayerTurnStartEarly(PlayerChoiceContext ctx, Player player)` | 玩家回合开始后（早阶段） |
| `AfterPlayerTurnStart` | `Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)` | 玩家回合开始后 |
| `AfterPlayerTurnStartLate` | `Task AfterPlayerTurnStartLate(PlayerChoiceContext ctx, Player player)` | 玩家回合开始后（晚阶段） |
| `BeforeTurnEndVeryEarly` | `Task BeforeTurnEndVeryEarly(PlayerChoiceContext ctx, CombatSide side)` | 回合结束前（极早阶段） |
| `BeforeTurnEndEarly` | `Task BeforeTurnEndEarly(PlayerChoiceContext ctx, CombatSide side)` | 回合结束前（早阶段） |
| `BeforeTurnEnd` | `Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)` | 回合结束前 |
| `AfterTurnEnd` | `Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)` | 回合结束后 |
| `AfterTurnEndLate` | `Task AfterTurnEndLate(PlayerChoiceContext ctx, CombatSide side)` | 回合结束后（晚阶段） |
| `AfterTakingExtraTurn` | `Task AfterTakingExtraTurn(Player player)` | 额外回合结束后 |

### 6.2 卡牌相关

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterCardDrawnEarly` | `Task AfterCardDrawnEarly(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)` | 卡牌抽到后（早阶段） |
| `AfterCardDrawn` | `Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)` | 卡牌抽到后 |
| `BeforeCardPlayed` | `Task BeforeCardPlayed(CardPlay cardPlay)` | 卡牌打出前 |
| `AfterCardPlayed` | `Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)` | 卡牌打出后 |
| `AfterCardPlayedLate` | `Task AfterCardPlayedLate(PlayerChoiceContext ctx, CardPlay cardPlay)` | 卡牌打出后（晚阶段） |
| `BeforeCardAutoPlayed` | `Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)` | 卡牌自动打出前 |
| `AfterCardDiscarded` | `Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)` | 卡牌被丢弃后 |
| `AfterCardExhausted` | `Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)` | 卡牌被消耗后 |
| `BeforeCardRemoved` | `Task BeforeCardRemoved(CardModel card)` | 卡牌被移除前 |
| `AfterCardChangedPiles` | `Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)` | 卡牌切换牌堆后 |
| `AfterCardChangedPilesLate` | `Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)` | 卡牌切换牌堆后（晚阶段） |
| `AfterCardEnteredCombat` | `Task AfterCardEnteredCombat(CardModel card)` | 卡牌进入战斗后 |
| `AfterCardGeneratedForCombat` | `Task AfterCardGeneratedForCombat(CardModel card, Player? creator)` | 卡牌为战斗生成后 |
| `AfterAddToDeckPrevented` | `Task AfterAddToDeckPrevented(CardModel card)` | 添加卡牌到牌组被阻止后 |

### 6.3 伤害与攻击

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeAttack` | `Task BeforeAttack(AttackCommand command)` | 攻击前 |
| `AfterAttack` | `Task AfterAttack(PlayerChoiceContext ctx, AttackCommand command)` | 攻击后 |
| `AfterDamageGiven` | `Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)` | 造成伤害后 |
| `BeforeDamageReceived` | `Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` | 受到伤害前 |
| `AfterDamageReceived` | `Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)` | 受到伤害后 |
| `AfterDamageReceivedLate` | `Task AfterDamageReceivedLate(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)` | 受到伤害后（晚阶段） |

### 6.4 生命值与格挡

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterCurrentHpChanged` | `Task AfterCurrentHpChanged(Creature creature, decimal delta)` | HP 变化后 |
| `BeforeBlockGained` | `Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)` | 获得格挡前 |
| `AfterBlockGained` | `Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)` | 获得格挡后 |
| `AfterBlockCleared` | `Task AfterBlockCleared(Creature creature)` | 格挡被清除后 |
| `AfterBlockBroken` | `Task AfterBlockBroken(Creature creature)` | 格挡被打碎后 |

### 6.5 死亡相关

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeDeath` | `Task BeforeDeath(Creature creature)` | 死亡前 |
| `AfterDeath` | `Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)` | 死亡后 |
| `AfterDiedToDoom` | `Task AfterDiedToDoom(PlayerChoiceContext ctx, IReadOnlyList<Creature> creatures)` | 因毁灭死亡后 |
| `AfterOstyRevived` | `Task AfterOstyRevived(Creature osty)` | Osty 复活后 |
| `AfterPreventingDeath` | `Task AfterPreventingDeath(Creature creature)` | 阻止死亡后 |

### 6.6 能量相关

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterEnergyReset` | `Task AfterEnergyReset(Player player)` | 能量重置后 |
| `AfterEnergyResetLate` | `Task AfterEnergyResetLate(Player player)` | 能量重置后（晚阶段） |
| `AfterEnergySpent` | `Task AfterEnergySpent(CardModel card, int amount)` | 能量消耗后 |

### 6.7 抽牌/弃牌阶段

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeHandDraw` | `Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState state)` | 抽牌前 |
| `BeforeHandDrawLate` | `Task BeforeHandDrawLate(Player player, PlayerChoiceContext ctx, ICombatState state)` | 抽牌前（晚阶段） |
| `BeforeFlush` | `Task BeforeFlush(PlayerChoiceContext ctx, Player player)` | 弃牌前 |
| `BeforeFlushLate` | `Task BeforeFlushLate(PlayerChoiceContext ctx, Player player)` | 弃牌前（晚阶段） |
| `AfterFlush` | `Task AfterFlush(PlayerChoiceContext ctx, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)` | 弃牌后 |
| `AfterHandEmptied` | `Task AfterHandEmptied(PlayerChoiceContext ctx, Player player)` | 手牌清空后 |
| `AfterPreventingDraw` | `Task AfterPreventingDraw()` | 阻止抽牌后 |

### 6.8 能力/球体/洗牌

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterCreatureAddedToCombat` | `Task AfterCreatureAddedToCombat(Creature creature)` | 生物加入战斗后 |
| `BeforePowerAmountChanged` | `Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)` | 力量值改变前 |
| `AfterPowerAmountChanged` | `Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)` | 力量值改变后 |
| `AfterOrbChanneled` | `Task AfterOrbChanneled(PlayerChoiceContext ctx, Player player, OrbModel orb)` | 法球被引导后 |
| `AfterOrbEvoked` | `Task AfterOrbEvoked(PlayerChoiceContext ctx, OrbModel orb, IEnumerable<Creature> targets)` | 法球被激发后 |
| `AfterShuffle` | `Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)` | 洗牌后 |

### 6.9 药水相关

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforePotionUsed` | `Task BeforePotionUsed(PotionModel potion, Creature? target)` | 药水使用前 |
| `AfterPotionUsed` | `Task AfterPotionUsed(PotionModel potion, Creature? target)` | 药水使用后 |
| `AfterPotionDiscarded` | `Task AfterPotionDiscarded(PotionModel potion)` | 药水被丢弃后 |
| `AfterPotionProcured` | `Task AfterPotionProcured(PotionModel potion)` | 药水被获取后 |

### 6.10 地图/房间/事件/商店/奖励

| 方法 | 签名 | 说明 |
|------|------|------|
| `BeforeRoomEntered` | `Task BeforeRoomEntered(AbstractRoom room)` | 进入房间前 |
| `AfterRoomEntered` | `Task AfterRoomEntered(AbstractRoom room)` | 进入房间后 |
| `AfterMapGenerated` | `Task AfterMapGenerated(ActMap map, int actIndex)` | 地图生成后 |
| `AfterItemPurchased` | `Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)` | 购买物品后 |
| `AfterRewardTaken` | `Task AfterRewardTaken(Player player, Reward reward)` | 领取奖励后 |
| `AfterRestSiteHeal` | `Task AfterRestSiteHeal(Player player, bool isMimicked)` | 篝火治疗后 |
| `AfterRestSiteSmith` | `Task AfterRestSiteSmith(Player player)` | 篝火锻造后 |
| `AfterGoldGained` | `Task AfterGoldGained(Player player)` | 获得金币后 |
| `AfterStarsGained` | `Task AfterStarsGained(int amount, Player gainer)` | 获得星星后 |
| `AfterStarsSpent` | `Task AfterStarsSpent(int amount, Player spender)` | 消耗星星后 |
| `AfterForge` | `Task AfterForge(decimal amount, Player forger, AbstractModel? source)` | 锻造后 |
| `AfterSummon` | `Task AfterSummon(PlayerChoiceContext ctx, Player summoner, decimal amount)` | 召唤后 |

### 6.11 遗物本身

| 方法 | 签名 | 说明 |
|------|------|------|
| `AfterObtained` | `Task AfterObtained()` | 获得遗物后 |
| `AfterRemoved` | `Task AfterRemoved()` | 遗物被移除后 |
| `IsAllowed` | `bool IsAllowed(IRunState runState)` | 是否允许在当前跑酷中 |
| `IsAllowedAtNeow` | `bool IsAllowedAtNeow(Player player)` | 是否允许在 Neow 处获得 |

---

## 7. Modify 修改器方法 — 数值修改

这些方法通过**返回修改后的值**来影响游戏数值。默认返回不修改的原始值，遗物可 override 来叠加效果。

### 7.1 伤害修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `0m` | 伤害加法修正 |
| `ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `1m` | 伤害乘法修正（如 PenNib 返回 `2m`） |
| `ModifyDamageCap(Creature target, ValueProp props, Creature dealer, CardModel cardSource)` | `decimal.MaxValue` | 伤害上限 |
| `ModifyAttackHitCount(AttackCommand attack, int hitCount)` | `hitCount` | 攻击命中次数 |

### 7.2 格挡修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyBlockAdditive(Creature? target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay)` | `0m` | 格挡加法修正 |
| `ModifyBlockMultiplicative(Creature? target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay)` | `1m` | 格挡乘法修正 |

### 7.3 能量修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyEnergyGain(Player player, decimal amount)` | `amount` | 能量获取修改（如 Ectoplasm） |
| `ModifyMaxEnergy(Player player, decimal amount)` | `amount` | 最大能量修改 |

### 7.4 抽牌修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyHandDraw(Player player, decimal count)` | `count` | 抽牌数修改（如 BagOfPreparation） |
| `ModifyHandDrawLate(Player player, decimal count)` | `count` | 抽牌数修改（晚阶段） |

### 7.5 HP 相关修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyRestSiteHealAmount(Creature creature, decimal amount)` | `amount` | 篝火治疗量修改 |
| `ModifyHpLostBeforeOsty(Creature? target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)` | `amount` | Osty 触发前 HP 损失修改 |
| `ModifyHpLostBeforeOstyLate(...)` | `amount` | Osty 前修改（晚阶段） |
| `ModifyHpLostAfterOsty(...)` | `amount` | Osty 触发后 HP 损失修改 |
| `ModifyHpLostAfterOstyLate(...)` | `amount` | Osty 后修改（晚阶段） |

### 7.6 力量修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)` | `amount` | 给予力量值修改 |

### 7.7 卡牌相关修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyCardPlayCount(CardModel card, Creature? target, int playCount)` | `playCount` | 卡牌打出次数修改 |
| `ModifyCardPlayResultPileTypeAndPosition(CardModel? card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)` | `(pileType, position)` | 修改卡牌打出后去向 |
| `ModifyXValue(CardModel card, int originalValue)` | `originalValue` | 修改 X 卡牌的 X 值 |
| `ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)` | `options` | 修改卡牌奖励创建选项 |
| `ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)` | `odds` | 修改卡牌奖励升级概率 |

### 7.8 商店/地图/其他修改器

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)` | `cost` | 商店价格修改 |
| `ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)` | `options` | 商店卡池修改 |
| `ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)` | `map` | 修改生成的地图 |
| `ModifyOrbValue(OrbModel orb, decimal value)` | `value` | 球体数值修改 |
| `ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount)` | `triggerCount` | 球体被动触发次数修改 |
| `ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)` | `amount` | 召唤数量修改 |

---

## 8. Should 守卫方法 — 行为允许/阻止

这些方法返回 `bool`，返回 `false` 可阻止对应行为发生。

| 方法 | 默认返回 | 说明 |
|------|---------|------|
| `ShouldGainGold(decimal amount, Player player)` | `true` | 是否允许获得金币（如 Ectoplasm 返回 `false`） |
| `ShouldGainStars(decimal amount, Player player)` | `true` | 是否允许获得星星 |
| `ShouldGainBlock(Creature creature, decimal amount)` | `true` | 是否允许获得格挡 |
| `ShouldDraw(Player player, bool fromHandDraw)` | `true` | 是否允许抽牌 |
| `ShouldDie(Creature creature)` | `true` | 是否允许死亡 |
| `ShouldDieLate(Creature creature)` | `true` | 是否允许死亡（晚阶段） |
| `ShouldClearBlock(Creature creature)` | `true` | 是否清除格挡 |
| `ShouldPlay(CardModel card, AutoPlayType autoPlayType)` | `true` | 是否允许打出卡牌 |
| `ShouldFlush(Player player)` | `true` | 是否允许弃牌 |
| `ShouldPlayerResetEnergy(Player player)` | `true` | 是否允许重置能量 |
| `ShouldAllowTargeting(Creature target)` | `true` | 是否允许选择目标 |
| `ShouldAllowHitting(Creature creature)` | `true` | 是否允许攻击目标 |
| `ShouldAddToDeck(CardModel card)` | `true` | 是否允许添加卡牌到牌组 |
| `ShouldAfflict(CardModel card, AfflictionModel affliction)` | `true` | 是否允许附加异常状态 |
| `ShouldPowerBeRemovedOnDeath(PowerModel power)` | `true` | 死亡时是否移除力量 |
| `ShouldStopCombatFromEnding()` | `false` | 是否阻止战斗结束 |
| `ShouldTakeExtraTurn(Player player)` | `false` | 是否进行额外回合 |
| `ShouldGainEnergy(Player player, decimal amount)` | `true` | 是否允许获得能量 |
| `ShouldGainMaxEnergy(Player player, decimal amount)` | `true` | 是否允许增加最大能量 |
| `ShouldGenerateTreasure(Player player)` | `true` | 是否允许生成宝藏 |
| `ShouldEtherealTrigger(CardModel card)` | `true` | 是否触发虚无 |
| `ShouldRefillMerchantEntry(MerchantEntry entry, Player player)` | `false` | 是否补充商店条目 |

---

## 9. Try 条件修改方法

这些方法返回 `bool`（表示是否进行了修改）+ `out` 参数（修改后的值）。

| 方法 | 说明 |
|------|------|
| `bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)` | 修改添加到牌组的卡牌 |
| `bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)` | 修改战斗中卡牌费用 |
| `bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)` | 修改接收的力量值 |
| `bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)` | 修改篝火选项 |
| `bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)` | 修改篝火治疗奖励 |
| `bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)` | 修改奖励列表 |

---

## 10. After*Modifying 通知方法

这些方法在其他 Hook 的 Modify 方法修改完数值后触发，用于观察最终修改结果。

| 方法 | 说明 |
|------|------|
| `Task AfterModifyingDamageAmount(CardModel? cardSource)` | 伤害数值被修改后 |
| `Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay cardPlay)` | 格挡数值被修改后 |
| `Task AfterModifyingEnergyGain()` | 能量获取被修改后 |
| `Task AfterModifyingHandDraw()` | 抽牌数被修改后 |
| `Task AfterModifyingCardPlayCount(CardModel card)` | 卡牌打出次数被修改后 |
| `Task AfterModifyingCardRewardOptions()` | 卡牌奖励选项被修改后 |
| `Task AfterModifyingPowerAmountReceived(PowerModel power)` | 接收力量值被修改后 |
| `Task AfterModifyingRewards()` | 奖励被修改后 |

---

## 11. 常用命令 (Commands)

在遗物 Hook 回调中常用的命令：

命名空间: `MegaCrit.Sts2.Core.Commands`

| 命令 | 说明 | 示例 |
|------|------|------|
| `CardPileCmd.Draw(ctx, count, player)` | 抽牌 | `await CardPileCmd.Draw(ctx, 1, player)` |
| `DamageCmd.Attack(amount)` | 造成伤害 | `.FromCard(this).Targeting(target).Execute(ctx)` |
| `BlockCmd.GainBlock(amount)` | 获得格挡 | `.FromCard(this).Execute(ctx)` |
| `PowerCmd.ApplyPower(power, target)` | 施加能力 | `.Execute(ctx)` |

### 11.1 async/await 说明

- 所有 Hook 回调方法返回 `Task`，可使用 `async/await`
- `await` 会等待当前效果动画播放完毕后再继续
- 不需要异步时返回 `Task.CompletedTask`

---

## 12. 资源配置 (RelicAssetProfile)

### 12.1 基本配置

```csharp
public override RelicAssetProfile AssetProfile => new(
    IconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",           // 小图标（原版 85x85）
    IconOutlinePath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",    // 轮廓图标（原版 85x85）
    BigIconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png"         // 大图标（原版 256x256）
);
```

### 12.2 图片尺寸参考

| 类型 | 推荐尺寸 | 说明 |
|------|---------|------|
| IconPath | 85x85 | 小图标（遗物栏显示） |
| IconOutlinePath | 85x85 | 轮廓图标 |
| BigIconPath | 256x256 | 大图标（检查/详情界面） |

### 12.3 图片文件位置

```
{{MODID}}/{{MODID}}/images/relics/
├── TestRelic.png              # 图标（可复用为三种图标）
├── TestRelic_outline.png      # 轮廓（推荐分开）
└── big/
    └── TestRelic.png          # 大图标（推荐分开）
```

### 12.4 原版遗物资源路径约定

原版遗物资源路径（Mod 不需要遵循，但可参考）：

| 资源 | 路径 |
|------|------|
| 图集 | `atlases/relic_atlas.sprites/{iconbasename}.tres` |
| 大图标 | `relics/{iconbasename}.png` |
| 轮廓 | `relics/{iconbasename}_outline.png` |

`iconbasename` = `Entry.ToLowerInvariant()`

---

## 13. 遗物池注册

### 13.1 注册到已有遗物池

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models.RelicPools;

// 注册到共享遗物池
[RegisterRelic(typeof(SharedRelicPool))]
public class TestRelic : ModRelicTemplate { ... }
```

### 13.2 注册为角色起始遗物

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
[RegisterCharacterStarterRelic(typeof(MyCharacter))]
public class MyStarterRelic : ModRelicTemplate { ... }
```

### 13.3 三种注册方式

```csharp
// 方式1: 属性注册（需 ModTypeDiscoveryHub.RegisterModAssembly）
[RegisterRelic(typeof(SharedRelicPool))]
public class MyRelic : ModRelicTemplate { ... }

// 方式2: 流式构建器
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Relic<SharedRelicPool, MyRelic>()
    .Apply();

// 方式3: Manifest 注册
new RelicRegistrationEntry<SharedRelicPool, MyRelic>()
```

### 13.4 自定义遗物池

```csharp
// 定义自定义遗物池
[RegisterSharedRelicPool]
public class MyRelicPool : TypeListRelicPoolModel
{
    public override string Title => "My Relic Pool";
    // 其他必要属性...
}

// 注册到自定义池
[RegisterRelic(typeof(MyRelicPool))]
public class MyRelic : ModRelicTemplate { ... }
```

---

## 14. 本地化

### 14.1 文件位置

```
{{MODID}}/{{MODID}}/localization/eng/relics.json
{{MODID}}/{{MODID}}/localization/zhs/relics.json
```

### 14.2 格式

```json
{
    "{{MODID_UPPER}}_RELIC_TEST_RELIC.title": "测试遗物",
    "{{MODID_UPPER}}_RELIC_TEST_RELIC.description": "每回合开始时，抽[blue]{Cards}[/blue]张牌。",
    "{{MODID_UPPER}}_RELIC_TEST_RELIC.flavor": "觉得很眼熟？"
}
```

### 14.3 遗物本地化三个字段

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 遗物名称 | 是 |
| `description` | 遗物效果描述 | 是 |
| `flavor` | 风味文本（斜体展示） | 推荐 |

### 14.4 描述占位符

| 占位符 | 对应 | 说明 |
|--------|------|------|
| `{Cards}` | `CardsVar(n)` | 抽牌/数值 |
| `{Heal:diff()}` | `HealVar(n)` | 治疗量 |
| `{Damage:diff()}` | `DamageVar(n)` | 伤害值 |
| `{Block:diff()}` | `BlockVar(n)` | 格挡值 |
| `{MagicNumber:diff()}` | `MagicNumberVar(n)` | 通用数值 |
| `{Energy:energyIcons()}` | 能量 | 渲染能量图标 |

### 14.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮（用于关键词） |
| `[blue]文字[/blue]` | 蓝色（用于数值） |
| `[b]文字[/b]` | 加粗 |
| `[purple]文字[/purple]` | 紫色 |

---

## 15. 完整代码模板

### 15.1 回合开始抽牌遗物（Hook 模式）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class DrawEachTurnRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png"
    );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}
```

### 15.2 战斗胜利回血遗物（可堆叠）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class HealAfterCombatRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool IsStackable => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(6)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png"
    );

    public override Task AfterCombatVictory(CombatRoom room)
    {
        // 堆叠时 StackCount 自动累加，治疗量也随之增加
        // 治疗逻辑需通过命令执行
        return Task.CompletedTask;
    }
}
```

### 15.3 修改抽牌数遗物（Modify 模式）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class ExtraDrawRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner) return count;
        return count + DynamicVars.Cards.BaseValue;
    }
}
```

### 15.4 伤害翻倍遗物（Modify 乘法模式）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class DoubleDamageRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override decimal ModifyDamageMultiplicative(
        Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
    {
        if (dealer != Owner) return 1m;
        return 2m;
    }
}
```

### 15.5 计数器遗物（如笔尖 PenNib 模式）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class CounterRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;

    private int _attackCount;

    public override int DisplayAmount => _attackCount;

    public override decimal ModifyDamageMultiplicative(
        Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
    {
        if (dealer != Owner || _attackCount < 10) return 1m;
        return 2m;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card?.CardType == CardType.Attack)
        {
            _attackCount++;
        }
        return Task.CompletedTask;
    }
}
```

### 15.6 阻止行为遗物（Should 模式，如黏液凝胶）

```csharp
[RegisterRelic(typeof(SharedRelicPool))]
public class NoGoldRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Boss;

    // 阻止自己获得金币
    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return player != Owner;
    }

    // 修改最大能量
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner) return amount;
        return amount + 1;
    }
}
```

### 15.7 使用抽象基类统一管理（推荐）

```csharp
[RegisterRelic(typeof(SharedRelicPool), Inherit = true)]
public abstract class {{MODID}}RelicModel : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
    IconPath: $"res://{{MODID}}/images/relics/{GetType().Name}.png",
    IconOutlinePath: $"res://{{MODID}}/images/relics/{GetType().Name}_outline.png",
    BigIconPath: $"res://{{MODID}}/images/relics/big/{GetType().Name}.png"
    );
}

// 子类只需关注逻辑
public class MyDrawRelic : {{MODID}}RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}
```

### 15.8 最简遗物模板（快速起步）

```csharp
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MyRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;
}
```

> 注意：最简模板缺少图标配置和效果回调，仅用于快速验证注册是否成功。正式遗物需补充 `AssetProfile` 和至少一个 Hook 回调。

---

## 16. 遗物效果实现模式总结

### 16.1 Hook 模式 — 事件触发后执行逻辑

适用场景：卡牌打出后、战斗胜利后、抽牌后等"某事后做某事"。

```csharp
// 例：每回合开始抽牌
public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
{
    await CardPileCmd.Draw(ctx, 1, player);
}
```

### 16.2 Modify 模式 — 修改游戏数值

适用场景：增加伤害、增加格挡、增加抽牌数等"数值加成"。

```csharp
// 例：伤害 +3
public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
{
    return dealer == Owner ? amount + 3 : 0m;
}

// 例：抽牌数 +1
public override decimal ModifyHandDraw(Player player, decimal count)
{
    return player == Owner ? count + 1 : count;
}
```

### 16.3 Should 模式 — 允许/阻止行为

适用场景：阻止死亡、阻止获得金币、阻止弃牌等"阻止某事发生"。

```csharp
// 例：阻止死亡
public override bool ShouldDie(Creature creature)
{
    return creature != Owner;
}
```

### 16.4 Counter 模式 — 计数器 + 条件触发

适用场景：N 次攻击后双倍伤害、积累次数后触发效果等。

```csharp
public override bool ShowCounter => true;
private int _count;
public override int DisplayAmount => _count;
```

### 16.5 选择模式（如果需要额外选择）

遗物获得后打开选择界面：

```csharp
public override bool HasUponPickupEffect => true;
public override async Task AfterObtained()
{
    // 显示选择界面或直接执行效果
}
```

---

## 17. 文件组织

```
{{MODID}}/{{MODID}}Code/Relics/
├── {{MODID}}RelicModel.cs          # 抽象基类（可选）
├── DrawEachTurnRelic.cs           # 回合抽牌遗物
├── HealAfterCombatRelic.cs        # 战后回血遗物
└── OnCardPlayRelic.cs             # 卡牌打出遗物

{{MODID}}/{{MODID}}/
├── images/
│   └── relics/
│       ├── DrawEachTurnRelic.png       # 图标
│       ├── DrawEachTurnRelic_outline.png  # 轮廓
│       ├── HealAfterCombatRelic.png
│       ├── HealAfterCombatRelic_outline.png
│       └── big/                         # 大图标（可选）
│           ├── DrawEachTurnRelic.png
│           └── HealAfterCombatRelic.png
└── localization/
    ├── eng/
    │   └── relics.json            # 英文本地化
    └── zhs/
        └── relics.json            # 中文本地化
```

---

## 18. 参考已有遗物实现

需要查找类似功能的遗物时，在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 战后回血 | `Models/Relics/` | `BurningBlood`, `RedVeil` |
| 回合抽牌 | `Models/Relics/` | `OrnamentalFan`, `BagOfPreparation` |
| 能量相关 | `Models/Relics/` | `IceCream`, `CrackedCore`, `Ectoplasm` |
| 格挡增强 | `Models/Relics/` | `Breach`, `Calipers`, `BronzeScales` |
| 伤害增强 | `Models/Relics/` | `Vajra`, `PenNib` |
| 金币相关 | `Models/Relics/` | `HandDrum`, `MembershipCard` |
| 卡牌相关 | `Models/Relics/` | `Astrolabe`, `MoltenEgg`, `Claws` |
| 计数器 | `Models/Relics/` | `PenNib`, `QuestionCard` |
| 阻止行为 | `Models/Relics/` | `Ectoplasm`, `Ostalith` |

源码位置: `{{STS2_SOURCE_ROOT}}\Models\Relics\`

---

## 19. 调试

- 战斗中按 `~` 打开控制台
- 输入 `relic {{MODID_UPPER}}_RELIC_TEST_RELIC` 获取指定遗物
- 检查遗物是否正确注册：在控制台查看 `relic list` 或类似命令
- 遗物描述显示为原始键名是本地化文件缺失的信号

---

## 20. 编写审查清单

### 20.1 基础检查

- [ ] 是否继承了 `ModRelicTemplate`？
- [ ] 是否重写了 `Rarity` 属性？
- [ ] 是否添加了 `[RegisterRelic(typeof(XxxPool))]` 属性？
- [ ] 命名空间是否正确？

### 20.2 数值检查

- [ ] `CanonicalVars` 中是否定义了所有需要的变量？
- [ ] 描述中的占位符名是否与变量名匹配（如 `{Cards}` 对应 `CardsVar`）？
- [ ] 数值是否合理平衡？

### 20.3 逻辑检查

- [ ] Hook 回调方法签名是否与基类虚方法**完全一致**？
- [ ] 是否正确使用了 `async/await`？
- [ ] 是否处理了 `IsStackable`（堆叠遗物需考虑多次触发的数值）？
- [ ] 是否处理了 `null` 检查（如 `cardPlay.Card` 可能为 null）？
- [ ] Modify 方法中是否正确检查了 `Owner`（只影响自己的遗物）？

### 20.4 资源检查

- [ ] `AssetProfile` 中的图标路径是否正确？
- [ ] 图标 PNG 文件是否存在于对应位置？
- [ ] 文件名大小写是否与类名一致？
- [ ] 图标尺寸是否符合要求（85x85 / 256x256）？

### 20.5 本地化检查

- [ ] `relics.json` 中是否添加了 `{MODID}_RELIC_{CLASSNAME}.title`？
- [ ] `relics.json` 中是否添加了 `{MODID}_RELIC_{CLASSNAME}.description`？
- [ ] `relics.json` 中是否添加了 `{MODID}_RELIC_{CLASSNAME}.flavor`？
- [ ] 描述中的 BBCode 标签是否正确闭合？

### 20.6 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] 遗物池类型是否正确（`SharedRelicPool` 或自定义池）？
- [ ] 起始遗物是否额外添加了 `[RegisterCharacterStarterRelic]`？

---

## 21. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 遗物图标显示为空白 | 图标路径错误或缺失 | 检查 `AssetProfile` 三种路径和文件是否存在 |
| 描述显示原始键名 | 本地化 JSON 缺少对应条目 | 检查 relics.json 中键名是否为 `{MODID}_RELIC_{CLASSNAME}.xxx` |
| 遗物效果不触发 | Hook 方法签名不匹配或未正确重写 | 确认方法签名与基类完全一致（参数类型、返回类型） |
| 遗物不在遗物池中出现 | 未注册或注册失败 | 确认 `RegisterModAssembly` 已在 Entry.Init() 中调用 |
| 堆叠后数值异常 | 未考虑 `IsStackable` 时的叠加逻辑 | 堆叠遗物中 `StackCount` 会累加，需在回调中处理 |
| `{Cards}` 显示为 0 | `CanonicalVars` 中未定义对应变量 | 在 `CanonicalVars` 中添加 `CardsVar` 或对应类型 |
| 获得遗物后无反应 | `AfterObtained` 中逻辑有误 | 检查 `AfterObtained` 回调逻辑 |
| 编译错误：找不到类型 | 缺少 using 引用 | 确认引用了 `STS2RitsuLib.Scaffolding.Content`、`STS2RitsuLib.Interop.AutoRegistration` 等命名空间 |
| Modify 无效果 | 未检查 `Owner` | 在 Modify 方法中检查 `dealer == Owner` 或 `player == Owner` |
| Should 阻止了所有人的行为 | 未检查目标 | 在 Should 方法中判断 `player/creature != Owner` 时返回 `true` |

---

## 22. Hook 系统速查（遗物相关）

遗物中的生命周期方法本质上是对 `AbstractModel` 虚方法的重写。直接 override 即可。

在能力或自定义逻辑中需要监听事件时，使用 `Hook` 静态类：

```csharp
// 在能力中使用 Hook（非遗物场景）
Hook.AfterCardPlayed += OnCardPlayed;

// 或者通过 HookListener 接口
```

高级用法可参考 `sts2-core-ref` Skill 的 `references/hooks-reference.md`。

---

*最后更新：2026-05-12*
