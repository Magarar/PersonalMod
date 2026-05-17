// CurtainCallTracker - 移除能力触发器
// 监听所有 Creature 的 PowerRemoved 事件，
// 当实现了 IRemovablePower 接口的能力被移除时，调用其 OnRemoved 方法。
//
// 使用方式：
// 1. 让能力实现 IRemovablePower 接口
// 2. 在能力中保存好 LastRemovalContext（如 ChantPower 自动保存 LastChantContext）
// 3. 确保 CurtainCallTracker.Initialize() 在 Mod 初始化时被调用

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using PersonalMod.PersonalModCode;
using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

/// <summary>
/// 实现此接口的能力在被移除时，CurtainCallTracker 会自动调用 OnRemoved。
/// 能力需自行保存上下文（如 ChantPower 的 LastChantContext）。
/// </summary>
public interface IRemovablePower
{
    /// <summary>
    /// 能力被移除时调用。
    /// </summary>
   public abstract Task OnRemoved();
}

/// <summary>
/// 全局可移除能力追踪器。
/// 自动监听所有生物的能力移除事件，派发到 IRemovablePower 接口。
/// </summary>
public static class CurtainCallTracker
{
    private static bool _initialized;

    /// <summary>
    /// 已订阅的 Creature 及其事件引用，用于战斗结束时取消订阅。
    /// </summary>
    private static readonly Dictionary<Creature, Action<PowerModel>> _subscriptions = new();

    /// <summary>
    /// 已触发过的 PowerModel，防止同一能力因多次订阅导致重复触发。
    /// </summary>
    private static readonly HashSet<PowerModel> _notified = new();

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        // 战斗开始时清理所有订阅
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ =>
        {
            UnsubscribeAll();
        });

        // 每次玩家回合开始时，检查是否有新 Creature 需要订阅
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side != CombatSide.Player)
                return;

            var combatState = evt.CombatState;
            if (combatState == null)
                return;

            _notified.Clear();

            foreach (var creature in combatState.PlayerCreatures)
            {
                TrySubscribe(creature);
            }
        });
    }

    private static void UnsubscribeAll()
    {
        foreach (var kvp in _subscriptions)
        {
            try
            {
                kvp.Key.PowerRemoved -= kvp.Value;
            }
            catch
            {
                // 忽略已释放的 Godot 对象
            }
        }
        _subscriptions.Clear();
        _notified.Clear();
    }

    private static void TrySubscribe(Creature creature)
    {
        if (_subscriptions.ContainsKey(creature))
            return;

        void Handler(PowerModel power) => OnPowerRemoved(power);

        _subscriptions[creature] = Handler;
        creature.PowerRemoved += Handler;

        MainFile.Logger.Info($"Subscribed to {creature}");
    }

    private static void OnPowerRemoved(PowerModel power)
    {
        if (power is not IRemovablePower removable)
            return;

        // 幂等检查：同一 PowerModel 只触发一次
        if (!_notified.Add(power))
            return;

        MainFile.Logger.Warn($"{power} removed");
        _ = removable.OnRemoved();
    }
}
