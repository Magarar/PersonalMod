// RapidTracker - 连击 (Rapid) 属性系统
// 本回合中打出卡牌的计数器，用于"连击X：本回合出牌达条件后触发额外效果"
//
// 与 连携(Combo) 的区别：
//   连携 = 本场战斗累计 | 连击 = 本回合累计（每回合重置）
//
// 使用方式（与连携完全一致）：
// 1. 让卡牌实现 IRapidCard 接口
// 2. 在 CanonicalVars 中用 RapidDamageVar 替换 DamageVar
// 3. 添加 RapidProgress(0) 和 RapidThreshold(阈值)
// 4. 实现 RefreshRapidDisplay()
// 5. 在 OnPlay() 中检查效果

using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

/// <summary>
/// 连击（Rapid）卡牌接口。与 IComboCard 结构相同，但按回合重置。
/// </summary>
public interface IRapidCard
{
    /// <summary>
    /// 连击阈值：本回合出牌数达到此值后触发。
    /// </summary>
    int RapidThreshold { get; }

    /// <summary>
    /// 连击附加伤害（或其它奖励），自动加入伤害预览。
    /// </summary>
    int RapidBonus { get; }

    /// <summary>
    /// 由 RapidTracker 自动调用，更新 RapidProgress DynamicVar。
    /// </summary>
    void RefreshRapidDisplay(int cardsPlayedThisTurn);
}

/// <summary>
/// 自定义伤害变量，预览时自动加上连击奖励。
/// 替换 DamageVar 使用，{Damage:diff()} 自动显示含连击的总伤害。
/// </summary>
public class RapidDamageVar : DamageVar
{
    public RapidDamageVar(decimal damage, ValueProp props)
        : base("Damage", damage, props)
    {
    }

    public override void UpdateCardPreview(
        CardModel card, CardPreviewMode previewMode,
        Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        if (card is IRapidCard rapidCard &&
            RapidTracker.CardsPlayedThisTurn >= rapidCard.RapidThreshold)
        {
            PreviewValue += rapidCard.RapidBonus;
        }
    }
}

/// <summary>
/// 全局连击追踪器。每回合重置，统计本回合出牌数。
/// </summary>
public static class RapidTracker
{
    private static int _cardsPlayedThisTurn;
    private static bool _initialized;

    /// <summary>
    /// 本回合已打出的卡牌总数。
    /// </summary>
    public static int CardsPlayedThisTurn => _cardsPlayedThisTurn;

    /// <summary>
    /// 初始化 RapidTracker，订阅生命周期事件。
    /// 必须在 MainFile.Initialize 中调用。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        // 战斗开始时重置
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            _cardsPlayedThisTurn = 0;
        });

        // 玩家回合开始时重置
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side == CombatSide.Player)
                _cardsPlayedThisTurn = 0;
        });

        // 出牌前递增（与连携一样用 CardPlayingEvent）
        RitsuLibFramework.SubscribeLifecycle<CardPlayingEvent>(evt =>
        {
            _cardsPlayedThisTurn++;
        });

        // 出牌后刷新显示
        RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(evt =>
        {
            var combatState = evt.CombatState;
            if (combatState?.Players == null || combatState.Players.Count == 0)
                return;

            var player = combatState.Players[0];
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };

            foreach (var pileType in piles)
            {
                var pile = pileType.GetPile(player);
                if (pile == null)
                    continue;

                foreach (var card in pile.Cards)
                {
                    if (card is IRapidCard rapidCard)
                    {
                        rapidCard.RefreshRapidDisplay(_cardsPlayedThisTurn);
                    }
                }
            }
        });
    }
}
