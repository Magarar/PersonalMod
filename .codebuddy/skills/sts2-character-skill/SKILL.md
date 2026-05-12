---
name: sts2-character-skill
description: >-
  该 Skill 为使用 RitsuLib 框架编写杀戮尖塔2 (Slay the Spire 2) Mod 新人物 (Character) 提供全面的参考与自动检查。
  涵盖人物定义 (ModCharacterTemplate<TPool, TRelicPool, TPotionPool>)、
  人物属性 (StartingHp / StartingGold / MaxEnergy / StartingDeck / StartingRelics)、
  人物池 (CardPool / RelicPool / PotionPool)、
  人物资源 (CharacterAssetProfile / 视觉场景 / 能量计数器 / UI图标 / 音频)、
  注册方式 ([RegisterCharacter])、人物解锁与 Epoch 关联、
  人物场景要求 (NCreatureVisuals 结构)、代理人机制 (PlaceholderCharacterId)、
  本地化文本 (characters.json / title / titleObject / 代词)、
  以及完整的代码模板与审查清单。
  当用户要求创建新人物、修改人物配置、或排查人物相关 Mod 问题时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 人物编写 Skill (RitsuLib)

## 1. 概述

在 RitsuLib 框架中编写 STS2 Mod 新人物 (Character)，核心步骤：
1. 创建人物类，继承 `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>`
2. 用 `[RegisterCharacter]` 注册
3. 重写基础属性（`StartingHp`、`StartingGold`、`NameColor` 等）
4. 重写 `AssetProfile` 配置场景、UI 和音频资源
5. 创建卡池 (`TypeListCardPoolModel`)、遗物池、药水池
6. 创建起始牌组和起始遗物
7. **创建场景** (视觉/能量计数器/角色选择图标等)
8. 配置解锁体系（Epoch + Story）
9. 编写本地化 JSON（characters.json）

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

---

## 2. Model ID 规则

RitsuLib 注册的人物 ID 格式：

```
<MODID>_CHARACTER_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `MyCharacter` | `{{MODID_UPPER}}_CHARACTER_MY_CHARACTER` |
| `Ironclad` | `{{MODID_UPPER}}_CHARACTER_IRONCLAD` |

本地化键使用此 ID + 游戏固定后缀：

```json
{
  "{{MODID_UPPER}}_CHARACTER_MY_CHARACTER.title": "戈多",
  "{{MODID_UPPER}}_CHARACTER_MY_CHARACTER.description": "一个等待者..."
}
```

---

## 3. 基类: ModCharacterTemplate

继承链: `ModCharacterTemplate<T1, T2, T3>` → `CharacterModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Scaffolding.Content`

### 3.1 类型参数

```csharp
public class MyCharacter : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
```

| 类型参数 | 说明 |
|---------|------|
| `TCardPool` | 人物专属卡池（`TypeListCardPoolModel`） |
| `TRelicPool` | 人物专属遗物池 |
| `TPotionPool` | 人物专属药水池 |

### 3.2 必须重写

| 成员 | 类型 | 说明 |
|------|------|------|
| `StartingHp` | `abstract int` | 初始生命值 |
| `StartingGold` | `abstract int` | 初始金币 |
| `NameColor` | `abstract Color` | 名称颜色 |
| `Gender` | `abstract CharacterGender` | 角色性别 |
| `StartingDeck` | `abstract IEnumerable<CardModel>` | 起始牌组（打击防御等） |
| `StartingRelics` | `abstract IReadOnlyList<RelicModel>` | 起始遗物 |

### 3.3 推荐重写

| 成员 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AssetProfile` | `CharacterAssetProfile` | — | 资源配置 |
| `MaxEnergy` | `virtual int` | `3` | 最大能量 |
| `BaseOrbSlotCount` | `virtual int` | `0` | 基础球槽数（Defect 为 3） |
| `PlaceholderCharacterId` | `virtual string?` | `"ironclad"` | 占位角色（资源回退） |
| `AttackAnimDelay` | `virtual float` | — | 攻击动画延迟 |
| `CastAnimDelay` | `virtual float` | — | 施法动画延迟 |
| `EnergyLabelOutlineColor` | `virtual Color` | — | 能量标签轮廓颜色 |
| `DialogueColor` | `virtual Color` | — | 对话框颜色 |
| `SpeechBubbleColor` | `virtual VfxColor` | — | 对话气泡颜色 |
| `MapDrawingColor` | `virtual Color` | — | 地图绘制颜色 |

### 3.4 CharacterModel 完整属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `LocString` | 名称（`characters/{Entry}.title`） |
| `TitleObject` | `LocString` | "xxx" 格式（`characters/{Entry}.titleObject`） |
| `StartingHp` | `int` | 初始生命 |
| `StartingGold` | `int` | 初始金币 |
| `MaxEnergy` | `int` | 最大能量（默认 3） |
| `BaseOrbSlotCount` | `int` | 球槽数 |
| `Gender` | `CharacterGender` | 性别 |
| `NameColor` | `Color` | 名称颜色 |
| `CardPool` | `CardPoolModel` | 卡池 |
| `RelicPool` | `RelicPoolModel` | 遗物池 |
| `PotionPool` | `PotionPoolModel` | 药水池 |
| `StartingDeck` | `IEnumerable<CardModel>` | 起始牌组 |
| `StartingRelics` | `IReadOnlyList<RelicModel>` | 起始遗物 |
| `StartingPotions` | `IReadOnlyList<PotionModel>` | 起始药水 |
| `PronounSubject` | `LocString` | 主格代词（他/她） |
| `PronounObject` | `LocString` | 宾格代词（他/她） |
| `PossessiveAdjective` | `LocString` | 物主形容词（他的/她的） |
| `PronounPossessive` | `LocString` | 所有格代词（他的/她的） |

---

## 4. 资源配置 (CharacterAssetProfile)

```csharp
public override CharacterAssetProfile AssetProfile => new(
    Scenes: new(
        VisualsPath: "res://PersonalMod/scenes/character/my_character.tscn",       // 战斗视觉场景
        EnergyCounterPath: "res://PersonalMod/scenes/ui/energy/my_energy_counter.tscn" // 能量计数器
    ),
    Ui: new(
        IconTexturePath: "res://PersonalMod/images/ui/top_panel/character_icon.png",    // 顶部面板图标
        MapMarkerPath: "res://PersonalMod/images/map/map_marker.png"                    // 地图标记
    ),
    Audio: new(
        AttackSfx: "event:/sfx/characters/my_character/attack",          // 攻击音效
        CastSfx: "event:/sfx/characters/my_character/cast",              // 施法音效
        DeathSfx: "event:/sfx/characters/my_character/death",            // 死亡音效
        CharacterSelectSfx: "event:/sfx/characters/my_character/select", // 选择音效
        CharacterTransitionSfx: "event:/sfx/ui/wipe_ironclad"            // 转场音效（可复用原版）
    )
);
```

### 4.1 占位角色回退

未设置的资源字段自动从占位角色（`PlaceholderCharacterId`，默认 `"ironclad"`）获取回退值：

```csharp
public override string? PlaceholderCharacterId => "ironclad";  // 回退到铁甲战士
public override string? PlaceholderCharacterId => null;         // 禁用回退
```

### 4.2 辅助方法

调用 `CharacterAssetProfiles.Ironclad()` / `.Silent()` / `.Defect()` / `.Regent()` / `.Necrobinder()` 直接获取原版角色的资源配置副本。

---

## 5. 角色池定义

### 5.1 卡池

```csharp
using Godot;

public class MyCardPool : TypeListCardPoolModel
{
    public override string Title => "My Pool";
    public override string EnergyColorName => "orange";
    public override string CardFrameMaterialPath => "card_frame_orange";
    public override Color DeckEntryCardColor => new("d2a15a");
    public override bool IsColorless => false;
}
```

| 属性 | 说明 |
|------|------|
| `Title` | 卡池标题 |
| `EnergyColorName` | 能量颜色名（用于颜色区域） |
| `CardFrameMaterialPath` | 卡牌边框材质路径 |
| `DeckEntryCardColor` | 牌组条目颜色 |
| `IsColorless` | 是否为无色 |

### 5.2 遗物池

继承自 `TypeListRelicPoolModel` 或 `PotionPoolModel`（参考原版角色专属池）。

### 5.3 药水池

继承自 `PotionPoolModel`（参考原版角色专属药水池）。

---

## 6. CharacterGender

```csharp
CharacterGender.Neutral    // 中性
CharacterGender.Feminine   // 女性
CharacterGender.Masculine  // 男性
```

---

## 7. 起始牌组与遗物

```csharp
// 起始牌组：5 打击 + 4 防御 + 1 特殊
public override IEnumerable<CardModel> StartingDeck =>
[
    ModelDb.Card<StrikeIronclad>(),
    ModelDb.Card<StrikeIronclad>(),
    ModelDb.Card<StrikeIronclad>(),
    ModelDb.Card<StrikeIronclad>(),
    ModelDb.Card<StrikeIronclad>(),
    ModelDb.Card<DefendIronclad>(),
    ModelDb.Card<DefendIronclad>(),
    ModelDb.Card<DefendIronclad>(),
    ModelDb.Card<DefendIronclad>(),
    ModelDb.Card<MySignatureCard>(),
];

// 起始遗物：1 个
public override IReadOnlyList<RelicModel> StartingRelics =>
    [ModelDb.Relic<MyStarterRelic>()];
```

---

## 8. 人物视觉场景

### 8.1 战斗视觉场景

场景根节点必须为 `Node2D`，建议通过 Spine 动画节点展示角色：

```
MyCharacter (Node2D)
├── Visuals (Node2D) %            # 角色视觉主体
├── Bounds (Control) %            # 碰撞箱/血条大小
├── IntentPos (Marker2D) %        # 意图位置
├── CenterPos (Marker2D) %        # 中心位置
└── TalkPos (Marker2D) %          # 对话气泡位置
```

**要求**：
- `Visuals`、`Bounds`、`IntentPos`、`CenterPos` 节点名**不能改**
- 每个节点需右键勾选"作为唯一名称访问"（显示 `%` 标记）

### 8.2 能量计数器场景

```gdscript
[gd_scene format=3]

[node name="MyEnergyCounter" type="Control"]
# 能量计数 UI
```

### 8.3 角色选择图标

角色选择界面需要：
- 选择背景场景: `screens/char_select/char_select_bg_{entry}.tscn`
- 选择图标: `packed/character_select/char_select_{entry}.png`
- 锁定图标: `packed/character_select/char_select_{entry}_locked.png`

---

## 9. 注册与解锁

### 9.1 注册

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterCharacter]
public class MyCharacter : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
```

### 9.2 解锁体系

人物通常需要配合 Epoch 系统实现解锁。在人物类上添加解锁条件属性：

```csharp
[RegisterCharacter]
[RequireEpoch(typeof(MyCharacterEpoch))]              // 依赖此 Epoch 解锁
[UnlockEpochAfterRunAs(typeof(MyCardEpoch))]           // 打一局解锁卡牌
[UnlockEpochAfterWinAs(typeof(MyVictoryEpoch))]        // 赢一局
[UnlockEpochAfterEliteVictories(typeof(MyEliteEpoch))] // 累计精英
[UnlockEpochAfterBossVictories(typeof(MyBossEpoch))]   // 累计 Boss
[RevealAscensionAfterEpoch(typeof(MyVictoryEpoch))]    // 胜利后解锁进阶
public class MyCharacter : ModCharacterTemplate<...>
```

### 9.3 内容包注册

```csharp
RitsuLibFramework.CreateContentPack("{{MODID}}")
    .Character<MyCharacter>()
    .Apply();
```

---

## 10. 本地化

### 10.1 文件位置

```
PersonalMod/PersonalMod/localization/eng/characters.json
PersonalMod/PersonalMod/localization/zhs/characters.json
```

### 10.2 格式

```json
{
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.title": "戈多",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.titleObject": "戈多",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.description": "一个等待者。",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.pronounSubject": "他",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.pronounObject": "他",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.pronounPossessive": "他的",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.possessiveAdjective": "他的",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.cardsModifierTitle": "戈多的卡牌",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.cardsModifierDescription": "属于戈多的卡牌。",
  "PERSONAL_MOD_CHARACTER_MY_CHARACTER.eventDeathPrevention": "...拒绝..."
}
```

### 10.3 字段说明

| 字段 | 说明 | 必需 |
|------|------|------|
| `title` | 角色名称 | 是 |
| `titleObject` | 角色名称（宾格/作为对象时） | 推荐 |
| `description` | 角色描述（角色选择界面） | 推荐 |
| `pronounSubject` | 主格代词（他/她/它） | 推荐 |
| `pronounObject` | 宾格代词 | 推荐 |
| `pronounPossessive` | 所有格代词（他的/她的） | 推荐 |
| `possessiveAdjective` | 物主形容词 | 推荐 |
| `cardsModifierTitle` | 牌组修饰器标题 | 可选 |
| `cardsModifierDescription` | 牌组修饰器描述 | 可选 |
| `eventDeathPrevention` | 防止死亡事件时文字 | 可选 |

---

## 11. 完整代码模板

### 11.1 完整人物定义

```csharp
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Entities.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace {{MODID}}.{{MODID}}Code.Characters;

[RegisterCharacter]
[RequireEpoch(typeof(MyCharacterEpoch))]
public class MyCharacter : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
    // === 基础属性 ===
    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override int MaxEnergy => 3;
    public override Color NameColor => StsColors.orange;
    public override CharacterGender Gender => CharacterGender.Neutral;

    // === 起始牌组 ===
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<MySignatureCard>(),
    ];

    // === 起始遗物 ===
    public override IReadOnlyList<RelicModel> StartingRelics =>
        [ModelDb.Relic<MyStarterRelic>()];

    // === 资源配置 ===
    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new(
VisualsPath: "res://{{MODID}}/scenes/character/my_character.tscn",
        EnergyCounterPath: "res://{{MODID}}/scenes/ui/energy/my_energy_counter.tscn"
        ),
        Ui: new(
            IconTexturePath: "res://{{MODID}}/images/ui/top_panel/my_character_icon.png",
            MapMarkerPath: "res://{{MODID}}/images/map/my_map_marker.png"
        ),
        Audio: new(
            AttackSfx: "event:/sfx/characters/my_character/attack",
            CharacterSelectSfx: "event:/sfx/characters/my_character/select"
        )
    );

    // === 视觉相关 ===
    public override VfxColor SpeechBubbleColor => VfxColor.Pink;
    public override Color DialogueColor => new("704240");
    public override Color EnergyLabelOutlineColor => new("801212FF");
    public override Color MapDrawingColor => new("E15847FF");
}
```

### 11.2 卡池定义

```csharp
using Godot;

public class MyCardPool : TypeListCardPoolModel
{
    public override string Title => "My Pool";
    public override string EnergyColorName => "orange";
    public override string CardFrameMaterialPath => "card_frame_orange";
    public override Color DeckEntryCardColor => new("d2a15a");
    public override bool IsColorless => false;
}
```

### 11.3 人物 + Epoch 解锁（最简）

```csharp
[RegisterCharacter]
[RequireEpoch(typeof(MyCharacterEpoch))]
public class MyCharacter : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override Color NameColor => Colors.White;
    public override CharacterGender Gender => CharacterGender.Neutral;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
        [ModelDb.Relic<BurningBlood>()];

    public override CharacterAssetProfile AssetProfile =>
        CharacterAssetProfiles.Ironclad(); // 临时复用铁甲战士资源
}
```

---

## 12. 文件组织

```
{{MODID}}/{{MODID}}Code/Characters/
├── MyCharacter.cs                         # 人物定义

{{MODID}}/{{MODID}}Code/CardPools/
├── MyCardPool.cs                          # 卡池定义

{{MODID}}/{{MODID}}/
├── scenes/
│   ├── character/
│   │   └── my_character.tscn              # 战斗视觉场景 (NCreatureVisuals)
│   └── ui/
│       └── energy/
│           └── my_energy_counter.tscn      # 能量计数器
├── images/
│   ├── ui/
│   │   └── top_panel/
│   │       └── my_character_icon.png       # 顶部面板图标
│   └── map/
│       └── my_map_marker.png               # 地图标记
└── localization/
    ├── eng/
    │   └── characters.json                # 英文本地化
    └── zhs/
        └── characters.json                # 中文本地化
```

---

## 13. 参考已有角色实现

| 角色 | 学习要点 |
|------|---------|
| `Ironclad` | 基础实现、5打击4防御、红色配色 |
| `Silent` | 毒系、绿色配色 |
| `Defect` | 球体系、BaseOrbSlotCount = 3 |
| `Necrobinder` | 灵魂机制、紫色配色 |
| `Regent` | 粉色配色、独特机制 |

源码位置: `D:\杀戮尖塔2Mod\st2代码\sts2\MegaCrit\sts2\Core\Models\Characters\`

---

## 14. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 角色不显示在角色选择 | 未注册或资源缺失 | 确认 `[RegisterCharacter]` 存在，检查场景和图标路径 |
| 角色视觉显示异常 | 场景结构不对 | 确保 `Visuals`/`Bounds`/`IntentPos`/`CenterPos` 有 `%` 标记 |
| 卡牌/遗物/药水不出现 | 池未关联或注册 | 确认 `ModCharacterTemplate` 类型参数正确 |
| 能量计数器不显示 | 场景路径错误或缺失 | 检查 `EnergyCounterPath` |
| 地图标记不显示 | `MapMarkerPath` 路径错误 | 检查地图图标路径 |
| 角色描述显示原始键名 | 本地化 JSON 缺少条目 | 检查 characters.json 中的键 |
| 角色无法选择 | 未设置 Epoch 或未解锁 | 确保 `[RequireEpoch]` 属性存在且 Epoch 已解锁 |
| Placeholder 回退无效 | `PlaceholderCharacterId` 设为 null | 不设置或设为 `"ironclad"` 等现有角色名 |

---

## 15. 编写审查清单

### 15.1 基础检查

- [ ] 是否继承了 `ModCharacterTemplate<TPool, TRelicPool, TPotionPool>`？
- [ ] 是否添加了 `[RegisterCharacter]` 属性？
- [ ] 是否重写了 `StartingHp`、`StartingGold`、`NameColor`、`Gender`？
- [ ] 是否定义了 `StartingDeck` 和 `StartingRelics`？

### 15.2 池检查

- [ ] 卡池是否已定义（`TypeListCardPoolModel`）？
- [ ] 遗物池和药水池是否已定义？
- [ ] 角色类的类型参数是否与池类型一致？

### 15.3 资源检查

- [ ] `CharacterAssetProfile` 中的场景路径是否正确？
- [ ] 战斗视觉场景是否包含 `Visuals`/`Bounds`/`IntentPos`/`CenterPos` 且都有 `%` 标记？
- [ ] 能量计数器场景是否创建？
- [ ] 顶部面板图标和地图标记 PNG 是否存在？

### 15.4 解锁检查

- [ ] 是否需要 Epoch 解锁系统？
- [ ] `[RequireEpoch]` 属性是否正确关联？
- [ ] 如果需要进度解锁，是否添加了 `[UnlockEpochAfter*]` 属性？

### 15.5 本地化检查

- [ ] `characters.json` 中是否添加了 `title`？
- [ ] 是否添加了 `titleObject` 和代词相关字段？
- [ ] 是否添加了 `description`（角色选择界面显示）？

### 15.6 注册检查

- [ ] `RegisterModAssembly` 在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
