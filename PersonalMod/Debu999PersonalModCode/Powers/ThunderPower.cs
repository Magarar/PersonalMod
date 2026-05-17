using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Powers;

/// <summary>
/// Mod排序没法调，暂时先放这里
/// </summary>
[RegisterPower]
public class ThunderPower: ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://Debu999PersonalMod/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://Debu999PersonalMod/images/powers/big/{GetType().Name}.png"
    );
    

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if(side == CombatSide.Player)
            await PowerCmd.Remove(this);
    }

    public async Task AfterChantTrigger(PlayerChoiceContext choiceContext)
    {
        if (Owner != null)
        {
            var combatState = Owner.CombatState;
            if (combatState != null)
            {
                var aliveEnemies = combatState.Enemies?
                    .Where(e => e.IsAlive)
                    .ToList();

                if (aliveEnemies != null && aliveEnemies.Count > 0)
                {
                    _ = await CreatureCmd.Damage(
                        choiceContext,
                        aliveEnemies,
                        Amount,
                        ValueProp.Unpowered,
                        Owner
                    );
                }
            }
        }
    }

    
}