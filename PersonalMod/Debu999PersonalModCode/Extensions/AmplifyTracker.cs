// AmplifyTracker - 魔力增幅 (Amplify) 属性系统
// 可叠加的增幅层数，打出技能牌时自动触发，在手牌中逐层积累，
// 打出卡牌时消耗所有层数获得加成。
//
// 使用方式：
// 1. 卡牌实现 IAmplifyCard 接口
// 2. CanonicalVars 中用 AmplifyDamageVar 替换 DamageVar
// 3. 添加 AmplifyStacks(0) 和 AmplifyDamage(每层加成值)
// 4. OnPlay 中调用 AmplifyTracker.ConsumeAndGetStacks(this) 消耗层数
// 5. 默认每打出一张技能牌自动触发 1 次增幅，可调

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
/// 魔力增幅卡牌接口。
/// 实现此接口的卡牌会在手牌中积累增幅层数，打出时消耗。
/// </summary>
public interface IAmplifyCard
{
    /// <summary>
    /// 每层增幅提供的伤害加成值（如 6 表示每层 +6 伤害）。
    /// </summary>
    int BurstPerAmplify { get; }
}

/// <summary>
/// 自定义伤害变量，预览时自动加上增幅层数的伤害加成。
/// 替换 DamageVar 使用，{Damage:diff()} 自动显示含增幅的总伤害。
/// </summary>
public class AmplifyDamageVar : DamageVar
{
    public AmplifyDamageVar(decimal damage, ValueProp props)
        : base("Damage", damage, props)
    {
    }

    public override void UpdateCardPreview(
        CardModel card, CardPreviewMode previewMode,
        Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        if (card.DynamicVars.ContainsKey("AmplifyStacks") &&
            card.DynamicVars.ContainsKey("AmplifyDamage"))
        {
            int stacks = (int)card.DynamicVars["AmplifyStacks"].BaseValue;
            int perStack = (int)card.DynamicVars["AmplifyDamage"].BaseValue;
            PreviewValue += stacks * perStack;
        }
    }
}

/// <summary>
/// 魔力增幅管理器。
/// 提供自动/手动触发增幅、读取层数、消耗层数的静态方法。
/// </summary>
public static class AmplifyTracker
{
    private static bool _initialized;

    /// <summary>
    /// 打出技能牌时自动触发的增幅层数。
    /// 设为 0 可禁用自动触发。
    /// </summary>
    public static int SkillTriggerCount { get; set; } = 1;

    /// <summary>
    /// 初始化 AmplifyTracker，订阅生命周期事件。
    /// 在 MainFile.Initialize 中调用。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;

        // 打出技能牌后自动触发增幅
        RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(evt =>
        {
            if (SkillTriggerCount <= 0)
                return;

            var cardPlay = evt.CardPlay;
            if (cardPlay?.Card?.Type != CardType.Skill)
                return;

            var combatState = evt.CombatState;
            var player = combatState?.Players?.FirstOrDefault();
            if (player == null)
                return;

            TriggerAll(player, SkillTriggerCount);
        });
    }

    /// <summary>
    /// 对玩家手牌中所有 IAmplifyCard 触发 N 次增幅。
    /// </summary>
    /// <param name="player">当前玩家</param>
    /// <param name="count">增幅次数（如 3 表示一次性触发 3 层）</param>
    public static void TriggerAll(Player player, int count)
    {
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToList())
        {
            if (card is IAmplifyCard)
            {
                card.DynamicVars["AmplifyStacks"].BaseValue += count;
            }
        }
    }

    /// <summary>
    /// 获取当前卡牌的增幅层数。
    /// </summary>
    public static int GetStacks(CardModel card)
    {
        if (card.DynamicVars.ContainsKey("AmplifyStacks"))
            return (int)card.DynamicVars["AmplifyStacks"].BaseValue;
        return 0;
    }

    /// <summary>
    /// 消耗卡牌的所有增幅层数，并返回消耗的层数。
    /// </summary>
    public static int ConsumeAndGetStacks(CardModel card)
    {
        int stacks = GetStacks(card);
        if (card.DynamicVars.ContainsKey("AmplifyStacks"))
            card.DynamicVars["AmplifyStacks"].BaseValue = 0;
        return stacks;
    }
}
