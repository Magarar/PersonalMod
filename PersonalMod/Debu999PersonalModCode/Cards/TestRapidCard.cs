// TestRapidCard - 连击 (Rapid) 属性示例卡牌
// 1费攻击牌，单目标，无色池
// 效果：造成 9+X 点伤害，X = 本回合已出牌数（连击数）
// 例如本回合第 3 张打出 → 9 + 3 = 12 点伤害

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

public class TestRapidCard : ModCardTemplate, IRapidCard
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Common;
    private const TargetType Target = TargetType.AnyEnemy;

    // IRapidCard：阈值设为 0，始终触发连击
    public virtual int RapidThreshold => 0;

    // 连击奖励 = 当前连击数（X），即本回合已出牌数
    int IRapidCard.RapidBonus => RapidTracker.CardsPlayedThisTurn;
    
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Rapid];


    public TestRapidCard()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new RapidDamageVar(9, ValueProp.Move),  // {Damage} 预览时自动加连击数
        new DynamicVar("RapidProgress", 0),      // 当前连击数（自动更新）
        new DynamicVar("RapidThreshold", RapidThreshold),
    ];

    public void RefreshRapidDisplay(int cardsPlayedThisTurn)
    {
        DynamicVars["RapidProgress"].BaseValue = cardsPlayedThisTurn;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        RefreshRapidDisplay(RapidTracker.CardsPlayedThisTurn);

        // 总伤害 = 9 + 当前连击数（含本张牌）
        var totalDamage = 9 + RapidTracker.CardsPlayedThisTurn;

        _ = await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        // 升级后基础伤害 12，连击奖励不变（仍然是"+X"）
    }
}
