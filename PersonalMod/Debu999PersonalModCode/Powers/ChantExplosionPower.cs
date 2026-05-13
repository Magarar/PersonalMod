// ChantExplosionPower - 吟唱·爆破
// 吟唱1，无回合效果（OnChantTrigger 空实现）。
// 能力被移除时（通过 CurtainCallTracker + IRemovablePower），对所有敌人造成固定伤害。

using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using PersonalMod.Debu999PersonalModCode.Extensions;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Powers;

/// <summary>
/// 吟唱·爆破：吟唱1，无回合效果。
/// 能力被移除时对所有敌人造成 FixedDamage 点伤害。
/// </summary>
[RegisterPower]
public class ChantExplosionPower : ChantPower, IRemovablePower
{
    private bool _triggered;
    private decimal _fixedDamage = 20m;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://Debu999PersonalMod/images/powers/chant_explosion.png",
        BigIconPath: "res://Debu999PersonalMod/images/powers/chant_explosion.png"
    );

    PlayerChoiceContext? IRemovablePower.LastRemovalContext => LastChantContext;

    Task IRemovablePower.OnRemoved() => OnChantRemoved();
    
    /// <summary>
    /// 设置固定伤害值，卡牌打出后调用。
    /// </summary>
    public void SetDamage(decimal damage)
    {
        AssertMutable();
        _fixedDamage = damage;
    }

    /// <summary>
    /// 吟唱期间无回合效果。
    /// </summary>
    public override Task OnChantTrigger(PlayerChoiceContext choiceContext)
        => Task.CompletedTask;

    /// <summary>
    /// 能力被移除时，对所有敌人造成伤害。
    /// （由 CurtainCallTracker 在监听到 Creature.PowerRemoved 事件后调用）
    /// </summary>
    protected override async Task OnChantRemoved()
    {
        if (_triggered)
            return;
        _triggered = true;

        var ctx = LastChantContext;
        if (ctx == null)
            return;

        if (Owner == null)
            return;

        var combatState = Owner.CombatState;
        if (combatState == null)
            return;

        var aliveEnemies = combatState.Enemies?
            .Where(e => e.IsAlive).ToList();
        if (aliveEnemies == null || aliveEnemies.Count == 0)
            return;

        _ = await CreatureCmd.Damage(
            ctx,
            aliveEnemies,
            _fixedDamage,
            ValueProp.Unpowered,
            Owner
        );
    }
}
