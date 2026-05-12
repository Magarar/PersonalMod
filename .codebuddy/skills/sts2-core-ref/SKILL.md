---
name: sts2-core-ref
description: >-
  该 Skill 提供杀戮尖塔2 (Slay the Spire 2) 反编译源码 (Core 模块) 的导航与参考能力。
  当编写 STS2 Mod 时需要查阅游戏底层 API、基类定义、Hook 回调、枚举值、
  资源路径约定或已有游戏内容的实现方式时，使用此 Skill。
  源码位于 D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\。
---

# STS2 Core 源码参考

## 1. 概述

杀戮尖塔2 游戏引擎反编译源码，基于 Godot + C# 开发。命名空间 `MegaCrit.Sts2.Core`。

**源码根目录**: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\`

查阅详细的参考文件以获取完整的 API 信息：
- `references/base-classes.md` — 所有模型基类及其可 override 成员
- `references/hooks-reference.md` — 完整 Hook 回调列表
- `references/key-systems.md` — 关键系统概览与枚举速查

## 2. 代码目录导航

需要查找特定功能时，根据以下映射定位源文件：

### 2.1 Models/ — 游戏数据定义

所有游戏内容继承自 `AbstractModel`。通过类名搜索即可定位文件。

| 要查找 | 目录 | 基类 | 数量 |
|--------|------|------|------|
| 卡牌实现 | `Models/Cards/` | `CardModel` | 500 |
| 能力实现 | `Models/Powers/` | `PowerModel` | 260 |
| 遗物实现 | `Models/Relics/` | `RelicModel` | 298 |
| 怪物实现 | `Models/Monsters/` | `MonsterModel` | 121 |
| 事件实现 | `Models/Events/` | `EventModel` | 68 |
| 药水实现 | `Models/Potions/` | `PotionModel` | 64 |
| 角色实现 | `Models/Characters/` | `CharacterModel` | 9 |
| 遭遇配置 | `Models/Encounters/` | `EncounterModel` | ~90 |
| 附魔效果 | `Models/Enchantments/` | `EnchantmentModel` | ~30 |
| 痛苦效果 | `Models/Afflictions/` | `AfflictionModel` | 7 |
| 跑酷修饰器 | `Models/Modifiers/` | `ModifierModel` | 16 |
| 灵魂球 | `Models/Orbs/` | `OrbModel` | 5 |
| 徽章 | `Models/Badges/` | `BadgeModel` | ~35 |
| 卡池 | `Models/CardPools/` | - | 13 |

**基类文件** (均在 `Models/` 根目录):
- `AbstractModel.cs` (42.57 KB) — 根基类
- `CardModel.cs` (74.65 KB) — 卡牌基类
- `PowerModel.cs` (23.94 KB) — 能力基类
- `RelicModel.cs` (24.86 KB) — 遗物基类
- `MonsterModel.cs` (21.2 KB) — 怪物基类
- `ModelDb.cs` (27.05 KB) — 模型数据库

### 2.2 Entities/ — 运行时实体

| 目录 | 说明 |
|------|------|
| `Entities/Players/Player.cs` | 玩家实例 (38.55 KB) |
| `Entities/Creatures/Creature.cs` | 生物基类 (30.92 KB) |
| `Entities/Cards/` | 运行时卡牌状态 |

### 2.3 系统/逻辑

| 目录 | 说明 |
|------|------|
| `Hooks/Hook.cs` | Hook 回调系统 (94 KB) |
| `Combat/` | 战斗核心循环 |
| `GameActions/` | 玩家动作 |
| `Commands/` | 战斗动画/效果命令 |
| `Runs/` | 跑酷流程管理 |
| `Rooms/` | 房间类型 |
| `Map/` | 地图生成 |
| `Rewards/` | 奖励系统 |
| `MonsterMoves/` | 怪物 AI/意图 |
| `Localization/` | 本地化 |
| `Modding/` | Mod 系统 |
| `Modding/ModInitializerAttribute.cs` | Mod 入口属性 |
| `Saves/` | 存档系统 |
| `Timeline/` | 时间线/解锁系统 |

## 3. 使用流程

编写 Mod 时遇到以下场景，按步骤操作：

### 3.1 查看已有实现作为参考

需要创建卡牌/能力/遗物时：

1. 在 `Models/Cards/` 中搜索类似功能的已有卡牌 (如搜索 `Strike` 查看攻击卡、`Defend` 查看防御卡)
2. 在 `Models/Powers/` 中搜索类似能力 (如 `StrengthPower` 查看数值修改型能力)
3. 在 `Models/Relics/` 中搜索类似遗物 (如 `BurningBlood` 查看战斗后触发型遗物)
4. 阅读其 override 的方法和属性，模仿其模式

### 3.2 查找可用 API

1. 需要知道卡牌能 override 什么 → 阅读 `references/base-classes.md` 中 CardModel 部分
2. 需要知道遗物能响应什么事件 → 阅读 `references/base-classes.md` 中 RelicModel 部分
3. 需要知道能力能修改什么数值 → 阅读 `references/base-classes.md` 中 PowerModel 部分
4. 需要监听特定游戏事件 → 阅读 `references/hooks-reference.md`
5. 需要查看枚举值 → 阅读 `references/key-systems.md` 中枚举速查

### 3.3 查看资源路径约定

需要为 Mod 资源命名时，参考已有游戏内容的路径格式：

- 卡牌肖像: `atlases/card_atlas.sprites/{pool_title}/{entry}.tres`
- 能力图标: `atlases/power_atlas.sprites/{entry}.tres`
- 遗物图标: `atlases/relic_atlas.sprites/{iconbasename}.tres`
- 本地化键: `{category}/{entry}.{field}`

### 3.4 查找系统交互

需要与特定系统交互时：

- 战斗中修改伤害 → 搜索 `ModifyDamage` 在 `Hook.cs` 和 `PowerModel.cs`
- 战斗中修改格挡 → 搜索 `ModifyBlock` 在 `Hook.cs` 和 `PowerModel.cs`
- 卡牌打出效果 → override `CardModel.OnPlay()`
- 战斗结束触发 → override `RelicModel.AfterCombatVictory()` 或 Hook
- 抽牌/弃牌时触发 → 查阅 `references/hooks-reference.md` 中卡牌相关 Hook

## 4. 反编译代码阅读提示

源码为 ILSpy/反编译产出，有以下特征：

- `// Token: 0x...` 注释是 IL 元数据标记，可忽略
- `[Nullable]`, `[NullableContext]` 属性是反编译器添加的可空注解
- `<>z__ReadOnlySingleElementList` 是编译器生成的只读列表类型
- async 方法的 state machine 结构 (`<MethodName>d__N`) 是编译器生成
- `DefaultInterpolatedStringHandler` 是字符串插值的编译结果
- 部分泛型参数名丢失 (显示为 `T` 而非原始名)

**阅读技巧**: 关注业务逻辑而非编译器生成代码。重点看方法签名、override 的属性、构造函数参数和 DynamicVars 定义。
