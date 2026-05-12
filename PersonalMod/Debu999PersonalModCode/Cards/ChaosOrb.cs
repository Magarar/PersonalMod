// ChaosOrb - 混沌之球
// 0费 技能牌 自身 罕见 缺陷卡池
// 效果：将身上已有的充能球随机变为其他充能球

using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

[RegisterCard(typeof(DefectCardPool))]
public class ChaosOrb : ModCardTemplate
{
    private const int EnergyCost = 0;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType Target = TargetType.Self;

    // 新充能球的初始数值（0 表示无额外叠加层数）
    private const int OrbInitialAmount = 0;

    public ChaosOrb()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var currentOrbs = player.PlayerCombatState.OrbQueue.Orbs.ToList();

        if (currentOrbs.Count == 0)
            return;

        foreach (var oldOrb in currentOrbs)
        {
            // 不断随机直到获得与当前不同的充能球类型
            OrbModel newOrbTemplate;
            do
            {
                newOrbTemplate = OrbModel.GetRandomOrb(Rng.Chaotic);
            }
            while (newOrbTemplate.Id == oldOrb.Id);

            await OrbCmd.Replace(oldOrb, newOrbTemplate.ToMutable(OrbInitialAmount), player);
        }
    }

    protected override void OnUpgrade()
    {
        // 0费卡牌，随机化效果已足够强力，升级不改变数值
    }
}
