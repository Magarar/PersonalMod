// ComboTracker - 连携 (Combo) 属性系统
// 本场战斗中打出卡牌的全局计数器，用于"连携X：达成条件后触发额外效果"
//
// 使用方式：
// 1. 让你的卡牌实现 IComboCard 接口
// 2. 在 CanonicalVars 中添加 ComboProgress(0) 和 ComboThreshold(阈值) DynamicVar
// 3. 实现 RefreshComboDisplay() 来更新卡牌的 DynamicVar
// 4. 在 OnPlay() 中检查 combo 状态并应用额外效果
//
// ComboTracker 会自动监听 CardPlayedEvent 并更新所有实现 IComboCard 的卡牌的进度显示

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
/// 连携（Combo）卡牌需要实现的接口。
/// 实现此接口的卡牌会自动获得实时进度更新和伤害预览。
/// </summary>
public interface IComboCard
{
    /// <summary>
    /// 连携触发所需的本场战斗出牌数阈值。
    /// 例如 ComboThreshold = 7 表示出牌≥7张后触发连携效果。
    /// </summary>
    int ComboThreshold { get; }

    /// <summary>
    /// 连携附加伤害（或其它数值奖励），达到条件时自动加入伤害预览。
    /// </summary>
    int ComboBonus { get; }

    /// <summary>
    /// 由 ComboTracker 自动调用，更新卡牌上的 ComboProgress DynamicVar。
    /// </summary>
    void RefreshComboDisplay(int cardsPlayedThisCombat);
}

/// <summary>
/// 自定义伤害变量，在预览时自动加上连携奖励。
/// 替换普通的 DamageVar 使用，即可让 {Damage:diff()} 实时显示包含连携奖励的总伤害。
/// </summary>
public class ComboDamageVar : DamageVar
{
    public ComboDamageVar(decimal damage, ValueProp props)
        : base("Damage", damage, props)
    {
    }

    public override void UpdateCardPreview(
        CardModel card, CardPreviewMode previewMode,
        Creature? target, bool runGlobalHooks)
    {
        // 先执行 DamageVar 原有的预览逻辑（附魔加成、全局 Hook 等）
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        // 如果卡牌实现了 IComboCard 且连携条件达成，在预览值上追加奖励
        if (card is IComboCard comboCard &&
            ComboTracker.CardsPlayedThisCombat >= comboCard.ComboThreshold)
        {
            PreviewValue += comboCard.ComboBonus;
        }
    }
}

/// <summary>
/// 全局连携追踪器。
/// 统计本场战斗中所有卡牌的打出的次数（含所有玩家），
/// 并在每次出牌后自动刷新所有 IComboCard 的进度显示。
/// </summary>
public static class ComboTracker
{
    private static int _cardsPlayedThisCombat;
    private static bool _initialized;

    /// <summary>
    /// 本场战斗已打出的卡牌总数（跨所有来源）。
    /// </summary>
    public static int CardsPlayedThisCombat => _cardsPlayedThisCombat;

    /// <summary>
    /// 初始化 ComboTracker，订阅生命周期事件。
    /// 必须在 Mod 初始化时调用（在 MainFile.Initialize 中）。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        // 战斗开始时重置计数器
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            _cardsPlayedThisCombat = 0;
        });

        // 在卡牌 OnPlay 之前递增计数器（CardPlayingEvent），
        // 这样 OnPlay 中读到的就是包含当前卡牌的准确计数
        RitsuLibFramework.SubscribeLifecycle<CardPlayingEvent>(evt =>
        {
            _cardsPlayedThisCombat++;
        });

        // 出牌完成后刷新所有连携卡牌的显示（此时计数器已包含当前卡牌）
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
                    if (card is IComboCard comboCard)
                    {
                        comboCard.RefreshComboDisplay(_cardsPlayedThisCombat);
                    }
                }
            }
        });
    }
}
