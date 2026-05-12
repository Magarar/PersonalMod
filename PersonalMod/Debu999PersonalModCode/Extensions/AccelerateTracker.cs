// AccelerateTracker - 激奏 (Accelerate) 属性系统
// 当费用低于原始费用时，不发动原本卡牌效果，当作技能牌打出发动激奏效果。
// 与爆能强化相反：爆能是能量充足时变强，激奏是能量不足时变招。
//
// 使用方式：
// 1. 卡牌实现 IAccelerateCard 接口
// 2. OnPlay 中判断 IsAccelerateMode 执行不同效果

using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

/// <summary>
/// 激奏（Accelerate）卡牌接口。
/// 当能量不足支付原始费用时，改为以更低费用打出激奏效果。
/// </summary>
public interface IAccelerateCard
{
    /// <summary>
    /// 原始费用（如 2）。
    /// </summary>
    int BaseCost { get; }

    /// <summary>
    /// 激奏费用（如 1）。能量 ≥ BaseCost 时正常打出，≤ AccelCost 时触发激奏。
    /// </summary>
    int AccelCost { get; }

    /// <summary>
    /// 由 AccelerateTracker 自动管理。true = 当前处于激奏模式。
    /// </summary>
    bool IsAccelerateMode { get; set; }
}

/// <summary>
/// 激奏管理器。自动切换手牌中激奏卡的UI费用和状态。
/// </summary>
public static class AccelerateTracker
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side == CombatSide.Player)
                RefreshAllCosts(evt.CombatState);
        });

        RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(evt =>
        {
            RefreshAllCosts(evt.CombatState);
        });
    }

    private static void RefreshAllCosts(ICombatState? combatState)
    {
        var player = combatState?.Players?.FirstOrDefault();
        if (player == null) return;
        
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard ,PileType.Exhaust,PileType.Deck,PileType.Play};

        // 遍历所有牌堆，更新激奏卡的费用和状态
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;

            foreach (var card in pile.Cards.ToList())
            {
                if (card is IAccelerateCard accel)
                {
                    int currentEnergy = player.PlayerCombatState.Energy;
                    // 能量 ≥ 基础费用 → 正常模式（显示基础费用）
                    // 能量 < 基础费用 且 ≥ 激奏费用 → 激奏模式（显示激奏费用）
                    bool canAccel = currentEnergy < accel.BaseCost;

                    accel.IsAccelerateMode = canAccel;
                    // 激奏时显示激奏费用，否则显示基础费用
                    int displayCost = canAccel ? accel.AccelCost : accel.BaseCost;
                    card.EnergyCost.SetUntilPlayed(displayCost);
                }
            }
        }
    }
}
