// ChantOfDamage - 吟唱·伤害
// 1费能力牌，目标自身
// 效果：获得吟唱·伤害能力，3回合后对所有敌人造成10点伤害
// 升级：吟唱计数 -1（提前1回合触发）

using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using PersonalMod.Debu999PersonalModCode.Powers;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class ChantOfDamage : ModCardTemplate
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Power;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.Self;

    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Chant];

    public ChantOfDamage()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ChantCount", 2m),
        new DynamicVar("ChantDamage", 10m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = (int)DynamicVars["ChantCount"].BaseValue;
        var damage = DynamicVars["ChantDamage"].BaseValue;

        var power = await PowerCmd.Apply<ChantDamagePower>(
            choiceContext,
            Owner.Creature,
            count,
            Owner.Creature,
            this
        );
        power.SetDamage(damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChantCount"].UpgradeValueBy(2);
        //DynamicVars["ChantDamage"].UpgradeValueBy(5m);
    }
}
