// TestComboCard - 连携 (Combo) 属性示例卡牌
// 2费攻击牌，单目标，无色池
// 效果：造成 5(7) 点伤害
// 连携 7：本场战斗出牌 ≥7 张后，此卡额外造成 10 点伤害
//
// 〖使用 Combo 属性的步骤〗
// 1. 让卡牌实现 IComboCard 接口
// 2. 在 CanonicalVars 中添加 ComboProgress(0) 和 ComboThreshold(阈值)
// 3. 实现 RefreshComboDisplay() 更新 DynamicVar
// 4. 在 OnPlay() 中通过 ComboTracker.CardsPlayedThisCombat 检查并应用效果
// 5. 在本地化描述中使用 {ComboProgress}/{ComboThreshold} 显示进度
// 6. 连携进度由 ComboTracker 自动实时刷新，无需额外操作

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using PersonalMod.Debu999PersonalModCode.Extensions;
using PersonalMod.PersonalModCode.CardKeywords;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class TestComboCard : ModCardTemplate, IComboCard
{
    // ===== 卡牌基础属性 =====
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Attack;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType Target = TargetType.AnyEnemy;

    // ===== 连携配置（可自由重写） =====
    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Combo];
    
    public virtual int ComboThreshold => 7;

    /// <summary>
    /// IComboCard 接口的显式实现，供 ComboDamageVar 自动读取。
    /// </summary>
    int IComboCard.ComboBonus => ComboBonusDamage;

    /// <summary>
    /// 连携附加伤害（protected，便于子类 override）
    /// </summary>
    protected virtual int ComboBonusDamage => 10;

    public TestComboCard()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    /// <summary>
    /// 动态变量：
    /// - ComboDamageVar → {Damage:diff()} 自动显示含连携奖励的总伤害
    /// - ComboProgress → 当前出牌数（ComboTracker 自动更新）
    /// - ComboThreshold → 阈值
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ComboDamageVar(9, ValueProp.Move),  // {Damage}预览时自动加连携奖励→显示总伤害
        new DynamicVar("ComboProgress", 0),
        new DynamicVar("ComboThreshold", ComboThreshold),
        new DynamicVar("ComboBonus", ComboBonusDamage),  // 连携额外伤害（用于说明文字）
    ];

    public void RefreshComboDisplay(int cardsPlayedThisCombat)
    {
        DynamicVars["ComboProgress"].BaseValue = cardsPlayedThisCombat;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        RefreshComboDisplay(ComboTracker.CardsPlayedThisCombat);

        var totalDamage = (int)DynamicVars.Damage.BaseValue;
        if (ComboTracker.CardsPlayedThisCombat >= ComboThreshold)
        {
            totalDamage += ComboBonusDamage;
        }

        _ = await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
