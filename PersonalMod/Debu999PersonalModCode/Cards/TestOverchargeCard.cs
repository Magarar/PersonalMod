// TestOverchargeCard - 爆能强化 (Overcharge) 示例卡牌
// 1费攻击牌，单目标，无色池
// 基础效果：造成 9 点伤害
// 爆能强化2：改为造成 21 点伤害
// 能量≥2时UI自动显示费用为2，预览伤害变为21

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
public class TestOverchargeCard : ModCardTemplate, IOverchargeCard
{
    // ===== 爆能配置 =====
    public int BaseCost => 1;
    public int PlusedCost => 2;
    public int PlusedDamage => 21;
    public bool IsOverchargedMode { get; set; }  // OverchargeTracker 自动管理
    
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Overcharge];


    private const int EnergyCost = 1;
    private const int CardDamage = 9;
    private const CardType Type = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Token;
    private const TargetType Target = TargetType.AnyEnemy;

    public TestOverchargeCard()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new OverchargeDamageVar(CardDamage, ValueProp.Move),
        new DynamicVar("PlusedCost", PlusedCost),
        new DynamicVar("PlusedDamage", PlusedDamage),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        int damage = IsOverchargedMode ? PlusedDamage : CardDamage;
        IsOverchargedMode = false;  // 重置状态

        _ = await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["PlusedDamage"].UpgradeValueBy(5);
    }
}
