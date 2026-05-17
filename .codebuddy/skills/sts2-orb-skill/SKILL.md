---
name: sts2-orb-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 充能球 (Orb) 提供全面的参考与自动检查。
  涵盖 Orb 定义 (ModOrbTemplate)、被动值 (PassiveVal) 与激发值 (EvokeVal)、
  被动/激发效果 (Passive / Evoke)、回合触发回调 (AfterTurnStartOrbTrigger / BeforeTurnEndOrbTrigger)、
  视觉场景 (OrbAssetProfile)、音效系统 (PassiveSfx / EvokeSfx / ChannelSfx)、
  注册方式 ([RegisterOrb])、引导命令 (OrbCmd.Channel)、
  ModifyOrbValue 集中修改、本地化文本 (orbs.json)、
  以及完整的代码模板与审查清单。
  当用户要求创建新充能球、修改已有 Orb 逻辑、或排查 Orb 相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 充能球 (Orb) 编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 充能球 (Orb)，核心步骤：
1. 创建 Orb 类，继承 `ModOrbTemplate`
2. 用 `[RegisterOrb]` 属性注册
3. 重写 `PassiveVal` 属性（必须，被动效果数值）
4. 重写 `EvokeVal` 属性（必须，激发效果数值）
5. 重写 `DarkenedColor` 属性（必须，暗色色调）
6. 重写 `AssetProfile` 配置图标和场景路径
7. 重写 `TryCreateOrbSprite` 创建视觉精灵
8. 重写 `Passive` / `Evoke` 编写被动/激发效果逻辑
9. 创建 Orb 视觉场景 (Node2D)
10. 编写本地化 JSON（title + description + smartDescription）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-08-add-orb/

---

## 2. Model ID 规则

RitsuLib 注册的 Orb ID 格式：

```
<MODID>_ORB_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestOrb` | `{{MODID_UPPER}}_ORB_TEST_ORB` |
| `LightningOrb` | `{{MODID_UPPER}}_ORB_LIGHTNING_ORB` |
| `DrawOrb` | `{{MODID_UPPER}}_ORB_DRAW_ORB` |

本地化键必须使用此 ID：

```json
{
  "{{MODID_UPPER}}_ORB_TEST_ORB.title": "测试球",
  "{{MODID_UPPER}}_ORB_TEST_ORB.description": "充能球：回合开始时抽牌。",
  "{{MODID_UPPER}}_ORB_TEST_ORB.smartDescription": "[gold]被动：[/gold]回合开始时，抽[blue]{Passive}[/blue]张牌。\n[gold]激发：[/gold]抽[blue]{Evoke}[/blue]张牌。"
}
```

---

## 3. 基类: ModOrbTemplate

继承链: `ModOrbTemplate` → `OrbModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `PassiveVal` | `abstract decimal` | 被动效果数值（支持 `ModifyOrbValue()` 计算集中） |
| `EvokeVal` | `abstract decimal` | 激发效果数值 |
| `DarkenedColor` | `abstract Color` | 球体的暗色色调 |

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `OrbAssetProfile` | — | 图标和视觉场景路径配置 |
| `PassiveSfx` | `protected virtual string` | `""` | 被动触发音效 FMOD 路径 |
| `EvokeSfx` | `protected virtual string` | `""` | 激发音效 FMOD 路径 |
| `ChannelSfx` | `protected virtual string` | `""` | 引导音效 FMOD 路径 |
| `ExtraHoverTips` | `protected virtual IEnumerable<IHoverTip>` | 空数组 | 额外悬停提示 |

### 3.3 核心方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `Passive` | `Task Passive(PlayerChoiceContext choiceContext, Creature? target)` | **被动效果**：每回合触发一次（默认不做事） |
| `Evoke` | `Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)` | **激发效果**：球被激发时触发，返回受影响的生物 |
| `AfterTurnStartOrbTrigger` | `Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)` | 回合开始触发（供重写触发时机） |
| `BeforeTurnEndOrbTrigger` | `Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)` | 回合结束前触发（供重写触发时机） |
| `ModifyOrbValue(decimal)` | `protected decimal` | **集中计算**：将原始值传入集中修改系统，返回计算后的值 |
| `Trigger()` | `void` | 触发视觉反馈（触发 Triggered 事件，更新 UI） |
| `PlayPassiveSfx()` | `void` | 播放被动音效 |
| `PlayEvokeSfx()` | `void` | 播放激发音效 |
| `PlayChannelSfx()` | `void` | 播放引导音效 |
| `TryCreateOrbSprite()` | `protected virtual Node2D?` | 创建球体视觉精灵节点 |

### 3.4 重要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题本地化 (`orbs/{Entry}.title`) |
| `Description` | `LocString` | 描述本地化 (`orbs/{Entry}.description`) |
| `SmartDescription` | `LocString` | 智能描述 (`orbs/{Entry}.smartDescription`)，支持 `{Passive}` 和 `{Evoke}` 占位符 |
| `DarkenedColor` | `Color` | 暗色（用于球体空位背景色） |
| `Owner` | `Player` | 球体所属玩家 |
| `CombatState` | `ICombatState` | 当前战斗状态 |
| `Icon` | `CompressedTexture2D` | 图标纹理 |
| `HoverTips` | `IEnumerable<IHoverTip>` | 悬停提示列表 |

---

## 4. 生命周期与触发时机

充能球有两种触发时机：

### 4.1 触发顺序

```
回合开始
  └─ AfterTurnStartOrbTrigger (默认调用 Passive，但可重写改变行为)
       └─ Passive 执行被动效果
...
回合结束前
  └─ BeforeTurnEndOrbTrigger (默认不做事，可重写)
...
激发 (球满格时/手动激发)
  └─ Evoke 执行激发效果
```

### 4.2 被动触发

默认行为：`AfterTurnStartOrbTrigger` 会在回合开始时调用 `Passive`。

```csharp
// 重写触发时机（如果需要改变默认行为）
public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
{
    // 可以执行额外逻辑后再调用 Passive
    await Passive(choiceContext, null);
}
```

### 4.3 激发触发

当球体已满（达到最大槽位）再有新球体引导时，最左侧的球体会被激发。

```csharp
// 激发时执行效果，返回受影响的生物列表
public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
{
    PlayEvokeSfx();  // 播放激发音效
    // 执行效果逻辑...
    return [Owner.Creature];  // 返回受影响的生物
}
```

---

## 5. 数值系统 — ModifyOrbValue

### 5.1 集中修改 (Focus)

`ModifyOrbValue()` 是将球体数值传递给集中 (Focus) 修改系统的关键方法。通过此方法，玩家的集中能力可以影响球体的数值。

```csharp
public override decimal PassiveVal => ModifyOrbValue(3);   // 基础被动值 3，受集中影响
public override decimal EvokeVal => ModifyOrbValue(8);     // 基础激发值 8，受集中影响
```

### 5.2 不受集中影响的数值

如果希望球体数值不受集中影响，直接返回固定值：

```csharp
public override decimal EvokeVal => 8m;  // 固定 8，不受集中影响
```

### 5.3 动态数值

球体可以在运行时动态改变数值：

```csharp
// 如 DarkOrb：被动叠加累积到 EvokeVal 中
private decimal _evokeVal = 6m;

public override decimal EvokeVal => _evokeVal;

public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
{
    Trigger();
    _evokeVal += PassiveVal;  // 每次被动叠加数值
    // 更新 UI...
}
```

---

## 6. 资源配置 (OrbAssetProfile)

### 6.1 基本配置

```csharp
public override OrbAssetProfile AssetProfile => new(
    IconPath: "res://{{MODID}}/images/orbs/test_orb.png",       // 提示文本小图标
    VisualsScenePath: "res://{{MODID}}/scenes/orbs/test_orb.tscn" // 球体视觉场景
);
```

| 参数 | 说明 |
|------|------|
| `IconPath` | 悬停提示中显示的小图标路径 |
| `VisualsScenePath` | 球体的 3D/2D 视觉场景路径（`Node2D` 类型） |

### 6.2 TryCreateOrbSprite

RitsuLib 提供了从场景路径创建视觉精灵的方法。只需复制以下代码即可：

```csharp
protected override Node2D? TryCreateOrbSprite() =>
    RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);
```

### 6.3 原版资源路径约定

| 资源 | 路径 |
|------|------|
| 图标 | `orbs/{entry}.png` |
| 视觉场景 | `orbs/orb_visuals/{entry}` |

`entry` = `Entry.ToLowerInvariant()`

---

## 7. 视觉场景

### 7.1 场景要求

- 根节点类型必须为 `Node2D`
- 至少要有一个 `Sprite2D` 子节点来显示球体外观

### 7.2 场景示例

```gdscript
[gd_scene load_steps=2 format=3]

[ext_resource type="Texture2D" path="res://{{MODID}}/images/orbs/test_orb_visual.png" id="1"]

[node name="TestOrb" type="Node2D"]

[node name="Icon" type="Sprite2D" parent="."]
texture = ExtResource("1")
```

### 7.3 场景创建步骤

1. 在 Godot 编辑器中创建新场景
2. 根节点选择 `Node2D`
3. 添加 `Sprite2D` 子节点，设置纹理为球体图片
4. 保存到 `{{MODID}}/scenes/orbs/` 目录

---

## 8. 音效系统

### 8.1 自定义音效

通过重写音效属性添加自定义 FMOD 事件音效：

```csharp
protected override string PassiveSfx => "event:/sfx/characters/defect/defect_lightning_passive";
protected override string EvokeSfx => "event:/sfx/characters/defect/defect_lightning_evoke";
protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";
```

### 8.2 调试音效

如果不设置自定义音效（返回 `""`），系统会自动使用调试音频，从 `debug_audio/` 目录查找 `{entry}_passive.mp3`、`{entry}_evoke.mp3`、`{entry}_channel.mp3`。

---

## 9. 注册方式

### 9.1 属性注册（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

[RegisterOrb]
public class TestOrb : ModOrbTemplate
{
    // ...
}
```

前提：在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

### 9.2 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Orb<TestOrb>()
    .Apply();
```

### 9.3 Manifest 注册

```csharp
new OrbRegistrationEntry<TestOrb>()
```

---

## 10. 引导命令 — OrbCmd.Channel

使用 `OrbCmd.Channel` 引导充能球到指定玩家身上：

```csharp
using MegaCrit.Sts2.Core.Commands;

// 引导 TestOrb 到卡牌所有者
await OrbCmd.Channel<TestOrb>(choiceContext, cardPlay.Card.Owner);

// 引导到当前玩家
await OrbCmd.Channel<TestOrb>(choiceContext, Owner);
```

---

## 11. 常用命令参考

在 Orb 的 `Passive` 和 `Evoke` 方法中常用命令：

| 命令 | 说明 | 示例 |
|------|------|------|
| `BlockCmd.GainBlock(amount)` | 获得格挡 | `await BlockCmd.GainBlock(amount).Execute(choiceContext)` |
| `PowerCmd.Apply<TPower>(target, amount, applier, source)` | 施加能力 | `await PowerCmd.Apply<StrengthPower>(Owner, 2, Owner, null)` |
| `DamageCmd.Attack(amount)` | 造成伤害 | `await DamageCmd.Attack(amount).Targeting(target).Execute(choiceContext)` |
| `CardPileCmd.Draw(ctx, count, player)` | 抽牌 | `await CardPileCmd.Draw(choiceContext, count, Owner)` |
| `CreatureCmd.Heal(target, amount)` | 治疗 | `await CreatureCmd.Heal(Owner, 10).Execute(choiceContext)` |

---

## 12. 本地化

### 12.1 文件位置

```
{{MODID}}/{{MODID}}/localization/eng/orbs.json
{{MODID}}/{{MODID}}/localization/zhs/orbs.json
```

### 12.2 格式

```json
{
    "{{MODID_UPPER}}_ORB_TEST_ORB.title": "测试球",
    "{{MODID_UPPER}}_ORB_TEST_ORB.description": "充能球：回合开始时抽牌。",
    "{{MODID_UPPER}}_ORB_TEST_ORB.smartDescription": "[gold]被动：[/gold]回合开始时，抽[blue]{Passive}[/blue]张牌。\n[gold]激发：[/gold]抽[blue]{Evoke}[/blue]张牌。"
}
```

### 12.3 字段说明

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 球体名称 | 是 |
| `description` | 效果描述（静态文本） | 推荐 |
| `smartDescription` | 智能描述（支持 `{Passive}` 和 `{Evoke}` 占位符，自动显示当前数值） | 推荐 |

### 12.4 smartDescription 占位符

| 占位符 | 说明 |
|--------|------|
| `{Passive}` | 被动效果数值（动态显示 `PassiveVal`） |
| `{Evoke}` | 激发效果数值（动态显示 `EvokeVal`） |

### 12.5 BBCode 标签

| 标签 | 效果 |
|------|------|
| `[gold]文字[/gold]` | 金色高亮（用于关键词） |
| `[blue]文字[/blue]` | 蓝色（用于数值） |
| `[b]文字[/b]` | 加粗 |
| `[purple]文字[/purple]` | 紫色 |

---

## 13. 完整代码模板

### 13.1 抽牌充能球（完整模板）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Orbs;

[RegisterOrb]
public class TestOrb : ModOrbTemplate
{
    // 被动效果数值（受集中影响）
    public override decimal PassiveVal => ModifyOrbValue(1);

    // 激发效果数值（受集中影响）
    public override decimal EvokeVal => ModifyOrbValue(2);

    // 暗色色调
    public override Color DarkenedColor => new(0.1f, 0.2f, 0.5f);

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/orbs/test_orb.png",
        VisualsScenePath: "res://{{MODID}}/scenes/orbs/test_orb.tscn"
    );

    // 创建视觉精灵（无需手动挂脚本）
    protected override Node2D? TryCreateOrbSprite() =>
        RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    // 回合开始时触发被动
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    // 被动效果：抽牌
    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();                                                 // 触发视觉反馈
        await CardPileCmd.Draw(choiceContext, (int)PassiveVal, Owner);
    }

    // 激发效果：抽更多牌，返回受影响的生物
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();                                             // 播放激发音效
        await CardPileCmd.Draw(playerChoiceContext, (int)EvokeVal, Owner);
        return [Owner.Creature];                                    // 返回受影响的生物
    }
}
```

### 13.2 闪电球（伤害型 Orb）

```csharp
[RegisterOrb]
public class LightningOrb : ModOrbTemplate
{
    public override decimal PassiveVal => ModifyOrbValue(3);
    public override decimal EvokeVal => ModifyOrbValue(8);
    public override Color DarkenedColor => new("796606");

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/orbs/lightning_orb.png",
        VisualsScenePath: "res://{{MODID}}/scenes/orbs/lightning_orb.tscn"
    );

    protected override Node2D? TryCreateOrbSprite() =>
        RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    protected override string PassiveSfx => "event:/sfx/characters/defect/defect_lightning_passive";
    protected override string EvokeSfx => "event:/sfx/characters/defect/defect_lightning_evoke";
    protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";

    // 被动：对随机敌人造成伤害
    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await DamageCmd.Attack(PassiveVal)
            .FromOrb(this)
            .TargetingRandomEnemy()
            .Execute(choiceContext);
    }

    // 激发：对随机敌人造成大量伤害
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();
        var result = await DamageCmd.Attack(EvokeVal)
            .FromOrb(this)
            .TargetingRandomEnemy()
            .Execute(playerChoiceContext);
        return result.Select(x => x.target);
    }
}
```

### 13.3 冰霜球（格挡型 Orb）

```csharp
[RegisterOrb]
public class FrostOrb : ModOrbTemplate
{
    public override decimal PassiveVal => ModifyOrbValue(2);
    public override decimal EvokeVal => ModifyOrbValue(5);
    public override Color DarkenedColor => new("7860a7");

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/orbs/frost_orb.png",
        VisualsScenePath: "res://{{MODID}}/scenes/orbs/frost_orb.tscn"
    );

    protected override Node2D? TryCreateOrbSprite() =>
        RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    // 被动：回合结束时获得格挡（BeforeTurnEnd 触发）
    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await Passive(choiceContext, null);
    }

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await BlockCmd.GainBlock((int)PassiveVal)
            .FromOrb(this)
            .Execute(choiceContext);
    }

    // 激发：获得大量格挡
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();
        await BlockCmd.GainBlock((int)EvokeVal)
            .FromOrb(this)
            .Execute(playerChoiceContext);
        return [Owner.Creature];
    }
}
```

### 13.4 暗影球（累积型 Orb）

参考原版 `DarkOrb`：被动积累伤害数值，激发时一次性释放：

```csharp
[RegisterOrb]
public class DarkOrb : ModOrbTemplate
{
    private decimal _evokeVal = 6m;

    public override decimal PassiveVal => ModifyOrbValue(6);
    public override decimal EvokeVal => _evokeVal;
    public override Color DarkenedColor => new("9001d3");

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://{{MODID}}/images/orbs/dark_orb.png",
        VisualsScenePath: "res://{{MODID}}/scenes/orbs/dark_orb.tscn"
    );

    protected override Node2D? TryCreateOrbSprite() =>
        RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    // 被动：累积伤害值
    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        _evokeVal += PassiveVal;
        // 更新 UI 显示
        await Task.CompletedTask;
    }

    // 激发：对最高血量敌人造成累计伤害
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();
        // 对全体敌人造成累计伤害，或对单一敌人
        return [Owner.Creature];
    }
}
```

### 13.5 使用抽象基类统一管理（推荐）

```csharp
[RegisterOrb]
public abstract class {{MODID}}OrbModel : ModOrbTemplate
{
    public override OrbAssetProfile AssetProfile => new(
        IconPath: $"res://{{MODID}}/images/orbs/{GetType().Name}.png",
        VisualsScenePath: $"res://{{MODID}}/scenes/orbs/{GetType().Name}.tscn"
    );

    protected override Node2D? TryCreateOrbSprite() =>
        RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);
}

// 子类只需关注逻辑
[RegisterOrb]
public class DrawOrb : {{MODID}}OrbModel
{
    public override decimal PassiveVal => ModifyOrbValue(1);
    public override decimal EvokeVal => ModifyOrbValue(3);
    public override Color DarkenedColor => new(0.1f, 0.2f, 0.5f);

    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger();
        await CardPileCmd.Draw(choiceContext, (int)PassiveVal, Owner);
    }

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext pc)
    {
        PlayEvokeSfx();
        await CardPileCmd.Draw(pc, (int)EvokeVal, Owner);
        return [Owner.Creature];
    }
}
```

### 13.6 最简 Orb 模板（快速起步）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Orbs;

[RegisterOrb]
public class MyOrb : ModOrbTemplate
{
    public override decimal PassiveVal => ModifyOrbValue(1);
    public override decimal EvokeVal => ModifyOrbValue(2);
    public override Color DarkenedColor => new(0.5f, 0.5f, 0.5f);
}
```

> 最简模板缺少图标、场景和效果回调，仅用于快速验证注册是否成功。正式 Orb 需补充 `AssetProfile`、`TryCreateOrbSprite`、`Passive` 和 `Evoke`。

---

## 14. 文件组织

```
{{MODID}}/{{MODID}}Code/Orbs/
├── {{MODID}}OrbModel.cs             # 抽象基类（可选）
├── TestOrb.cs                       # 抽牌球
├── LightningOrb.cs                  # 闪电球
└── FrostOrb.cs                      # 冰霜球

{{MODID}}/{{MODID}}/
├── scenes/
│   └── orbs/
│       ├── test_orb.tscn            # 球体视觉场景
│       └── test_orb.tscn.uid        # 场景 UID（自动生成）
├── images/
│   └── orbs/
│       ├── test_orb.png             # 提示图标
│       └── test_orb_visual.png      # 球体外观纹理
└── localization/
    ├── eng/
    │   └── orbs.json                # 英文本地化
    └── zhs/
        └── orbs.json                # 中文本地化
```

---

## 15. 控制台调试

在游戏中按 `~` 打开控制台：

```
orb {{MODID_UPPER}}_ORB_TEST_ORB
```

快速检查 Orb 是否注册成功：在控制台尝试引导该 Orb。

---

## 16. 参考已有 Orb 实现

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 伤害被动/激发 | `Models/Orbs/` | `LightningOrb` |
| 格挡被动/激发 | `Models/Orbs/` | `FrostOrb` |
| 累积型被动 | `Models/Orbs/` | `DarkOrb` |
| 能量型 Orb | `Models/Orbs/` | `PlasmaOrb` |
| 固定+集中值 | `Models/Orbs/` | `GlassOrb`（被动 = 固定，激发 = 被动 * 2） |
| 回合结束触发 | `Models/Orbs/` | `FrostOrb`（BeforeTurnEndOrbTrigger） |
| 回合开始触发 | `Models/Orbs/` | `LightningOrb`（AfterTurnStartOrbTrigger） |

源码位置: `{{STS2_SOURCE_ROOT}}\Models\Orbs\` (5 个原版 Orb)

---

## 17. 调色建议

| 球体 | 十六进制颜色 | 说明 |
|------|-------------|------|
| 闪电球 | `"796606"` | 金色/黄色系 |
| 冰霜球 | `"7860a7"` | 紫色/蓝色系 |
| 暗影球 | `"9001d3"` | 紫色/暗色系 |
| 玻璃球 | `"008585"` | 青色/绿色系 |
| 等离子球 | `"cc7837"` | 橙色系 |

`DarkenedColor` 用于表示空球槽位的背景色，应使用球体主色的较暗版本。

---

## 18. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 球体不显示 | 视觉场景路径错误或场景格式不对 | 检查 `AssetProfile.VisualsScenePath`，确认场景根节点是 `Node2D` |
| 球体无任何效果 | `Passive`/`Evoke` 未重写 | 确认已重写并实现 `Passive` 和 `Evoke` 方法 |
| 被动不触发 | 触发时机回调未重写 | 确认已重写 `AfterTurnStartOrbTrigger` 或 `BeforeTurnEndOrbTrigger` |
| 图标显示为空白 | 图片路径错误 | 检查 `AssetProfile.IconPath` |
| 描述显示原始键名 | 本地化 JSON 缺少条目 | 检查 `orbs.json` 中键名 |
| `{Passive}` 显示为 0 | smartDescription 格式错误 | 确认为 `smartDescription` 字段并包含 `{Passive}` 占位符 |
| 集中不生效 | 未使用 `ModifyOrbValue()` | `PassiveVal`/`EvokeVal` 需要使用 `ModifyOrbValue(n)` 包装 |
| 激发后无反应 | `Evoke` 返回空列表 | `Evoke` 必须返回受影响的 `Creature` 列表 |
| 编译错误：找不到类型 | 缺少 using 引用 | 确认引用了 `STS2RitsuLib.Scaffolding.Content` 等命名空间 |
| TryCreateOrbSprite 需要但不实现 | 缺少视觉精灵创建 | 实现 `TryCreateOrbSprite()` 调用 `RitsuGodotNodeFactories` |
| 音效不播放 | FMOD 路径错误或调试音频文件不存在 | 设置 `PassiveSfx`/`EvokeSfx`/`ChannelSfx` 或添加 `debug_audio/` 文件 |

---

## 19. 编写审查清单

### 19.1 基础检查

- [ ] 是否继承了 `ModOrbTemplate`？
- [ ] 是否重写了 `PassiveVal`（使用 `ModifyOrbValue()` 包装）？
- [ ] 是否重写了 `EvokeVal`（使用 `ModifyOrbValue()` 包装）？
- [ ] 是否重写了 `DarkenedColor`？
- [ ] 是否添加了 `[RegisterOrb]` 属性？
- [ ] 命名空间是否正确？（`{{MODID}}.{{MODID}}Code.Orbs`）

### 19.2 逻辑检查

- [ ] 是否重写了 `Passive` 方法并使用了 `async Task`？
- [ ] 是否重写了 `Evoke` 方法并返回了 `Task<IEnumerable<Creature>>`？
- [ ] `Evoke` 返回的列表是否包含了受影响的生物？
- [ ] 是否调用了 `Trigger()` 触发视觉反馈？
- [ ] 是否调用了 `PlayPassiveSfx()` / `PlayEvokeSfx()` 播放音效？
- [ ] 触发时机是否选择正确（回合开始/回合结束）？

### 19.3 资源检查

- [ ] `AssetProfile.IconPath` 路径是否正确？
- [ ] `AssetProfile.VisualsScenePath` 路径是否正确？
- [ ] 视觉场景根节点是否为 `Node2D`？
- [ ] 是否实现了 `TryCreateOrbSprite()`？

### 19.4 本地化检查

- [ ] `orbs.json` 中是否添加了 `title`？
- [ ] `orbs.json` 中是否添加了 `smartDescription`？
- [ ] `smartDescription` 中是否使用了 `{Passive}` 和 `{Evoke}` 占位符？
- [ ] BBCode 标签是否正确闭合？

### 19.5 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 是否在 `Entry.Init()` 中调用？

---

## 20. 原版 Orb 类型速查

| Orb 类型 | Passive | Evoke | 颜色 |
|---------|---------|-------|------|
| `LightningOrb` | 对随机敌人造成 3 点伤害 | 对随机敌人造成 8 点伤害 | `"796606"` |
| `FrostOrb` | 获得 2 点格挡 | 获得 5 点格挡 | `"7860a7"` |
| `DarkOrb` | 累积 6 点伤害到激发值 | 造成累积的伤害 | `"9001d3"` |
| `PlasmaOrb` | 获得等离子效果 | 获得[0.5]点能量 | `"cc7837"` |
| `GlassOrb` | 累积伤害并自增速度最快 | 造成被动值 2 倍伤害 | `"008585"` |

---

*最后更新：2026-05-12*
