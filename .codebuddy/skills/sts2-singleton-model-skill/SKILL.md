---
name: sts2-singleton-model-skill
description: >-
  该 Skill 用于在杀戮尖塔2 (Slay the Spire 2) Mod 中创建单例模型 (SingletonModel)。
  当请求的功能没有对应的专用 Skill（如卡牌/遗物/能力/怪物等），且需要添加全局性的新系统或中间件时，
  使用此 Skill 创建单例模型来承载该功能。
  涵盖单例定义 (SingletonModel / HookedSingletonModel)、
  注册方式 ([RegisterSingleton] / 手动注册)、
  生命周期钩子订阅 (combat hooks / run hooks)、
  规范实例与可变副本模式 (MutableClone)、
  以及完整的代码模板与审查清单。
  当用户需要添加一个不属于现有内容类型（卡牌/遗物/能力/药水/怪物/事件/附魔/充能球等）的全局功能时，
  或当需要编写一个轻量级中间件来连接多个游戏系统时，使用此 Skill。
auto_trigger: false
trigger_priority: 3
---

# STS2 单例模型 (SingletonModel) 编写 Skill (RitsuLib)

## 1. 概述

单例模型 (SingletonModel) 是杀戮尖塔2 中的一种特殊模型类型，继承自 `AbstractModel`，用作**标记类型**来标识那些不属于常规内容类别（卡牌/遗物/能力等），但又需要全局访问的游戏系统或中间件。

**何时使用单例模型**：
- 需要添加一个不属于现有 Skill（卡牌/遗物/能力/药水/怪物/Ancient/事件/附魔/充能球等）的全局功能
- 需要一个全局中间件在跑酷和战斗生命周期中监听事件
- 原版例子：`MultiplayerScalingModel`（多人模式格挡缩放）

**当前项目 ModId**: `PersonalMod`

---

## 2. 工作原理

### 2.1 单例模型不是"单例"

在 STS2 中，`SingletonModel` 是继承自 `AbstractModel` 的空抽象类，本身不添加任何成员。它更像一个标记，其工作方式如下：

1. **注册**: 和所有 `AbstractModel` 子类一样，在 `ModelDb.Init()` 时被统一实例化为**规范实例 (canonical instance)**
2. **访问**: 通过 `ModelDb.Singleton<T>()` 从全局字典中获取
3. **可变副本**: 规范实例是只读的，运行时通过 `.MutableClone()` 创建可变副本使用

```csharp
// 获取规范实例
var canonical = ModelDb.Singleton<MySingleton>();

// 创建可变副本使用
var mutable = canonical.MutableClone();
```

---

## 3. 基类

### 3.1 SingletonModel（原版基类）

继承链: `SingletonModel` → `AbstractModel`

命名空间: `MegaCrit.Sts2.Core.Models`

```csharp
public abstract class SingletonModel : AbstractModel { }
```

没有任何必须重写的成员。这是最基础的标记类。

### 3.2 HookedSingletonModel（RitsuLib 便利基类）

继承链: `HookedSingletonModel` → `SingletonModel` → `AbstractModel`

命名空间: `STS2RitsuLib.Models`

**推荐使用**，省去手动订阅 Hook 回调的样板代码：

```csharp
public class MySingleton : HookedSingletonModel
{
    public MySingleton() : base(receiveCombatHooks: true, receiveRunHooks: true)
    {
        // 自动订阅战斗和跑酷钩子
    }
}
```

构造函数参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `receiveCombatHooks` | `bool` | 是否自动订阅战斗钩子回调 |
| `receiveRunHooks` | `bool` | 是否自动订阅跑酷钩子回调 |

子模型方法：

| 方法 | 说明 |
|------|------|
| `RunSubModels(RunState)` | 返回在跑酷钩子中应接收回调的子模型列表 |
| `CombatSubModels(CombatState)` | 返回在战斗钩子中应接收回调的子模型列表 |

---

## 4. 注册方式

### 4.1 属性自动注册（推荐）

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterSingleton]
public class MySingleton : HookedSingletonModel
{
    public MySingleton() : base(true, false) { }
}
```

### 4.2 手动注册

```csharp
using STS2RitsuLib.Content;

// 在 Entry.Init() 中
ModContentRegistry.RegisterSingleton<MySingleton>();

// 或使用非泛型重载
ModContentRegistry.RegisterSingleton(typeof(MySingleton));
```

### 4.3 前提

确保在 `Entry.Init()` 中调用了：
```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly(), Logger);
ModTypeDiscoveryHub.RegisterModAssembly(Assembly.GetExecutingAssembly());
```

---

## 5. Model ID 规则

与所有 RitsuLib 注册的模型一样，ModelId.Entry 格式为：

```
<MODID>_SINGLETON_<TYPENAME>
```

所有段落标准化为 UPPER_SNAKE_CASE。示例：

| C# 类型名 | ModelId.Entry |
|-----------|---------------|
| `MySingleton` | `PERSONALMOD_SINGLETON_MY_SINGLETON` |
| `MultiplayerScalingModel` | `PERSONALMOD_SINGLETON_MULTIPLAYER_SCALING_MODEL` |

---

## 6. 生命周期与 Hook 回调

### 6.1 战斗钩子 (Combat Hooks)

当 `ShouldReceiveCombatHooks` 返回 `true` 时，单例模型可以 override 所有 `AbstractModel` 中的战斗相关 Hook 方法：

```csharp
public override async Task BeforeCombatStart() { }
public override async Task AfterCombatEnd(CombatRoom room) { }
public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player) { }
public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay) { }
public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side) { }
public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource) { }
public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay) { }
```

完整 Hook 列表参考 `sts2-core-ref` 的 `references/hooks-reference.md`。

### 6.2 跑酷钩子 (Run Hooks)

```csharp
public override async Task AfterRoomEntered(AbstractRoom room) { }
public override async Task AfterRoomExited(AbstractRoom room) { }
public override async Task AfterGoldGained(Player player) { }
public override async Task AfterRewardTaken(Player player, Reward reward) { }
```

### 6.3 HookedSingletonModel 自动订阅说明

使用 `HookedSingletonModel` 时注意事项：

- 它是一个**便利基类**，会自动处理 Hook 流订阅
- 构造函数参数控制具体订阅哪些流，而非 override `ShouldReceiveCombatHooks`
- 如果需要在同个单例中组合多个逻辑模块，通过 `RunSubModels`/`CombatSubModels` 返回子模型列表

---

## 7. 初始化模式

原版 `MultiplayerScalingModel` 展示了典型的单例初始化模式：

```csharp
// 在 RunState 中初始化
runState.MultiplayerScalingModel = ModelDb.Singleton<MultiplayerScalingModel>()
    .MutableClone();
runState.MultiplayerScalingModel.Initialize(runState);

// 进入战斗时
runState.MultiplayerScalingModel.OnCombatEntered(combatState);

// 战斗结束时
runState.MultiplayerScalingModel.OnCombatFinished();
```

**模式**：`ModelDb.Singleton<T>()` → `.MutableClone()` → 调用自定义初始化方法。

---

## 8. 完整代码模板

### 8.1 基础单例模型

```csharp
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace PersonalMod.PersonalModCode.Singletons;

[RegisterSingleton]
public class MySingleton : HookedSingletonModel
{
    private RunState? _runState;

    public MySingleton() : base(receiveCombatHooks: true, receiveRunHooks: false) { }

    public void Initialize(RunState runState)
    {
        _runState = runState;
    }

    // 监听玩家回合开始
    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player)
    {
        // 全局逻辑...
        await Task.CompletedTask;
    }
}
```

### 8.2 完整带初始化的单例

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace PersonalMod.PersonalModCode.Singletons;

/// <summary>
/// 全局计数器示例：记录玩家击杀的怪物数量并向其他系统广播。
/// 当需要跨战斗/房间追踪状态且不属于现有内容类型时使用。
/// </summary>
[RegisterSingleton]
public class KillCounterSingleton : HookedSingletonModel
{
    private RunState? _runState;
    private CombatState? _combatState;
    private int _killCount;

    public KillCounterSingleton() : base(receiveCombatHooks: true, receiveRunHooks: true) { }

    // 由外部在 RunState 初始化时调用
    public void Initialize(RunState runState)
    {
        _runState = runState;
        _killCount = 0;
    }

    // 设置战斗状态引用
    public void OnCombatEntered(CombatState combatState)
    {
        _combatState = combatState;
    }

    // 清除战斗状态引用
    public void OnCombatFinished()
    {
        _combatState = null;
    }

    // 战斗结束后统计击杀
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (_combatState == null) return;
        // 统计死亡生物
        // ...
        await Task.CompletedTask;
    }

    // 获得金币时的事件
    public override async Task AfterGoldGained(Player player)
    {
        if (_runState == null) return;
        // 全局金币事件逻辑
        await Task.CompletedTask;
    }

    public int KillCount => _killCount;
}
```

### 8.3 纯数据单例（无 Hook）

```csharp
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace PersonalMod.PersonalModCode.Singletons;

/// <summary>
/// 纯数据单例：用于存储全局配置数据。
/// 不需要监听任何游戏事件，仅作为全局数据持有者。
/// </summary>
[RegisterSingleton]
public class ConfigSingleton : SingletonModel
{
    public bool SomeGlobalFlag { get; set; } = true;
    public int SomeGlobalValue { get; set; } = 42;
}
```

### 8.4 带子模型的 HookedSingleton（组合模式）

```csharp
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace PersonalMod.PersonalModCode.Singletons;

// 子模块：战斗生命周期监听
public class CombatSubModule : AbstractModel
{
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeCombatStart()
    {
        // 战斗开始逻辑
        await Task.CompletedTask;
    }
}

// 子模块：跑酷生命周期监听
public class RunSubModule : AbstractModel
{
    public override bool ShouldReceiveRunHooks => true;

    public override async Task AfterGoldGained(Player player)
    {
        // 金币变化逻辑
        await Task.CompletedTask;
    }
}

// 主单例：组合多个子模块
[RegisterSingleton]
public class CompositeSingleton : HookedSingletonModel
{
    private readonly CombatSubModule _combatModule = new();
    private readonly RunSubModule _runModule = new();

    public CompositeSingleton() : base(false, false) { }

    public override IEnumerable<AbstractModel> CombatSubModels(CombatState state)
    {
        yield return _combatModule;
    }

    public override IEnumerable<AbstractModel> RunSubModels(RunState state)
    {
        yield return _runModule;
    }
}
```

---

## 9. 文件组织

```
PersonalMod/PersonalModCode/Singletons/
├── MySingleton.cs                     # 全局功能单例
├── KillCounterSingleton.cs            # 全局计数器
├── ConfigSingleton.cs                 # 纯数据单例
└── CompositeSingleton.cs              # 组合模式单例
```

---

## 10. 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 单例效果不触发 | `ShouldReceiveCombatHooks` 或 `ShouldReceiveRunHooks` 返回 `false` | 设置 `return true` 或 HookedSingletonModel 构造函数传 `true` |
| HookedSingletonModel 不触发无法 override | 试图 override `ShouldReceiveCombatHooks` | HookedSingletonModel 通过构造函数参数控制，不要 override |
| `ModelDb.Singleton<T>()` 找不到 | 未注册或 `[RegisterSingleton]` 缺失 | 检查注册属性，确认 ModAssembly 已加载 |
| 单例状态在战斗中不持久 | 未创建可变副本 | 使用 `.MutableClone()` 创建副本并保存引用 |
| 子模型 Hook 不触发 | 未在 `CombatSubModels`/`RunSubModels` 中返回 | 确保 override 了这两个方法并返回子模型 |

---

## 11. 编写审查清单

### 11.1 基础检查

- [ ] 是否继承了 `SingletonModel` 或 `HookedSingletonModel`？
- [ ] 是否添加了 `[RegisterSingleton]` 属性？
- [ ] 是否需要 Hook 回调？（选择正确的基类）

### 11.2 Hook 检查

- [ ] 使用 `HookedSingletonModel` 时，构造函数参数是否正确？
- [ ] 使用普通 `SingletonModel` 时，`ShouldReceiveCombatHooks`/`ShouldReceiveRunHooks` 是否正确 override？
- [ ] 是否使用了 `ModContentRegistry.RegisterSingleton<T>()` 手动注册（非属性注册时）？

### 11.3 初始化检查

- [ ] 是否需要自定义 `Initialize()` 方法？
- [ ] 是否需要处理战斗进入/退出事件？
- [ ] 是否通过 `ModelDb.Singleton<T>()` + `.MutableClone()` 获取可变副本？

### 11.4 注册检查

- [ ] `RegisterModAssembly` 在 `Entry.Init()` 中调用？
- [ ] `EnsureGodotScriptsRegistered` 在 `Entry.Init()` 中调用？

---

*最后更新：2026-05-12*
