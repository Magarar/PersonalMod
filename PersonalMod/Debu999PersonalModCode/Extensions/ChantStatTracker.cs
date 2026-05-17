// ChantStatTracker - 吟唱全局计数器
// 统计每场战斗中吟唱能力触发的总次数，战斗开始时归零。
// 计数逻辑写在 ChantPower.TriggerChant 中。

using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

/// <summary>
/// 吟唱全局统计。记录每场战斗中吟唱能力触发的总次数。
/// </summary>
public static class ChantStatTracker
{
    private static bool _initialized;

    /// <summary>本场战斗吟唱已触发的次数</summary>
    public static int TriggerCount { get; internal set; }

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ =>
        {
            TriggerCount = 0;
        });
    }
}
