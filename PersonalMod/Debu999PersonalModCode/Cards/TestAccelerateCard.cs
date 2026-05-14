// TestAccelerateCard - 激奏 (Accelerate) 示例卡牌
// 2费攻击牌，单目标，无色池
// 基础效果：造成 19 点伤害
// 激奏1：改为获得 9 点格挡（当作技能牌）
// 能量≥2时正常打出，能量≤1时自动切换为激奏模式

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
public class TestAccelerateCard : ModCardTemplate, IAccelerateCard
{
    // ===== 激奏配置 =====
    public int BaseCost => 2;
    public int AccelCost => 1;
    public bool IsAccelerateMode { get; set; }  // AccelerateTracker 自动管理

    private const int EnergyCost = 2;
    private const int CardDamage = 19;
    private const int AccelBlockAmount = 9;
    private const CardType CardTypeValue = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Token;
    private const TargetType Target = TargetType.AnyEnemy;

    /// <summary>
    /// 激奏时卡牌类型变为 Skill，让其他效果正确识别。
    /// </summary>
    public override CardType Type => IsAccelerateMode ? CardType.Skill : CardTypeValue;

    public override TargetType TargetType => IsAccelerateMode ? TargetType.Self : Target;

    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Accelerate];


    public TestAccelerateCard()
        : base(EnergyCost, CardTypeValue, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(CardDamage, ValueProp.Move),
        new DynamicVar("AccelBlock", AccelBlockAmount),
        new DynamicVar("AccelCost", AccelCost)
    ];
    
    

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsAccelerateMode&&!cardPlay.IsAutoPlay)
        {
            // 激奏模式：获得格挡（当作技能牌效果）
            IsAccelerateMode = false;
            _ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["AccelBlock"].BaseValue, ValueProp.Unpowered, cardPlay);
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
        DynamicVars.Damage.UpgradeValueBy(5);
        DynamicVars["AccelBlock"].UpgradeValueBy(3);
    }
}
