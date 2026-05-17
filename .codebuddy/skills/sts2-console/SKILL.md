---
name: sts2-console
description: >-
  该 Skill 提供杀戮尖塔2 (Slay the Spire 2) 内置控制台的全部指令参考。
  当用户询问如何通过控制台获取某物（卡牌/遗物/药水/能力等）、跳转房间、触发事件、调试战斗时，
  使用此 Skill 返回确切的控制台指令。
  涵盖卡牌操作、战斗调试、地图跳转、物品获取、内容解锁、日志管理等全部内置命令。
  在战斗中按 `~` 打开控制台，Tab 补全，↑↓ 历史记录。
auto_trigger: true
trigger_priority: 0
---

# STS2 控制台指令 Skill

> **ModId 约定**：本 Skill 中所有 `{{MODID}}` / `{{MODID_UPPER}}` 占位符由总调度 Skill (sts2-manager) 定义并注入上下文。

## 1. 使用方式

- 游戏内按 **`~`** 打开/关闭控制台
- **Tab** 键自动补全当前输入
- **↑ ↓** 方向键选择补全候补 / 历史记录
- **Enter** 执行命令

---

## 2. 快速查询表

**用户说"我要..." → 控制台指令**

| 用户需求 | 控制台指令 | 章节 |
|---------|-----------|------|
| 获取一张牌 | `card <卡牌ID> [hand/draw/discard/exhaust/master_deck]` | §3 |
| 移除一张牌 | `remove_card <卡牌ID> [牌库名]` | §3 |
| 升级一张牌 | `upgrade <手牌位置>` | §3 |
| 给卡牌加附魔 | `enchant <附魔ID> [层数] [手牌位置]` | §3 |
| 获得一个遗物 | `relic [add] <遗物ID>` | §4 |
| 移除一个遗物 | `relic remove <遗物ID>` | §4 |
| 获得一瓶药水 | `potion <药水ID>` | §4 |
| 施加一个能力 | `power <能力ID> <层数> <目标索引>` | §4 |
| 造成伤害 | `damage <数值> [目标索引]` | §5 |
| 加格挡 | `block <数值> [目标索引]` | §5 |
| 回血 | `heal <数值> [索引]` | §5 |
| 加能量 | `energy <数值>` | §5 |
| 加金币 | `gold <数值>` | §5 |
| 抽牌 | `draw <数量>` | §5 |
| 直接胜利 | `win` | §5 |
| 直接死亡 | `die` | §5 |
| 杀死敌人 | `kill [目标索引\|all]` | §5 |
| 无敌模式 | `godmode` | §5 |
| 跳转章节 | `act <幕数\|名称>` | §6 |
| 跳转房间 | `room <房间ID>` | §6 |
| 触发事件 | `event <事件ID>` | §6 |
| 触发战斗 | `fight <遭遇ID>` | §6 |
| 触发Ancient | `ancient <AncientID> [遗物ID]` | §6 |
| 地图传送 | `travel` | §6 |
| 解锁全部内容 | `unlock all` | §7 |
| 查看帮助 | `help [命令名]` | §7 |
| 加速模式 | `instant` | §7 |
| 打印所有 Model ID | `dump` | §7 |

---

## 3. 卡牌相关 (Card)

| 操作 | 指令 | 说明 |
|------|------|------|
| **获得卡牌** | `card <卡牌ID> [牌库名]` | 生成卡牌到指定牌堆。牌库名：`hand`（手牌，默认）、`draw`（抽牌堆）、`discard`（弃牌堆）、`exhaust`（消耗堆）、`master_deck`（主牌组） |
| **移除卡牌** | `remove_card <卡牌ID> [牌库名]` | 从指定牌库移除卡牌 |
| **升级卡牌** | `upgrade <手牌位置>` | 升级手牌中指定位置的卡牌（0=最左侧） |
| **附加附魔** | `enchant <附魔ID> [层数] [手牌位置]` | 对手牌卡牌附加指定附魔。层数默认1。手牌位置默认0（最左侧） |
| **附加诅咒** | `afflict <诅咒ID> [层数] [手牌位置]` | 对手牌卡牌附加指定诅咒。层数默认1。手牌位置默认0 |
| **抽牌** | `draw <数量>` | 抽指定数量的牌 |

### 3.1 获取卡牌示例

```
# 将打击（StrikeIronclad）加入手牌
card StrikeIronclad hand

# 将打击加入抽牌堆
card StrikeIronclad draw

# 将打击加入主牌组
card StrikeIronclad master_deck

# 获取 Mod 卡牌
card {{MODID_UPPER}}_CARD_MY_CARD hand
```

### 3.2 附魔示例

```
# 给最左侧手牌附加 Adroit 附魔
enchant Adroit

# 给第3张手牌附加 3层 Adroit 附魔
enchant Adroit 3 2
```

---

## 4. 物品获取

| 操作 | 指令 | 说明 |
|------|------|------|
| **获得遗物** | `relic [add] <遗物ID>` | 为玩家添加遗物（add 可省略） |
| **移除遗物** | `relic remove <遗物ID>` | 移除玩家的遗物 |
| **获得药水** | `potion <药水ID>` | 添加一瓶指定 ID 的药水 |
| **施加能力** | `power <能力ID> <层数> <目标索引>` | 对指定目标施加能力 |
| **加金币** | `gold <数值>` | 增加金币（可为负数） |
| **加辉星** | `stars <数值>` | 添加辉星 |

### 4.1 获取物品示例

```
# 获得 BurningBlood 遗物
relic BurningBlood

# 获得 Mod 遗物
relic {{MODID_UPPER}}_RELIC_MY_RELIC

# 移除遗物
relic remove {{MODID_UPPER}}_RELIC_MY_RELIC

# 获得药水
potion {{MODID_UPPER}}_POTION_TEST_POTION

# 给自己施加 5 层力量
power {{MODID_UPPER}}_POWER_STRENGTH_POWER 5 0

# 给第一个敌人施加 2 层易伤
power {{MODID_UPPER}}_POWER_VULNERABLE_POWER 2 1

# 加 100 金币
gold 100
```

**目标索引说明**：
- 0 = 你自己（玩家）
- 1+ = 敌人（按生成顺序）
- 联机模式下 1+ 先轮到其他玩家

---

## 5. 战斗调试

| 操作 | 指令 | 说明 |
|------|------|------|
| **伤害** | `damage <数值> [目标索引]` | 对所有敌人造成伤害。指定索引可指定单一目标 |
| **格挡** | `block <数值> [目标索引]` | 为玩家添加格挡。指定索引可为指定生物添加 |
| **治疗** | `heal <数值> [索引]` | 恢复玩家生命值。指定索引可治疗其他生物 |
| **能量** | `energy <数值>` | 增加能量（可为负数） |
| **斩杀** | `kill [目标索引\|all]` | 杀死指定目标。`all` 杀死全部敌人，默认第一个敌人 |
| **胜利** | `win` | 直接赢得战斗 |
| **死亡** | `die` | 直接输掉 |
| **无敌** | `godmode` | 切换无敌模式（再次输入关闭） |

### 5.1 战斗调试示例

```
# 对全体敌人造成 50 点伤害
damage 50

# 对第一个敌人造成 50 点伤害
damage 50 1

# 给自己加 20 格挡
block 20

# 给第一个敌人加 10 格挡
block 10 1

# 治疗 30 点生命
heal 30

# 增加 3 点能量
energy 3

# 杀死第一个敌人
kill

# 杀死全部敌人
kill all
```

---

## 6. 地图与房间跳转

| 操作 | 指令 | 说明 |
|------|------|------|
| **跳转章节** | `act <幕数\|名称>` | 跳转到指定章节。可用整数或章节 ID |
| **跳转房间** | `room <房间ID>` | 跳转到指定房间（如 BOSS、SHOP 等，看 Tab 补全） |
| **跳转事件** | `event <事件ID>` | 跳转到指定事件 |
| **跳转战斗** | `fight <遭遇ID>` | 跳转到指定怪物遭遇战 |
| **地图传送** | `travel` | 切换地图传送模式，可点击任意房间跳转 |
| **Ancient** | `ancient <AncientID> [遗物ID]` | 跳转到指定 Ancient，可加参数使必定出现某遗物选项 |

### 6.1 跳转示例

```
# 跳转到第二幕
act 2

# 跳转到 BOSS 房间
room BOSS

# 触发 Mod 事件
event {{MODID_UPPER}}_EVENT_TEST_EVENT

# 触发 Mod Ancient，必定出现 Akabeko 遗物
ancient {{MODID_UPPER}}_ANCIENT_TEST_ANCIENT Akabeko

# 触发指定怪物遭遇
fight {{MODID_UPPER}}_ENCOUNTER_TEST_ENCOUNTER

# 切换地图传送模式
travel
```

---

## 7. 其他命令

| 操作 | 指令 | 说明 |
|------|------|------|
| **帮助** | `help [命令名]` | 列出所有命令，或查看指定命令的详细帮助，如 `help card` |
| **打开路径** | `open <logs\|saves\|root\|build-logs\|loc-override>` | 在文件管理器中打开常用目录 |
| **解锁内容** | `unlock <类型>` | 标记内容已发现：`cards`、`potions`、`relics`、`monsters`、`events`、`epochs`、`ascensions`。`all` 解锁全部 |
| **成就** | `achievement <unlock\|revoke> [ID]` | 解锁或撤销成就 |
| **打印ID** | `dump` | 将所有 Model 的 ID 输出到控制台和日志文件 |
| **日志级别** | `log [类型] <级别>` | 设置日志级别：`verydebug`、`debug`、`info`、`warn`、`error` |
| **缺失美术** | `art <类型>` | 列出缺少美术资源的内容。类型：`card`、`relic`、`potion`、`enchant` 等 |
| **加速模式** | `instant` | 跳过所有动画延迟 |
| **图鉴** | `bestiary` | 打开怪物图鉴 |
| **排行榜** | `leaderboard [选项] [名称] <分数> [数量]` | 提交或测试排行榜分数 |
| **获取日志** | `getlogs [test-feedback] <名称>` | 收集日志打包为 zip |
| **Sentry** | `sentry <test\|message\|exception\|crash\|status> [文本]` | 测试错误报告功能 |
| **预告模式** | `trailer` | 切换预告模式，数字键 0-9 切换 UI 显隐 |
| **云存档** | `cloud delete` | 删除 Steam 云存档 |
| **多人模式** | `multiplayer [test]` | 打开多人菜单或测试场景 |

### 7.1 调试示例

```
# 查看 card 命令的详细用法
help card

# 解锁 Mod 的所有内容
unlock all

# 查看 Mod 的卡牌 ID 列表
dump

# 快速跳过动画
instant

# 打开存档目录
open saves
```

---

## 8. Model ID 命名规则

Mod 注册的各类内容的控制台 ID 遵循以下格式：

| 类型 | 格式 | 示例 |
|------|------|------|
| 卡牌 | `<MODID>_CARD_<TYPENAME>` | `{{MODID_UPPER}}_CARD_STRIKE` |
| 遗物 | `<MODID>_RELIC_<TYPENAME>` | `{{MODID_UPPER}}_RELIC_TEST_RELIC` |
| 能力 | `<MODID>_POWER_<TYPENAME>` | `{{MODID_UPPER}}_POWER_TEST_POWER` |
| 药水 | `<MODID>_POTION_<TYPENAME>` | `{{MODID_UPPER}}_POTION_TEST_POTION` |
| 怪物 | `<MODID>_MONSTER_<TYPENAME>` | `{{MODID_UPPER}}_MONSTER_TEST_MONSTER` |
| 事件 | `<MODID>_EVENT_<TYPENAME>` | `{{MODID_UPPER}}_EVENT_TEST_EVENT` |
| Ancient | `<MODID>_ANCIENT_<TYPENAME>` | `{{MODID_UPPER}}_ANCIENT_TEST_ANCIENT` |
| 附魔 | `<MODID>_ENCHANTMENT_<TYPENAME>` | `{{MODID_UPPER}}_ENCHANTMENT_ADROIT` |
| 遭遇 | `<MODID>_ENCOUNTER_<TYPENAME>` | `{{MODID_UPPER}}_ENCOUNTER_TEST` |
| 充能球 | `<MODID>_ORB_<TYPENAME>` | `{{MODID_UPPER}}_ORB_TEST_ORB` |

---

## 9. 常见问题

**Q**: 我需要卡牌/能力/遗物的完整 ID，怎么看？
> **A**: 在控制台输入 `dump` 命令，所有已注册 Model 的完整 ID 会输出到控制台和日志文件。

**Q**: 控制台 Tab 补全没有我的 Mod ID？
> **A**: 确认 Mod 已正确加载，`RegisterModAssembly` 已调用。部分命令可能需要先进入战斗或开始一局游戏。

**Q**: 控制台命令不生效？
> **A**: 检查参数格式，`<X>` 为必填参数，`[X]` 为可选参数。使用 `help <命令名>` 查看详细用法。

**Q**: 怎么知道有哪些可用的 Ancient？
> **A**: 输入 `help ancient` 或使用 Tab 补全查看。

---

*最后更新：2026-05-12*
