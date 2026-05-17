---
name: harmony
description: 0Harmony (HarmonyLib) 运行时方法补丁库编码规范，包含语法审查、自我检测和最佳实践
auto_trigger: true
trigger_priority: 2
---

# 0Harmony (HarmonyLib) 编码规范与审查规则

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

## 一、基础约定

### 1.1 命名空间与引用

```csharp
using HarmonyLib;  // 0Harmony 的 C# 命名空间
```

- 命名空间为 `HarmonyLib`（对应程序集 `0Harmony.dll`）
- 在杀戮尖塔2 Mod 中通过 `MainFile` 的 `Initialize()` 入口配合 `ModInitializer` 属性使用
- `new Harmony("mod.id")` 创建实例，ID 应使用与 `ModId` 一致的字符串

### 1.2 Harmony 实例管理

```csharp
// ✅ 推荐：每个 Mod 一个 Harmony 实例，缓存在静态字段
private static readonly Harmony HarmonyInstance = new("{{MODID}}");

// ✅ 推荐：在入口方法中统一 Patch
public static void Initialize()
{
    HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
}

// ❌ 禁止：多次创建 Harmony 实例
public void SomeMethod()
{
    var harmony = new Harmony("{{MODID}}");  // 不要重复创建
}
```

---

## 二、Patch 类规范

### 2.1 基本结构

```csharp
// ✅ 正确：使用 [HarmonyPatch] 属性 + nameof() 指定目标
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class TargetClass_TargetMethod_Patch
{
    static void Postfix(/* ... */)
    {
        // patch 逻辑
    }
}

// ✅ 正确：方法名使用 原始类_方法_Patch 格式
[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeDamage))]
public static class PlayerController_TakeDamage_Patch
{
    // ...
}

// ❌ 错误：使用硬编码字符串
[HarmonyPatch(typeof(PlayerController), "TakeDamage")]  // 重构不安全

// ❌ 错误：类名不含上下文
public static class MyPatch { }  // 难以辨识 patch 目标
```

### 2.2 命名对照表

| 元素 | 命名规则 | 示例 |
|------|---------|------|
| Harmony 实例 ID | 与 ModId 一致 | `"{{MODID}}"` |
| Patch 类名 | `TargetClass_MethodName_Patch` | `PlayerController_TakeDamage_Patch` |
| Prefix 方法 | `Prefix` 或 `[HarmonyPrefix]` + 自定义名 | `Prefix` |
| Postfix 方法 | `Postfix` 或 `[HarmonyPostfix]` + 自定义名 | `Postfix` |
| Transpiler 方法 | `Transpiler` 或 `[HarmonyTranspiler]` + 自定义名 | `Transpiler` |
| Finalizer 方法 | `Finalizer` 或 `[HarmonyFinalizer]` + 自定义名 | `Finalizer` |
| 静态缓存字段 | `_camelCase` | `_targetMethodInfo` |

---

## 三、Patch 方法类型详解

### 3.1 Prefix（前缀补丁）

在原始方法执行前运行。可以修改参数、设置返回值、跳过原始方法。

```csharp
[HarmonyPatch(typeof(Enemy), nameof(Enemy.TakeDamage))]
public static class Enemy_TakeDamage_Patch
{
    /// <summary>
    /// 读取和修改参数：直接使用参数名，需要修改时加 ref
    /// </summary>
    static void Prefix(int amount, ref int criticalMultiplier)
    {
        criticalMultiplier = 2;  // 用 ref 修改参数
    }

    /// <summary>
    /// 跳过原始方法：返回 false 跳过原始，返回 true 正常执行
    /// </summary>
    static bool Prefix(ref int __result)
    {
        __result = 0;  // 设置返回值
        return false;  // 跳过原始方法
    }

    /// <summary>
    /// 在 Prefix 和 Postfix 之间传递状态
    /// </summary>
    static void Prefix(out Stopwatch __state)
    {
        __state = Stopwatch.StartNew();
    }
}
```

**Prefix 注入参数规则：**

| 参数 | 说明 | 必须条件 |
|------|------|---------|
| `ref T __result` | 设置原始方法的返回值 | 原始方法返回值类型为 T |
| `bool` 返回值 | `false` 跳过原始方法 | 需要跳过原始时 |
| `out T __state` | 向 Postfix 传递状态 | 需要 Prefix↔Postfix 通信 |
| 原始参数名 | 按名称匹配读取参数 | 参数名必须与原始方法完全一致 |
| `ref 原始参数名` | 按名称匹配修改参数 | 参数名必须与原始方法完全一致 |
| `object __instance` | 实例方法的 this 引用 | 原始方法是实例方法 |

### 3.2 Postfix（后缀补丁）

在原始方法执行后运行。**始终执行**，不受 Prefix 跳过影响。

```csharp
[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.CalculateDamage))]
public static class PlayerController_CalculateDamage_Patch
{
    /// <summary>
    /// 读取/修改返回值
    /// </summary>
    static void Postfix(ref int __result)
    {
        __result = Mathf.Max(__result, 1);  // 保底至少 1 点伤害
    }

    /// <summary>
    /// 读取 Prefix 传递的状态
    /// </summary>
    static void Postfix(Stopwatch __state)
    {
        __state.Stop();
        Logger.Log($"Calculation took {__state.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Pass-through Postfix：适用于 IEnumerable 等无法 ref 的类型
    /// </summary>
    static IEnumerable<int> Postfix(IEnumerable<int> values)
    {
        foreach (var value in values)
            yield return value * 2;
    }
}
```

### 3.3 Transpiler（IL 转译器）

修改原始方法的 IL 指令码。仅在补丁时执行一次，不是运行时调用。

```csharp
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ResolveAttack))]
public static class CombatManager_ResolveAttack_Patch
{
    // 缓存需要匹配的字段和方法信息
    private static readonly FieldInfo F_damageMultiplier =
        AccessTools.Field(typeof(CombatManager), nameof(CombatManager.damageMultiplier));

    private static readonly MethodInfo M_ApplyCritBonus =
        SymbolExtensions.GetMethodInfo(() => ApplyCritBonus(default));

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            yield return instruction;

            // 在特定字段存储之后插入自定义方法调用
            if (instruction.StoresField(F_damageMultiplier))
            {
                yield return new CodeInstruction(OpCodes.Call, M_ApplyCritBonus);
            }
        }
    }

    private static void ApplyCritBonus(CombatManager instance)
    {
        instance.damageMultiplier *= 1.5f;
    }
}
```

### 3.4 Finalizer（终结器）

异常处理补丁。包裹原始方法和所有其他补丁，始终执行。

```csharp
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveGame))]
public static class SaveManager_SaveGame_Patch
{
    /// <summary>
    /// 抑制异常：返回 null 吞掉异常
    /// </summary>
    static Exception Finalizer(Exception __exception)
    {
        return null;  // 不抛出异常
    }

    /// <summary>
    /// 包装异常：将异常替换为自定义异常
    /// </summary>
    static Exception Finalizer(Exception __exception)
    {
        if (__exception is not null)
        {
            FileLog.Log($"Save failed: {__exception}");
            return new CustomSaveException("Save failed", __exception);
        }
        return null;
    }

    /// <summary>
    /// 清理资源：无论成功失败都执行
    /// </summary>
    static Exception Finalizer(Exception __exception)
    {
        SaveManager.sharedStream?.Flush();
        return __exception;  // 透传原始异常
    }
}
```

### 3.5 Reverse Patch（反向补丁）

将原始方法的代码复制到你的 stub 方法中，便于直接调用私有方法。

```csharp
[HarmonyPatch]
public static class EnemyUtils
{
    /// <summary>
    /// stub 方法签名必须与原始方法匹配（实例方法额外接收 __instance 作为首个参数）
    /// </summary>
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Enemy), "CalculateWeakness")]
    public static int CalculateWeaknessStub(object __instance, int baseDamage, Element element)
    {
        // stub 方法体不会执行，仅用于定义签名
        throw new NotImplementedException();
    }
}
```

---

## 四、注解属性完整参考

### 4.1 目标定位属性

```csharp
// 基础属性（可组合使用）
[HarmonyPatch]                                                    // 标记为 Patch 类
[HarmonyPatch(Type declaringType)]                               // 指定目标类
[HarmonyPatch(string methodName)]                                // 指定方法名
[HarmonyPatch(string methodName, params Type[] argumentTypes)]   // 方法名 + 参数类型（重载）
[HarmonyPatch(MethodType methodType)]                            // 方法类型
[HarmonyPatch(Type[] argumentTypes)]                             // 参数类型
[HarmonyPatch(Type[] argumentTypes, ArgumentType[] variations)]  // 参数类型 + ref/out/pointer

// 组合属性
[HarmonyPatch(Type, string)]                                     // 类 + 方法名
[HarmonyPatch(Type, string, params Type[])]                      // 类 + 方法名 + 参数类型
[HarmonyPatch(Type, MethodType)]                                 // 类 + 方法类型
[HarmonyPatch(Type, MethodType, params Type[])]                  // 类 + 方法类型 + 参数类型

// 方法类型枚举
MethodType.Method       // 普通方法
MethodType.Getter       // 属性 Getter
MethodType.Setter       // 属性 Setter
MethodType.Constructor  // 构造函数
```

### 4.2 补丁方法属性

```csharp
[HarmonyPrefix]       // 标记为 Prefix 方法
[HarmonyPostfix]      // 标记为 Postfix 方法
[HarmonyTranspiler]   // 标记为 Transpiler 方法
[HarmonyFinalizer]    // 标记为 Finalizer 方法
[HarmonyReversePatch] // 标记为 Reverse Patch（stub 方法）
[HarmonyPrepare]      // 在补丁前运行的验证方法
[HarmonyCleanup]      // 补丁卸载时的清理方法
```

### 4.3 优先级与排序属性

```csharp
// 数值越小越先执行，默认为 Priority.Normal (400)
[HarmonyPriority(int priority)]

// 常用优先级常量
Priority.First      // -1000
Priority.VeryHigh   // -100
Priority.High       // 0
Priority.Normal     // 400
Priority.Low        // 600
Priority.VeryLow    // 800
Priority.Last       // 1000

// 指定在其他 Mod 之前/之后执行
[HarmonyBefore(params string[] harmonyIDs)]
[HarmonyAfter(params string[] harmonyIDs)]

// 示例
[HarmonyPriority(Priority.First)]
[HarmonyAfter(["com.othermod.attackmod", "com.othermod.defensemod"])]
static void Postfix(ref int __result) { }

// 分类补丁
[HarmonyPatchCategory(string category)]  // 配合 harmony.PatchCategory() 使用
```

### 4.4 多目标补丁

```csharp
[HarmonyPatch]
public static class MultiTargetPatch
{
    // 动态指定多个目标方法
    static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetTypesFromAssembly(assembly)
            .SelectMany(type => type.GetMethods())
            .Where(m => m.Name.StartsWith("Apply"))
            .Cast<MethodBase>();
    }

    static void Prefix(object[] __args, MethodBase __originalMethod)
    {
        Logger.Log($"Patching: {__originalMethod.Name}");
    }
}
```

---

## 五、语法检测规则（自动审查项）

### 5.1 编译级检查

| 检查项 | 规则 | 严重级别 |
|--------|------|---------|
| 缺少 `[HarmonyPatch]` | Patch 类必须有至少一个 `[HarmonyPatch]` 属性 | 🔴 必须修复 |
| Patch 方法非静态 | Prefix/Postfix/Transpiler/Finalizer 必须为 `static` | 🔴 必须修复 |
| Transpiler 返回类型错误 | 必须返回 `IEnumerable<CodeInstruction>` | 🔴 必须修复 |
| Prefix 返回类型错误 | 跳过原始时必须返回 `bool` | 🔴 必须修复 |
| Finalizer 异常处理 | 修改/抑制异常时返回类型必须为 `Exception` | 🔴 必须修复 |
| Reverse Patch stub 抛异常 | stub 方法体必须 `throw new NotImplementedException()` | 🟡 建议修复 |
| 缺少 `using HarmonyLib` | 使用 Harmony 功能必须有命名空间引用 | 🔴 必须修复 |

### 5.2 命名检查

| 检查项 | 规则 | 严重级别 |
|--------|------|---------|
| Patch 类名无上下文 | 类名应包含 `TargetClass_MethodName_Patch` 格式 | 🟡 建议修复 |
| 使用硬编码字符串 | 方法名应使用 `nameof()` 代替字符串字面量 | 🟡 建议修复 |
| Harmony ID 不一致 | `new Harmony(id)` 的 ID 应与 ModId 一致 | 🟡 建议修复 |

### 5.3 API 使用检查

| 检查项 | 规则 | 严重级别 |
|--------|------|---------|
| `ref __result` 类型不匹配 | `__result` 类型必须与原始方法返回类型兼容 | 🔴 必须修复 |
| 参数名拼写错误 | 注入参数名必须与原始方法参数完全一致 | 🔴 必须修复 |
| `__state` 未配对 | Prefix 的 `out __state` 必须有对应的 Postfix 使用 | 🟡 建议修复 |
| Transpiler 缺少缓存 | `FieldInfo`/`MethodInfo` 必须缓存为静态只读字段 | 🟡 建议修复 |
| Transpiler 未匹配到目标 | 应添加找不到目标指令时的错误报告 | 🟡 建议修复 |
| Prefix 优先用 skip | 不必要时应避免 `return false`，优先用 Postfix | 🟡 建议修复 |
| 未处理 Harmony 版本兼容 | 应考虑其他 Mod 可能也 patch 同一方法 | 🟡 建议修复 |

### 5.4 性能检查

| 检查项 | 规则 | 严重级别 |
|--------|------|---------|
| Prefix/Postfix 中分配对象 | 避免在每帧调用的 patch 中 new 对象 | 🔴 必须修复 |
| Patch 方法中反射调用 | 应缓存反射结果，不应在 patch 方法中重复反射 | 🔴 必须修复 |
| Transpiler 使用 `yield return` | 推荐 `yield return` 模式而非 `ToList()` 以减少内存 | 🟢 可选优化 |
| 多次创建 Harmony 实例 | 应只创建一次并缓存 | 🔴 必须修复 |

---

## 六、特殊注入参数完整参考

### 6.1 前缀双下划线参数

| 参数名 | 类型 | 可用于 | 说明 |
|--------|------|--------|------|
| `__instance` | 原始类的类型 | Prefix, Postfix, Finalizer | 实例方法的 `this` 引用 |
| `__result` | 原始返回类型 | Prefix, Postfix | 原始方法的返回值（需 `ref`） |
| `__state` | 任意类型 | Prefix → Postfix | Prefix↔Postfix 状态传递（Prefix 用 `out`，Postfix 用普通读取） |
| `__originalMethod` | `MethodBase` | Prefix, Postfix, Finalizer | 被补丁的原始方法信息 |
| `__runOriginal` | `ref bool` | Prefix | 可强制运行/跳过原始方法 |
| `__0`, `__1`, ... | 对应参数类型 | Prefix, Postfix | 按位置访问原始参数（类型必须匹配） |

### 6.2 注入规则

```csharp
[HarmonyPatch(typeof(Target), nameof(Target.Method))]
public static class Patch
{
    // ✅ 正确：按名称匹配原始参数
    static void Prefix(int health, ref string name) { }

    // ✅ 正确：使用 __instance 访问实例
    static void Prefix(PlayerController __instance) { }

    // ✅ 正确：使用 __result 控制返回值
    static void Postfix(ref float __result) { }

    // ✅ 正确：Prefix 中 out __state，Postfix 中读取
    static void Prefix(out bool __state) { __state = true; }
    static void Postfix(bool __state) { }

    // ✅ 正确：按位置注入参数（当参数名不确定时）
    static void Prefix(int __0, string __1) { }

    // ❌ 错误：__result 类型不匹配
    static void Postfix(ref int __result)  // 原始方法返回 string

    // ❌ 错误：参数名拼写不一致
    static void Prefix(int helth)  // 应为 health
}
```

---

## 七、代码模板

### 7.1 标准 Prefix + Postfix 模板

```csharp
using HarmonyLib;

namespace {{MODID}}.{{MODID}}Code.Patches;

/// <summary>
/// 补丁 <see cref="TargetClass.TargetMethod"/> 以实现 XXX 功能。
/// </summary>
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
public static class TargetClass_TargetMethod_Patch
{
    static void Prefix(int inputParam, ref float modifier)
    {
        modifier *= 1.5f;
    }

    static void Postfix(ref int __result)
    {
        __result = Mathf.Max(__result, 1);
    }
}
```

### 7.2 带 __state 传递的模板

```csharp
using System.Diagnostics;
using HarmonyLib;

namespace {{MODID}}.{{MODID}}Code.Patches;

[HarmonyPatch(typeof(BattleManager), nameof(BattleManager.ProcessTurn))]
public static class BattleManager_ProcessTurn_Patch
{
    static void Prefix(out Stopwatch __state)
    {
        __state = Stopwatch.StartNew();
    }

    static void Postfix(Stopwatch __state)
    {
        __state.Stop();
        MainFile.Logger.Log($"Turn processed in {__state.ElapsedMilliseconds}ms");
    }
}
```

### 7.3 Transpiler 模板

```csharp
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace {{MODID}}.{{MODID}}Code.Patches;

[HarmonyPatch(typeof(CombatResolver), nameof(CombatResolver.ResolveDamage))]
public static class CombatResolver_ResolveDamage_Patch
{
    private static readonly FieldInfo F_baseDamage =
        AccessTools.Field(typeof(CombatResolver), nameof(CombatResolver.baseDamage));

    private static readonly MethodInfo M_ApplyBonus =
        SymbolExtensions.GetMethodInfo(() => ApplyDamageBonus(default));

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = false;
        foreach (var instruction in instructions)
        {
            if (instruction.StoresField(F_baseDamage))
            {
                yield return new CodeInstruction(OpCodes.Call, M_ApplyBonus);
                found = true;
            }
            yield return instruction;
        }

        if (!found)
            MainFile.Logger.Log("WARN: Could not find target field in CombatResolver.ResolveDamage");
    }

    private static void ApplyDamageBonus(CombatResolver instance)
    {
        instance.baseDamage = Mathf.RoundToInt(instance.baseDamage * 1.2f);
    }
}
```

### 7.4 Finalizer 模板

```csharp
using HarmonyLib;

namespace {{MODID}}.{{MODID}}Code.Patches;

[HarmonyPatch(typeof(DataLoader), nameof(DataLoader.LoadSaveFile))]
public static class DataLoader_LoadSaveFile_Patch
{
    static Exception Finalizer(Exception __exception)
    {
        if (__exception is not null)
        {
            MainFile.Logger.Log($"Save load failed: {__exception.Message}");
            // 可选：抑制异常或替换为自定义异常
            return null;
        }
        return null;
    }
}
```

---

## 八、绝对禁止行为

| 编号 | 禁止行为 | 原因 |
|------|---------|------|
| 1 | Patch 方法为实例方法（非 static） | Harmony 无法序列化实例状态 |
| 2 | 在 Prefix/Postfix 中使用反射查找方法 | 应在类初始化时缓存 MethodInfo |
| 3 | Transpiler 不报告匹配失败 | 静默失败导致 patch 无效，极难调试 |
| 4 | 多个 Mod 硬编码相同字符串 | 应使用唯一的 Harmony ID |
| 5 | 在 Finalizer 中忘记返回异常 | 可能意外抑制所有异常 |
| 6 | `ref __result` 类型与返回类型不兼容 | 运行时 Harmony 拒绝应用补丁 |
| 7 | Patch 实例方法时遗漏 `__instance` | 无法访问原始对象 |
| 8 | 在 Prefix 中 `new` 分配大对象 | 每次调用都分配导致 GC 压力 |
| 9 | Reverse Patch stub 不抛 NotImplementedException | 可能意外执行 stub 体 |
| 10 | `__state` 跨类传递 | Harmony 按声明类型匹配 state |

---

## 九、自我审查清单

### 9.1 基础审查

- [ ] 所有 Patch 类是否都有 `[HarmonyPatch]` 属性？
- [ ] 是否使用了 `using HarmonyLib;`？
- [ ] 方法名是否使用了 `nameof()` 而非硬编码字符串？
- [ ] Patch 类名是否包含目标类和方法名上下文？
- [ ] Harmony 实例 ID 是否与 ModId 一致？

### 9.2 Patch 方法审查

- [ ] Prefix/Postfix/Transpiler/Finalizer 是否都是 `static`？
- [ ] Prefix 跳过原始时是否返回 `bool`？
- [ ] Transpiler 是否返回 `IEnumerable<CodeInstruction>`？
- [ ] Finalizer 返回 `Exception` 类型（当需要修改异常时）？
- [ ] Reverse Patch stub 方法体是否 `throw new NotImplementedException()`？

### 9.3 注入参数审查

- [ ] `ref __result` 类型是否与原始返回类型兼容？
- [ ] 注入的原始参数名是否与原始方法签名完全一致？
- [ ] `__state` 是否仅在同一个 Patch 类的 Prefix↔Postfix 之间传递？
- [ ] 访问实例成员时是否使用了 `__instance` 参数？
- [ ] 是否避免了在 patch 方法中使用反射？

### 9.4 性能与兼容性审查

- [ ] 是否避免了在每帧调用的 patch 中分配新对象？
- [ ] Transpiler 中的 `FieldInfo`/`MethodInfo` 是否缓存为静态字段？
- [ ] Transpiler 是否有匹配失败时的错误报告？
- [ ] 是否优先使用 Postfix 而非 Prefix 跳过原始方法？
- [ ] 是否考虑了其他 Mod 可能也 patch 同一方法（优先级/Before/After）？

### 9.5 Finalizer 特殊审查

- [ ] Finalizer 返回类型是否正确（`void` 用于观察，`Exception` 用于修改）？
- [ ] 抑制异常时是否有充分的理由和日志记录？
- [ ] 是否使用了 `return __exception` 透传而非静默吞掉异常？

---

## 十、调试技巧

### 10.1 常用调试方法

```csharp
// 使用 Harmony 内置的 FileLog
FileLog.Log("Debug info");       // 写入桌面 harmony.log.txt
FileLog.Reset();                  // 清空日志文件

// 使用 Mod 自己的 Logger
MainFile.Logger.Log("Info message");

// 使用 Transpiler 的 IL 可视化
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    foreach (var instr in instructions)
        FileLog.Log($"{instr.opcode} {instr.operand}");
    return instructions;
}
```

### 10.2 Transpiler 辅助方法

```csharp
// Transpilers 工具类（HarmonyLib 提供）
Transpilers.EmitDelegate<Action>(() => { /* 自定义代码 */ });  // 插入匿名委托
Transpilers.Manipulator(instructions, match, action);  // 匹配并操作指令序列

// CodeInstruction 扩展方法
instruction.StoresField(fieldInfo);       // 检查是否存储到字段
instruction.LoadsField(fieldInfo);        // 检查是否加载字段
instruction.Calls(methodInfo);            // 检查是否调用方法
instruction.Is(OpCodes.Ldarg_0);          // 检查 opcode
```

---

*最后更新：2026-05-11*
*参考文档：https://harmony.pardeike.net*
