// EnergyChangeHelper - 战斗变更监听共享助手
//
// 通过 Harmony 补丁游戏所有与卡牌模式相关的 Hook 方法，
// 战斗中的任何相关变化（能量、卡牌移动、卡牌生成等）都会自动刷新
// Accelerate / Overcharge / Crystallize 的状态和费用显示。
//
// 刷新时跳过正在打出的卡（AfterEnergySpent），避免中途篡改模式。

using System.Reflection;
using GodotPlugins.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using PersonalMod.PersonalModCode;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

internal static class EnergyChangeHelper
{
    private static bool _patched;

    // 要补丁的 Hook 列表：(Hook方法名, 是否需要skipCard)
    private static readonly (string methodName, bool skipCard)[] _hooks =
    {
        // ── 能量变化 ──
        ("AfterEnergyReset",                false),
        ("AfterEnergySpent",                true),   // 跳过消耗能量的那张卡

        // ── 卡牌生命周期（进入/离开牌堆、生成、打出） ──
        ("AfterCardGeneratedForCombat",     false),
        ("AfterCardDrawn",                  false),
        ("AfterCardDiscarded",              false),
        ("AfterCardExhausted",              false),
        ("AfterCardPlayed",                 false),

        // ── 回合切换 ──
        ("AfterSideTurnStart",              false),
    };

    public static void EnsurePatched()
    {
        if (_patched) return;
        _patched = true;

        try
        {
            var harmony = new Harmony("Debu999PersonalMod.EnergyChangeHelper");
            var hookType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Hooks.Hook");
            if (hookType == null) return;

            foreach (var (methodName, skipCard) in _hooks)
            {
                var target = hookType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (target == null) continue;

                var postfix = typeof(HookPatch).GetMethod(
                    skipCard ? nameof(HookPatch.OnRefreshSkipCard) : nameof(HookPatch.OnRefreshAll),
                    BindingFlags.Static | BindingFlags.NonPublic)!;

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            }

            // ── 战斗开始/结束 Hook（参数顺序不同，单独处理）──
            foreach (var combatHook in new[] { "BeforeCombatStart", "AfterCombatEnd" })
            {
                var method = hookType.GetMethod(combatHook, BindingFlags.Public | BindingFlags.Static);
                if (method == null) continue;
            
                var postfix = typeof(HookPatch).GetMethod(
                    nameof(HookPatch.OnCombatBoundary),
                    BindingFlags.Static | BindingFlags.NonPublic)!;
                harmony.Patch(method, postfix: new HarmonyMethod(postfix));
            }
        }
        catch
        {
            // Harmony 补丁失败不影响 SideTurnStartedEvent
        }
    }

    /// <summary>
    /// Harmony Postfix — 所有 Postfix 共用同一套分发。
    /// OnRefreshSkipCard 需根据 Hook 参数位置提取 CardModel。
    /// </summary>
    private static class HookPatch
    {
        /// <summary>全量刷新（所有卡）</summary>
        internal static void OnRefreshAll(ICombatState combatState)
        {
            DoRefresh(combatState);
        }

        /// <summary>刷新时跳过参数中的 CardModel（用于 AfterEnergySpent）</summary>
        internal static void OnRefreshSkipCard(ICombatState combatState, CardModel card)
        {
            DoRefresh(combatState, card);
        }
        
        internal static void OnCombatBoundary(ICombatState combatState)
        {
            DoRefresh(combatState);
        }

        private static void DoRefresh(ICombatState? combatState,CardModel? skipCard = null)
        {
            if (combatState == null) return;
            MainFile.Logger.Warn("EnergyChangeHelper.DoRefresh");
            AccelerateTracker.RefreshAllCosts(combatState, skipCard);
            OverchargeTracker.RefreshCosts(combatState, skipCard);
            CrystallizeTracker.RefreshAllCosts(combatState, skipCard);
        }
    }
}
