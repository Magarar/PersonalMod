// CrystallizeTracker - 结晶 (Crystallize) 属性系统
// 当费用低于原始费用时，不发动原本卡牌效果，当作能力牌打出发动结晶效果。
// 与激奏区别：激奏→技能牌，结晶→能力牌

using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;

namespace PersonalMod.Debu999PersonalModCode.Extensions;

public interface ICrystallizeCard
{
    int BaseCost { get; }
    int CrystallizeCost { get; }
    bool IsCrystallizeMode { get; set; }
}

public static class CrystallizeTracker
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

        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Deck, PileType.Play };

        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;

            foreach (var card in pile.Cards.ToList())
            {
                if (card is ICrystallizeCard cry)
                {
                    int currentEnergy = player.PlayerCombatState.Energy;
                    bool canCrystallize = currentEnergy < cry.BaseCost;

                    cry.IsCrystallizeMode = canCrystallize;
                    int displayCost = canCrystallize ? cry.CrystallizeCost : cry.BaseCost;
                    card.EnergyCost.SetUntilPlayed(displayCost);
                }
            }
        }
    }
}
