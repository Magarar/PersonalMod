# STS2 Hook 系统参考

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Hooks\Hook.cs` (94 KB)

## 概述

`Hook` 是一个静态类，提供游戏事件回调系统。所有 Hook 方法返回 `Task` (异步)，在特定游戏事件触发时被调用。Hook 的监听者通过 `combatState.IterateHookListeners()` 或 `runState.IterateHookListeners()` 遍历。

---

## 1. 战斗伤害相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforeAttack` | `(ICombatState, AttackCommand)` | 攻击前 |
| `AfterAttack` | `(ICombatState, PlayerChoiceContext, AttackCommand)` | 攻击后 |
| `AfterDamageGiven` | `(PlayerChoiceContext, ICombatState, Creature?, DamageResult, ValueProp, Creature, CardModel?)` | 造成伤害后 |
| `BeforeDamageReceived` | `(PlayerChoiceContext, IRunState, ICombatState?, Creature, decimal, ValueProp, Creature?, CardModel?)` | 受到伤害前 |
| `AfterDamageReceived` | `(PlayerChoiceContext, IRunState, ICombatState?, Creature, DamageResult, ValueProp, Creature?, CardModel?)` | 受到伤害后 |

## 2. 格挡相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforeBlockGained` | `(ICombatState, Creature, decimal, ValueProp, CardModel?)` | 获得格挡前 |
| `AfterBlockGained` | `(ICombatState, Creature, decimal, ValueProp, CardModel?)` | 获得格挡后 |
| `AfterBlockBroken` | `(ICombatState, Creature)` | 格挡被打破后 |
| `AfterBlockCleared` | `(ICombatState, Creature)` | 格挡清除后 |
| `AfterPreventingBlockClear` | `(ICombatState, AbstractModel, Creature)` | 阻止格挡清除后 |

## 3. 卡牌相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforeCardPlayed` | `(ICombatState, CardPlay)` | 卡牌打出前 |
| `AfterCardPlayed` | `(ICombatState, PlayerChoiceContext, CardPlay)` | 卡牌打出后 |
| `AfterCardDrawn` | `(ICombatState, PlayerChoiceContext, CardModel, bool)` | 卡牌抽到后 |
| `AfterCardDiscarded` | `(ICombatState, PlayerChoiceContext, CardModel)` | 卡牌弃掉后 |
| `AfterCardExhausted` | `(ICombatState, PlayerChoiceContext, CardModel, bool)` | 卡牌消耗后 |
| `AfterCardChangedPiles` | `(IRunState, ICombatState?, CardModel, PileType, AbstractModel?)` | 卡牌换堆后 |
| `AfterCardEnteredCombat` | `(ICombatState, CardModel)` | 卡牌进入战斗后 |
| `AfterCardGeneratedForCombat` | `(ICombatState, CardModel, Player?)` | 卡牌被生成后 |
| `BeforeCardAutoPlayed` | `(ICombatState, CardModel, Creature?, AutoPlayType)` | 自动打出前 |
| `BeforeCardRemoved` | `(IRunState, CardModel)` | 卡牌移除前 |

## 4. 回合相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `AfterPlayerTurnStart` | `(ICombatState, PlayerChoiceContext, Player)` | 玩家回合开始后 |
| `BeforeTurnEnd` | `(ICombatState, CombatSide)` | 回合结束前 |
| `AfterTurnEnd` | `(ICombatState, CombatSide)` | 回合结束后 |
| `BeforeSideTurnStart` | `(ICombatState, CombatSide)` | 任意方回合开始前 |
| `AfterSideTurnStart` | `(ICombatState, CombatSide)` | 任意方回合开始后 |
| `AfterAutoPrePlayPhaseEntered` | `(HookPlayerChoiceContext, ICombatState, Player)` | 自动前打出阶段进入后 |
| `AfterAutoPostPlayPhaseEntered` | `(HookPlayerChoiceContext, ICombatState, Player)` | 自动后打出阶段进入后 |
| `BeforeHandDraw` | `(ICombatState, Player, PlayerChoiceContext)` | 抽牌前 |
| `AfterHandEmptied` | `(ICombatState, PlayerChoiceContext, Player)` | 手牌打空后 |
| `BeforeFlush` | `(ICombatState, Player)` | 回合结束弃牌前 |
| `AfterFlush` | `(ICombatState, Player, PlayerChoiceContext, IReadOnlyCollection<CardModel>, IReadOnlyCollection<CardModel>)` | 弃牌后 (含保留) |

## 5. 战斗流程

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforeCombatStart` | `(IRunState, ICombatState?)` | 战斗开始前 |
| `AfterCombatEnd` | `(IRunState, ICombatState?, CombatRoom)` | 战斗结束后 |
| `AfterCombatVictory` | `(IRunState, ICombatState?, CombatRoom)` | 战斗胜利后 |
| `AfterCreatureAddedToCombat` | `(ICombatState, Creature)` | 生物加入战斗后 |
| `BeforeDeath` | `(IRunState, ICombatState?, Creature)` | 死亡前 |
| `AfterDeath` | `(IRunState, ICombatState?, Creature, bool, float)` | 死亡后 |
| `AfterPreventingDeath` | `(IRunState, ICombatState?, AbstractModel, Creature)` | 阻止死亡后 |

## 6. HP/能量/星币

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `AfterCurrentHpChanged` | `(IRunState, ICombatState?, Creature, decimal)` | HP变化后 |
| `AfterEnergyReset` | `(ICombatState, Player)` | 能量重置后 |
| `AfterEnergySpent` | `(ICombatState, CardModel, int)` | 能量消耗后 |
| `AfterStarsGained` | `(ICombatState, int, Player)` | 获得星币后 |
| `AfterStarsSpent` | `(ICombatState, int, Player)` | 消耗星币后 |
| `AfterGoldGained` | `(IRunState, Player)` | 获得金币后 |
| `AfterForge` | `(ICombatState, decimal, Player, AbstractModel?)` | 锻造后 |

## 7. 能力相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforePowerAmountChanged` | `(ICombatState, PowerModel, decimal, Creature, Creature?, CardModel?)` | 能力值变化前 |
| `AfterPowerAmountChanged` | `(ICombatState, PlayerChoiceContext, PowerModel, decimal, Creature?, CardModel?)` | 能力值变化后 |

## 8. 药水相关

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `BeforePotionUsed` | `(IRunState, ICombatState?, PotionModel, Creature?)` | 使用药水前 |
| `AfterPotionUsed` | `(IRunState, ICombatState?, PotionModel, Creature?)` | 使用药水后 |
| `AfterPotionDiscarded` | `(IRunState, ICombatState?, PotionModel)` | 丢弃药水后 |
| `AfterPotionProcured` | `(IRunState, ICombatState?, PotionModel)` | 获得药水后 |

## 9. 跑酷/地图/房间

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `AfterActEntered` | `(IRunState)` | 进入新幕后 |
| `BeforeRoomEntered` | `(IRunState, AbstractRoom)` | 进入房间前 |
| `AfterRoomEntered` | `(IRunState, AbstractRoom)` | 进入房间后 |
| `AfterMapGenerated` | `(IRunState, ActMap, int)` | 地图生成后 |
| `AfterRestSiteHeal` | `(IRunState, Player, bool)` | 休息点治疗后 |
| `AfterRestSiteSmith` | `(IRunState, Player)` | 休息点锻造后 |
| `AfterRewardTaken` | `(IRunState, Player, Reward)` | 领取奖励后 |
| `AfterItemPurchased` | `(IRunState, Player, MerchantEntry, int)` | 商店购买后 |

## 10. 灵魂球/召唤/杂项

| Hook 方法 | 签名 | 说明 |
|-----------|------|------|
| `AfterOrbChanneled` | `(ICombatState, PlayerChoiceContext, Player, OrbModel)` | 引导球后 |
| `AfterOrbEvoked` | `(PlayerChoiceContext, ICombatState, OrbModel, IEnumerable<Creature>)` | 激发球后 |
| `AfterSummon` | `(ICombatState, PlayerChoiceContext, Player, decimal)` | 召唤后 |
| `AfterTakingExtraTurn` | `(ICombatState, Player)` | 额外回合后 |
| `AfterShuffle` | `(ICombatState, PlayerChoiceContext, Player)` | 洗牌后 |
| `AfterDiedToDoom` | `(ICombatState, IReadOnlyList<Creature>)` | 因毁灭死亡后 |
| `AfterOstyRevived` | `(ICombatState, Creature)` | Osty 复活后 |
| `AfterPreventingDraw` | `(ICombatState, AbstractModel)` | 阻止抽牌后 |

---

## Hook 修改器方法 (Modifier Hooks)

这些方法不是事件回调，而是数值修改器，遍历所有 HookListener 并聚合结果：

| 方法 | 说明 | 返回值 |
|------|------|--------|
| `ModifyBlock(...)` | 修改格挡值 | 修改后的格挡值 |
| `ModifyDamage(...)` | 修改伤害值 | 修改后的伤害值 |
| `ModifyAttackHitCount(...)` | 修改攻击次数 | 修改后的次数 |
| `ModifyCardPlayCount(...)` | 修改卡牌打出次数 | 修改后的次数 |
| `ModifyCardBeingAddedToDeck(...)` | 修改加入牌组的卡牌 | 修改后的卡牌 |
| `ModifyCardRewardAlternatives(...)` | 修改卡牌奖励选项 | 修改者列表 |
| `ModifyCardRewardCreationOptions(...)` | 修改卡牌奖励创建选项 | 修改后的选项 |
| `ModifyCardRewardUpgradeOdds(...)` | 修改卡牌奖励升级概率 | 修改后的概率 |
| `ModifyCardPlayResultPileTypeAndPosition(...)` | 修改卡牌打出后去向 | (PileType, Position) |

**ModifyDamageHookType** 标志枚举:
- `Additive` — 加法修改
- `Multiplicative` — 乘法修改
