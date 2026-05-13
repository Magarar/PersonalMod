// EmergencyAvoidance - 技能牌
// 花费：1  |  稀有度：罕见  |  目标：自身
// 效果：获得 10(15) 点格挡
// 回合开始时（抽完牌后），如果有怪物的意图为攻击且此牌不处于消耗堆，则将此牌放入你的手牌

using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace PersonalMod.Debu999PersonalModCode.Cards;

public class EmergencyAvoidance : ModCardTemplate
{
    private const int EnergyCost = 1;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Uncommon;
    private const TargetType Target = TargetType.Self;

    // 静态生命周期订阅，确保战斗开始时注册钩子
    static EmergencyAvoidance()
    {
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side != CombatSide.Player)
                return;

            var combatState = evt.CombatState;
            if (combatState?.Players == null || combatState.Players.Count == 0)
                return;

            // 检查是否有存活的怪物处于攻击意图
            bool anyMonsterAttacking = combatState.Enemies
                .Where(c => c.IsAlive)
                .Any(c => c.Monster?.NextMove?.Intents?
                    .Any(intent => intent.IntentType == IntentType.Attack) == true);

            if (!anyMonsterAttacking)
                return;

            var player = combatState.Players[0];

            // 遍历所有非消耗堆，找出 EmergencyAvoidance 卡牌
            var pilesToCheck = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
            foreach (var pileType in pilesToCheck)
            {
                var pile = pileType.GetPile(player);
                if (pile == null) continue;

                foreach (var card in pile.Cards.ToList())
                {
                    if (card is EmergencyAvoidance && pileType != PileType.Hand)
                    {
                        // 异步将卡牌移动到手中（fire-and-forget）
                        _ = CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, card);
                    }
                }
            }
        });
    }

    public EmergencyAvoidance()
        : base(EnergyCost, Type, Rarity, Target)
    {
    }

    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://Debu999PersonalMod/images/card_portraits/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 不自动消耗此牌，让它回到弃牌堆以便后续回合循环
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5);
    }
}
