// OverchargeTracker - 爆能强化 (Overcharge) 属性系统
// 当玩家能量 >= 爆能费用时，卡牌UI费用自动切换为爆能费用，
// 打出时触发爆能效果（如更高伤害），否则触发基础效果。
//
// 使用方式：
// 1. 卡牌实现 IOverchargeCard 接口
// 2. CanonicalVars 中用 OverchargeDamageVar 替换 DamageVar
// 3. 添加 PlusedDamage(爆能伤害) 和 PlusedCost(爆能费用)
// 4. OnPlay 中判断 IsOverchargedMode 属性，执行不同逻辑

using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

/// <summary>
/// 爆能强化卡牌接口。
/// 实现后UI费用随能量自动切换，打出时根据是否爆能执行不同效果。
/// </summary>
public interface IOverchargeCard
{
    /// <summary>
    /// 基础费用（如 1）。
    /// </summary>
    int BaseCost { get; }

    /// <summary>
    /// 爆能费用（如 2）。玩家能量 ≥ 此值时显示为此费用。
    /// </summary>
    int PlusedCost { get; }

    /// <summary>
    /// 爆能伤害（预览时自动切换显示）。
    /// </summary>
    int PlusedDamage { get; }

    /// <summary>
    /// 由 OverchargeTracker 自动设置。
    /// true = 当前处于爆能模式（费用已切换为爆能费用）。
    /// </summary>
    bool IsOverchargedMode { get; set; }
}

/// <summary>
/// 自定义伤害变量，预览时根据能量自动切换显示基础伤害或爆能伤害。
/// </summary>
public class OverchargeDamageVar : DamageVar
{
    public OverchargeDamageVar(decimal damage, ValueProp props)
        : base("Damage", damage, props)
    {
    }

    public override void UpdateCardPreview(
        CardModel card, CardPreviewMode previewMode,
        Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        if (!card.DynamicVars.ContainsKey("PlusedDamage") ||
            !card.DynamicVars.ContainsKey("PlusedCost"))
            return;

        var player = card.Owner;
        if (player?.PlayerCombatState == null)
            return;

        int plusedCost = (int)card.DynamicVars["PlusedCost"].BaseValue;
        if (player.PlayerCombatState.Energy >= plusedCost)
        {
            decimal baseDamage = BaseValue;
            decimal basePreview = PreviewValue;
            decimal bonusFromHooks = basePreview - baseDamage;
            int plusedDamage = (int)card.DynamicVars["PlusedDamage"].BaseValue;
            PreviewValue = plusedDamage + bonusFromHooks;
        }
    }
}

/// <summary>
/// 爆能强化管理器。自动切换手牌中爆能卡的UI费用和状态。
/// </summary>
public static class OverchargeTracker
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
                RefreshCosts(evt.CombatState);
        });
    }

    /// <summary>
    /// 刷新所有爆能卡的费用和状态。
    /// </summary>
    internal static void RefreshCosts(ICombatState? combatState, CardModel? skipCard = null)
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
                if (card == skipCard) continue;
                if (card is IOverchargeCard oc)
                {
                    int currentEnergy = player.PlayerCombatState.Energy;
                    bool canOvercharge = currentEnergy >= oc.PlusedCost;

                    oc.IsOverchargedMode = canOvercharge;
                    card.EnergyCost.SetUntilPlayed(canOvercharge ? oc.PlusedCost : oc.BaseCost);
                }
            }
        }
    }
}
