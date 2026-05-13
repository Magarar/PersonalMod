// TestCrystallizeCard - 结晶 (Crystallize) 示例卡牌
// 1费攻击牌，单目标，无色池
// 基础效果：造成 9 点伤害
// 结晶0：改为获得 3 点力量（当作能力牌）
// 能量=0时自动切换为结晶模式，费用显示0，类型变为Power

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using PersonalMod.Debu999PersonalModCode.Extensions;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

public class TestCrystallizeCard : ModCardTemplate, ICrystallizeCard
{
    // ===== 结晶配置 =====
    public int BaseCost => 1;
    public int CrystallizeCost => 0;
    public bool IsCrystallizeMode { get; set; }

    private const int EnergyCost = 1;
    private const int CardDamage = 9;
    private const int CrystallizeStrengthAmount = 3;
    private const CardType CardTypeValue = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType Target = TargetType.AnyEnemy;
    
    public override TargetType TargetType => IsCrystallizeMode ? TargetType.Self : Target;
    public override CardType Type => IsCrystallizeMode ? CardType.Power : CardTypeValue;

    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Crystallize];


    public TestCrystallizeCard()
        : base(EnergyCost, CardTypeValue, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(CardDamage, ValueProp.Move),
        new DynamicVar("CrystallizeStrength", CrystallizeStrengthAmount),
        new DynamicVar("CrystallizeCost", CrystallizeCost),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsCrystallizeMode)
        {
            // 结晶模式：获得力量（当作能力牌）
            IsCrystallizeMode = false;
            _ = await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature,
                DynamicVars["CrystallizeStrength"].BaseValue, null, null, false);
        }
        else
        {
            // 正常模式：造成伤害
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            _ = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["CrystallizeStrength"].UpgradeValueBy(2);
    }
}
