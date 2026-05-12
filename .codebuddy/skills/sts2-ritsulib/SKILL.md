---
name: sts2-ritsulib
description: >-
  该 Skill 为使用 RitsuLib 框架 (v0.2.29+) 开发杀戮尖塔2 (Slay the Spire 2) Mod 提供全面指导。
  涵盖内容注册、卡牌/遗物/能力编写、角色创建、自定义事件、时间线/解锁系统、本地化、
  持久化、补丁系统、生命周期事件、生物视觉/动画、FMOD 音频、Mod 设置界面、诊断与 Shell 主题。
  当用户询问创建或修改依赖 STS2-RitsuLib 的杀戮尖塔2 Mod、编写使用 RitsuLib API 的 Mod 代码、
  或排查 RitsuLib 相关 Mod 问题时，使用此 Skill。
---

# RitsuLib - 杀戮尖塔2 Mod 开发框架 Skill

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。示例中使用 `MyMod` 作为通用占位符。

## 1. 概述

RitsuLib 是杀戮尖塔2的 Mod 开发框架，围绕以下核心设计目标构建：

- **显式注册**，而非不透明的自动发现
- **固定模型标识**，通过确定性 `ModelId.Entry`
- **可组合的资源配置**，而非大型继承层次
- **场景替换**，而非就地修改原版资源
- **窄范围兼容回退**，仅在基础游戏没有安全扩展点时使用

文档来源：https://sts2-ritsulib.ritsukage.com/guide/

特定主题的详细参考材料请查阅 `references/` 目录下的参考文件。

## 2. 项目设置

### 2.1 依赖声明

在 `mod_manifest.json` 中添加：

```json
{
  "id": "MyMod",
  "name": "My Mod",
  "dependencies": ["STS2-RitsuLib"]
}
```

### 2.2 Mod 初始化器

```csharp
using System.Reflection;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer(nameof(Initialize))]
public static class MyMod
{
    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        Logger = RitsuLibFramework.CreateLogger("MyMod");
        RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);

        var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
        patcher.RegisterPatches<MyModPatches>();
        patcher.PatchAll();

        RitsuLibFramework.CreateContentPack("MyMod")
            .Character<MyCharacter>()
            .Card<MyCardPool, MyCard>()
            .Relic<MyRelicPool, MyRelic>()
            .Apply();
    }
}
```

## 3. Model ID 规则

所有 RitsuLib 注册的模型使用固定 `ModelId.Entry`：

```
<MODID>_<CATEGORY>_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型 | 分类 | ModelId.Entry |
|---------|------|---------------|
| MyCard | card | MY_MOD_CARD_MY_CARD |
| MyRelic | relic | MY_MOD_RELIC_MY_RELIC |
| MyCharacter | character | MY_MOD_CHARACTER_MY_CHARACTER |

本地化键使用此固定条目格式：

```json
{
  "MY_MOD_CARD_MY_CARD.title": "打击",
  "MY_MOD_CARD_MY_CARD.description": "造成 {damage} 点伤害。"
}
```

## 4. 内容注册

### 4.1 流式构建器（推荐）

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Story<MyStory>()
    .Epoch<MyCharacterEpoch>()
    .RequireEpoch<MyCard, MyEpoch>()
    .Custom(ctx => { /* 任意注册逻辑 */ })
    .Apply();
```

### 4.2 Manifest 注册

```csharp
var contentEntries = new IContentRegistrationEntry[]
{
    new CharacterRegistrationEntry<MyCharacter>(),
    new CardRegistrationEntry<MyCardPool, MyCard>(),
    new EnchantmentRegistrationEntry<MyEnchantment>(),
    new PowerRegistrationEntry<MyPower>(),
};

RitsuLibFramework.CreateContentPack("MyMod")
    .Manifest(contentEntries, keywordEntries)
    .Apply();
```

### 4.3 注册时机

所有注册必须在早期启动期间内容注册冻结之前完成。延迟注册会抛出异常。冻结由 `ContentRegistrationClosedEvent` 信号指示。

### 4.4 内容类型注册矩阵

完整的注册矩阵覆盖所有内容类型（角色、幕、卡牌、遗物、药水、能力、球体、附魔、苦痛、成就、单例、共享池、事件、Ancient、怪物、占位符），详见 `references/content-registration-matrix.md`。

## 5. 卡牌系统

### 5.1 卡池定义

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

**不要**覆写 `CardTypes`（已标记 `[Obsolete]`），应使用内容包注册。

### 5.2 卡牌定义

```csharp
public class MyCard : ModCardTemplate(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    public override string Title => "Strike";
    public override string Description => "Deal {Damage} damage.";

    public override string? CustomPortraitPath => "res://MyMod/art/strike.png";

    // 统一资源配置（推荐）
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath      = "res://MyMod/art/my_card.png",
        FramePath         = "res://MyMod/art/frame.png",
        FrameMaterialPath = "res://MyMod/art/frame.material",
    };

    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        ctx.DealDamage(user, target, Damage);
    }
}
```

### 5.3 卡牌动态变量

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private static readonly DynamicVar _charges =
        ModCardVars.Int("charges", amount: 3)
            .WithSharedTooltip("my_mod_charges");

    public override DynamicVarSet CreateDynamicVars() =>
        new DynamicVarSet().Add(_charges);
}

// 运行时读取：
int charges = card.DynamicVars.GetIntOrDefault("charges");
decimal val = card.DynamicVars.GetValueOrDefault("charges");
bool active = card.DynamicVars.HasPositiveValue("charges");
```

提示绑定选项：`.WithSharedTooltip("key")`、`.WithTooltip(titleTable, titleKey, iconPath)`、`.WithTooltip(var => new HoverTip(...))`。

## 6. 角色系统

### 6.1 角色资源配置

```csharp
public override CharacterAssetProfile AssetProfile => new(
    Scenes: new(
        VisualsPath: "res://MyMod/scenes/character/my_character.tscn",
        EnergyCounterPath: "res://MyMod/ui/energy/my_energy_counter.tscn"),
    Ui: new(
        IconTexturePath: "res://MyMod/ui/top_panel/icon.png",
        MapMarkerPath: "res://MyMod/map/map_marker.png"),
    Audio: new(
        AttackSfx: "event:/sfx/characters/my_character/attack"));
```

### 6.2 占位角色回退

```csharp
public virtual string? PlaceholderCharacterId => "ironclad";
```

显式 `AssetProfile` 字段优先读取；缺失字段从占位角色填充。设为 `null` 可禁用回退。

### 6.3 资源配置辅助方法

`CharacterAssetProfiles.Ironclad()` / `.Silent()` / `.Defect()` / `.Regent()` / `.Necrobinder()`，`ContentAssetProfiles.Card(...)` / `.Relic(...)` / `.Power(...)` 等。

## 7. 自定义事件

### 7.1 普通事件

```csharp
public sealed class MyEvent : ModEventTemplate
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, Accept, InitialOptionKey("ACCEPT")),
            new EventOption(this, Leave, InitialOptionKey("LEAVE")),
        ];
    }

    private Task Accept()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
        return Task.CompletedTask;
    }
}
```

始终使用 `ModEventTemplate` / `ModAncientEventTemplate`（而非原始 `EventModel`）以避免选项键不匹配。

### 7.2 事件注册

```csharp
// 共享事件
.SharedEvent<MyEvent>()

// 指定幕的事件
.ActEvent<MyAct, MyEvent>()

// Ancient
.SharedAncient<MyAncient>()
.ActAncient<TAct, MyAncient>()
```

### 7.3 事件本地化键

格式：`<MODID>_EVENT_<TYPENAME>`，例如 `MY_MOD_EVENT_MY_EVENT`

```json
{
  "MY_MOD_EVENT_MY_EVENT.title": "奇异的泉水",
  "MY_MOD_EVENT_MY_EVENT.pages.INITIAL.description": "...",
  "MY_MOD_EVENT_MY_EVENT.pages.INITIAL.options.ACCEPT.title": "饮用"
}
```

## 8. 时间线与解锁

### 8.1 Story 和 Epoch 注册

```csharp
// Story：仅实现 StoryKey
public class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my-story";
}

// 注册：
RitsuLibFramework.CreateContentPack("MyMod")
    .Story<MyStory>()
    .Epoch<MyCharacterEpoch>()
    .RequireEpoch<MyLateCard, MyLateContentEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyCharacterEpoch>()
    .UnlockEpochAfterAscensionWin<MyCharacter, MyLateContentEpoch>(10)
    .Apply();
```

### 8.2 Epoch 模板

- `CharacterUnlockEpochTemplate<TCharacter>` - 解锁角色
- `CardUnlockEpochTemplate` - 解锁额外卡牌
- `RelicUnlockEpochTemplate` - 解锁额外遗物
- `PotionUnlockEpochTemplate` - 解锁额外药水

全部支持 `ExpansionEpochTypes` 用于链式解锁。

### 8.3 解锁规则

```csharp
.UnlockEpochAfterRunAs<TCharacter, TEpoch>()
.UnlockEpochAfterWinAs<TCharacter, TEpoch>()
.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)
.UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory)
.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)
.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)
```

### 8.4 RequireEpoch

跨 `UnlockState.Characters`、卡牌/遗物/药水池查询、共享 Ancient 列表和幕生成事件进行门控。不仅仅是 UI 过滤。

## 9. 本地化

### 9.1 双层体系

- **LocString**（游戏原生）：通过本地化表处理模型标题、描述（卡牌、遗物、能力、角色、card_keywords）
- **I18N**（RitsuLib）：Mod 辅助文本（设置、说明）

```csharp
var i18n = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-I18N",
    resourceFolders: ["MyMod.localization"],
    pckFolders: ["res://MyMod/localization"]);
```

来源合并顺序：文件系统（最高优先级）> 嵌入资源 > PCK 文件夹。

### 9.2 关键词

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");
keywords.RegisterCardKeywordOwnedByLocNamespace("brew", iconPath: "res://MyMod/art/kw.png");
```

运行时关键词附加：`card.AddModKeyword("brew")`、`card.HasModKeyword("brew")`

### 9.3 Ancient 对话

键格式：`<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient/.char`
可选后缀：`.r`（重复）、`.sfx`、`-visit`、`-attack`

## 10. 持久化

```csharp
public sealed class CounterData
{
    public int Value { get; set; }
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");
    store.Register<CounterData>(
        key: "counter",
        fileName: "counter.json",
        scope: SaveScope.Profile,
        defaultFactory: () => new CounterData(),
        autoCreateIfMissing: true);
}
```

- `SaveScope.Global`：跨所有档案共享（Mod 设置、缓存）
- `SaveScope.Profile`：每档案隔离（解锁、进度）

读取/写入：

```csharp
var counter = store.Get<CounterData>("counter");
store.Modify<CounterData>("counter", data => { data.Value += 1; });
store.Save("counter");
```

### 10.1 数据迁移

```csharp
store.Register<MyData>(
    key: "settings",
    fileName: "settings.json",
    scope: SaveScope.Global,
    defaultFactory: () => new MyData(),
    migrationConfig: new ModDataMigrationConfig(currentDataVersion: 2, minimumSupportedDataVersion: 1),
    migrations: [new SettingsV1ToV2Migration()]);
```

### 10.2 AttachedState / SavedAttachedState

`AttachedState<TKey, TValue>` 用于仅运行时的附加状态。`SavedAttachedState<TKey, TValue>` 用于参与原版存档序列化的模型上的持久化状态。

## 11. 补丁系统

### 11.1 标准工作流

```csharp
var patcher = RitsuLibFramework.CreatePatcher("MyMod", "core-patches");
patcher.RegisterPatches<MyPatchSet>();
if (!patcher.PatchAll())
    throw new InvalidOperationException("必需补丁失败。");
```

### 11.2 IPatchMethod

```csharp
public class ExamplePatch : IPatchMethod
{
    public static string PatchId => "example_patch";
    public static string Description => "方法运行时记录日志";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(SomeType), nameof(SomeType.SomeMethod))];
    }

    public static void Prefix() { /* Harmony 前缀 */ }
    // Postfix、Transpiler、Finalizer 通过名称发现
}
```

- `IsCritical = true`：失败会中止补丁器
- `IsCritical = false`：记录日志但补丁器仍可能成功
- `ModPatchTarget` 上的 `ignoreIfMissing: true`：目标缺失是预期行为

### 11.3 动态补丁

```csharp
var builder = new DynamicPatchBuilder("my_dynamic")
    .AddMethod(typeof(SomeType), "SomeMethod",
        postfix: DynamicPatchBuilder.FromMethod(typeof(MyRuntimePatch), nameof(MyRuntimePatch.Postfix)),
        isCritical: false);
patcher.ApplyDynamic(builder);
```

## 12. 生命周期事件

### 12.1 订阅方式

```csharp
// 按类型订阅（推荐）
var sub = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt => { /* ... */ });
sub.Dispose(); // 取消订阅

// 通过 ILifecycleObserver
public class MyObserver : ILifecycleObserver
{
    public void OnEvent(IFrameworkLifecycleEvent evt)
    {
        if (evt is CombatStartingEvent combat) HandleCombatStart(combat);
    }
}
RitsuLibFramework.SubscribeLifecycle(new MyObserver());
```

可重放事件（`IReplayableFrameworkLifecycleEvent`）：延迟订阅会立即触发回调。

### 12.2 关键事件

| 分类 | 事件 |
|------|------|
| 框架 | FrameworkInitializedEvent、ProfileServicesInitializedEvent |
| 引导 | ContentRegistrationClosedEvent、ModelIdsInitializedEvent、GameReadyEvent |
| 战局 | RunStartedEvent、RunEndedEvent、RunLoadedEvent |
| 房间/幕 | RoomEnteredEvent、RoomExitedEvent、ActEnteredEvent |
| 战斗 | CombatStartingEvent、CombatEndedEvent、CardDrawnEvent、CardPlayedEvent、SideTurnStartingEvent、CardsFlushedEvent |
| 生物 | CreatureDyingEvent、CreatureDiedEvent |
| 奖励 | GoldGainedEvent、RelicObtainedEvent、PotionProcuredEvent |
| 解锁 | EpochObtainedEvent、EpochRevealedEvent |
| 存档 | RunSavingEvent、RunSavedEvent、ProfileDataChangedEvent |

完整事件参考见 `references/lifecycle-events.md`。

## 13. Godot 场景编写

### 13.1 包装器模式

始终将 `.tscn` 场景绑定到 Mod 本地子类，而非直接绑定游戏类型：

```csharp
namespace MyMod.Scripts
{
    public partial class MyEnergyCounter : NEnergyCounter { }
}
```

常见需要包装器的类型：`NEnergyCounter`、`NRestSiteCharacter`、`NCreatureVisuals`、`NSelectionReticle`、`MegaLabel`。

### 13.2 运行时脚本注册

```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(
    Assembly.GetExecutingAssembly(), Logger);
```

在内容注册之前调用。

## 14. 生物视觉与动画

### 14.1 工厂接口

| 目的 | 接口 |
|------|------|
| 替换 CreateVisuals | `IModCreatureVisualsFactory` |
| 替换 Spine GenerateAnimator | `IModCreatureAnimatorFactory` |
| 非 Spine 状态机 | `IModNonSpineAnimationStateMachineFactory` |
| 商人/休息点状态机 | `IModCharacterMerchantAnimationStateMachineFactory` |

### 14.2 非 Spine 状态机

```csharp
protected override ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(
    Node visualsRoot, MonsterModel monster)
{
    var backend = new AnimatedSprite2DBackend(wolfVisuals.GetAnimatedSprite());
    return ModAnimStateMachineBuilder.Create()
        .AddState("idle", loop: true).AsInitial().Done()
        .AddState("attack").WithNext("idle").Done()
        .AddState("hurt").WithNext("idle").Done()
        .AddState("die").Done()
        .AddAnyState("Idle",   "idle")
        .AddAnyState("Attack", "attack")
        .AddAnyState("Hit",    "hurt")
        .AddAnyState("Dead",   "die")
        .Build(backend);
}
```

动画后端：`AnimatedSprite2DBackend`、`GodotAnimationPlayerBackend`、`CueAnimationBackend`、`SpineAnimationBackend`、`CompositeAnimationBackend`。

## 15. FMOD 音频

### 15.1 API 选择

| 需求 | 使用 |
|------|------|
| 与原版对齐的播放 | `GameFmod.Playback`、`GameFmod.Studio` |
| 与 SfxCmd 相同的保护逻辑 | `Sts2SfxAlignedFmod` |
| 加载/卸载 Bank | `FmodStudioServer` |
| 在 FmodServer 上直接播放一次性音效 | `FmodStudioDirectOneShots` |
| 总线/快照控制 | `FmodStudioBus`、`FmodStudioSnapshots` |

### 15.2 快速示例

```csharp
// 原版对齐的一次性音效
Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
GameFmod.Studio.PlayMusic("event:/music/menu_update");

// Mod Bank 加载
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.bank");
FmodStudioServer.TryWaitForAllLoads();
FmodStudioServer.TryLoadStudioGuidMappings("res://mods/MyMod/banks/MyMod.guids.txt");

// 流式音乐
var handle = GameFmod.Playback.PlayMusic(
    AudioSource.StreamingMusic(musicPath),
    new AudioPlaybackOptions { Volume = 0.7f, Scope = AudioLifecycleScope.Room });

// 限制快速触发
if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
```

## 16. Mod 设置界面

### 16.1 架构

1. 在 `ModDataStore` 中注册持久化模型
2. 仅对玩家可编辑字段创建绑定
3. 围绕绑定注册页面/分区
4. 所有可见文本进行本地化

### 16.2 示例

```csharp
// 数据
public sealed class MyModSettings
{
    public bool EnableFancyVfx { get; set; } = true;
    public double ScreenShakeScale { get; set; } = 1.0;
}

// 注册
using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    store.Register<MyModSettings>(key: "settings", fileName: "settings.json",
        scope: SaveScope.Global, defaultFactory: () => new MyModSettings());
}

// UI
var fancyVfx = ModSettingsBindings.Global<MyModSettings, bool>(
    "MyMod", "settings", m => m.EnableFancyVfx, (m, v) => m.EnableFancyVfx = v);

RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithModDisplayName(ModSettingsText.I18N(settingsLoc, "mod.display_name", "My Mod"))
    .AddSection("general", section => section
        .AddToggle("fancy_vfx", ModSettingsText.I18N(settingsLoc, "fancy_vfx.label", "Fancy VFX"),
            fancyVfx, ModSettingsText.I18N(settingsLoc, "fancy_vfx.desc", "Enable VFX."))));
```

支持的控件：`AddToggle`、`AddSlider`、`AddIntSlider`、`AddChoice`、`AddEnumChoice`、`AddColor`、`AddKeyBinding`、`AddImage`、`AddButton`、`AddSubpage`、`AddList`、`AddHeader`、`AddParagraph`。

## 17. 诊断与兼容性

- **资源路径诊断**：缺失路径记录一次性警告，回退到基础资源
- **调试兼容模式**：在 Mod 设置中切换，用于 LocTable 占位符、无效解锁 Epoch、THE_ARCHITECT 缺失对话
- **注册冲突**：Model ID、Epoch ID 和 Story ID 冲突会抛出错误
- **冻结错误**：冻结后延迟注册会抛出异常
- 设置路径（Windows）：`%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json`

## 18. Shell 主题 (DTFM)

设置界面使用 W3C 设计令牌格式模块 (DTFM) 主题系统。

令牌层级：`core`（原语）-> `semantic`（别名）-> `components`（组件令牌）。

作用域合并顺序：Mod 默认值 -> 继承链 -> 全局 -> shell -> modSettings -> mod:<id>。

```csharp
// 类型化访问
var t = RitsuShellTheme.Current;
t.Color.White;
t.Component.Toggle.On.Bg;
t.Metric.Radius.Default;

// 路径字符串访问
theme.GetColor("components.relicPicker.selected.bg");

// Mod 注册
RitsuShellThemeRuntime.RegisterModTokens("my_mod", defaults, onApply: ApplyTheme);
```

## 19. 常见错误规避

- 在冻结后注册内容
- 覆写已过时的 `CardTypes` 而非使用内容包注册
- 使用原始 `EventModel` 而非 `ModEventTemplate`
- 忘记为 Godot 场景脚本调用 `EnsureGodotScriptsRegistered`
- 将 `.tscn` 绑定到游戏类型而非 Mod 本地包装器
- 在 `ignoreIfMissing` 才是真实意图时使用 `IsCritical = false`
- 重命名已发布的 CLR 类型时没有迁移计划
- 注册 Epoch 时没有可解锁它的解锁规则
- 混合来自不同 FMOD Studio 构建的 Bank 和 GUID 文件
