# 内容注册矩阵

通过 RitsuLib 注册所有内容类型的完整参考。

## 三种等效注册路径

1. **流式构建器** - `CreateContentPack(...)` 构建器方法
2. **注册器** - `RitsuLibFramework.GetContentRegistry(modId)` 方法
3. **Manifest 条目** - `IContentRegistrationEntry` 类型

## 内容类型

| 内容 | 流式构建器 | 注册器 | Manifest 条目 |
|------|-----------|--------|--------------|
| 角色 | `.Character<T>()` | `RegisterCharacter<T>()` | `CharacterRegistrationEntry<T>` |
| 幕 | `.Act<T>()` | `RegisterAct<T>()` | `ActRegistrationEntry<T>` |
| 卡池中的卡牌 | `.Card<TPool,TCard>()` | `RegisterCard<TPool,TCard>()` | `CardRegistrationEntry<TPool,TCard>` |
| 遗物池中的遗物 | `.Relic<TPool,TRelic>()` | `RegisterRelic<TPool,TRelic>()` | `RelicRegistrationEntry<TPool,TRelic>` |
| 药水池中的药水 | `.Potion<TPool,TPotion>()` | `RegisterPotion<TPool,TPotion>()` | `PotionRegistrationEntry<TPool,TPotion>` |
| 能力 | `.Power<T>()` | `RegisterPower<T>()` | `PowerRegistrationEntry<T>` |
| 球体 | `.Orb<T>()` | `RegisterOrb<T>()` | `OrbRegistrationEntry<T>` |
| 附魔 | `.Enchantment<T>()` | `RegisterEnchantment<T>()` | `EnchantmentRegistrationEntry<T>` |
| 苦痛 | `.Affliction<T>()` | `RegisterAffliction<T>()` | `AfflictionRegistrationEntry<T>` |
| 成就 | `.Achievement<T>()` | `RegisterAchievement<T>()` | `AchievementRegistrationEntry<T>` |
| 单例 | `.Singleton<T>()` | `RegisterSingleton<T>()` | `SingletonRegistrationEntry<T>` |
| 每日修饰符（正面） | `.GoodModifier<T>()` | `RegisterGoodModifier<T>()` | `GoodModifierRegistrationEntry<T>` |
| 每日修饰符（负面） | `.BadModifier<T>()` | `RegisterBadModifier<T>()` | `BadModifierRegistrationEntry<T>` |
| 共享卡池 | `.SharedCardPool<T>()` | `RegisterSharedCardPool<T>()` | `SharedCardPoolRegistrationEntry<T>` |
| 共享遗物池 | `.SharedRelicPool<T>()` | `RegisterSharedRelicPool<T>()` | `SharedRelicPoolRegistrationEntry<T>` |
| 共享药水池 | `.SharedPotionPool<T>()` | `RegisterSharedPotionPool<T>()` | `SharedPotionPoolRegistrationEntry<T>` |
| 共享事件 | `.SharedEvent<T>()` | `RegisterSharedEvent<T>()` | `SharedEventRegistrationEntry<T>` |
| 幕遭遇 | `.ActEncounter<TAct,TEncounter>()` | `RegisterActEncounter<TAct,TEncounter>()` | `ActEncounterRegistrationEntry<TAct,TEncounter>` |
| 幕事件 | `.ActEvent<TAct,TEvent>()` | `RegisterActEvent<TAct,TEvent>()` | `ActEventRegistrationEntry<TAct,TEvent>` |
| 共享 Ancient | `.SharedAncient<T>()` | `RegisterSharedAncient<T>()` | `SharedAncientRegistrationEntry<T>` |
| 幕 Ancient | `.ActAncient<TAct,TAnc>()` | `RegisterActAncient<TAct,TAnc>()` | `ActAncientRegistrationEntry<TAct,TAnc>` |
| 怪物 | （无流式构建器） | `RegisterMonster<T>()` | `MonsterRegistrationEntry<T>` |
| 占位卡牌 | `.PlaceholderCard<TPool>(...)` | `RegisterPlaceholderCard<TPool>(...)` | `PlaceholderCardRegistrationEntry<...>` |
| 占位遗物 | `.PlaceholderRelic<TPool>(...)` | `RegisterPlaceholderRelic<TPool>(...)` | `PlaceholderRelicRegistrationEntry<...>` |
| 占位药水 | `.PlaceholderPotion<TPool>(...)` | `RegisterPlaceholderPotion<TPool>(...)` | `PlaceholderPotionRegistrationEntry<...>` |

## 生成的占位符

无需编写完整 CLR 类型即可获得稳定的 ModelId：

```csharp
ctx.Content.RegisterPlaceholderCard<MyCardPool>("wip_reward_attack",
    new PlaceholderCardDescriptor(
        BaseCost: 1,
        Type: CardType.Attack,
        Rarity: CardRarity.Common,
        Target: TargetType.AnyEnemy));
```

**注意：**
- 条目一旦进入存档/解锁即成为长期契约
- 无游戏效果（空的 OnPlay/OnUse）
- 缺失的翻译会显示原始键
- 不同的 Mod 集合 -> 不同的哈希 -> 多人游戏不兼容

## 附加注册器

- `ModCardPileRegistry.For(modId)` - Mod 拥有的卡牌堆
- `ModTopBarButtonRegistry.For(modId)` - Mod 拥有的顶栏按钮
- `ModKeywordRegistry` - 关键词定义
- `ModTimelineRegistry` - Story/Epoch 注册
- `ModUnlockRegistry` - 解锁规则

## 基于属性的注册（可选）

`STS2RitsuLib.Interop.AutoRegistration` 中的 CLR 属性（如 `[RegisterSharedCardPool]`、`[RegisterCard(typeof(MyPool))]`）：

- 需要 `ModTypeDiscoveryHub.RegisterModAssembly(modId, Assembly.GetExecutingAssembly())`
- `AutoRegistrationAttribute.Inherit` 默认为 `false`
- 抽象基类被跳过；仅注册具体类型
