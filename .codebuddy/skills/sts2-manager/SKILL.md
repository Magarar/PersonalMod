---
name: sts2-manager
description: >-
  STS2 Mod 开发总调度入口。支持两种调用方式：
  (1) 自然语言：直接描述你要创建的内容；
  (2) 快捷命令：输入 /card /relic /power 等直接进入对应子 Skill 工作流。
  它会根据你的需求自动判断任务类型，调度对应的子 Skill 完成具体工作，
  并在完成后执行自检。
auto_trigger: true
trigger_priority: 0
---

# STS2 Mod 开发总调度 Skill

## 一、概述

本 Skill 是 STS2 Mod 开发的**总入口**。支持**两种调用方式**：

### 方式一：快捷命令（推荐）

直接输入 `/命令` 进入对应内容的创建/修改流程，省去自然语言解析步骤：

```
/card     → 创建一张卡牌
/relic    → 创建一个遗物
/power    → 创建一个能力
/potion   → 创建一个药水
/event    → 创建一个事件
/monster  → 创建一个怪物
/character → 创建一个人物
/orb      → 创建一个充能球
/enchantment → 创建一个附魔
/ancient  → 创建一个先古之民
/epoch    → 创建一个纪元/解锁
/keyword  → 注册卡牌关键词/Tag
/audio    → 添加音效
/harmony  → 编写 Harmony 补丁
/singleton → 创建单例模型
/utils    → 查 ritsulib-utils 小工具
/ref      → 查游戏 API/Hook/枚举（sts2-core-ref）
/resource → 查源工程资源路径（sts2-resources）
/console  → 查控制台调试指令
```

**示例**:
- `/card 创建一个 FireSlash，1费攻击牌，造成12点伤害，无色池`
- `/relic 创建一个 BurningHeart，战斗胜利回6血，共享池`
- `/power 创建一个 DrawStrengthPower，抽牌获得力量`

### 方式二：自然语言

直接描述需求，本 Skill 自动识别内容类型并路由：

```
"帮我写一张卡牌 FireSlash..."
"创建一个遗物，每回合开始抽1张牌..."
"给上一个创建的遗物加音效..."
```

---

当你说"我要写一个XXX"时，本 Skill 会：

1. **识别内容类型** — 判断你要创建/修改什么（卡牌、能力、遗物、药水等）
2. **创建文件和文件夹** — 按项目规范创建 C# 脚本文件和资源文件夹
3. **处理资源图片** — 如有图片，调用 `sts2-image-resizer` 缩放裁切后放入规范目录，并提示 `.import` 文件
4. **调度子 Skill** — 调用对应的子 Skill 完成具体编写工作
5. **参考资源** — 需要参考源工程资源时，查 `sts2-resources`；需要 API 参考时，查 `sts2-core-ref`；需要 Harmony 补丁时，查 `harmony`
6. **编译验证** — 执行 `read_lints` 或 `dotnet build` 检查语法错误，**编译不通过则不会继续**
7. **自检** — 完成后按审查清单逐项检查
8. **纳入版本控制** — 将新建/修改的文件加入 Git 跟踪
9. **中断报告** — 如果某一步无法继续，会明确告知卡在哪一步，由你决定如何处理

**{{MODID}}** = `Debu999PersonalMod`（PascalCase，用于命名空间、路径、代码中）
**{{MODID_UPPER}}** = `DEBU999_PERSONAL_MOD`（UPPER_SNAKE_CASE，用于 ModelId.Entry 和本地化键,不要去除字符串的下划线）
**项目根目录**: `d:\杀戮尖塔2Mod\PersonalMod\`

---

## 二、全局配置与 ModId 约定

本 Skill 定义以下模板变量，所有子 Skill 统一使用：

| 变量 | 当前值 | 用途 |
|------|--------|------|
| `{{MODID}}` | `PersonalMod` | C# 命名空间前缀、Godot 资源路径 (`res://{{MODID}}/`)、物理目录名 |
| `{{MODID_UPPER}}` | `PERSONALMOD` | ModelId.Entry 前缀（UPPER_SNAKE_CASE）、本地化键前缀 |

> **移植到其他项目时**只需在此处修改这两个变量的值，所有子 Skill 自动适配。

---

## 三、命令系统与内容类型路由

### 2.1 命令速查表

| 命令 | 类型 | 调度子 Skill | 常用参数 | 示例 |
|------|------|-------------|---------|------|
| `/card` 或 `/c` | 卡牌 | `sts2-card-skill` | 类名、费用、类型、稀有度、目标、卡池、图片 | `/card FireSlash 1 Attack Common SingleEnemy Colorless img=fire.png` |
| `/relic` 或 `/r` | 遗物 | `sts2-relic-skill` | 类名、稀有度、效果、池、图片 | `/relic BurningHeart Rare "战斗胜利回6血" Shared img=heart.png` |
| `/power` 或 `/p` | 能力 | `sts2-power-skill` | 类名、Buff/Debuff、Counter/Duration、效果、图片 | `/power DrawStrength Buff Counter "抽牌时获得力量" img=draw.png` |
| `/potion` | 药水 | `sts2-potion-skill` | 类名、稀有度、使用时机、目标、效果、图片 | `/potion FirePotion Common CombatOnly SingleEnemy "造成15点伤害" img=fire.png` |
| `/event` 或 `/e` | 事件 | `sts2-event-skill` | 类名、所属幕、选项、图片 | `/event TestEvent Glory 2选项 img=bg.png` |
| `/monster` 或 `/m` | 怪物 | `sts2-monster-skill` | 类名、HP范围、AI、幕 | `/monster TestMonster 30-40 循环 1幕` |
| `/character` | 人物 | `sts2-character-skill` | 类名、HP、金币、能量 | `/character MyCharacter 70 100 3` |
| `/orb` | 充能球 | `sts2-orb-skill` | 类名、被动值、激发值 | `/orb TestOrb passive=3 evoke=8` |
| `/enchant` | 附魔 | `sts2-enchantment-skill` | 类名、效果 | `/enchant SharpEnchant "伤害+3"` |
| `/ancient` | 先古之民 | `sts2-ancient-skill` | 类名、幕 | `/ancient TestAncient Glory` |
| `/epoch` | 纪元 | `sts2-epoch-skill` | 类名、解锁类型 | `/epoch MyEpoch CharacterUnlock` |
| `/keyword` | 关键词/Tag | `sts2-keyword-skill` | 关键词名、图标 | `/keyword Unique icon=unique.svg` |
| `/audio` | 音效 | `sts2-audio` | 音效事件路径 | `/audio block_gain` |
| `/harmony` | Harmony 补丁 | `harmony` | 目标类、目标方法 | `/harmony TargetClass TargetMethod` |
| `/singleton` | 单例模型 | `sts2-singleton-model-skill` | 类名 | `/singleton MySystem` |
| `/utils` | 查小工具 | `sts2-ritsulib-utils` | 功能描述 | `/utils 手牌上限` |
| `/ref` | 查 API/枚举/Hook | `sts2-core-ref` | 查询内容 | `/ref Hook AfterCardPlayed` |
| `/resource` | 查资源路径 | `sts2-resources` | 资源类型 | `/resource card_portraits` |
| `/console` | 查控制台指令 | `sts2-console` | 指令查询 | `/console card` |
| `/help` | 显示本命令表 | — | — | `/help` |

> **短别名**: `/c` = `/card`, `/r` = `/relic`, `/p` = `/power`, `/e` = `/event`, `/m` = `/monster`

### 2.2 命令与参数格式

命令采用**前缀匹配**，你不需要写完整的命令格式，只要以 `/命令` 开头即可：

```
/card 创建一个 FireSlash，1费攻击牌，造成12点伤害，无色池
```

系统识别 `/card` 后，提取剩余文本中的关键信息。如果信息不全，会**主动提问**补充缺失项。

缺失信息的处理方式：
- 有参但缺信息 → 逐一提问（类名？费用？卡牌类型？...）
- 无参（仅 `/card`）→ 依次询问所有必要参数
- 错误命令（如 `/abc`）→ 提示未知命令，展示 `/help` 表

### 2.3 自然语言内容类型速查

当不使用命令前缀时，根据描述自动判断：

| 你想创建/修改 | 类型键 | 调度子 Skill | 需要额外什么 |
|-------------|--------|-------------|------------|
| 卡牌（攻击/技能/能力牌） | `card` | `sts2-card-skill` | 卡图 PNG、卡池名 |
| 能力（战斗中的 Buff/Debuff） | `power` | `sts2-power-skill` | 图标 PNG(64x64) |
| 遗物（可收集的装备） | `relic` | `sts2-relic-skill` | 图标 PNG(85x85)、轮廓 PNG |
| 药水（战斗消耗品） | `potion` | `sts2-potion-skill` | 本体 PNG、轮廓 PNG |
| 事件（房间里的选择事件） | `event` | `sts2-event-skill` | 背景插图 PNG |
| 怪物（战斗中的敌人） | `monster` | `sts2-monster-skill` | 视觉场景(.tscn)、贴图 |
| 人物（可玩角色） | `character` | `sts2-character-skill` | 大量场景/图标/音效 |
| 充能球（角色专属 Orb） | `orb` | `sts2-orb-skill` | 图标、视觉场景 |
| 附魔（卡牌额外效果） | `enchantment` | `sts2-enchantment-skill` | 图标 PNG |
| 先古之民（Ancient 事件角色） | `ancient` | `sts2-ancient-skill` | 背景场景配置 |
| 纪元/剧情解锁 | `epoch` | `sts2-epoch-skill` | — |
| 卡牌关键词/Tag/属性 | `keyword` | `sts2-keyword-skill` | 关键词图标(可选) |

### 2.4 通用功能路由

| 你的需求 | 路由至 | 说明 |
|---------|--------|------|
| 编写/修改本地化文本 | `sts2-localization` | 不单独调度，在每种内容创建时顺带完成 |
| 添加音效 | `sts2-audio` | 卡牌/能力/遗物/怪物的音效附加 |
| 需要控制台调试指令 | `sts2-console` | 查询调试命令 |
| 需要小工具功能 | `sts2-ritsulib-utils` | **优先检查！** 手牌上限/泛光/血条覆盖/数据保存等 |
| 需要全局系统/中间件 | `sts2-singleton-model-skill` | 不属于上述任何类型的新功能 |
| 需要用 Harmony 打补丁 | `harmony` | 修改原版游戏逻辑 |
| 需要查游戏 API/Hook/枚举 | `sts2-core-ref` | 参考 Skill，不直接调度 |

---

## 三、标准工作流程

### 3.1 通用创建流程

```
用户输入: "创建一张卡牌XXX，效果是XXX，图片在YYY"
    │
    ▼
┌─ Step 1: 识别内容类型 ─────────────────────────────┐
│ 从描述中判断: card / power / relic / potion / ...  │
│ 提取: 类名(如 TestCard)、效果描述、图片路径、卡池    │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 2: 检查 ritsulib-utils ──────────────────────┐
│ 如果功能较小（手牌上限/泛光/血条覆盖/数据保存等）， │
│ 先查 sts2-ritsulib-utils 是否已有现成方案           │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 3: 创建文件和目录 ───────────────────────────┐
│ 根据类型键，创建对应的 C# 脚本文件、资源目录         │
│ (详见 §4)                                           │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 4: 更新 .csproj 索引 ───────────────────────┐
│ 将新建的文件加入项目，确保 IDE 解决方案能看见       │
│ (详见 §4.5)                                         │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 5: 处理资源图片 ─────────────────────────────┐
│ 如有用户提供的图片路径:                               │
│ 1. 确认图片存在                                      │
│ 2. 调用 sts2-image-resizer (resize_image.py) 缩放    │
│ 3. 输出到规范目录（如 images/card_portraits/）       │
│ 4. 命名与类名一致 (TestCard.png)                    │
│ 5. 检查/生成 Godot .import 文件（§4.4）             │
│ 如果没有图片：                                       │
│ └→ 参考 sts2-resources 源工程中的类似资源            │
│    创建占位文件或临时引用源工程的资源                 │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 6: 调度子 Skill ────────────────────────────┐
│ 调用对应的子 Skill 编写核心逻辑                      │
│ (如 card → sts2-card-skill)                        │
│ 子 Skill 会负责:                                    │
│ ├─ 生成 C# 代码（继承基类、实现逻辑）               │
│ ├─ 本地化 JSON 条目                                 │
│ └─ 代码审查清单                                     │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 7: 编译验证 ────────────────────────────────┐
│ 调用 dotnet build 或 read_lints 检查语法错误        │
│ ❌ 有编译错误 → 修复后重新验证                      │
│ ✅ 编译通过 → 继续                                 │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 8: 自检清单 ────────────────────────────────┐
│ 逐项执行 §5 的通用自检清单                          │
│ 同时执行子 Skill 中的审查清单                       │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 9: 纳入版本控制 ────────────────────────────┐
│ 将创建/修改的所有文件加入 Git                       │
│ git add <所有新文件/修改文件>                       │
└─────────────────────────┬──────────────────────────┘
                          ▼
┌─ Step 10: 报告完成 ─────────────────────────────┐
│ 列出所有创建/修改的文件、资源路径、关键决策点       │
│ 以及控制台调试命令                                 │
└─────────────────────────────────────────────────┘
```

### 3.2 修改已有内容的流程

```
用户输入: "修改遗物XXX，让它变成每回合抽2张牌"
    │
    ▼
┌─ 识别内容类型 → relic ──────────────────────────┐
└────────────────────────┬────────────────────────┘
                         ▼
┌─ 调度 sts2-relic-skill ─────────────────────────┐
│ 子 Skill 负责修改代码逻辑、更新本地化、自检        │
└─────────────────────────────────────────────────┘
```

---

## 四、文件创建与资源整理规范

### 4.1 各内容类型的文件结构

#### 卡牌 (card)
```
# C# 文件
{{MODID}}Code/Cards/<ClassName>.cs

# 资源文件
{{MODID}}/images/card_portraits/<ClassName>.png

# 本地化
{{MODID}}/localization/eng/cards.json  (添加条目)
{{MODID}}/localization/zhs/cards.json  (添加条目)
```

#### 能力 (power)
```
# C# 文件
{{MODID}}Code/Powers/<ClassName>.cs

# 资源文件
{{MODID}}/images/powers/<ClassName>.png        (64x64)
{{MODID}}/images/powers/big/<ClassName>.png     (256x256, 可选)

# 本地化
{{MODID}}/localization/eng/powers.json
{{MODID}}/localization/zhs/powers.json
```

#### 遗物 (relic)
```
# C# 文件
{{MODID}}Code/Relics/<ClassName>.cs

# 资源文件
{{MODID}}/images/relics/<ClassName>.png              (85x85)
{{MODID}}/images/relics/<ClassName>_outline.png       (85x85)
{{MODID}}/images/relics/big/<ClassName>.png           (256x256, 可选)

# 本地化
{{MODID}}/localization/eng/relics.json
{{MODID}}/localization/zhs/relics.json
```

#### 药水 (potion)
```
# C# 文件
{{MODID}}Code/Potions/<ClassName>.cs

# 资源文件
{{MODID}}/images/potions/<ClassName>.png              (64x96)
{{MODID}}/images/potions/<ClassName>_outline.png       (64x96)

# 本地化
{{MODID}}/localization/eng/potions.json
{{MODID}}/localization/zhs/potions.json
```

#### 事件 (event)
```
# C# 文件
{{MODID}}Code/Events/<ClassName>.cs

# 资源文件
{{MODID}}/images/events/<ClassName>.png

# 本地化
{{MODID}}/localization/eng/events.json
{{MODID}}/localization/zhs/events.json
```

#### 怪物 (monster)
```
# C# 文件
{{MODID}}Code/Monsters/<ClassName>.cs

# 遭遇文件
{{MODID}}Code/Encounters/<EncounterClass>.cs

# 场景
{{MODID}}/scenes/monsters/<ClassName>.tscn

# 本地化
{{MODID}}/localization/eng/monsters.json
{{MODID}}/localization/eng/encounters.json
{{MODID}}/localization/zhs/monsters.json
{{MODID}}/localization/zhs/encounters.json
```

### 4.2 图片处理规则

当用户提供了图片路径时，按以下步骤处理：

1. **检查图片存在** — 确认用户提供的路径下的文件可读取
2. **调用 image-resizer** — 使用以下命令格式：
   ```bash
   py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py <input_path> <type> --name <ClassName>
   ```
3. **类型对应表**（传给 image-resizer 的 type 参数）：

| 内容类型 | type 参数 | 需同时生成的类型 |
|---------|----------|-----------------|
| 卡牌 | `card` | 如果用户有 big 图 → `card_big` |
| 能力 | `power` | `power_big` |
| 遗物 | `relic`, `relic_outline` | `relic_big` |
| 药水 | `potion`, `potion_outline` | — |
| 先古卡牌 | `card_ancient` | — |

4. **验证输出** — 确认输出目录中存在对应的 `<ClassName>.png` 文件
5. **检查 Godot .import 文件** — 详见 §4.4

### 4.3 无图片时的资源参考

如果用户没有提供图片，应该：

1. 参考 `sts2-resources` 中源工程的**同类资源路径**（如卡牌参考原版卡图位置）
2. 在 C# 代码的 `AssetProfile` 中填写占位路径
3. 告知用户需要提供图片后才能正常运行

### 4.4 Godot 资源导入索引（.import 文件）

Godot 首次导入图片后会生成 `.import` 元数据文件，**没有 `.import` 文件游戏将无法识别资源**。有两种处理方式：

#### 方式 A：在 Godot 编辑器中打开（推荐）

首次在 Godot 编辑器打开项目后，Godot 会自动为 `PersonalMod/` 目录下的所有图片生成 `.import` 文件。这是最终需要的状态。

**因此在创建资源后，应告知用户**：
> "新图片已放置到目录，请在 Godot 编辑器中打开项目，Godot 会自动生成 `.import` 文件。"

#### 方式 B：手动创建 .import 文件

如果不方便打开 Godot，可以复制同目录下已有图片的 `.import` 文件并修改映射路径。

**查找参考**：参考 `sts2-resources` 中源工程的 `.import` 文件或项目中原有图片的 `.import` 文件。

```bash
# 例如：从已存在的 card.png 复制 .import
copy PersonalMod\images\card_portraits\card.png.import PersonalMod\images\card_portraits\NewCard.png.import
```

#### 在代码中引用资源的路径格式

C# 代码中的资源路径使用 `res://` 前缀指向 Godot 项目资源目录：

```csharp
// 卡牌
public override CardAssetProfile AssetProfile => new(
    PortraitPath: "res://{{MODID}}/images/card_portraits/TestCard.png"
);
```

> **重要**：`res://` 对应的物理路径是 `{{MODID}}/{{MODID}}/`（Godot 项目根下的 `{{MODID}}/` 目录）。

### 4.5 .csproj 项目文件索引

新建的 C# 脚本和资源文件必须加入 `.csproj` 项目文件，否则 IDE（Visual Studio / Rider）的解决方案资源管理器中看不到它们。

#### 方式 A：使用通配符模式（推荐，一劳永逸）

`Godot.NET.Sdk` 会自动包含项目目录下所有 `.cs` 文件，**不需要**手动添加 `Compile` 条目。只需要为资源文件添加通配符即可：

```xml
<!-- 自动包含所有 Mod 资源（图片/本地化/配置等） -->
<ItemGroup>
  <Content Include="PersonalMod\**\*" />
</ItemGroup>
```

> **注意**：不要添加 `<Compile Include=".../*.cs" />`，SDK 已默认包含所有 `.cs` 文件，加显式 Compile 会导致 `NETSDK1022` 重复项错误。

#### 方式 B：显式添加每个文件

如果不使用通配符，每次新建文件后需手动在 `.csproj` 中添加对应 `<Compile>` 或 `<Content>` 条目：

```xml
<!-- C# 文件 -->
<Compile Include="PersonalModCode\Cards\TestCard.cs" />

<!-- 图片文件 -->
<Content Include="PersonalMod\images\card_portraits\TestCard.png" />

<!-- 本地化文件 -->
<Content Include="PersonalMod\localization\eng\cards.json" />
```

项目文件位置：`PersonalMod/PersonalMod.csproj`

### 4.6 代码注释规范

生成的所有 C# 代码必须包含清晰的注释，参照原版反编译代码的注释风格：

#### 注释层级要求

| 层级 | 必须 | 说明 |
|------|------|------|
| **类注释** | ✓ | 简要说明类的用途，如 `// TestCard - 抽牌+伤害+格挡` |
| **CanonicalVars 行尾注释** | ✓ | 每个变量行尾标注含义，如 `new DamageVar(1, ValueProp.Move),  // 1点伤害` |
| **OnPlay 段落注释** | ✓ | 每个效果前一段注释，如 `// 抽牌`、`// 获得格挡`、`// 造成伤害` |
| **OnUpgrade 注释** | ✓ | 说明升级变化，如 `// 升级：伤害+2，格挡+2，抽牌+1` |
| **API 来源注释** | ✓ | 关键 API 调用注明参照来源，如 `// 参照 PommelStrike 反编译的抽牌模式` |

#### 注释检查点

```
□ 每个效果步骤前都有注释说明段
□ CanonicalVars 每个变量都有行尾注释
□ 关键 API 调用有注释注明参照来源
□ 升级方法说明了数值变化
□ 类上方有简要用途说明
□ 注释语言统一（全部中文或全部英文，不混用）
```

## 五、通用自检清单

在子 Skill 完成各自的审查清单后，还需额外检查以下全局项。

### 5.1 编译验证（必须通过才能继续）

> **这是最重要的一步！未通过编译的自检视为失败，必须修复后重试。**

自检工具：
- 使用 `read_lints` 工具检查 IDE 诊断信息
- 或执行 `dotnet build` 检查编译错误

```
dotnet build D:\杀戮尖塔2Mod\PersonalMod\PersonalMod\PersonalMod.csproj
```

| 检查项 | 工具 | 通过条件 |
|--------|------|---------|
| 语法/类型错误 | `read_lints(文件路径)` | 返回 0 个错误 |
| 缺少 using | `read_lints(文件路径)` | 返回 0 个错误 |
| 类型不存在/方法签名不匹配 | `read_lints(文件路径)` | 返回 0 个错误 |

**如果没有 linting 工具可用，至少人工检查**：
- [ ] 所有引用的类名拼写是否正确（`CreatureCmd` / `BlockCmd` / `DamageCmd` 等）
- [ ] 所有方法参数数量、类型是否与文档一致
- [ ] `using` 语句是否覆盖了所有使用的命名空间
- [ ] 所有花括号是否成对闭合

> 如果编译失败，**不得继续**。必须修复错误后再重新运行自检。

### 5.2 注释完整性检查

每处生成的代码必须按 §4.6 注释规范逐项检查：

- [ ] 类上方是否有用途注释？
- [ ] `CanonicalVars` 每个变量是否有行尾注释？
- [ ] `OnPlay` 中每个效果前是否有段落注释？
- [ ] `OnUpgrade` 是否说明了升级变化？
- [ ] 关键 API 调用是否注明了参照来源？
- [ ] 注释语言是否统一？

> 缺少注释的自检视为**不完整**，必须补充注释后再继续。

### 5.3 文件完整性
- [ ] C# 脚本文件是否已创建在正确的目录下？
- [ ] 文件名是否与类名完全一致（PascalCase）？
- [ ] 图片文件是否已存在于资源目录中？
- [ ] 图片是否有对应的 `.import` 文件？（如果没有，告知用户需在 Godot 编辑器中打开项目生成）
- [ ] 本地化 JSON 文件中是否添加了对应的条目？
- [ ] 新文件是否已在 `.csproj` 中索引？（检查 `PersonalMod.csproj` 是否有对应条目，或使用了通配符模式）

### 5.3 IDE 解决方案可见性检查
- [ ] 在 IDE 中刷新解决方案（**重新加载项目**），确认新文件出现在解决方案资源管理器中
- [ ] 如果文件不可见，检查 `.csproj` 是否遗漏了该文件的 `<Compile>` 或 `<Content>` 条目
- [ ] 确认 `.csproj` 的路径是相对路径且正确（相对于 `.csproj` 文件所在目录）

### 5.3 注册检查
- [ ] 类上是否添加了正确的注册属性（`[RegisterCard]` / `[RegisterPower]` / 等）？
- [ ] 注册属性中的池类型参数是否正确？
- [ ] `Entry.Init()` 中是否确保调用了 `RegisterModAssembly` 和 `EnsureGodotScriptsRegistered`？

### 5.4 资源路径检查
- [ ] `AssetProfile` 中的资源路径是否为 `res://PersonalMod/...` 格式？
- [ ] 资源路径的文件名是否与类名和实际文件名一致（区分大小写）？
- [ ] 如果有引用源工程的资源，路径是否确认正确？

### 5.5 本地化检查
- [ ] 本地化键是否为 `{MODID}_{CATEGORY}_{CLASSNAME}.field` 格式？
- [ ] 卡牌/遗物/药水是否有 `title` + `description`？
- [ ] 能力是否有 `title` + `description` + `smartDescription`？
- [ ] 遗物是否有 `flavor`？
- [ ] 描述中的 BBCode 标签是否正确闭合？
- [ ] 描述中的动态变量占位符是否与 C# 中定义的 `CanonicalVars` 匹配？

### 5.6 数值平衡检查
- [ ] 伤害/格挡/治疗数值是否合理？
- [ ] 能量消耗是否合理？
- [ ] 稀有度与效果是否匹配？

### 5.7 版本控制检查
- [ ] 所有新增/修改的文件是否已 `git add`？
- [ ] 检查 git 状态确认没有遗漏的文件：
  ```bash
  git status
  ```
- [ ] 二进制文件（PNG 图片）是否也包含在内？
- [ ] `.import` 文件是否已添加（如果已生成）？

### 5.8 控制台调试
- [ ] 确认了该内容的控制台调试命令并告知用户：
  - 卡牌: `card {ID}`
  - 能力: `power {ID} <层数> <目标>`
  - 遗物: `relic {ID}`
  - 药水: `potion {ID}`
  - 事件: `event {ID}`
  - 怪物: `spawn {ID}`

### 5.9 自检不通过的处理流程

```
自检清单逐项检查
    │
    ▼
┌─ 全部通过？ ──── 是 ──→ 继续下一步
    │ 否
    ▼
┌─ 判断错误类型 ──────────────────────────────────┐
│                                                    │
│  编译错误（5.1）：                                  │
│  → 修复代码后重新执行 Step 6 编译验证               │
│  → 直到编译通过才能继续                             │
│                                                    │
│  资源/本地化错误（5.2~5.6）：                       │
│  → 修复文件后继续自检                               │
│                                                    │
│  Git 遗漏（5.7）：                                  │
│  → 执行 git add 补充遗漏文件                        │
│  → 继续下一步                                      │
└─────────────────────────────────────────────────────┘
```

---

## 六、中断处理协议

当流程无法继续时，按照以下规则处理：

### 6.1 需要中断的场景

| 场景 | 中断原因 | 处理方式 |
|------|---------|---------|
| 用户未提供图片路径 | 无法创建资源文件 | **暂停**，询问用户是否有图片 |
| 图片处理脚本失败 | 图片格式或路径问题 | **暂停**，告知错误信息，让用户处理图片 |
| 图片缺少 `.import` 文件 | Godot 无法索引资源 | **提示**，告知用户需在 Godot 编辑器中打开项目自动生成 |
| 新建文件不在 `.csproj` 中 | IDE 解决方案中不可见 | **暂停**，添加文件索引到 `.csproj` 后重新加载项目 |
| 编译验证失败 | 代码有语法/类型错误 | **暂停**，列出错误信息，修复后重新编译验证 |
| Git 操作失败 | 文件冲突或路径问题 | **暂停**，告知错误信息，让用户手动处理 |
| 需要参考源工程但不确定具体路径 | 无法确定正确引用 | **暂停**，列出可选项，让用户选择 |
| 需要 Harmony 补丁但目标方法不确定 | 无法确定 patch 目标 | **暂停**，让用户确认要修改哪个方法 |
| 功能需要 ritsulib-utils 中的方案 | 需确认是否适用 | **暂停**，展示方案让用户确认 |
| 创建新卡池/遗物池/人物等大型基础设施 | 涉及范围较大 | **暂停**，说明需要额外创建的内容 |
| 需要参考源工程代码 | 需确认参考方向 | **暂停**，列出可参考的源文件，让用户确认 |

### 6.2 中断时的报告格式

```
[步骤 X 中断] - <中断原因描述>
──────────────────────────────────────
当前进度：
  - 已完成: 文件A, 文件B, ...
  - 未完成: <具体说明>

需要你做决定：
  1. <选项A>
  2. <选项B>
  3. 或者告诉我你的想法

请继续指令。
```

---

## 七、完整内容创建速查

### 7.1 完整的卡牌创建流程

1. 询问/提取：类名、能量消耗、卡牌类型(Attack/Skill/Power)、稀有度、目标类型、效果描述、图片路径、卡池名
2. 创建 C# 文件 `PersonalModCode/Cards/<ClassName>.cs`
3. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
4. 如有图片 → 调用 `sts2-image-resizer` 处理 `card` 类型
5. 处理图片后→ 检查 `.import` 文件（§4.4），告知用户需在 Godot 编辑器中打开
6. 调度 `sts2-card-skill` 生成卡牌代码
7. 在 `PersonalMod/localization/eng/cards.json` 添加本地化
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `card {ID}` 调试命令

### 7.2 完整的能力创建流程

1. 询问/提取：类名、PowerType(Buff/Debuff)、StackType(Counter/Intensity/Duration)、效果描述、图片路径
2. 创建 C# 文件 `PersonalModCode/Powers/<ClassName>.cs`
3. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
4. 如有图片 → 调用 `sts2-image-resizer` 处理 `power` + `power_big`
5. 处理图片后→ 检查 `.import` 文件（§4.4），告知用户需在 Godot 编辑器中打开
6. 调度 `sts2-power-skill` 生成能力代码
7. 在 `PersonalMod/localization/eng/powers.json` 添加本地化
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `power {ID} <层数> <目标>` 调试命令

### 7.3 完整的遗物创建流程

1. 询问/提取：类名、稀有度、效果描述、图片路径、遗物池
2. 创建 C# 文件 `PersonalModCode/Relics/<ClassName>.cs`
3. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
4. 如有图片 → 调用 `sts2-image-resizer` 处理 `relic` + `relic_outline` + `relic_big`
5. 处理图片后→ 检查 `.import` 文件（§4.4），告知用户需在 Godot 编辑器中打开
6. 调度 `sts2-relic-skill` 生成遗物代码
7. 在 `PersonalMod/localization/eng/relics.json` 添加本地化
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `relic {ID}` 调试命令

### 7.4 完整的药水创建流程

1. 询问/提取：类名、稀有度、使用时机(CombatOnly/AnyTime/Automatic)、目标类型、效果描述、图片路径
2. 创建 C# 文件 `PersonalModCode/Potions/<ClassName>.cs`
3. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
4. 如有图片 → 调用 `sts2-image-resizer` 处理 `potion` + `potion_outline`
5. 处理图片后→ 检查 `.import` 文件（§4.4），告知用户需在 Godot 编辑器中打开
6. 调度 `sts2-potion-skill` 生成药水代码
7. 在 `PersonalMod/localization/eng/potions.json` 添加本地化
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `potion {ID}` 调试命令

### 7.5 完整的事件创建流程

1. 询问/提取：类名、所属幕、事件描述、选项列表、图片路径、出现条件
2. 创建 C# 文件 `PersonalModCode/Events/<ClassName>.cs`
3. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
4. 如有图片 → PNG 放置到 `images/events/<ClassName>.png`（事件插图不经过 image-resizer）
5. 处理图片后→ 检查 `.import` 文件（§4.4），告知用户需在 Godot 编辑器中打开
6. 调度 `sts2-event-skill` 生成事件代码
7. 在 `PersonalMod/localization/eng/events.json` 添加本地化
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `event {ID}` 调试命令

### 7.6 完整的怪物创建流程

1. 询问/提取：类名、HP范围、AI行为描述、所属幕、视觉参考
2. 创建 C# 文件 `PersonalModCode/Monsters/<ClassName>.cs`
3. 创建遭遇文件 `PersonalModCode/Encounters/<Encounter>.cs`
4. **更新 `.csproj`** → 确认 `PersonalMod.csproj` 包含新建文件的索引（§4.5）
5. 创建视觉场景 `PersonalMod/scenes/monsters/<ClassName>.tscn`
6. 调度 `sts2-monster-skill` 生成怪物代码
7. 在本地化文件中添加 monsters.json + encounters.json 条目
8. **编译验证** → 执行 `read_lints` 或 `dotnet build` 检查语法错误
9. 执行自检清单
10. **Git 提交** → `git add` 所有新增/修改的文件
11. 报告完成，给出 `spawn {ID}` 调试命令

---

## 八、参考 Skill 使用场景速查

### 8.1 sts2-core-ref — API 参考

当你需要以下信息时，引用此 Skill：

| 需求 | 查什么 |
|------|--------|
| CardModel 有哪些可 override 的虚方法 | `references/base-classes.md` → CardModel |
| PowerModel 有哪些 Hook 回调 | `references/base-classes.md` → PowerModel |
| RelicModel 有哪些 Modify/Should 方法 | `references/base-classes.md` → RelicModel |
| 所有 Hook 事件列表 | `references/hooks-reference.md` |
| 枚举值速查（CardType/IntentType等） | `references/key-systems.md` |
| 查看已有卡牌/能力/遗物的实现 | `Models/Cards/` / `Models/Powers/` / `Models/Relics/` |

### 8.2 sts2-resources — 资源路径参考

当你需要以下信息时，引用此 Skill：

| 需求 | 查什么 |
|------|--------|
| 卡牌肖像路径约定 | §4.1 卡牌资源路径 |
| 能力图标路径约定 | §4.2 能力资源路径 |
| 遗物图标路径约定 | §4.3 遗物资源路径 |
| 本地化 JSON 格式 | §5 本地化资源 |
| 查找源工程的同类资源 | §11.5 快速路径 |

### 8.3 harmony — Harmony 补丁

当需要修改原版游戏逻辑时（如修改原版卡牌/怪物行为），使用此 Skill。

### 8.4 sts2-ritsulib — RitsuLib 框架参考

当需要了解 RitsuLib 框架的完整 API（内容注册、生命周期事件、持久化、补丁系统等）时，引用此 Skill。

### 8.5 sts2-ritsulib-utils — 小工具优先

**在编写任何功能前，优先检查此 Skill**。如果功能属于以下类别，直接使用已有方案：

| 功能 | 使用方式 |
|------|---------|
| 修改手牌上限 | 实现 `IMaxHandSizeModifier` |
| 卡牌金色/红色发光 | override `ShouldGlowGoldInternal` / `ShouldGlowRedInternal` |
| 任意颜色泛光 | `.CardHandOutline<TCard>()` 注册 |
| 血条覆盖层 | 实现 `IHealthBarForecastSource` |
| 局内数据保存 | `SavedAttachedState<TOwner, TValue>` |

---

## 九、内容类型与 Model ID 对应

创建任何内容时，ModelId.Entry 格式必须严格遵守：

```
<{{MODID_UPPER}}>_<CATEGORY>_<TYPENAME>
```

其中 `{{MODID_UPPER}} = PERSONALMOD`，`TYPENAME` 为类名转 UPPER_SNAKE_CASE。

| 内容类型 | CATEGORY | ModelId.Entry 示例 |
|---------|----------|-------------------|
| 卡牌 | `CARD` | `{{MODID_UPPER}}_CARD_TEST_CARD` |
| 能力 | `POWER` | `{{MODID_UPPER}}_POWER_TEST_POWER` |
| 遗物 | `RELIC` | `{{MODID_UPPER}}_RELIC_TEST_RELIC` |
| 药水 | `POTION` | `{{MODID_UPPER}}_POTION_TEST_POTION` |
| 事件 | `EVENT` | `{{MODID_UPPER}}_EVENT_TEST_EVENT` |
| 怪物 | `MONSTER` | `{{MODID_UPPER}}_MONSTER_TEST_MONSTER` |
| 人物 | `CHARACTER` | `{{MODID_UPPER}}_CHARACTER_MY_CHARACTER` |
| 充能球 | `ORB` | `{{MODID_UPPER}}_ORB_TEST_ORB` |
| 附魔 | `ENCHANTMENT` | `{{MODID_UPPER}}_ENCHANTMENT_ADROIT_ENCHANT` |
| 先古之民 | `ANCIENT` | `{{MODID_UPPER}}_ANCIENT_TEST_ANCIENT` |
| 关键词 | `KEYWORD` | `{{MODID_UPPER}}_KEYWORD_UNIQUE` |
| Tag | `TAG` | `{{MODID_UPPER}}_TAG_HEAVY` |

---

## 十、使用示例

### 示例 1: 快捷命令创建卡牌

```
用户: /card FireSlash 1 Attack Common SingleEnemy Colorless img=C:\art\fire_slash.png
```

```
总调度流程:
  匹配命令 /card
  提取参数: 类名=FireSlash, 费用=1, 类型=Attack, 稀有度=Common,
            目标=SingleEnemy, 池=Colorless, 图片=C:\art\fire_slash.png
  Step 1: 确认参数完整 → 进入 card 工作流
  Step 2: 检查 ritsulib-utils → 不涉及
  Step 3: 创建文件 Cards/FireSlash.cs
  Step 4: 调用 image-resizer → resize_image.py C:\art\fire_slash.png card --name FireSlash
  Step 5: 检查图片 → 提示"新图片需在 Godot 编辑器中打开生成 .import 文件"
  Step 6: 调度 sts2-card-skill 编写卡牌逻辑
          ├─ 生成 FireSlash.cs（继承 ModCardTemplate，DamageVar，OnPlay）
          ├─ 添加本地化 cards.json 条目
          └─ 执行 card-skill 的自检
  Step 7: 编译验证 → read_lints(FireSlash.cs)
          ✅ 无错误 → 继续
  Step 8: 执行本 Skill 的通用自检清单
  Step 9: Git 纳入 → git add Cards/FireSlash.cs localization/eng/cards.json
  Step 10: 报告完成，给出 card PERSONALMOD_CARD_FIRE_SLASH 调试指令
```

### 示例 2: 快捷命令创建能力（带小工具功能）

```
用户: /power ExtraDrawPower Buff Counter "让手牌上限+2" img=C:\icons\draw.png
```

```
总调度流程:
  匹配命令 /power
  提取参数: 类名=ExtraDrawPower, Type=Buff, StackType=Counter,
            效果="让手牌上限+2", 图片=C:\icons\draw.png
  Step 1: 确认参数
  Step 2: 检查 ritsulib-utils → 发现 IMaxHandSizeModifier 正好适用！
          告知用户将使用此方案
  Step 3: 创建文件 Powers/ExtraDrawPower.cs
  Step 4: 调用 image-resizer → power + power_big
  Step 5: 提示 .import 文件
  Step 6: 调度 sts2-power-skill（告知需要实现 IMaxHandSizeModifier）
  Step 7: 编译验证 → 检查语法
  Step 8: 自检清单
  Step 9: git add 所有文件
  Step 10: 报告完成
```

### 示例 3: 自然语言创建遗物

```
用户: 创建一个遗物 BurningHeart，稀有级别，战斗胜利回6血，共享池
```

```
总调度流程:
  自然语言识别 → relic
  提取: 类名=BurningHeart, Rarity=Rare, 效果=战斗胜利回6血, 池=Shared
  后续流程与命令方式一致...
```

### 示例 4: 命令参数不全时主动提问

```
用户: /card FireSlash
```

```
总调度流程:
  匹配命令 /card
  提取参数: 类名=FireSlash
  缺少: 费用、类型、稀有度、目标、卡池、图片
  → 主动提问:
  "类名为 FireSlash，还需要以下信息：
   1. 能量消耗？(如 1)
   2. 卡牌类型？(Attack/Skill/Power)
   3. 稀有度？(Basic/Common/Uncommon/Rare)
   4. 目标类型？(SingleEnemy/AllEnemies/Self)
   5. 注册到哪个卡池？(ColorlessCardPool / 自定义)
   6. 有卡图图片吗？(路径)
   请依次告诉我。"
  → 等待用户回复后继续
```

---

## 十一、注意事项

1. **优先使用子 Skill** — 每个内容类型都有对应的子 Skill，不要跨 Skill 写不相关的内容
2. **先查 ritsulib-utils** — 任何小功能实现前，先检查是否有现成的接口/模板
3. **不要重复造轮子** — 参考 `sts2-core-ref` 中源工程的实现风格和 API
4. **图片资源是可选但推荐的** — 没有图片也能生成代码，只是运行时显示空白
5. **本地化默认英文** — 创建内容时先在 `eng/` 下添加本地化，`zhs/` 按需提供
6. **注册不可忘** — 所有内容类型都需要注册属性（`[RegisterCard]`、`[RegisterPower]` 等）
7. **Entry.Init() 是前提** — 必须确保 `RegisterModAssembly` 和 `EnsureGodotScriptsRegistered` 在 Mod 入口被调用
8. **中断时如实报告** — 如果卡在某个步骤无法自动完成，立刻暂停并告知具体原因

---

*最后更新：2026-05-12*
