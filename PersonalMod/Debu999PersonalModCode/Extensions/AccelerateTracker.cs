// AccelerateTracker - 激奏 (Accelerate) 属性系统
// 当费用低于原始费用时，不发动原本卡牌效果，当作技能牌打出发动激奏效果。
// 与爆能强化相反：爆能是能量充足时变强，激奏是能量不足时变招。
//
// 使用方式：
// 1. 卡牌实现 IAccelerateCard 接口
// 2. OnPlay 中判断 IsAccelerateMode 执行不同效果
//
// 能量变更检测：通过 EnergyChangeHelper 统一 Harmony 补丁，
// 覆盖所有能量变化场景（回合重置、打牌扣费、遗物/能力/药水加费等）。

using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
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
    /// 激奏费用（如 1）。能量 &ge; BaseCost 时正常打出，能量 &lt; BaseCost 时触发激奏。
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

        // 回合开始时的刷新兜底
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side == CombatSide.Player)
                RefreshAllCosts(evt.CombatState);
        });
    }

    /// <summary>
    /// 刷新所有激奏卡的费用和状态。
    /// </summary>
    /// <param name="skipCard">可指定跳过某张卡（避免打出途中篡改其模式）</param>
    internal static void RefreshAllCosts(ICombatState? combatState, CardModel? skipCard = null)
    {
        var player = combatState?.Players?.FirstOrDefault();
        if (player == null) return;

        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Deck, PileType.Play };

        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;

            foreach (var card in pile.Cards.ToList())
            {
                if (card == skipCard) continue;  // 跳过打出中的卡
                if (card is IAccelerateCard accel)
                {
                    int currentEnergy = player.PlayerCombatState.Energy;
                    bool canAccel = currentEnergy < accel.BaseCost;

                    accel.IsAccelerateMode = canAccel;
                    int displayCost = canAccel ? accel.AccelCost : accel.BaseCost;
                    card.EnergyCost.SetUntilPlayed(displayCost);
                    var ncard = NCard.FindOnTable(card);
                    if (ncard != null)
                    {
                        ncard.Call("Reload");
                    }
                }
            }
        }
    }
}
