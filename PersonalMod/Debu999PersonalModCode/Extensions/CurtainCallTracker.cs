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
    System.Threading.Tasks.Task OnRemoved();
}

/// <summary>
/// 全局可移除能力追踪器。
/// 自动监听所有生物的能力移除事件，派发到 IRemovablePower 接口。
/// </summary>
public static class CurtainCallTracker
{
    private static bool _initialized;
    private static readonly HashSet<Creature> _subscribed = new();

    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        // 战斗开始时清理订阅记录
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ =>
        {
            _subscribed.Clear();
        });

        // 每次玩家回合开始时，检查是否有新 Creature 需要订阅
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side != CombatSide.Player)
                return;

            var combatState = evt.CombatState;
            if (combatState == null)
                return;

            foreach (var creature in combatState.PlayerCreatures)
            {
                TrySubscribe(creature);
            }
        });
    }

    private static void TrySubscribe(Creature creature)
    {
        if (!_subscribed.Add(creature))
            return;

        MainFile.Logger.Info($"Subscribed to {creature}");
        creature.PowerRemoved += OnPowerRemoved;
    }

    private static void OnPowerRemoved(PowerModel power)
    {
        if (power is not IRemovablePower removable)
            return;

        MainFile.Logger.Warn($"{power} removed");
        _ = removable.OnRemoved();
        
        
    }
}
