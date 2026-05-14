using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using PersonalMod.Debu999PersonalModCode.Extensions;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace PersonalMod.PersonalModCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Debu999PersonalMod"; //Used for resource filepath

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        EnergyChangeHelper.EnsurePatched();
        // 初始化连携 (Combo) 追踪系统
        ComboTracker.Initialize();
        // 初始化连击 (Rapid) 追踪系统
        RapidTracker.Initialize();
        // 初始化魔力增幅 (Amplify) 追踪系统
        AmplifyTracker.Initialize();
        // 初始化爆能强化 (Overcharge) 追踪系统
        OverchargeTracker.Initialize();
        // 初始化激奏 (Accelerate) 追踪系统
        AccelerateTracker.Initialize();
        // 初始化结晶 (Crystallize) 追踪系统
        CrystallizeTracker.Initialize();
        // 初始化能力移除 (Removal) 追踪系统
        CurtainCallTracker.Initialize();

    }
}
