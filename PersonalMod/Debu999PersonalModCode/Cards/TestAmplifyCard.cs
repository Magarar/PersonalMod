// TestAmplifyCard - 魔力增幅 (Amplify) 示例攻击卡
// 1费攻击牌，单目标，无色池
// 效果：造成 5+(增幅层数×6) 点伤害
// 每层魔力增幅使此卡下次打出时伤害 +6，层数由其他牌触发，打出后归零

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using PersonalMod.Debu999PersonalModCode.Extensions;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class TestAmplifyCard : ModCardTemplate, IAmplifyCard
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType Target = TargetType.AnyEnemy;
    
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.BURST];


    /// <summary>
    /// IAmplifyCard：每层增幅提供的伤害加成值。
    /// </summary>
    public int BurstPerAmplify => 6;

    public TestAmplifyCard()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AmplifyDamageVar(9, ValueProp.Move),  // {Damage} 预览时自动加 层数×6
        new DynamicVar("AmplifyStacks", 0),         // 当前增幅层数
        new DynamicVar("AmplifyDamage", BurstPerAmplify),  // 每层伤害加成
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 消耗所有增幅层数，计算总伤害
        int stacks = AmplifyTracker.ConsumeAndGetStacks(this);
        var totalDamage = (int)DynamicVars.Damage.BaseValue + stacks * BurstPerAmplify;

        _ = await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
