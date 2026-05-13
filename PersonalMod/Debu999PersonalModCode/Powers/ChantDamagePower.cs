// ChantDamagePower - 吟唱·伤害
// 吟唱结束后对所有敌人造成固定伤害
// 用法示例：给玩家施加 Amount=2 的 ChantDamagePower
// → 第1回合 Amount: 2→1
// → 第2回合 Amount: 1→0，触发对所有敌人造成10点伤害并移除

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Powers;

/// <summary>
/// 吟唱·伤害：吟唱结束后对所有敌人造成 DamageVar 点伤害。
/// </summary>
[RegisterPower]
public class ChantDamagePower : ChantPower
{
    /// <summary>
    /// 图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    /// </summary>
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://Debu999PersonalMod/images/powers/chant_damage.png",
        BigIconPath: "res://Debu999PersonalMod/images/powers/chant_damage.png"
    );

    /// <summary>
    /// 设置吟唱爆炸伤害值。
    /// 卡牌打出后可通过此方法覆盖默认伤害。
    /// </summary>
    public void SetDamage(decimal damage)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue = damage;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
        => new[] { new DamageVar(10m, ValueProp.Unpowered) };

    public override async Task OnChantTrigger(PlayerChoiceContext choiceContext)
    {
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
            choiceContext,
            aliveEnemies,
            DynamicVars.Damage.BaseValue,
            ValueProp.Unpowered,
            Owner
        );
    }
}
