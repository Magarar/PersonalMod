// TestCard - 测试卡牌
// 0费技能牌，单目标，效果：抽1张牌 + 造成1点伤害 + 获得1点格挡，无色池

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

public class TestCard : ModCardTemplate
{
    // 构造参数：0费、技能牌、普通稀有度、单个敌人目标
    private const int EnergyCost = 0;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.AnyEnemy;

    public TestCard()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    // GainsBlock → 引擎标记有格挡效果，预览时显示格挡值
    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1, ValueProp.Move),    // 1点伤害，受力量/敏捷修正
        new BlockVar(1, ValueProp.Move),     // 1点格挡，受敏捷修正
        new CardsVar(1),                     // 抽1张牌
    ];

    // 参照 PommelStrike 反编译的 OnPlay 模式
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 抽牌（用 CardsVar 定义的数值，不硬编码）
        _ = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        // 获得格挡
        _ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 造成伤害（参照 PommelStrike 的 WithHitFx 模式）
        _ = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    // 升级：伤害+2，格挡+2，抽牌+1
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
