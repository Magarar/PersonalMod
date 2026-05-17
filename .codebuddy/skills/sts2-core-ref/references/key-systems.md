# STS2 Core 关键系统参考

源码位置: `{{STS2_SOURCE_ROOT}}`

---

## 1. 战斗系统 (Combat/)

**核心文件**:
| 文件 | 大小 | 说明 |
|------|------|------|
| `Combat/CombatManager.cs` | 37.9 KB | 战斗管理器，管理战斗状态和流程 |
| `Combat/CombatState.cs` | 19.65 KB | 战斗运行时状态 |
| `Combat/ICombatState.cs` | - | 战斗状态接口 |

**关键概念**:
- `CombatSide` — 战斗方 (Player / Enemy)
- `PlayerTurnPhase` — 玩家回合阶段
- `PlayerChoiceContext` — 玩家选择上下文
- `AttackCommand` — 攻击命令
- `DamageResult` — 伤害结果
- `ValueProp` — 数值属性 (区分不同来源的伤害/格挡)
- `CardPlay` — 卡牌打出信息

---

## 2. 命令系统 (Commands/)

用于构建战斗动画和效果链。

| 文件 | 大小 | 说明 |
|------|------|------|
| `Commands/Cmd.cs` | - | 命令基类 |
| `Commands/CardCmd.cs` | 27.88 KB | 卡牌命令 |
| `Commands/CreatureCmd.cs` | 18.86 KB | 生物命令 |
| `Commands/VfxCmd.cs` | 11.67 KB | 特效命令 |
| `Commands/PowerCmd.cs` | - | 能力命令 |
| `Commands/Builders/AttackCommand.cs` | 16.01 KB | 攻击命令构建器 |

---

## 3. 游戏动作 (GameActions/)

| 文件 | 说明 |
|------|------|
| `GameActions/GameAction.cs` | 动作基类 |
| `GameActions/PlayCardAction.cs` | 打出卡牌 |
| `GameActions/UsePotionAction.cs` | 使用药水 |
| `GameActions/EndPlayerTurnAction.cs` | 结束回合 |
| `GameActions/PickRelicAction.cs` | 选择遗物 |
| `GameActions/PlayerChoiceResult.cs` | 玩家选择结果 |

---

## 4. 跑酷管理 (Runs/)

| 文件 | 大小 | 说明 |
|------|------|------|
| `Runs/RunManager.cs` | 58.91 KB | 跑酷管理器，控制整个游戏流程 |
| `Runs/RunState.cs` | 26.65 KB | 跑酷运行时状态 |
| `Runs/CardCreationOptions.cs` | - | 卡牌生成选项 |
| `Runs/ScoreUtility.cs` | - | 分数计算 |

**IRunState** — 跑酷状态接口，提供牌组、金币、遗物列表等访问。

---

## 5. 房间系统 (Rooms/)

| 类 | 说明 |
|------|------|
| `AbstractRoom` | 房间基类 |
| `CombatRoom` | 战斗房间 |
| `EventRoom` | 事件房间 |
| `MapRoom` | 地图房间 |
| `MerchantRoom` | 商店房间 |
| `TreasureRoom` | 宝箱房间 |
| `RestSiteRoom` | 休息站点 |

---

## 6. 地图系统 (Map/)

| 文件 | 说明 |
|------|------|
| `Map/StandardActMap.cs` | 标准章节地图 |
| `Map/SpoilsActMap.cs` | Spoils 地图 |
| `Map/GoldenPathActMap.cs` | 黄金路径 |
| `Map/MapPoint.cs` | 地图节点 |

---

## 7. 奖励系统 (Rewards/)

| 类 | 说明 |
|------|------|
| `Reward` | 奖励基类 |
| `CardReward` | 卡牌奖励 |
| `GoldReward` | 金币奖励 |
| `RelicReward` | 遗物奖励 |
| `PotionReward` | 药水奖励 |
| `CardRemovalReward` | 卡牌移除奖励 |

---

## 8. 怪物行动系统 (MonsterMoves/)

| 子目录 | 说明 |
|--------|------|
| `MonsterMoves/Intents/` | 怪物意图 (Attack, Defend, Buff, Debuff, Stun, Summon 等) |
| `MonsterMoves/MonsterMoveStateMachine/` | 怪物行动状态机 (条件分支、随机分支) |

---

## 9. 本地化系统 (Localization/)

| 文件 | 说明 |
|------|------|
| `Localization/LocManager.cs` | 本地化管理器 |
| `Localization/LocString.cs` | 本地化字符串 |
| `Localization/DynamicVars/` | 动态变量 (伤害、格挡、能量数值显示) |
| `Localization/Formatters/` | 数值格式化器 |

**本地化键约定**:
- 卡牌标题: `cards/{Entry}.title`
- 卡牌描述: `cards/{Entry}.description`
- 能力标题: `powers/{Entry}.title`
- 能力描述: `powers/{Entry}.description`
- 遗物标题: `relics/{Entry}.title`
- 遗物描述: `relics/{Entry}.description`
- 遗物风味: `relics/{Entry}.flavor`

---

## 10. Mod 系统 (Modding/)

| 文件 | 说明 |
|------|------|
| `Modding/ModManager.cs` | Mod 管理器 |
| `Modding/ModManifest.cs` | Mod 清单 |
| `Modding/ModInitializerAttribute.cs` | Mod 初始化属性 |
| `Modding/ModHelper.cs` | Mod 辅助工具 |

**Mod 注册流程**:
1. `[ModInitializer(nameof(Initialize))]` 标记入口类
2. 在 `Initialize()` 方法中注册 Mod 程序集
3. `ModelDb.Inject(Type)` 注入自定义模型

---

## 11. 模型数据库 (Models/ModelDb.cs)

**核心 API**:
```csharp
ModelDb.Inject(typeof(MyCard));      // 注入模型
ModelDb.Remove(typeof(MyCard));      // 移除模型
ModelDb.GetId<MyCard>()             // 获取 ModelId
ModelDb.AllCards                     // 所有卡牌
ModelDb.AllPowers                    // 所有能力
ModelDb.AllRelics                    // 所有遗物
ModelDb.Contains(typeof(MyCard))     // 检查是否已注册
```

---

## 12. 存档系统 (Saves/)

| 文件 | 大小 | 说明 |
|------|------|------|
| `Saves/SaveManager.cs` | 35.74 KB | 存档管理器 |
| `Saves/ProgressState.cs` | 63.98 KB | 进度状态 |
| `Saves/SerializableRun.cs` | - | 可序列化跑酷数据 |

---

## 13. 时间线/解锁系统 (Timeline/)

STS2 新系统，替代 STS1 的解锁机制。

| 子目录 | 说明 |
|--------|------|
| `Timeline/Epochs/` | ~60 个纪元解锁节点 |
| `Timeline/Stories/` | 角色故事线 |

---

## 14. 枚举类型速查

**CardType**: `None`, `Attack`, `Skill`, `Power`, `Status`, `Curse`, `Quest`
**CardRarity**: `Basic`, `Common`, `Uncommon`, `Rare`, `Special`, `Ancient`
**TargetType**: `Self`, `SingleEnemy`, `AllEnemies`, `SelfOrEnemy`
**CardTag**: `Strike`, `Defend`, 等等
**PowerType**: `Buff`, `Debuff`
**PowerStackType**: `Counter`, `Intensity`, `Duration`
**RelicRarity**: `Starter`, `Common`, `Uncommon`, `Rare`, `Boss`, `Special`
**PileType**: `Draw`, `Hand`, `Discard`, `Exhaust`, `Play`
**ValueProp**: `Move` (格挡), `PoweredAttack` (力量攻击), 等等
