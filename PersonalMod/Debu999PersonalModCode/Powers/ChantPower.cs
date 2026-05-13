// ChantPower - 吟唱基础能力属性
// 每回合开始时计数器-1，归零时触发 OnChantTrigger() 并移除自身
// 实现 IRemovablePower 接口，子类可重写 OnChantRemoved() 响应移除事件
//
// 使用方式：
// 1. 继承此类，实现 OnChantTrigger(choiceContext)
// 2. 可选重写 OnChantRemoved() 在能力被移除时触发效果
// 3. 在 CanonicalVars 中声明需要的动态变量
// 4. 在 localization 中添加 {MODID}_POWER_{类名大写下划线}.title/.description/.smartDescription
// 5. 子类添加 [RegisterPower] 属性实现自动注册

using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using PersonalMod.Debu999PersonalModCode.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Powers;

/// <summary>
/// 吟唱基础能力属性。
/// 计数器绑定到能力自身的 Amount，每玩家回合开始自动递减。
/// 归零时调用 OnChantTrigger() 执行具体效果并移除自身。
/// </summary>
public abstract class ChantPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 每张吟唱独立计数，互不干扰。
    /// </summary>
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>
    /// 最后一次 AfterPlayerTurnStart 的上下文。
    /// 用于 IRemovablePower 移除回调。
    /// </summary>
    protected PlayerChoiceContext LastChantContext { get; private set; }



    /// <summary>
    /// 能力被移除时回调。默认无操作，子类可重写。
    /// 注意：此时 LastChantContext 可用（前提是经过 AfterPlayerTurnStart 流程）。
    /// </summary>
    protected virtual Task OnChantRemoved() => Task.CompletedTask;

    /// <summary>
    /// 玩家回合开始时：递减计数器 → 触发吟唱效果 → 归零时移除。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        LastChantContext = choiceContext;

        if (Amount <= 0)
            return;

        await TriggerChant(choiceContext);
    }

    public async Task TriggerChant(PlayerChoiceContext choiceContext)
    {
        SetAmount(Amount - 1);

        await OnChantTrigger(choiceContext);

        if (Amount <= 0)
        {
            RemoveInternal();
        }
    }

    /// <summary>
    /// 子类重写此方法实现具体的吟唱触发效果。
    /// 在计数器-1之后、归零移除之前调用。
    /// </summary>
    public abstract Task OnChantTrigger(PlayerChoiceContext choiceContext);
}
