using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Powers;

[RegisterPower]
public class PuppetPower: ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://Debu999PersonalMod/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://Debu999PersonalMod/images/powers/big/{GetType().Name}.png"
    );
    
    public async Task AfterChantTrigger(PlayerChoiceContext choiceContext)
    {
        if (Owner != null)
        {
            var combatState = Owner.CombatState;
            if (combatState != null)
            {
                Flash();
                _ = await CreatureCmd.GainBlock(
                    Owner,
                    Amount,
                     ValueProp.Unpowered,
                    null
               );
            }
        }
    }
}