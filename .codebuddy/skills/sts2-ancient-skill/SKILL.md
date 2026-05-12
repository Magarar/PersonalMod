---
name: sts2-ancient-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 先古之民 (Ancient) 事件提供全面的参考与自动检查。
  涵盖 Ancient 定义 (ModAncientEventTemplate)、事件选项 (EventOption)、遗物选项 (CreateModRelicOption)、
  对话系统 (DefineDialogues / AncientDialogueSet)、聊天键格式 (talk key 格式)、
  场景配置 (EventAssetProfile / AncientPresentationAssetProfile)、
  注册方式 ([RegisterActAncient] / [RegisterSharedAncient])、
  本地化文本 (ancients.json)、配色自定义 (ButtonColor / DialogueColor)、
  权重池 (WeightedList)、选项池随机生成、自定义 Godot 场景编写、
  以及完整的代码模板与审查清单。
  当用户要求创建新先古之民、修改已有 Ancient 逻辑、或排查 Ancient 相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 先古之民 (Ancient) 编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 先古之民 (Ancient) 事件，核心步骤：
1. 创建 Ancient 类，继承 `ModAncientEventTemplate`
2. 用 `[RegisterActAncient<TAct>()]` 或 `[RegisterSharedAncient]` 注册
3. 重写 `ButtonColor` 和 `DialogueColor` 自定义配色
4. 重写 `AssetProfile` 配置背景场景路径
5. 重写 `AncientPresentationAssetProfile` 配置地图图标
6. 定义选项池 (`AllPossibleOptions`) 和生成规则 (`GenerateInitialOptions`)
7. 编写本地化 JSON (ancients.json) — title + epithet + 对话 + 选项
8. （可选）创建自定义 Godot 场景作为 Ancient 背景

**当前项目 ModId**: `PersonalMod`

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-07-add-ancient/

---

## 2. Model ID 规则

RitsuLib 注册的 Ancient ID 格式：

```
<MODID>_ANCIENT_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `TestAncient` | `PERSONALMOD_ANCIENT_TEST_ANCIENT` |
| `Neow` | `PERSONALMOD_ANCIENT_NEOW` |
| `Darv` | `PERSONALMOD_ANCIENT_DARV` |

本地化键必须使用此 ID：

```json
{
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.title": "戈多",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.epithet": "等待者"
}
```

---

## 3. 基类: ModAncientEventTemplate

继承链: `ModAncientEventTemplate` → `AncientEventModel` → `EventModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

无构造参数。

### 3.1 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `AllPossibleOptions` | `abstract IEnumerable<EventOption>` | 所有可能的选项池 |
| `GenerateInitialOptions()` | `abstract IReadOnlyList<EventOption>` | 本次生成的选项 |

> `GenerateInitialOptions()` 继承自 `EventModel`，返回本次 Ancient 实际显示的选项列表。

### 3.2 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ButtonColor` | `Color` | `Color(0, 0, 0, 0.35)` | 选项按钮颜色 |
| `DialogueColor` | `Color` | `Color("28454f")` | 对话框颜色 |
| `AmbientBgm` | `string` | `""` | 背景音乐 FMOD 路径 |
| `AssetProfile` | `EventAssetProfile` | — | 背景场景等资源配置 |
| `AncientPresentationAssetProfile` | `AncientEventPresentationAssetProfile` | — | 地图图标等 Ancient 展示配置 |
| `IsAllowed(IRunState)` | `bool` | `true` | 出现条件（如限制幕数） |

### 3.3 AncientEventModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 标题 (`ancients/{Entry}.title`) |
| `Epithet` | `LocString` | 称号 (`ancients/{Entry}.epithet`) |
| `DialogueSet` | `AncientDialogueSet` | 对话集 |
| `InitialDescription` | `LocString` | 初始描述 (`ancients/{Entry}.pages.INITIAL.description`) |
| `ButtonColor` | `Color` | 选项按钮背景色 |
| `DialogueColor` | `Color` | 对话框颜色 |
| `MapIcon` | `Texture2D` | 地图图标 |
| `MapIconOutline` | `Texture2D` | 地图图标轮廓 |
| `RunHistoryIcon` | `Texture2D` | 战绩历史图标 |
| `RunHistoryIconOutline` | `Texture2D` | 战绩历史图标轮廓 |
| `AmbientBgm` | `string` | 背景音乐 FMOD 事件路径 |
| `HasAmbientBgm` | `bool` | 是否有背景音乐 |
| `LayoutType` | `EventLayoutType` | 布局类型（返回 `EventLayoutType.Ancient`） |

### 3.4 ModAncientEventTemplate 特有成员

RitsuLib 提供以下辅助方法：

| 成员 | 说明 |
|------|------|
| `DefineDialogues()` | **默认实现**扫描 `ancients` JSON 的 `talk` 键自动加载对话 |
| `ModOptionKey(string page, string option)` | 构建带 Mod 命名空间的选项键 |
| `InitialOptionKey(string option)` | 简化构建 INITIAL 页面选项键 |
| `CreateModRelicOption<TRelic>()` | 创建遗物选项，选择后自动调用 `Done()` |
| `CreateModRelicOption<TRelic>(string pageName)` | 同上，可指定页面名 |

**关键设计**: RitsuLib 的 `DefineDialogues()` 默认实现会自动从 `ancients` JSON 读取对话（无需手动编写 `AncientDialogueSet`），这与原版 Ancient 必须在代码中定义对话不同。

---

## 4. 枚举速查

### 4.1 EventLayoutType

| 值 | 说明 |
|----|------|
| `Default` | 默认事件布局 |
| `Combat` | 战斗事件布局 |
| `Ancient` | Ancient 事件布局（AncientEventModel 固定返回此值） |

### 4.2 AncientDialogueSpeaker

```csharp
AncientDialogueSpeaker.None      // 无
AncientDialogueSpeaker.Ancient   // 先古之民说话（左侧对话框）
AncientDialogueSpeaker.Character // 角色说话（右侧对话框）
```

---

## 5. 选项系统

### 5.1 EventOption 构造

```csharp
new EventOption(
    this,               // Ancient 事件实例
    OnChosenCallback,   // 选择后的回调方法 (Func<Task>)
    "OPTION_KEY",       // 选项键（用于本地化查找）
    additionalHoverTips // 额外悬停提示（可选）
);
```

### 5.2 选项键

RitsuLib 提供辅助构建带命名空间选项键的方法：

```csharp
// 构建完整选项键：<MODID>_ANCIENT_<TYPENAME>.pages.<PAGE>.options.<OPTION>.title
// 本地化格式：{Entry}.pages.INITIAL.options.MY_OPTION.title

// 使用辅助方法
string key = InitialOptionKey("ACCEPT");
// 生成: "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.options.ACCEPT"

// 指定页面
string key2 = ModOptionKey("SECOND_PAGE", "LEAVE");
// 生成: "...pages.SECOND_PAGE.options.LEAVE"
```

### 5.3 CreateModRelicOption — 遗物选项

```csharp
// 创建遗物选项，选择后自动调用 Done() 完成事件
CreateModRelicOption<Anchor>();                          // 默认 INITIAL 页面
CreateModRelicOption<Anchor>("SECOND_PAGE");             // 指定页面
```

### 5.4 选项回调中的常用方法

| 方法 | 说明 |
|------|------|
| `Done()` | 完成事件（关闭 Ancient 界面） |
| `SetEventFinished(LocString)` | 设置完成描述 |
| `StartPreFinished()` | 以预完成状态开始 |

### 5.5 权重池 (WeightedList)

RitsuLib 提供 `WeightedList<T>` 用于带权重的随机选择：

```csharp
private WeightedList<EventOption> Pool3 => new()
{
    { CreateModRelicOption<YummyCookie>(), 2 },  // 权重 2
    { CreateModRelicOption<WingCharm>(), 1 }      // 权重 1
};

// 随机获取
EventOption chosen = Pool3.GetRandom(Rng);
```

### 5.6 固定池

```csharp
private IReadOnlyList<EventOption> Pool1 => [
    CreateModRelicOption<Akabeko>(),
    CreateModRelicOption<Anchor>(),
];

// 从池中随机获取一个
EventOption chosen = Rng.NextItem(Pool1)!;
```

---

## 6. 对话系统

### 6.1 RitsuLib 对话加载机制

RitsuLib 的 `ModAncientEventTemplate` 提供了 `DefineDialogues()` 的默认实现，自动从 `ancients` JSON 文件中读取 `talk` 键。你**无需**手动定义 `AncientDialogueSet`，只需在 JSON 中按格式写入对话即可。

### 6.2 对话键格式

```
<Entry>.talk.<CHAR_ENTRY>.<DIALOGUE_INDEX>-<LINE_INDEX>[r].<speaker>
```

各部分说明：

| 部分 | 说明 | 示例 |
|------|------|------|
| `Entry` | Ancient 的完整 ID | `PERSONALMOD_ANCIENT_TEST_ANCIENT` |
| `CHAR_ENTRY` | 角色 ID 或 `ANY`/`firstVisitEver` | `IRONCLAD`, `SILENT`, `ANY` |
| `DIALOGUE_INDEX` | 对话序号（每次访问递增） | `0`, `1`, `2` |
| `LINE_INDEX` | 行号（多行对话依次递增） | `0`, `1`, `2` |
| `r` | 可选，标记为重复对话（后续访问可复用） | 如 `0-0r` |
| `speaker` | 说话者 | `.ancient` 或 `.char` |

### 6.3 对话键详解

| 对话类型 | 键格式 | 说明 |
|---------|--------|------|
| 首次访问 | `talk.firstVisitEver.<DIALOGUE_INDEX>-<LINE_INDEX>.ancient` | 仅第一次访问时显示 |
| 角色对话 | `talk.<CHAR_ENTRY>.<DIALOGUE_INDEX>-<LINE_INDEX>.ancient` | 指定角色访问时的专属对话 |
| 角色回应 | `talk.<CHAR_ENTRY>.<DIALOGUE_INDEX>-<LINE_INDEX>.char` | 角色的回应台词 |
| 通用对话 | `talk.ANY.<DIALOGUE_INDEX>-<LINE_INDEX>.ancient` | 任意角色访问时的对话 |
| 重复对话 | 以上格式加 `r` 后缀，如 `talk.IRONCLAD.1-0r.ancient` | 后续访问会显示此对话 |
| 继续按钮 | `<LINE_KEY>.next` | 多行对话之间的"继续"按钮文本 |

### 6.4 对话键选择逻辑

系统按以下优先级选择对话：
1. **firstVisitEver** — 如果 totalVisits == 0 且有 firstVisitEver
2. **角色专属对话** — 如果 characterId 匹配且有对应 `VisitIndex` 的对话
3. **ANY 通用对话** — 如果角色没有独白，且 `AgnosticDialogues` 中有匹配的
4. **重复对话** — 如果设置了 `r` 后缀且 visitIndex 符合条件

### 6.5 对话示例

```json
{
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.firstVisitEver.0-0.ancient": "……第一次来？坐。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.firstVisitEver.0-1.ancient": "等一会儿你就习惯了。",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.ANY.0-0r.ancient": "你又来了？",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-0.ancient": "战士，你的火太亮。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-0.next": "继续说",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-1.char": "……我有要事。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-1.next": "继续",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-2.ancient": "一切都得等。",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.SILENT.0-0.ancient": "猎手，坐。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.SILENT.1-0r.ancient": "……还在。"
}
```

### 6.6 SFX 与音效

如果需要在对话行播放音效，可以在 `DialogueLine` 构造时传入 FMOD 路径，但通过 JSON 定义时不需要手动处理（RitsuLib 默认实现会处理）。

### 6.7 .char vs .ancient 后缀

| 后缀 | 说话者 | 显示位置 |
|------|--------|---------|
| `.ancient` | 先古之民 | 左侧对话气泡 |
| `.char` | 当前角色 | 右侧对话气泡 |

---

## 7. 资源配置

### 7.1 EventAssetProfile — 场景配置

```csharp
public override EventAssetProfile AssetProfile => new(
    BackgroundScenePath: "res://PersonalMod/scenes/ancient/test_ancient.tscn"
);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `BackgroundScenePath` | `string` | 自定义背景场景路径（Ancient 的背景画面） |
| `CustomPortraitPath` | `string?` | 可选的自定义肖像路径 |
| `CustomLayoutScenePath` | `string?` | 可选的自定义布局场景路径 |
| `CustomVfxScenePath` | `string?` | 可选的自定义特效场景路径 |

### 7.2 AncientPresentationAssetProfile — 展示配置

```csharp
public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
    MapIconPath: "res://PersonalMod/images/ancients/test_ancient_map.png",
    MapIconOutlinePath: "res://PersonalMod/images/ancients/test_ancient_map_outline.png",
    RunHistoryIconPath: "res://PersonalMod/images/ancients/test_ancient_history.png",
    RunHistoryIconOutlinePath: "res://PersonalMod/images/ancients/test_ancient_history_outline.png"
);
```

| 参数 | 说明 |
|------|------|
| `MapIconPath` | 地图节点图标 |
| `MapIconOutlinePath` | 地图节点图标轮廓 |
| `RunHistoryIconPath` | 战绩历史图标 |
| `RunHistoryIconOutlinePath` | 战绩历史图标轮廓 |

### 7.3 原版资源路径约定

原版 Ancient 资源路径（Mod 不需要遵循，但可参考）：

| 资源 | 路径 |
|------|------|
| 地图图标 | `packed/map/ancients/ancient_node_{entry}.png` |
| 地图轮廓 | `packed/map/ancients/ancient_node_{entry}_outline.png` |
| 战绩历史轮廓 | `ui/run_history/{entry}_outline.png` |
| 占位图 | `images/ancients/{entry}_placeholder.png` |

---

## 8. 注册方式

### 8.1 注册到指定幕（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

// 指定只在荣耀之章 (Glory) 生成
[RegisterActAncient(typeof(Glory))]
public class TestAncient : ModAncientEventTemplate { ... }
```

### 8.2 注册为共享 Ancient（在所有幕出现）

```csharp
[RegisterSharedAncient]
public class TestAncient : ModAncientEventTemplate { ... }
```

使用 `[RegisterSharedAncient]` 时，通常需要重写 `IsAllowed` 自定义生成条件：

```csharp
public override bool IsAllowed(IRunState runState)
{
    return runState.CurrentActIndex == 1; // 只在第二幕出现
}
```

### 8.3 自定义生成条件

```csharp
[RegisterSharedAncient]
public class ConditionalAncient : ModAncientEventTemplate
{
    // 自定义出现条件
    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1;
    }
}
```

### 8.4 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("PersonalMod")
    .SharedAncient<TestAncient>()               // 注册为共享 Ancient
    .ActAncient<Glory, TestAncient>()            // 注册到指定幕
    .Apply();
```

### 8.5 可用幕 (Act) 类型

| 幕 | 类型名 | 说明 |
|----|--------|------|
| 第一幕 | `Act1` | 由 `Glory` Act 或其他 Act 模型指定 |
| 第二幕 | `Act2` | — |
| 第三幕 | `Act3` | — |

具体幕类型需要通过 STS2 源码查找对应的 `ActModel` 子类。

---

## 9. 选项生成模式

### 9.1 固定池 + 随机选择

```csharp
// 定义固定选项池
private IReadOnlyList<EventOption> Pool1 => [
    CreateModRelicOption<Akabeko>(),
    CreateModRelicOption<Anchor>(),
];

// 生成时随机选择
protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return [
        Rng.NextItem(Pool1)!,
        Rng.NextItem(Pool2)!,
    ];
}
```

### 9.2 权重池

```csharp
private WeightedList<EventOption> Pool3 => new()
{
    { CreateModRelicOption<YummyCookie>(), 2 },
    { CreateModRelicOption<WingCharm>(), 1 }
};

protected override IReadOnlyList<EventOption> GenerateInitialOptions()
{
    return [
        Pool3.GetRandom(Rng),
    ];
}
```

### 9.3 所有可能的选项

```csharp
public override IEnumerable<EventOption> AllPossibleOptions => [
    .. Pool1,          // 展开 Pool1
    .. Pool2,          // 展开 Pool2
    .. Pool3,          // 展开 Pool3
    CustomOption(),    // 自定义选项
];
```

---

## 10. 自定义选项（非遗物奖励）

如果需要非遗物奖励的选项（如获得金币、受伤、抽牌等），可以创建自定义 `EventOption`：

```csharp
private EventOption HealOption => new(this, OnHealChosen, InitialOptionKey("HEAL"));

private async Task OnHealChosen()
{
    // 执行效果
    await CreatureCmd.Heal(Owner, 10).Execute(default);
    // 完成事件
    Done();
}
```

本地化：

```json
{
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.options.HEAL.title": "接受治疗",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.options.HEAL.description": "恢复 10 点生命"
}
```

---

## 11. 自定义 Godot 场景

### 11.1 场景要求

Ancient 背景场景必须以 `Control` 类型为根节点。

### 11.2 场景示例

```gdscript
[gd_scene load_steps=5 format=3]

[ext_resource type="Texture2D" path="res://PersonalMod/images/ancients/test_ancient_bg.png" id="1"]

[sub_resource type="Shader" id="Shader_8eo3w"]
code = "shader_type canvas_item;
// 自定义背景着色器...
"

[sub_resource type="ShaderMaterial" id="ShaderMaterial_n064g"]
shader = SubResource("Shader_8eo3w")

[node name="TestAncient" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0

[node name="Background" type="ColorRect" parent="."]
material = SubResource("ShaderMaterial_n064g")
layout_mode = 1
offset_left = -329.0
offset_top = -49.0
offset_right = 2253.0
offset_bottom = 1172.0

[node name="TextureRect" type="TextureRect" parent="."]
layout_mode = 1
offset_left = 694.0
offset_top = 165.0
offset_right = 1044.0
offset_bottom = 515.0
texture = ExtResource("1")
```

### 11.3 场景在 Godot 编辑器中

推荐在 Godot 编辑器中创建场景：
1. 创建新场景，根节点为 `Control`
2. 添加背景元素（`ColorRect`、`TextureRect`、`CPUParticles2D` 等）
3. 应用着色器材质实现动态效果
4. 保存到 `PersonalMod/scenes/ancient/` 目录

---

## 12. 本地化

### 12.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/ancients.json
PersonalMod/PersonalMod/localization/zhs/ancients.json
```

### 12.2 格式概览

```json
{
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.title": "戈多",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.epithet": "等待者",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.description": "你推开了一扇门……",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.options.ACCEPT.title": "接受",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.INITIAL.options.ACCEPT.description": "获得一件遗物。",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.pages.DONE.description": "你离开了这里。",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.firstVisitEver.0-0.ancient": "……有人推开了这扇门。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.ANY.0-0r.ancient": "你又来了？",

  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-0.ancient": "战士，坐。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-0.next": "继续",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-1.char": "……好。",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-1.next": "继续",
  "PERSONALMOD_ANCIENT_TEST_ANCIENT.talk.IRONCLAD.0-2.ancient": "很好。",
}
```

### 12.3 必填字段

| 字段 | 说明 |
|------|------|
| `title` | 先古之民名称 |
| `epithet` | 先古之民称号 |
| `pages.INITIAL.description` | 初始页面描述 |
| `pages.INITIAL.options.<KEY>.title` | 选项标题（每个选项必须） |
| `pages.DONE.description` | 完成页面描述 |

### 12.4 对话字段详解

| 字段 | 说明 | 必需 |
|------|------|------|
| `talk.firstVisitEver.<I>-<L>.ancient` | 首次访问 Ancient 对话 | 推荐 |
| `talk.<CHARACTER>.<I>-<L>.ancient` | Ancient 台词 | 推荐 |
| `talk.<CHARACTER>.<I>-<L>.char` | 角色台词 | 可选 |
| `<LINE_KEY>.next` | 继续按钮文本 | 可选（多行对话用） |

### 12.5 对话键的角色 ID

| 键 | 角色 |
|----|------|
| `IRONCLAD` | 铁甲战士 |
| `SILENT` | 静默猎手 |
| `DEFECT` | 缺陷 |
| `NECROBINDER` | 死灵法师 |
| `REGENT` | 摄政王 |
| `ANY` | 通用对话框（所有角色通用） |

---

## 13. 完整代码模板

### 13.1 基础 Ancient（随机遗物选项）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace PersonalMod.PersonalModCode.Ancients;

[RegisterActAncient(typeof(Glory))]
public class TestAncient : ModAncientEventTemplate
{
    // 配色
    public override Color ButtonColor => new(0.12f, 0.2f, 0.8f, 0.5f);
    public override Color DialogueColor => new(0.12f, 0.2f, 0.8f);

    // 背景场景
    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://PersonalMod/scenes/ancient/test_ancient.tscn"
    );

    // 地图图标
    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://PersonalMod/images/ancients/test_ancient_map.png",
        MapIconOutlinePath: "res://PersonalMod/images/ancients/test_ancient_map_outline.png",
        RunHistoryIconPath: "res://PersonalMod/images/ancients/test_ancient_history.png",
        RunHistoryIconOutlinePath: "res://PersonalMod/images/ancients/test_ancient_history_outline.png"
    );

    // 选项池
    private IReadOnlyList<EventOption> Pool1 => [
        CreateModRelicOption<Akabeko>(),
        CreateModRelicOption<Anchor>(),
    ];

    private IReadOnlyList<EventOption> Pool2 => [
        CreateModRelicOption<LizardTail>(),
        CreateModRelicOption<ArcaneScroll>(),
    ];

    private WeightedList<EventOption> Pool3 => new()
    {
        { CreateModRelicOption<YummyCookie>(), 2 },
        { CreateModRelicOption<WingCharm>(), 1 }
    };

    // 所有可能的选项
    public override IEnumerable<EventOption> AllPossibleOptions => [
        .. Pool1,
        .. Pool2,
        .. Pool3,
    ];

    // 生成本次选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Pool3.GetRandom(Rng),
        ];
    }
}
```

### 13.2 带自定义选项的 Ancient

```csharp
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Ancients;

[RegisterSharedAncient]
public class HealingAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.2f, 0.6f, 0.2f, 0.5f);
    public override Color DialogueColor => new(0.2f, 0.6f, 0.2f);

    // 自定义选项：治疗
    private EventOption HealOption => new(this, OnHealChosen, InitialOptionKey("HEAL"));
    // 自定义选项：离开
    private EventOption LeaveOption => new(this, OnLeave, InitialOptionKey("LEAVE"));

    public override IEnumerable<EventOption> AllPossibleOptions => [HealOption, LeaveOption];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [HealOption, LeaveOption];
    }

    private async Task OnHealChosen()
    {
        await CreatureCmd.Heal(Owner, 15).Execute(default);
        Done();
    }

    private Task OnLeave()
    {
        Done();
        return Task.CompletedTask;
    }
}
```

### 13.3 自定义场景 Ancient（完整示例）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace PersonalMod.PersonalModCode.Ancients;

[RegisterActAncient(typeof(Glory))]
public class MyAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.12f, 0.2f, 0.8f, 0.5f);
    public override Color DialogueColor => new(0.12f, 0.2f, 0.8f);

    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://PersonalMod/scenes/ancient/my_ancient.tscn"
    );

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://PersonalMod/images/ancients/my_ancient_map.png",
        MapIconOutlinePath: "res://PersonalMod/images/ancients/my_ancient_map_outline.png",
        RunHistoryIconPath: "res://PersonalMod/images/ancients/my_ancient_history.png",
        RunHistoryIconOutlinePath: "res://PersonalMod/images/ancients/my_ancient_history_outline.png"
    );

    private IReadOnlyList<EventOption> RelicOptions => [
        CreateModRelicOption<BurningBlood>(),
        CreateModRelicOption<Vajra>(),
    ];

    public override IEnumerable<EventOption> AllPossibleOptions => RelicOptions;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Rng.NextItem(RelicOptions)!];
    }
}
```

### 13.4 最简 Ancient 模板（快速起步）

```csharp
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.PersonalModCode.Ancients;

[RegisterActAncient(typeof(Glory))]
public class MyAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.5f, 0.5f, 0.5f, 0.5f);
    public override Color DialogueColor => new(0.5f, 0.5f, 0.5f);

    private IReadOnlyList<EventOption> Pool => [
        CreateModRelicOption<Anchor>(),
        CreateModRelicOption<Akabeko>(),
    ];

    public override IEnumerable<EventOption> AllPossibleOptions => Pool;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Rng.NextItem(Pool)!];
    }
}
```

> 最简模板缺少场景配置和图标配置，仅用于快速验证注册。

---

## 14. 文件组织

```
PersonalMod/PersonalModCode/Ancients/
├── TestAncient.cs                     # Ancient 事件类
├── HealingAncient.cs                  # 治疗 Ancient
└── MyAncient.cs                       # 自定义 Ancient

PersonalMod/PersonalMod/
├── scenes/
│   └── ancient/
│       ├── test_ancient.tscn          # Ancient 背景场景
│       └── test_ancient.tscn.uid      # 场景 UID（自动生成）
├── images/
│   └── ancients/
│       ├── test_ancient_map.png       # 地图图标
│       ├── test_ancient_map_outline.png
│       ├── test_ancient_history.png   # 战绩历史图标
│       └── test_ancient_history_outline.png
└── localization/
    ├── eng/
    │   └── ancients.json              # 英文本地化（含对话）
    └── zhs/
        └── ancients.json              # 中文本地化（含对话）
```

---

## 15. 调试

### 15.1 控制台命令

在游戏中按 `~` 打开控制台：

```
ancient PERSONALMOD_ANCIENT_TEST_ANCIENT
```

强制触发指定 Ancient 事件。

### 15.2 快速检查

1. 确认 Mod 编译成功
2. 确认 `Entry.Init()` 中调用了 `RegisterModAssembly`
3. 确认 Ancient 类有正确注册属性
4. 检查地图上是否出现 Ancient 节点（刚注册时可能需要新开一局）

---

## 16. 参考已有 Ancient 实现

需要查找类似功能的 Ancient 时，在源码目录中搜索：

| 需求 | 搜索路径 | 关键词 |
|------|---------|--------|
| 角色专属 Ancient | `Models/Events/` | `Neow`, `Darv`, `Pael` |
| 幕绑定 Ancient | `Models/Events/` | `Orobas`, `Tezcatara` |
| 遗物选项 | `Models/Events/` | `RelicOption` |
| 自定义回调 | `Models/Events/` | 搜索 `async Task` |
| 对话系统 | `Entities/Ancients/` | `AncientDialogueSet` |
| 地图图标 | `Models/` | `MapIconPath` |

源码位置:
- `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Events\` (Ancient 实现)
- `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\AncientEventModel.cs` (基类)
- `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Entities\Ancients\` (对话实体)

---

## 17. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| Ancient 不在地图上 | 未正确注册或幕不对 | 检查 `[RegisterActAncient]` 或 `[RegisterSharedAncient]` 属性 |
| Ancient 背景显示为空白 | 背景场景路径错误 | 检查 `AssetProfile.BackgroundScenePath` 路径 |
| 地图图标显示为空白 | 图标路径错误或缺失 | 检查 `AncientPresentationAssetProfile` 中的图标路径 |
| 对话不显示 | JSON 对话键格式错误 | 检查 `ancients.json` 中的 `talk` 键格式 |
| title 显示为原始键名 | 本地化 JSON 缺少条目 | 检查 `ancients.json` 中对应键名 |
| 选项按钮无法交互 | 按钮颜色不透明度过低 | 检查 `ButtonColor` 的 alpha 值 |
| 对话框颜色不正确 | `DialogueColor` 设置错误 | 检查十六进制颜色值 |
| 选项显示原始键名 | 选项键格式错误 | 使用 `InitialOptionKey()` 方法构建键名 |
| 编译错误：找不到 Glory | 缺少幕类型的引用 | 确认幕类型正确（或使用 `[RegisterSharedAncient]`） |
| 权重池不工作 | 未正确使用 `WeightedList` | 确认已添加 `using STS2RitsuLib.Utils` |
| 角色对话不匹配 | 角色键名拼写错误 | 确认角色键名为 `IRONCLAD`/`SILENT`/`DEFECT`/`NECROBINDER`/`REGENT` |

---

## 18. 编写审查清单

### 18.1 基础检查

- [ ] 是否继承了 `ModAncientEventTemplate`？
- [ ] 是否添加了 `[RegisterActAncient]` 或 `[RegisterSharedAncient]` 属性？
- [ ] 是否重写了 `AllPossibleOptions`？
- [ ] 是否重写了 `GenerateInitialOptions()`？
- [ ] 命名空间是否正确？（`PersonalMod.PersonalModCode.Ancients`）

### 18.2 配色检查

- [ ] `ButtonColor` 是否设置了合理的 alpha 值（0.3~0.7）？
- [ ] `DialogueColor` 是否设置了合适的颜色？
- [ ] 按钮和对话框颜色是否搭配？

### 18.3 资源检查

- [ ] `AssetProfile.BackgroundScenePath` 路径是否正确？
- [ ] 自定义背景场景是否以 `Control` 为根节点？
- [ ] `AncientPresentationAssetProfile` 中的图标路径是否正确？
- [ ] 地图图标和轮廓 PNG 文件是否存在？

### 18.4 选项检查

- [ ] 所有使用的遗物类型是否存在且可访问？
- [ ] 权重池中权重值是否合理？
- [ ] `AllPossibleOptions` 是否包含了所有选项池？
- [ ] 自定义选项的回调是否完整？

### 18.5 本地化检查

- [ ] `ancients.json` 中是否添加了 `title` 和 `epithet`？
- [ ] `ancients.json` 中是否添加了 `pages.INITIAL.description`？
- [ ] `ancients.json` 中是否添加了选项的 title/description？
- [ ] `ancients.json` 中是否添加了 `pages.DONE.description`？
- [ ] 对话键格式是否正确（`talk.<ROLE>.<I>-<L>.<speaker>`）？
- [ ] 多行对话是否添加了 `.next` 键？
- [ ] BBCode 标签是否正确闭合？

### 18.6 注册检查

- [ ] `RegisterModAssembly` 是否在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 是否在 `Entry.Init()` 中调用？
- [ ] 幕类型是否正确？（使用 `[RegisterSharedAncient]` 时不需要指定幕）

---

*最后更新：2026-05-12*
