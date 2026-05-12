---
name: sts2-image-resizer
description: >-
  该 Skill 提供杀戮尖塔2 Mod 开发中的图片缩放处理功能。
  当用户需要将图片放缩、拉伸到指定尺寸（如卡牌肖像、能力图标、遗物图标、药水图标等），
  并输出到符合项目规范的目录时，应使用此 Skill。
  支持多种资源类型和缩放模式，自动推导输出路径和文件名。
auto_trigger: false
trigger_priority: 5
---

# STS2 图片缩放 Skill

## 1. 概述

本 Skill 用于处理杀戮尖塔2 Mod 开发中的图片资源缩放需求。当用户有一张原始图片，需要将其调整为特定资源类型所需的尺寸并放入项目规范目录时，调用本 Skill 提供的脚本。

**适用场景**：
- 用户提供了原始美术图片，需要生成卡牌肖像
- 用户提供了图标素材，需要生成能力/遗物/药水图标
- 需要同时生成普通尺寸和 big 尺寸变体
- 需要将图片缩放到符合 STS2 游戏规范的尺寸

---

## 2. 支持的资源类型与尺寸

| 类型键 | 资源 | 尺寸 | 输出目录 | 说明 |
|--------|------|------|---------|------|
| `card` | 卡牌肖像 | 250×190 | `images/card_portraits/` | 普通卡牌 |
| `card_ancient` | 先古卡牌肖像 | 250×351 | `images/card_portraits/` | 先古居民卡牌 |
| `card_big` | 卡牌肖像(大) | 250×190 | `images/card_portraits/big/` | 大尺寸变体 |
| `power` | 能力图标 | 64×64 | `images/powers/` | 战斗中能力栏 |
| `power_big` | 能力图标(大) | 256×256 | `images/powers/big/` | 详情/悬浮显示 |
| `relic` | 遗物图标 | 85×85 | `images/relics/` | 遗物栏显示 |
| `relic_outline` | 遗物轮廓 | 85×85 | `images/relics/` | 遗物轮廓图 |
| `relic_big` | 遗物图标(大) | 256×256 | `images/relics/big/` | 详情界面 |
| `potion` | 药水图标 | 64×96 | `images/potions/` | 药水栏显示 |
| `potion_outline` | 药水轮廓 | 64×96 | `images/potions/` | 药水轮廓图 |
| `potion_big` | 药水图标(大) | 256×256 | `images/potions/big/` | 详情界面 |

**注意**: 输出目录为 `PersonalMod/PersonalMod/` 下的相对路径，与 Godot 的 `res://PersonalMod/` 路径对应。

---

## 3. 缩放模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `fit` (默认) | 保持比例缩放，居中放置在目标尺寸画布上，透明填充 | 大部分场景，特别是宽高比差异较大时 |
| `fill` | 保持比例缩放，裁剪填满目标尺寸 | 当图片可以接受部分裁切时 |
| `stretch` | 拉伸到目标尺寸，不保持比例 | 仅当图片需要精确匹配尺寸时 |

---

## 4. 脚本使用方法

脚本路径: `.codebuddy/skills/sts2-image-resizer/scripts/resize_image.py`

### 4.1 基本语法

```bash
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py <input_image> <type> [--name <output_name>] [--mode <resize_mode>] [--output <output_root>]
```

### 4.2 参数说明

| 参数 | 必填 | 说明 |
|------|------|------|
| `input_image` | 是 | 输入图片路径 |
| `type` | 是 | 资源类型（见 §2 类型键） |
| `--name` | 否 | 输出文件名（不含扩展名），默认使用输入文件名 |
| `--mode` | 否 | 缩放模式: `fit`(默认)/`fill`/`stretch` |
| `--output` | 否 | 输出根目录，默认为 `PersonalMod/PersonalMod/` |

### 4.3 使用示例

```bash
# 将 my_art.png 缩放为卡牌肖像，命名为 StrikeIronclad
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py my_art.png card --name StrikeIronclad

# 将 icon.png 缩放为能力图标，裁剪填满
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py icon.png power --mode fill

# 将 relic_art.png 同时生成遗物图标和遗物轮廓
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py relic_art.png relic --name BurningBlood
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py relic_art.png relic_outline --name BurningBlood

# 将 big_relic.png 生成遗物大图标
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py big_relic.png relic_big --name BurningBlood
```

---

## 5. 工作流程

当用户需要处理图片时，按以下流程操作：

### 5.1 单一资源处理

1. 确认用户提供的图片路径和目标资源类型
2. 确认输出文件名（如果用户指定了类名如 `StrikeIronclad`，使用该名称）
3. 选择合适的缩放模式（默认 `fit`，除非用户要求其他模式）
4. 执行脚本
5. 告知用户输出路径

### 5.2 批量生成（普通 + big 变体）

当用户需要同时生成普通尺寸和 big 变体时（常见于能力和遗物），执行两次脚本：

```bash
# 能力图标: 普通 + 大
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py input.png power --name MyPower
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py input.png power_big --name MyPower

# 遗物图标: 普通 + 轮廓 + 大
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py input.png relic --name MyRelic
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py input.png relic_outline --name MyRelic
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py input.png relic_big --name MyRelic
```

### 5.3 完整卡牌资源处理

创建卡牌时，通常需要生成卡牌肖像和可能的 big 变体：

```bash
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py card_art.png card --name MyCard
py .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py card_art.png card_big --name MyCard
```

---

## 6. 输出路径与 Godot 资源路径映射

脚本输出的文件路径与 Godot 资源路径对应关系：

| 物理路径 | Godot 资源路径 |
|---------|--------------|
| `PersonalMod/PersonalMod/images/card_portraits/MyCard.png` | `res://PersonalMod/images/card_portraits/MyCard.png` |
| `PersonalMod/PersonalMod/images/powers/MyPower.png` | `res://PersonalMod/images/powers/MyPower.png` |
| `PersonalMod/PersonalMod/images/relics/MyRelic.png` | `res://PersonalMod/images/relics/MyRelic.png` |
| `PersonalMod/PersonalMod/images/potions/MyPotion.png` | `res://PersonalMod/images/potions/MyPotion.png` |

这些路径可直接在 C# 代码的 `AssetProfile` 中使用：

```csharp
// 卡牌
public override CardAssetProfile AssetProfile => new(
    PortraitPath: $"res://PersonalMod/images/card_portraits/{GetType().Name}.png"
);

// 能力
public override PowerAssetProfile AssetProfile => new(
    IconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png",
    BigIconPath: $"res://PersonalMod/images/powers/{GetType().Name}.png"
);

// 遗物
public override RelicAssetProfile AssetProfile => new(
    IconPath: $"res://PersonalMod/images/relics/{GetType().Name}.png",
    IconOutlinePath: $"res://PersonalMod/images/relics/{GetType().Name}.png",
    BigIconPath: $"res://PersonalMod/images/relics/{GetType().Name}.png"
);
```

---

## 7. 注意事项

- **文件名规则**: 卡牌/能力/遗物/药水的图片文件名应使用 PascalCase 类名（如 `StrikeIronclad.png`），与 C# 类名一致
- **遗物轮廓**: `relic_outline` 类型自动给文件名加 `_outline` 后缀（如输入名 `BurningBlood` → 输出 `BurningBlood_outline.png`），除非文件名已包含此后缀
- **药水轮廓**: 同遗物轮廓规则，`potion_outline` 自动加 `_outline` 后缀
- **Pillow 依赖**: 脚本依赖 Pillow 库，首次运行时若未安装会自动尝试安装
- **图片格式**: 输出始终为 PNG（RGBA），确保透明度正确处理
- **Godot .import 文件**: 首次在 Godot 编辑器中打开项目后，Godot 会自动为图片生成 `.import` 文件，无需手动创建

---

*最后更新：2026-05-12*
