// ChantOfExplosion - 吟唱·爆破
// 1费能力牌，目标自身
// 效果：获得吟唱·爆破（吟唱1），下次回合开始时能力被移除时对所有敌人造成20点伤害
// 升级：伤害+10

using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using PersonalMod.Debu999PersonalModCode.Extensions;
using PersonalMod.Debu999PersonalModCode.Powers;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class ChantOfExplosion : ModCardTemplate
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Power;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.Self;
    
    public ChantOfExplosion()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }
    
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Chant, MyKeywords.CurtainCall];


    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<ChantExplosionPower>(
            choiceContext,
            Owner.Creature,
            1,    // Amount=1（吟唱1）
            Owner.Creature,
            this
        );
        power.SetDamage(20m);
    }

    protected override void OnUpgrade()
    {
        // 升级逻辑留给卡片打出后的 SetDamage 参数
        // 也可以在这里加附加值，但伤害由 SetDamage 控制更方便
    }
}
