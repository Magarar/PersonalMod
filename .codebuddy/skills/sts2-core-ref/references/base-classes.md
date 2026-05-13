# STS2 Core 基类参考

源码位置: `{{STS2_SOURCE_ROOT}}`

## 1. AbstractModel (所有游戏数据根基类)

**文件**: `Models/AbstractModel.cs`

所有游戏对象的最终基类。每个具体游戏内容（卡牌、能力、遗物、怪物等）都继承自此类。

```
命名空间: MegaCrit.Sts2.Core.Models
```

**关键成员**:
- `ModelId Id` — 模型唯一标识 (自动从类名生成)
- `bool IsMutable` — 是否为可变副本
- `bool IsCanonical` — 是否为规范实例
- `AbstractModel MutableClone()` — 创建可变副本
- `event Action<AbstractModel> ExecutionFinished`

**模型注册**: 所有 `AbstractModel` 子类在构造时会自动检查 `ModelDb`，防止重复注册。

---

## 2. CardModel (卡牌基类)

**文件**: `Models/CardModel.cs`

```
命名空间: MegaCrit.Sts2.Core.Models
构造函数: CardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
```

**必须/推荐 override 的成员**:

| 成员 | 类型 | 说明 |
|------|------|------|
| `OnPlay(PlayerChoiceContext, CardPlay)` | `virtual Task` | 卡牌打出时的核心逻辑 |
| `OnUpgrade()` | `virtual void` | 升级效果 |
| `CanonicalTags` | `virtual HashSet<CardTag>` | 卡牌标签 (Strike, Defend 等) |
| `CanonicalVars` | `virtual IEnumerable<DynamicVar>` | 动态变量 (伤害、格挡等数值) |
| `GainsBlock` | `virtual bool` | 是否获得格挡 |
| `Pool` | `virtual CardPoolModel` | 所属卡池 |
| `TargetType` | `virtual TargetType` | 目标类型 |

**重要属性**:
- `LocString TitleLocString` — 标题本地化 (`cards/{Entry}.title`)
- `LocString Description` — 描述本地化 (`cards/{Entry}.description`)
- `string PortraitPath` — 肖像路径 (自动从 Pool + Entry 生成)
- `int CurrentUpgradeLevel` / `int MaxUpgradeLevel`
- `bool IsUpgraded`

**事件**:
- `Played`, `Drawn`, `Upgraded`, `Forged`, `Exhausted`, `Discarded`
- `EnergyCostChanged`, `AfflictionChanged`, `EnchantmentChanged`

**资源路径约定**:
- 肖像: `atlases/card_atlas.sprites/{pool_title}/{entry}.tres`
- PNG: `packed/card_portraits/{pool_title}/{entry}.png`

**示例 — DefendIronclad**:
```csharp
namespace MegaCrit.Sts2.Core.Models.Cards
{
    public sealed class DefendIronclad : CardModel
    {
        public DefendIronclad() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true) { }

        public override bool GainsBlock => true;

        protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

        protected override IEnumerable<DynamicVar> CanonicalVars
            => new[] { new BlockVar(5m, ValueProp.Move) };

        protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 实际逻辑被反编译为 async state machine
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
        }
    }
}
```

---

## 3. PowerModel (能力基类)

**文件**: `Models/PowerModel.cs`

```
命名空间: MegaCrit.Sts2.Core.Models
无构造参数 (使用默认)
```

**必须 override**:
| 成员 | 类型 | 说明 |
|------|------|------|
| `Type` | `abstract PowerType` | Buff / Debuff |
| `StackType` | `abstract PowerStackType` | Counter / Intensity / Duration |

**推荐 override**:
| 成员 | 类型 | 说明 |
|------|------|------|
| `CanonicalVars` | `virtual IEnumerable<DynamicVar>` | 动态变量 |
| `AllowNegative` | `virtual bool` | 是否允许负值 |
| `ModifyDamageAdditive(Creature, decimal, ValueProp, Creature, CardModel)` | `virtual decimal` | 修改伤害 (加法) |
| `ModifyDamageMultiplicative(Creature, decimal, ValueProp, Creature, CardModel)` | `virtual decimal` | 修改伤害 (乘法) |
| `ModifyBlockAdditive(Creature, decimal, ValueProp, CardModel, CardPlay)` | `virtual decimal` | 修改格挡 |
| `BeforeApplied` / `AfterApplied` | `virtual Task` | 应用前/后回调 |
| `AfterRemoved` | `virtual Task` | 移除后回调 |

**重要属性**:
- `int Amount` — 能力层数/数值
- `int AmountOnTurnStart` — 回合开始时的数值
- `Creature Owner` — 拥有此能力的生物
- `Creature Target` — 目标生物 (用于指向性能力)
- `Creature Applier` — 施加者
- `ICombatState CombatState` — 当前战斗状态
- `LocString Title` (`powers/{Entry}.title`), `LocString Description` (`powers/{Entry}.description`)

**资源路径约定**:
- 图标: `atlases/power_atlas.sprites/{entry}.tres`
- 大图标: `powers/{entry}.png`

**示例 — StrengthPower**:
```csharp
namespace MegaCrit.Sts2.Core.Models.Powers
{
    public sealed class StrengthPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => true;

        public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
        {
            if (Owner != dealer) return 0m;
            if (!props.IsPoweredAttack()) return 0m;
            return Amount;
        }
    }
}
```

---

## 4. RelicModel (遗物基类)

**文件**: `Models/RelicModel.cs`

```
命名空间: MegaCrit.Sts2.Core.Models
无构造参数
```

**必须 override**:
| 成员 | 类型 | 说明 |
|------|------|------|
| `Rarity` | `abstract RelicRarity` | Starter / Common / Uncommon / Rare / Boss / Special |

**推荐 override**:
| 成员 | 类型 | 说明 |
|------|------|------|
| `CanonicalVars` | `virtual IEnumerable<DynamicVar>` | 动态变量 |
| `IsAllowedInShops` | `virtual bool` | 是否允许出现在商店 |
| `IsStackable` | `virtual bool` | 是否可堆叠 |
| `ShowCounter` | `virtual bool` | 是否显示计数器 |
| `IsAllowed(IRunState)` | `virtual bool` | 是否允许在当前跑酷中 |
| `AfterObtained()` | `virtual Task` | 获得后回调 |
| `AfterRemoved()` | `virtual Task` | 移除后回调 |

**重要属性**:
- `LocString Title` (`relics/{Entry}.title`)
- `LocString Flavor` (`relics/{Entry}.flavor`)
- `string PackedIconPath` — 图标路径
- `virtual string IconBaseName` — 图标基础名 (默认: Entry.ToLowerInvariant())

**资源路径约定**:
- 图标: `atlases/relic_atlas.sprites/{iconbasename}.tres`
- 大图标: `relics/{iconbasename}.png`
- 轮廓: `relics/{iconbasename}_outline.png`

**示例 — BurningBlood**:
```csharp
namespace MegaCrit.Sts2.Core.Models.Relics
{
    public sealed class BurningBlood : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        protected override IEnumerable<DynamicVar> CanonicalVars
            => new[] { new HealVar(6m) };

        public override Task AfterCombatVictory(CombatRoom _)
        {
            // 战斗胜利后回血逻辑
        }
    }
}
```

---

## 5. MonsterModel (怪物基类)

**文件**: `Models/MonsterModel.cs`

```
命名空间: MegaCrit.Sts2.Core.Models
```

**子类目录**: `Models/Monsters/` (121 个文件)

怪物包含 AI 状态机 (`MonsterMoveStateMachine`)、意图系统 (`Intents`)。

---

## 6. Creature (运行时生物实体)

**文件**: `Entities/Creatures/Creature.cs`

```
命名空间: MegaCrit.Sts2.Core.Entities.Creatures
```

**不是 AbstractModel 子类**。运行时实例，包含 HP、格挡、能力列表、意图等状态。

- `Player` 和 `Monster` 都继承自 `Creature`
- HP: `CurrentHp`, `MaxHp`
- 格挡: `Block`
- 能力: `Powers` (IReadOnlyList<PowerModel>)
- 意图: `Intent`

---

## 7. Player (玩家运行时实例)

**文件**: `Entities/Players/Player.cs`

```
命名空间: MegaCrit.Sts2.Core.Entities.Players
继承: Creature
```

管理手牌、能量、金币、遗物、药水、卡堆等。

---

## 8. 其他重要模型

| 基类 | 文件 | 子类数量 |
|------|------|---------|
| `PotionModel` | `Models/PotionModel.cs` | 64 |
| `EventModel` | `Models/EventModel.cs` | 68 |
| `EncounterModel` | `Models/EncounterModel.cs` | ~90 |
| `CharacterModel` | `Models/CharacterModel.cs` | 9 |
| `OrbModel` | `Models/OrbModel.cs` | 5 |
| `AfflictionModel` | `Models/AfflictionModel.cs` | 7 |
| `EnchantmentModel` | `Models/EnchantmentModel.cs` | ~30 |
| `ModifierModel` | `Models/ModifierModel.cs` | 16 |
| `ActModel` | `Models/ActModel.cs` | 6 |
