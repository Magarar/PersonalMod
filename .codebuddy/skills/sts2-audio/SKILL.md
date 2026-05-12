---
name: sts2-audio
description: >-
  该 Skill 为杀戮尖塔2 (Slay the Spire 2) Mod 开发中的音频系统提供全面的参考与自动检查。
  涵盖代码中播放音效的多种方式（SfxCmd、GameAudioService、GameFmod、Sts2SfxAlignedFmod、FmodStudioStreamingFiles）、
  伤害/格挡/能力等效果音效的挂载（WithHitFx、FromCard/FromPotion/FromPower）、
  Bank 加载（FmodStudioDeferredBankRegistration）、FMOD Studio 工程操作指引、
  AudioPlaybackOptions 配置（Volume/Scope/Channel/Routing）、
  流式音乐播放（PlayMusic / FollowAdaptiveMusic / PlayLoop）、生命期管理、音频文件加载、
  以及完整的代码模板与审查清单。
  当需要为卡牌/遗物/能力/药水等添加音效、或在 Mod 中播放自定义音频时，自动触发此 Skill。
auto_trigger: true
trigger_priority: 1
---

# STS2 音频系统 Skill

## 1. 概述

杀戮尖塔2 使用 FMOD Studio 作为音频中间件。Mod 开发者有三种方式处理音频：

| 方式 | 说明 | 适用场景 |
|------|------|---------|
| **代码音效 API** | 使用 `SfxCmd`、`GameAudioService`、`WithHitFx` 等 API | 使用游戏内已有音效，无需 FMOD 编辑器；由 AI agent 自动处理 |
| **FMOD Bank** | 用 FMOD Studio 创建工程 → 导入音频 → 构建 bank → 代码加载 | 需要自定义音频资源（新角色、新怪物等） |
| **直接加载音频文件** | 直接加载 WAV/OGG/MP3 文件播放 | 快速原型，无需 FMOD 工程 |

**约定**: AI agent 在编写卡牌、遗物、能力、药水、怪物等代码时，应使用 `SfxCmd.Play()`、`.WithHitFx()` 等代码 API 自动为效果添加音效。涉及 FMOD Studio 工程操作时，应提示用户自行完成 FMOD 相关步骤。

**参考教程**: https://glitchedreme.github.io/SlayTheSpire2ModdingTutorials/docs/04-ritsulib/04-10-add-audio/

---

## 2. 一键式音效 API（代码中最常用）

### 2.1 SfxCmd.Play — 播放任意音效

最通用的音效播放方式。命名空间 `MegaCrit.Sts2.Core.Commands`。

```csharp
using MegaCrit.Sts2.Core.Commands;

// 播放游戏原版的音效
SfxCmd.Play("event:/sfx/block_gain");          // 获得格挡
SfxCmd.Play("event:/sfx/heal");                // 治疗
SfxCmd.Play("event:/sfx/buff");                // 获得正面效果
SfxCmd.Play("event:/sfx/debuff");              // 获得负面效果
SfxCmd.Play("event:/sfx/potion_drink");         // 喝药水
SfxCmd.Play("event:/sfx/relic_get");            // 获得遗物
SfxCmd.Play("event:/sfx/ui/click");             // UI 点击
SfxCmd.Play("event:/sfx/card_deal");            // 发牌
SfxCmd.Play("event:/sfx/card_select");           // 选牌
```

### 2.2 WithHitFx — 伤害/攻击音效

在伤害命令链中直接挂载音效：

```csharp
await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
    .FromCard(this)
    .WithHitFx(sfx: "event:/sfx/sword_slash")   // 攻击命中音效
    .Targeting(cardPlay.Target!)
    .Execute(choiceContext);
```

### 2.3 BlockCmd / CreatureCmd 中的音效

```csharp
// 格挡命令（自动播放 block_gain 音效）
await BlockCmd.GainBlock(amount)
    .FromCard(this)
    .Execute(choiceContext);

// 治疗命令（自动播放 heal 音效）
await CreatureCmd.Heal(Owner, amount)
    .Execute(choiceContext);
```

### 2.4 Orb 音效

充能球可以通过重写音效属性播放音效：

```csharp
protected override string PassiveSfx => "event:/sfx/characters/defect/defect_lightning_passive";
protected override string EvokeSfx => "event:/sfx/characters/defect/defect_lightning_evoke";
protected override string ChannelSfx => "event:/sfx/characters/defect/defect_lightning_channel";

// 在 Passive/Evoke 中调用
PlayPassiveSfx();   // 播放被动音效
PlayEvokeSfx();     // 播放激发音效
PlayChannelSfx();   // 播放引导音效
```

### 2.5 卡牌音效（在 Use 中播放）

```csharp
public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
{
    SfxCmd.Play("event:/sfx/sword_slash");
    ctx.DealDamage(user, target, Damage);
}
```

### 2.6 常用游戏音效路径速查

| 类别 | FMOD 路径 | 说明 |
|------|----------|------|
| 战斗 | `event:/sfx/block_gain` | 获得格挡 |
| 战斗 | `event:/sfx/heal` | 治疗 |
| 战斗 | `event:/sfx/buff` | 获取正面状态 |
| 战斗 | `event:/sfx/debuff` | 获取负面状态 |
| 战斗 | `event:/sfx/potion_drink` | 喝药水 |
| 战斗 | `event:/sfx/relic_get` | 获得遗物 |
| UI | `event:/sfx/ui/click` | 点击 |
| UI | `event:/sfx/card_deal` | 发牌 |
| UI | `event:/sfx/card_select` | 选中卡牌 |
| UI | `event:/sfx/ui/wipe_ironclad` | 铁甲战士转场 |
| 角色 | `event:/sfx/characters/defect/defect_lightning_passive` | 闪电球被动 |
| 角色 | `event:/sfx/characters/defect/defect_lightning_evoke` | 闪电球激发 |
| 角色 | `event:/sfx/characters/defect/defect_lightning_channel` | 闪电球引导 |
| 角色 | `event:/sfx/characters/defect/defect_frost_channel` | 冰霜球引导 |
| 角色 | `event:/sfx/characters/defect/defect_dark_channel` | 暗影球引导 |
| 角色 | `event:/sfx/characters/defect/defect_glass_channel` | 玻璃球引导 |
| 音乐 | `event:/music/act1_a1` / `act1_a2` | 第一幕音乐 |
| 音乐 | `event:/music/act2_a1` / `act2_a2` | 第二幕音乐 |
| 音乐 | `event:/music/act3_a1` / `act3_a2` | 第三幕音乐 |
| 音乐 | `event:/music/menu_update` | 菜单音乐 |
| 环境 | `event:/sfx/ambience/act1_neow` | 纽奥背景 |

---

## 3. GameAudioService（RitsuLib 新 API）

RitsuLib 提供了统一的音频服务 API。**新代码优先使用此 API**。

### 3.1 播放一次性音效

```csharp
using STS2RitsuLib.Audio;

GameAudioService.Shared.PlayOneShot(
    AudioSource.Event("event:/MyMod/ui/click"),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Parameters = FmodParameterMap.Set(("intensity", 1f)),
        Scope = AudioLifecycleScope.Room,
    });
```

### 3.2 AudioSource 类型

| 创建方式 | 说明 |
|---------|------|
| `AudioSource.Event("event:/path")` | 从 FMOD 事件路径创建 |
| `AudioSource.Guid(Guid.Parse("..."))` | 从 GUID 创建 |
| `AudioSource.SoundFile("res://path.wav")` | 从音频文件创建 |
| `AudioSource.StreamingMusic("res://path.ogg")` | 从流式音乐文件创建 |

### 3.3 AudioPlaybackOptions

| 选项 | 类型 | 说明 |
|------|------|------|
| `Volume` | `float` | 音量（0.0 ~ 1.0） |
| `Parameters` | `FmodParameterMap` | FMOD 参数映射 |
| `Scope` | `AudioLifecycleScope` | 生命周期作用域 |
| `Routing` | `AudioRoutingOptions` | 路由配置 |
| `CooldownMs` | `int` | 防高频触发冷却（毫秒） |
| `UseVanillaRouting` | `bool` | 是否走原版路由路径 |

### 3.4 AudioLifecycleScope

| 值 | 说明 |
|----|------|
| `Manual` | 手动管理生命周期 |
| `Room` | 随房间生命周期自动停止 |
| `Combat` | 随战斗生命周期自动停止 |
| `Run` | 随跑酷生命周期自动停止 |

### 3.5 AudioRoutingOptions

```csharp
new AudioRoutingOptions
{
    Channel = "my_mod_ambience",           // 按 channel 分组，同 channel 替换之前的
    Tag = "ambience",                      // 按 tag 分组，可用 StopTag 停止
};
```

---

## 4. 游戏内置音频 API（旧但稳定）

### 4.1 GameFmod

```csharp
using MegaCrit.Sts2.Core.Audio;

// 播放一次性音效
GameFmod.Playback.PlayOneShot("event:/sfx/heal");

// 播放循环音乐
GameFmod.Playback.PlayMusic("event:/music/menu_update");

// Studio 操作
GameFmod.Studio.PlayMusic("event:/music/act3_a1_v1");
```

### 4.2 Sts2SfxAlignedFmod（推荐的旧 API）

与 `SfxCmd` 使用相同保护逻辑（如防静音、防重叠）：

```csharp
using STS2RitsuLib.Audio;

Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
```

### 4.3 FmodStudioDirectOneShots（绕过保护逻辑）

当需要不经过任何保护逻辑直接播放音效时使用：

```csharp
using STS2RitsuLib.Audio;

FmodStudioDirectOneShots.PlayOneShot("event:/sfx/heal");
```

---

## 5. 音频文件直接加载

### 5.1 准备工作

FMOD 只能加载**未被 Godot 处理过**的音频文件。有三种方式确保文件不被 Godot 导入过程改变：

**方法 1**（推荐）：安装 FMOD Godot 插件 `6.1.0-4.5.0`，在项目设置中启用。

**方法 2**：禁用对音频文件的导入。
- 在 Godot 文件系统 dock 中选中音频文件
- 在导入面板中勾选 **"导入" 旁边的 "保留数据"**
- 或点击 "预设..." → "保持文件原样"

**方法 3**：把音频文件放在 Mod 项目目录之外（与 Godot 项目同级目录），避免 Godot 处理。

### 5.2 预加载音频

```csharp
using STS2RitsuLib.Audio;

// 在 Entry.Init() 中预加载
FmodStudioStreamingFiles.TryPreloadAsSound("res://PersonalMod/audios/waveform.ogg");
```

### 5.3 播放音频文件

```csharp
// 播放一次
FmodStudioStreamingFiles.TryPlaySoundFile("res://PersonalMod/audios/waveform.ogg");
```

### 5.4 使用 GameAudioService 播放文件

```csharp
GameAudioService.Shared.PlayOneShot(
    AudioSource.SoundFile("res://PersonalMod/audios/effect.wav"),
    new AudioPlaybackOptions { Volume = 0.7f });

// 流式音乐
GameAudioService.Shared.PlayMusic(
    AudioSource.StreamingMusic("res://PersonalMod/audios/bgm.ogg"),
    new AudioPlaybackOptions { Volume = 0.8f, Scope = AudioLifecycleScope.Room });
```

---

## 6. 循环与音乐

### 6.1 PlayLoop — 循环播放

```csharp
var loop = GameAudioService.Shared.PlayLoop(
    AudioSource.Event("event:/MyMod/ambience/engine"),
    new AudioPlaybackOptions
    {
        Routing = new AudioRoutingOptions(Channel: "my_mod_ambience"),
        Scope = AudioLifecycleScope.Run,
    });

// 调整参数
loop?.TrySetParameter("danger", 0.5f);

// 停止
loop?.TryStop();
```

### 6.2 PlayMusic — 音乐播放

```csharp
var music = GameAudioService.Shared.PlayMusic(
    AudioSource.StreamingMusic("res://PersonalMod/audios/boss_battle.ogg"),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Scope = AudioLifecycleScope.Combat,
    });
```

### 6.3 FollowAdaptiveMusic — 自适应音乐

根据房间/战斗/胜利状态自动切换音乐：

```csharp
GameAudioService.Shared.FollowAdaptiveMusic(
    AudioSource.Event("event:/MyMod/music/combat"),
    AudioSource.Event("event:/MyMod/music/victory"));
```

### 6.4 FmodPlaybackThrottle — 防高频触发

避免同一音效在短时间内重复播放（如力量触发多次伤害时）：

```csharp
using STS2RitsuLib.Audio;

if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
{
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
}
```

---

## 7. FMOD Bank 加载（自定义音频资源）

> **AI agent 说明**：FMOD Studio 工程操作（创建工程、导入音频、构建 bank）需要用户手动完成。agent 负责提供明确的指引步骤和代码加载部分。

### 7.1 代码加载 Bank

```csharp
using STS2RitsuLib.Audio;

// 在 Entry.Init() 中加载
FmodStudioDeferredBankRegistration.RegisterBank("res://PersonalMod/audios/PersonalMod.bank");
FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings("res://PersonalMod/audios/GUIDs.txt");
```

### 7.2 可选 Bank 的加载

```csharp
using STS2RitsuLib.Audio;

if (!FmodStudioServer.TryLoadBank("res://PersonalMod/audios/optional.bank"))
{
    // 处理加载失败（如使用回退音效）
}
FmodStudioServer.TryWaitForAllLoads();
```

### 7.3 FMOD Studio 工程操作指引（供用户参考）

#### 7.3.1 下载与安装

1. 前往 [FMOD 官网](https://www.fmod.com/download#fmodstudio) 下载 **FMOD Studio 2.03.06**
2. 安装并打开 FMOD Studio

#### 7.3.2 获取参考工程

下载 RitsuLib 作者提供的音频示例工程：
- GitHub: https://github.com/BAKAOLC/STS2_FModProject_Minimal
- 网盘: https://pan.baidu.com/s/1yuxPkDpCV8EVLkDubqiirg?pwd=apar

#### 7.3.3 步骤

1. **导入音频**：左侧 Assets 栏 → 右键 Import Assets 或拖入音频文件
2. **重命名 Bank**：中间 Banks 栏 → 重命名为你的项目名（如 `PersonalMod`）
3. **创建 Event**：Events 栏 → 右键新建文件夹 → 右键新建 Event → Assign To Bank 选择你的 Bank
4. **配置 Routing**：Window → Mixer Routing → 创建 `master/sfx` 或 `master/music` routing
5. **创建 Sheet**：在 Event 中右键 → 新建 Timeline Sheet → 将音频素材拖入轨道
6. **构建导出**：
   - File → Build（构建 Bank）
   - File → Export GUIDs（导出 GUID 映射）
7. **复制文件**：从构建输出目录复制 `Build/Desktop/PersonalMod.bank` 和 `GUIDs.txt` 到 Mod 项目中
8. **导出设置**：确保 Godot 导出设置中包含 `.bank` 和 `.txt` 文件

#### 7.3.4 音频总线布局

| 音频类型 | 总线路径 | 受游戏哪项音量设置影响 |
|---------|---------|---------------------|
| 音效 | `master/sfx` | 音效音量 |
| 音乐 | `master/music` | 音乐音量 |

---

## 8. 效果音效挂载速查

### 8.1 卡牌音效

```csharp
// 方式 1: 在 Use() 中
SfxCmd.Play("event:/sfx/sword_slash");

// 方式 2: 在伤害链中
await DamageCmd.Attack(amount)
    .FromCard(this)
    .WithHitFx(sfx: "event:/sfx/sword_slash")
    .Targeting(target)
    .Execute(choiceContext);
```

### 8.2 遗物音效

```csharp
// 在 Hook 回调中
public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
{
    SfxCmd.Play("event:/sfx/buff");
    await CardPileCmd.Draw(ctx, 1, player);
}
```

### 8.3 能力音效

```csharp
// 在 Hook/Modify 方法中
public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
{
    SfxCmd.Play("event:/sfx/buff");
    // 其他逻辑
}
```

### 8.4 药水音效

```csharp
// 在 OnUse 中
protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
{
    SfxCmd.Play("event:/sfx/potion_drink");
    // 药水效果逻辑
}
```

---

## 9. 人物音频配置

```csharp
public override CharacterAssetProfile AssetProfile => new(
    Audio: new(
        AttackSfx: "event:/sfx/characters/my_character/attack",
        CastSfx: "event:/sfx/characters/my_character/cast",
        DeathSfx: "event:/sfx/characters/my_character/death",
        CharacterSelectSfx: "event:/sfx/characters/my_character/select",
        CharacterTransitionSfx: "event:/sfx/ui/wipe_ironclad"
    )
);
```

---

## 10. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 音效不播放 | FMOD 事件路径错误或 Bank 未加载 | 检查事件路径是否以 `event:/` 开头，确认 Bank 已加载 |
| 音频文件不播放 | Godot 修改了音频文件 | 使用方法 1/2/3 确保文件不被 Godot 处理 |
| Bank 加载失败 | 文件不存在或路径错误 | 检查 `res://` 路径是否正确，确认导出设置包含 `.bank` |
| GUID 加载失败 | GUIDs.txt 格式错误或路径错误 | 检查 GUID 文件是否从 FMOD Export GUIDs 生成 |
| 音频无音量 | 未配置正确的总线 Routing | 确认 FMOD 工程中 Event 的 Routing 是 `master/sfx` 或 `master/music` |
| 音效重叠 | 高频触发同一音效 | 使用 `FmodPlaybackThrottle.TryEnter("key", 120)` 限制频率 |
| 音乐不切换 | 未设置正确的 Scope | 使用 `AudioLifecycleScope.Room` / `Combat` / `Run` 自动管理生命周期 |
| 音频打断 | 同 Channel 的替换逻辑 | 使用不同的 `Routing.Channel` 名称避免冲突 |

---

## 11. 审查清单

### 11.1 代码音效检查

- [ ] 是否使用了 `SfxCmd.Play()` 或 `GameAudioService` 播放音效？
- [ ] 伤害命令是否使用了 `.WithHitFx(sfx: ...)`？
- [ ] FMOD 事件路径是否以 `event:/` 开头？
- [ ] 是否验证了路径拼写正确（与游戏原版习惯一致）？

### 11.2 Bank 加载检查（用户操作）

- [ ] FMOD Studio 工程中 Routing 是否正确配置？
- [ ] Bank 是否已构建并导出？
- [ ] GUID 映射是否已导出？
- [ ] `.bank` 和 `GUIDs.txt` 是否在 Godot 导出文件列表中？
- [ ] `FmodStudioDeferredBankRegistration` 是否在 `Entry.Init()` 中调用？

### 11.3 音频文件检查

- [ ] 音频文件是否避开了 Godot 导入处理？
- [ ] 是否需要预加载（`TryPreloadAsSound`）？
- [ ] 文件路径是否正确？

---

*最后更新：2026-05-12*
