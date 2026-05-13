---
name: sts2-monster-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 怪物 (Monster) 提供全面的参考与自动检查。
  涵盖怪物定义 (ModMonsterTemplate)、HP 与进阶缩放 (AscensionHelper)、
  状态机系统 (MonsterMoveStateMachine / MoveState / RandomBranchState / ConditionalBranchState)、
  意图系统 (SingleAttackIntent / MultiAttackIntent / DefendIntent / BuffIntent / DebuffIntent / StatusIntent 等)、
  执行效果命令 (DamageCmd / BlockCmd / PowerCmd / CreatureCmd)、
  怪物视觉场景 (NCreatureVisuals)、遭遇系统 (ModEncounterTemplate / 单怪/多怪遭遇)、
  注册方式 ([RegisterMonster] / [RegisterActEncounter])、
  本地化文本 (monsters.json / encounters.json)、
  以及完整的代码模板与审查清单。
  当用户要求创建新怪物、修改已有怪物 AI 或意图、新增遭遇时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 怪物编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 怪物 (Monster)，核心步骤：
1. 创建怪物类，继承 `ModMonsterTemplate`
2. 用 `[RegisterMonster]` 注册
3. 重写 `MinInitialHp` / `MaxInitialHp`（必须）
4. 重写 `MonsterAssetProfile` 配置视觉场景
5. 重写 `AfterAddedToRoom`（可选，开局 Buff）
6. 创建 `GenerateMoveStateMachine()` 定义 AI 状态机
7. 创建视觉场景 (tscn)
8. **创建遭遇类**，继承 `ModEncounterTemplate`，注册到指定幕
9. 编写本地化 JSON（monsters.json + encounters.json）

**遭遇系统**：要让怪物出现在游戏中，必须额外创建一个遭遇 (Encounter) 类，将其注册到指定幕的怪物池中。

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-11-add-monster/

---

## 2. Model ID 规则

RitsuLib 注册的怪物 ID 格式：

```
<MODID>_MONSTER_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestMonster` | `{{MODID_UPPER}}_MONSTER_TEST_MONSTER` |
| `Chomper` | `{{MODID_UPPER}}_MONSTER_CHOMPER` |
| `StoneGolem` | `{{MODID_UPPER}}_MONSTER_STONE_GOLEM` |

本地化键必须使用此 ID：

```json
{
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.name": "戈多",
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.BASIC_ATTACK.title": "基础攻击"
}
```

---

## 3. 基类: ModMonsterTemplate

继承链: `ModMonsterTemplate` → `MonsterModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `MinInitialHp` | `abstract int` | 最小初始 HP |
| `MaxInitialHp` | `abstract int` | 最大初始 HP |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `MonsterAssetProfile` | — | 视觉场景路径 |
| `CustomVisualPath` | `string?` | `null` | 自定义场景路径（优先于 AssetProfile） |
| `AttackSfx` | `protected virtual string` | `event:/sfx/enemy/enemy_attacks/{id}/{id}_attack` | 攻击音效 |
| `CastSfx` | `protected virtual string` | `event:/sfx/enemy/enemy_attacks/{id}/{id}_cast` | 施法音效 |
| `DeathSfx` | `virtual string` | `event:/sfx/enemy/enemy_attacks/{id}/{id}_die` | 死亡音效 |
| `HurtSfx` | `virtual string?` | `null` | 受伤音效 |
| `TakeDamageSfx` | `virtual string` | 继承 `HurtSfx` | 受击音效 |
| `IsHealthBarVisible` | `virtual bool` | `true` | 是否显示血条 |
| `ShouldFadeAfterDeath` | `virtual bool` | `true` | 死亡后是否渐隐 |
| `ShouldShowInCompendium` | `virtual bool` | `true` | 是否显示在图鉴中 |

### 3.3 MonsterModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 怪物名称（`monsters/{Entry}.name`） |
| `MinInitialHp` | `int` | 最小初始 HP |
| `MaxInitialHp` | `int` | 最大初始 HP |
| `Creature` | `Creature` | 运行时生物实体 |
| `Rng` | `Rng` | 随机数生成器 |
| `RunRng` | `RunRngSet` | 跑酷随机数集 |
| `IsPerformingMove` | `bool` | 是否正在执行行动 |
| `HpBarSizeReduction` | `float` | 血条大小缩减量 |
| `ExtraDeathVfxPadding` | `Vector2` | 死亡特效额外间距 |
| `DeathAnimLengthOverride` | `float` | 死亡动画时长覆盖 |
| `CanChangeScale` | `bool` | 是否可缩放 |
| `TakeDamageSfxType` | `DamageSfxType` | 受击音效类型 |

### 3.4 核心方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `GenerateMoveStateMachine()` | `abstract MonsterMoveStateMachine` | **核心方法**：定义怪物 AI 状态机 |
| `AfterAddedToRoom()` | `Task` | 怪物加入房间后调用（用于施加初始 Buff） |
| `GetIntents()` | `IEnumerable<AbstractIntent>` | 获取当前意图列表 |
| `CreateVisuals()` | `NCreatureVisuals` | 创建视觉节点 |

### 3.5 辅助方法

| 方法 | 说明 |
|------|------|
| `L10NMonsterLookup(string key)` | 怪物本地化查找（`monsters` 表） |

---

## 4. 进阶数值缩放 (AscensionHelper)

```csharp
using MegaCrit.Sts2.Core.Entities.Ascension;

// HP 进阶缩放：进阶 8+ 时增加 HP
public override int MinInitialHp => AscensionHelper.GetValueIfAscension(
    AscensionLevel.ToughEnemies, 20, 15);
public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(
    AscensionLevel.ToughEnemies, 30, 20);

// 伤害进阶缩放：进阶 3+ 时增加伤害
private int BasicDamage => AscensionHelper.GetValueIfAscension(
    AscensionLevel.DeadlyEnemies, 4, 3);
```

| AscensionLevel | 对应进阶等级 | 效果 |
|---------------|-------------|------|
| `ToughEnemies` | 进阶 8+ | 怪物血量增加 |
| `DeadlyEnemies` | 进阶 3+ | 怪物伤害增加 |

---

## 5. AI 状态机系统

### 5.1 MonsterMoveStateMachine

所有怪物通过 `GenerateMoveStateMachine()` 返回状态机定义 AI。

```csharp
protected override MonsterMoveStateMachine GenerateMoveStateMachine()
{
    // 创建状态
    var state1 = new MoveState("STATE_ID", OnPerformAction, intent1, intent2);
    var state2 = new MoveState("STATE_ID2", OnPerformAction, intent3);

    // 设置状态转移
    state1.FollowUpState = state2;
    state2.FollowUpState = state1;

    // 返回状态机
    return new MonsterMoveStateMachine([state1, state2], initialState: state1);
}
```

#### 构造函数

```csharp
new MonsterMoveStateMachine(
    IEnumerable<MonsterState> states,   // 所有状态列表
    MonsterState initialState           // 初始状态
)
```

### 5.2 MoveState — 行动状态

```csharp
new MoveState(
    string stateId,                                          // 状态 ID（用于本地化）
    Func<IReadOnlyList<Creature>, Task> onPerform,           // 执行回调
    params AbstractIntent[] intents                          // 展示的意图列表
)
```

#### 关键属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `StateId` | `string` | 状态唯一 ID |
| `Intents` | `IReadOnlyList<AbstractIntent>` | 展示给玩家的意图 |
| `FollowUpState` | `MonsterState?` | 下一个状态（直接引用） |
| `FollowUpStateId` | `string?` | 下一个状态 ID（字符串引用） |
| `MustPerformOnceBeforeTransitioning` | `bool` | 是否必须执行才能离开此状态 |

### 5.3 RandomBranchState — 随机分支

```csharp
var branch = new RandomBranchState("BRANCH");
branch.AddBranch(attackState, MoveRepeatType.CanRepeatForever, weight: 1.0f);
branch.AddBranch(defendState, MoveRepeatType.CannotRepeat, weight: 0.5f);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `targetState` | `MonsterState` | 目标状态 |
| `repeatType` | `MoveRepeatType` | 是否可重复 (`CanRepeatForever` / `CannotRepeat`) |
| `weight` | `float` | 权重 |

### 5.4 ConditionalBranchState — 条件分支

```csharp
var condition = new ConditionalBranchState("CONDITION");
condition.AddCondition(
    m => m.HealthPercent < 0.5f,   // 条件：HP < 50%
    berserkState                   // 满足时切换到此状态
);
condition.DefaultState = normalState; // 默认状态
```

---

## 6. 意图系统

### 6.1 IntentType 枚举

```csharp
IntentType.Attack        // 攻击
IntentType.Buff          // 增益
IntentType.Debuff        // 减益
IntentType.DebuffStrong  // 强力减益
IntentType.Defend        // 防御
IntentType.Escape        // 逃跑
IntentType.Heal          // 治疗
IntentType.Hidden        // 隐藏
IntentType.Summon        // 召唤
IntentType.Sleep         // 睡眠
IntentType.Stun          // 眩晕
IntentType.StatusCard    // 状态牌
IntentType.CardDebuff    // 卡牌减益
IntentType.DeathBlow     // 致命一击
IntentType.Unknown       // 未知
```

### 6.2 所有意图类

| 意图类 | 构造函数 | 说明 |
|--------|---------|------|
| `SingleAttackIntent(int damage)` | `int` 或 `Func<decimal>` | 单次攻击 |
| `MultiAttackIntent(int damage, int repeats)` | `(int, int)` 或 `(int, Func<int>)` | 多次攻击 |
| `DeathBlowIntent(Func<decimal> damageCalc)` | 函数 | 致命一击 |
| `DefendIntent()` | 无参 | 防御 |
| `BuffIntent()` | 无参 | 增益 |
| `DebuffIntent(bool strong = false)` | 可选布尔 | 减益 |
| `CardDebuffIntent()` | 无参 | 卡牌减益 |
| `StatusIntent(int count)` | 整数 | 状态牌 |
| `HealIntent()` | 无参 | 治疗 |
| `EscapeIntent()` | 无参 | 逃跑 |
| `SleepIntent()` | 无参 | 睡眠 |
| `StunIntent()` | 无参 | 眩晕 |
| `SummonIntent()` | 无参 | 召唤 |
| `HiddenIntent()` | 无参 | 隐藏意图 |
| `UnknownIntent()` | 无参 | 未知意图 |

### 6.3 多意图组合

一个 `MoveState` 可同时展示多个意图：

```csharp
new MoveState("BASIC_ATTACK", BasicAttackMove,
    new SingleAttackIntent(damage),   // 显示攻击图标 + 数值
    new DefendIntent()                // 显示防御图标
);
```

---

## 7. 执行效果命令

在怪物的行动回调中常用命令：

| 命令 | 说明 | 示例 |
|------|------|------|
| `DamageCmd.Attack(amount).FromMonster(this)` | 造成伤害 | `await DamageCmd.Attack(damage).FromMonster(this).WithHitFx("vfx/vfx_attack_blunt").Execute(null)` |
| `CreatureCmd.GainBlock(creature, amount, props, source)` | 获得格挡 | `await CreatureCmd.GainBlock(Creature, block, ValueProp.Move, null)` |
| `PowerCmd.Apply<TPower>(target, amount, applier, source)` | 施加能力 | `await PowerCmd.Apply<StrengthPower>(Creature, 2, Creature, null)` |
| `TalkCmd.Play(text, creature, color)` | 怪物说话 | `TalkCmd.Play(localizedText, Creature, VfxColor.Blue)` |
| `VfxCmd.Play(path)` | 播放特效 | — |

### 7.1 WithHitFx — 特效和音效

```csharp
await DamageCmd.Attack(HeavyDamage)
    .FromMonster(this)
    .WithAttackerFx(null, AttackSfx)       // 攻击者动画/音效
    .WithHitFx("vfx/vfx_attack_blunt")     // 命中特效
    .Execute(null);
```

### 7.2 常用 VFX 路径

| VFX 路径 | 说明 |
|---------|------|
| `vfx/vfx_attack_blunt` | 钝击特效 |
| `vfx/vfx_attack_slash` | 斩击特效 |
| `vfx/vfx_attack_pierce` | 穿刺特效 |
| `vfx/vfx_buff` | 增益特效 |
| `vfx/vfx_debuff` | 减益特效 |

### 7.3 TalkCmd — 怪物台词

```csharp
TalkCmd.Play(
    L10NMonsterLookup("{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.BASIC_ATTACK.banter"),
    Creature,
    VfxColor.Blue
);
```

---

## 8. 资源配置 (MonsterAssetProfile)

### 8.1 基本配置

```csharp
public override MonsterAssetProfile AssetProfile => new(
    VisualsScenePath: "res://{{MODID}}/scenes/monsters/test_monster.tscn"
);

// 或者使用 CustomVisualPath（替代 AssetProfile）
public override string? CustomVisualPath => "res://{{MODID}}/scenes/monsters/test_monster.tscn";
```

### 8.2 怪物视觉场景要求

场景根节点必须是 `NCreatureVisuals` 类型（或通过 `%unique_name` 绑定以下子节点）：

```
TestMonster (NCreatureVisuals)
├── Visuals (Node2D) %            # 显示怪物的主节点
├── Bounds (Control) %            # 碰撞箱/血条大小
├── IntentPos (Marker2D) %        # 意图显示位置
├── CenterPos (Marker2D) %        # 中心位置
└── TalkPos (Marker2D) %          # 对话气泡位置
```

**要求**：
- `Visuals`、`Bounds`、`IntentPos`、`CenterPos`、`TalkPos` 节点名**不能改**
- 每个节点需右键勾选"作为唯一名称访问"（显示 `%` 标记）
- `Bounds` 大小决定血条长度
- 怪物显示在 x 轴上方

### 8.3 场景示例

```gdscript
[gd_scene load_steps=2 format=3]

[ext_resource type="Texture2D" path="res://{{MODID}}/images/monsters/test_monster.png" id="1"]

[node name="TestCharacter" type="Node2D"]

[node name="Visuals" type="Sprite2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -73)
texture = ExtResource("1")

[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
layout_mode = 3
offset_left = -70.0
offset_top = -140.0
offset_right = 70.0

[node name="IntentPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -159)

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = Vector2(0, -72)
```

---

## 9. 注册

### 9.1 怪物注册

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterMonster]
public class TestMonster : ModMonsterTemplate { ... }
```

前提：在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

---

## 10. 遭遇系统 (Encounter)

**要让怪物出现在游戏中，必须创建遭遇类**。

### 10.1 注册属性

```csharp
[RegisterActEncounter(typeof(Glory))]  // 注册到指定幕
public class TestEncounter : ModEncounterTemplate { ... }
```

### 10.2 ModEncounterTemplate 基类

命名空间: `STS2RitsuLib.Scaffolding.Content`

#### 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `AllPossibleMonsters` | `IEnumerable<MonsterModel>` | 所有可能出现的怪物 |
| `RoomType` | `RoomType` | 房间类型 |
| `GenerateMonsters()` | `IReadOnlyList<(MonsterModel, string?)>` | 生成怪物列表 |

#### 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsWeak` | `virtual bool` | — | 是否为弱怪池 |
| `Slots` | `IReadOnlyList<string>` | — | 怪物槽位名列表 |
| `AssetProfile` | `EncounterAssetProfile` | — | 遭遇场景配置 |
| `GetCameraScaling()` | `float` | `1f` | 摄像机缩放 |
| `GetCameraOffset()` | `Vector2` | — | 摄像机偏移 |

### 10.3 单怪物遭遇

```csharp
[RegisterActEncounter(typeof(Glory))]
public class TestEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<TestMonster>()];

    public override RoomType RoomType => RoomType.Monster;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TestMonster>().ToMutable(), null)  // null = 自动分配槽位
    ];
}
```

### 10.4 多怪物遭遇

```csharp
[RegisterActEncounter(typeof(Glory))]
public class TestMultiEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<TestMonster>()];

    public override bool IsWeak => false;

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://{{MODID}}/scenes/encounters/test_multi_encounter.tscn"
    );

    public override IReadOnlyList<string> Slots => [
        "first", "second", "third", "fourth",
        "first2", "second2", "third2", "fourth2"
    ];

    public override RoomType RoomType => RoomType.Monster;

    public override float GetCameraScaling() => 0.8f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TestMonster>().ToMutable(), "first"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "second"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "third"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "fourth"),
    ];
}
```

### 10.5 多怪物遭遇场景

场景根节点为 `Control`，使用 `Marker2D` 标注每个怪物槽位：

```
TestMultiEncounter (Control)
├── first (Marker2D)     # 位置 = 怪物生成位置
├── second (Marker2D)
├── third (Marker2D)
├── fourth (Marker2D)
```

```gdscript
[gd_scene format=3]

[node name="Encounter" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
mouse_filter = 2

[node name="first" type="Marker2D" parent="."]
position = Vector2(882, 697)

[node name="second" type="Marker2D" parent="."]
position = Vector2(1157, 729)
```

### 10.6 RoomType 可选值

```csharp
RoomType.Monster       // 普通怪物
RoomType.Elite         // 精英怪物
RoomType.Boss          // Boss
RoomType.Treasure      // 宝箱
RoomType.Event         // 事件
RoomType.Merchant      // 商店
RoomType.RestSite      // 休息点
```

---

## 11. 本地化

### 11.1 monsters.json — 怪物文本

```json
{
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.name": "戈多",
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.BASIC_ATTACK.title": "基础攻击",
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.BASIC_ATTACK.banter": "[jitter]接下这招！[/jitter]",
  "{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.HEAVY_ATTACK.title": "重击"
}
```

| 字段 | 说明 | 必需 |
|------|------|------|
| `name` | 怪物名称 | 是 |
| `moves.<STATE_ID>.title` | 行动名称（意图显示） | 推荐 |
| `moves.<STATE_ID>.banter` | 行动台词（经 `TalkCmd` 使用） | 可选 |

### 11.2 encounters.json — 遭遇文本

```json
{
  "TEST-TEST_ENCOUNTER.title": "一只戈多",
  "TEST-TEST_ENCOUNTER.loss": "{character}被[gold]{encounter}[/gold]折磨而死。",
  "TEST-TEST_MULTI_ENCOUNTER.title": "很多戈多"
}
```

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 遭遇标题 | 是 |
| `loss` | 被击败文本（含 `{character}` 和 `{encounter}` 占位符） | 推荐 |

**本地化键格式说明**：遭遇的本地化键使用 `{ModId}-{类名}` 格式（短横线连接），而非下划线。

### 11.3 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色 |
| `[blue]文字[/blue]` | 蓝色 |
| `[jitter]文字[/jitter]` | 抖动效果 |

---

## 12. 完整代码模板

### 12.1 简单循环 AI 怪物

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Monsters;

[RegisterMonster]
public class TestMonster : ModMonsterTemplate
{
    // HP（进阶 8+ 增加）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 20, 15);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 30, 20);

    // 伤害（进阶 3+ 增加）
    private int BasicDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 4, 3);
    private int BasicBlock => 8;
    private int HeavyDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 8, 6);

    // 视觉场景
    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://{{MODID}}/scenes/monsters/test_monster.tscn"
    );

    // 开局 Buff
    public override async Task AfterAddedToRoom()
    {
        await PowerCmd.Apply<StrengthPower>(Creature, 2, Creature, null);
    }

    // AI 状态机
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 意图 1：攻击 + 格挡
        var basicAttack = new MoveState(
            "BASIC_ATTACK",
            BasicAttackMove,
            new SingleAttackIntent(BasicDamage),
            new DefendIntent()
        );

        // 意图 2：重击
        var heavyAttack = new MoveState(
            "HEAVY_ATTACK",
            async targets => await DamageCmd
                .Attack(HeavyDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null),
            new SingleAttackIntent(HeavyDamage)
        );

        // 循环
        basicAttack.FollowUpState = heavyAttack;
        heavyAttack.FollowUpState = basicAttack;

        return new MonsterMoveStateMachine(
            [basicAttack, heavyAttack],
            basicAttack
        );
    }

    // 意图 1 执行回调
    private async Task BasicAttackMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(
            L10NMonsterLookup("{{MODID_UPPER}}_MONSTER_TEST_MONSTER.moves.BASIC_ATTACK.banter"),
            Creature,
            VfxColor.Blue
        );

        await DamageCmd
            .Attack(BasicDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.GainBlock(Creature, BasicBlock, ValueProp.Move, null);
    }
}
```

### 12.2 随机分支 AI 怪物

```csharp
[RegisterMonster]
public class RandomMonster : ModMonsterTemplate
{
    public override int MinInitialHp => 30;
    public override int MaxInitialHp => 40;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://{{MODID}}/scenes/monsters/random_monster.tscn"
    );

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState(
            "ATTACK",
            async targets => await DamageCmd.Attack(10).FromMonster(this).Execute(null),
            new SingleAttackIntent(10)
        );

        var buff = new MoveState(
            "BUFF",
            async targets => await PowerCmd.Apply<StrengthPower>(Creature, 3, Creature, null),
            new BuffIntent()
        );

        var defend = new MoveState(
            "DEFEND",
            async targets => await CreatureCmd.GainBlock(Creature, 12, ValueProp.Move, null),
            new DefendIntent()
        );

        // 随机分支
        var branch = new RandomBranchState("CHOOSE");
        branch.AddBranch(attack, MoveRepeatType.CanRepeatForever, 2.0f);  // 高权重
        branch.AddBranch(buff, MoveRepeatType.CannotRepeat, 1.0f);
        branch.AddBranch(defend, MoveRepeatType.CanRepeatForever, 1.0f);

        return new MonsterMoveStateMachine([attack, buff, defend, branch], branch);
    }
}
```

### 12.3 条件分支 AI 怪物

```csharp
protected override MonsterMoveStateMachine GenerateMoveStateMachine()
{
    var normalAttack = new MoveState("ATTACK", /* ... */, new SingleAttackIntent(8));
    var berserkAttack = new MoveState("BERSERK", /* ... */, new SingleAttackIntent(20));

    var condition = new ConditionalBranchState("HP_CHECK");
    condition.AddCondition(
        m => m.HealthPercent < 0.3f,  // HP < 30% 时狂暴
        berserkAttack
    );
    condition.DefaultState = normalAttack;

    normalAttack.FollowUpState = condition;

    return new MonsterMoveStateMachine([normalAttack, berserkAttack, condition], condition);
}
```

### 12.4 单怪物遭遇

```csharp
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Encounters;

[RegisterActEncounter(typeof(Glory))]
public class TestEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<TestMonster>()];

    public override RoomType RoomType => RoomType.Monster;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TestMonster>().ToMutable(), null)
    ];
}
```

### 12.5 多怪物遭遇

```csharp
[RegisterActEncounter(typeof(Glory))]
public class TestMultiEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<TestMonster>()];

    public override bool IsWeak => false;

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://{{MODID}}/scenes/encounters/test_multi_encounter.tscn"
    );

    public override IReadOnlyList<string> Slots => [
        "first", "second", "third", "fourth"
    ];

    public override RoomType RoomType => RoomType.Monster;

    public override float GetCameraScaling() => 0.8f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TestMonster>().ToMutable(), "first"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "second"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "third"),
        (ModelDb.Monster<TestMonster>().ToMutable(), "fourth"),
    ];
}
```

---

## 13. 文件组织

```
{{MODID}}/{{MODID}}Code/Monsters/
├── TestMonster.cs                    # 怪物类

{{MODID}}/{{MODID}}Code/Encounters/
├── TestEncounter.cs                  # 遭遇类
├── TestMultiEncounter.cs             # 多怪遭遇

{{MODID}}/{{MODID}}/
├── scenes/
│   ├── monsters/
│   │   └── test_monster.tscn         # 怪物视觉场景 (NCreatureVisuals)
│   └── encounters/
│       └── test_multi_encounter.tscn # 多怪物遭遇场景 (Control + Marker2D)
├── images/
│   └── monsters/
│       └── test_monster.png          # 怪物贴图
└── localization/
    ├── eng/
    │   ├── monsters.json             # 怪物文本
    │   └── encounters.json           # 遭遇文本
    └── zhs/
        ├── monsters.json
        └── encounters.json
```

---

## 14. 参考已有怪物实现

在源码目录中搜索参考：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 简单循环 AI | `Models/Monsters/` | `Chomper` |
| 随机分支 AI | `Models/Monsters/` | `CorpseSlug`, `Crusher` |
| 条件分支 AI | `Models/Monsters/` | `CeremonialBeast` |
| 开局 Buff | `Models/Monsters/` | 搜索 `AfterAddedToRoom` |
| 多意图显示 | `Models/Monsters/` | 搜索 `MultiAttackIntent` |
| 召唤小怪 | `Models/Monsters/` | `BowlbugEgg` |
| 遭遇配置 | `Models/Encounters/` | 各类 Encounter 文件 |

源码位置:
- 怪物基类: `{{STS2_SOURCE_ROOT}}\Models\MonsterModel.cs`
- 怪物实现: `{{STS2_SOURCE_ROOT}}\Models\Monsters\`
- 意图系统: `{{STS2_SOURCE_ROOT}}\MonsterMoves\Intents\`
- 状态机: `{{STS2_SOURCE_ROOT}}\MonsterMoves\MonsterMoveStateMachine\`

---

## 15. 调试

### 15.1 控制台命令

在游戏中按 `~` 打开控制台：

```
spawn {{MODID_UPPER}}_MONSTER_TEST_MONSTER
```

### 15.2 常见检查步骤

1. 确认 [RegisterMonster] 属性存在且 ModAssembly 已注册
2. 确认视觉场景结构正确（Visuals / Bounds / IntentPos / CenterPos % 节点）
3. 确认遭遇已注册到正确的幕（[RegisterActEncounter]）
4. 确认 monsters.json 和 encounters.json 本地化文件存在

---

## 16. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 怪物不出现 | 未创建遭遇或遭遇未注册 | 创建 `ModEncounterTemplate` 并添加 `[RegisterActEncounter]` |
| 怪物视觉显示异常 | 场景结构不对 | 确保 `Visuals`/`Bounds`/`IntentPos`/`CenterPos` 有 `%` |
| 血条过短/过长 | `Bounds` 节点大小不对 | 调整 `Bounds` 的 offset |
| 意图不显示 | Intent 位置未设置 | 确保 `IntentPos` 节点存在且位置正确 |
| 怪物不动 | 状态机未正确设置 | 检查 `GenerateMoveStateMachine()` 的初始状态和转移 |
| 伤害不触发 | 未使用 `FromMonster(this)` | `DamageCmd` 链中必须调用 `.FromMonster(this)` |
| 进阶数值无效 | `AscensionHelper` 参数错误 | 确认 `AscensionLevel.ToughEnemies` / `DeadlyEnemies` |
| 怪物说话气泡不显示 | TalkPos 未设置 | 确保 `TalkPos` 节点存在 |
| 遭遇本地化键无效 | 遭遇键格式为 `{ModId}-{类名}` | 使用短横线而非下划线 |

---

## 17. 编写审查清单

### 17.1 怪物类检查

- [ ] 是否继承了 `ModMonsterTemplate`？
- [ ] 是否添加了 `[RegisterMonster]` 属性？
- [ ] 是否重写了 `MinInitialHp` / `MaxInitialHp`？
- [ ] 是否使用 `AscensionHelper` 处理进阶缩放？
- [ ] 是否重写了 `MonsterAssetProfile` 或 `CustomVisualPath`？
- [ ] `GenerateMoveStateMachine()` 是否正确定义了所有状态和转移？

### 17.2 视觉场景检查

- [ ] 场景根节点是否为 `Node2D` + `NCreatureVisuals` 结构？
- [ ] 是否包含 `Visuals` / `Bounds` / `IntentPos` / `CenterPos` 节点？
- [ ] 这些节点是否都勾选了"唯一名称访问"（`%`）？
- [ ] `Bounds` 大小是否合适？

### 17.3 遭遇检查

- [ ] 是否创建了 `ModEncounterTemplate` 子类？
- [ ] 是否添加了 `[RegisterActEncounter(typeof(ActType))]`？
- [ ] `AllPossibleMonsters` 是否包含了所有需要的怪物？
- [ ] `GenerateMonsters()` 中是否调用了 `.ToMutable()`？
- [ ] 多怪物遭遇是否创建了场景并使用 `Marker2D` 标注槽位？

### 17.4 本地化检查

- [ ] `monsters.json` 中是否添加了 `name`？
- [ ] 每个 `MoveState` 是否有对应的 `moves.{ID}.title`？
- [ ] `encounters.json` 中是否添加了 `title`？
- [ ] 本地化键名是否与代码匹配（`{MODID}_MONSTER_{CLASSNAME}`）？

### 17.5 注册检查

- [ ] `RegisterModAssembly` 在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
