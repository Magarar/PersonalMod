// TestAmplifyTrigger - 魔力增幅触发卡
// 1费技能牌，自身目标，无色池
// 效果：对手牌中所有含魔力增幅的卡牌触发 3 次增幅
// 每层增幅使对应卡牌下次打出时获得 +6 伤害

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using PersonalMod.Debu999PersonalModCode.Extensions;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

public class TestAmplifyTrigger : ModCardTemplate
{
    // 触发次数（可自由修改，方便重写）
    private const int AmplifyCount = 3;

    private const int EnergyCost = 1;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.Self;
    
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.BURST];


    public TestAmplifyTrigger()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AmplifyCount", AmplifyCount),
        new CardsVar(1),  
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对手牌中所有 Amplify 卡牌触发 N 次魔力增幅
        AmplifyTracker.TriggerAll(Owner, AmplifyCount);

        // 抽 1 张牌
        _ = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级后触发次数 +2
        DynamicVars["AmplifyCount"].UpgradeValueBy(2);
    }
}
