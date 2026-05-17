#!/usr/bin/env python3
"""
STS2 Mod 图片缩放工具
将图片放缩/拉伸到指定尺寸，输出到符合项目规范的文件夹。

用法:
    py resize_image.py <input_image> <type> [--name <output_name>] [--mode <resize_mode>] [--output <output_root>]

参数:
    input_image     输入图片路径
    type            目标类型，决定输出尺寸和文件夹:
                        card            - 卡牌肖像 (250x190)
                        card_ancient    - 先古卡牌肖像 (250x351)
                        card_big       - 卡牌肖像(大) (250x190)
                        power          - 能力图标 (64x64)
                        power_big      - 能力图标(大) (256x256)
                        relic          - 遗物图标 (85x85)
                        relic_outline  - 遗物轮廓 (85x85)
                        relic_big      - 遗物图标(大) (256x256)
                        potion         - 药水图标 (64x96)
                        potion_outline - 药水轮廓 (64x96)
                        potion_big     - 药水图标(大) (256x256)
    --name          输出文件名（不含扩展名），默认使用输入文件名
    --mode          缩放模式:
                        fit    - 保持比例，居中放置在目标尺寸画布上（默认，透明填充）
                        fill   - 保持比例，裁剪填满目标尺寸
                        stretch- 拉伸到目标尺寸（不保持比例）
    --output        输出根目录，默认自动检测 Mod ID

示例:
    py resize_image.py my_art.png card --name StrikeIronclad
    py resize_image.py icon.png power --mode fill
    py resize_image.py relic_art.png relic_big --name BurningBlood
"""

import argparse
import json
import os
import sys


# ── 尺寸与路径配置 ──────────────────────────────────────────────

# 每种类型的: (宽度, 高度, 相对输出目录)
TYPE_CONFIG = {
    "card":           (250, 190, os.path.join("images", "card_portraits")),
    "card_ancient":   (250, 351, os.path.join("images", "card_portraits")),
    "card_big":       (1000, 760, os.path.join("images", "card_portraits", "big")),
    "power":          ( 64,  64, os.path.join("images", "powers")),
    "power_big":      (256, 256, os.path.join("images", "powers", "big")),
    "relic":          ( 85,  85, os.path.join("images", "relics")),
    "relic_outline":  ( 85,  85, os.path.join("images", "relics")),
    "relic_big":      (256, 256, os.path.join("images", "relics", "big")),
    "potion":         ( 64,  96, os.path.join("images", "potions")),
    "potion_outline": ( 64,  96, os.path.join("images", "potions")),
    "potion_big":     (256, 256, os.path.join("images", "potions", "big")),
}

# ── Mod ID 自动检测 ────────────────────────────────────────────

# 排除的 JSON 文件名（非 Mod 元数据）
_SKIP_JSON = {
    "project.godot",
    "export_presets.cfg",
}


def detect_mod_id(godot_root):
    """扫描 Godot 项目根目录，从 Mod 元数据 JSON 中自动检测 Mod ID。

    查找逻辑：遍历 godot_root 下的 *.json 文件，读取其中包含 "id" 字段的 JSON，
    返回该 mod_id。如果找不到则返回 None。
    """
    if not os.path.isdir(godot_root):
        return None
    for fname in sorted(os.listdir(godot_root)):
        if not fname.endswith(".json"):
            continue
        if fname in _SKIP_JSON:
            continue
        fpath = os.path.join(godot_root, fname)
        if not os.path.isfile(fpath):
            continue
        try:
            with open(fpath, "r", encoding="utf-8") as f:
                data = json.load(f)
            if isinstance(data, dict) and "id" in data and isinstance(data["id"], str):
                return data["id"]
        except (json.JSONDecodeError, UnicodeDecodeError, OSError):
            continue
    return None


# ── 缩放逻辑 ────────────────────────────────────────────────────

def resize_fit(img, width, height):
    """保持比例缩放，居中放置在目标尺寸画布上（透明填充）。"""
    from PIL import Image
    img_ratio = img.width / img.height
    target_ratio = width / height
    if img_ratio > target_ratio:
        new_w = width
        new_h = int(width / img_ratio)
    else:
        new_h = height
        new_w = int(height * img_ratio)
    resized = img.resize((new_w, new_h), Image.LANCZOS)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    x = (width - new_w) // 2
    y = (height - new_h) // 2
    canvas.paste(resized, (x, y))
    return canvas


def resize_fill(img, width, height):
    """保持比例缩放，裁剪填满目标尺寸。"""
    from PIL import Image
    img_ratio = img.width / img.height
    target_ratio = width / height
    if img_ratio > target_ratio:
        new_h = img.height
        new_w = int(img.height * target_ratio)
        left = (img.width - new_w) // 2
        cropped = img.crop((left, 0, left + new_w, new_h))
    else:
        new_w = img.width
        new_h = int(img.width / target_ratio)
        top = (img.height - new_h) // 2
        cropped = img.crop((0, top, new_w, top + new_h))
    return cropped.resize((width, height), Image.LANCZOS)


def resize_stretch(img, width, height):
    """拉伸到目标尺寸（不保持比例）。"""
    from PIL import Image
    return img.resize((width, height), Image.LANCZOS)


RESIZE_MODES = {
    "fit": resize_fit,
    "fill": resize_fill,
    "stretch": resize_stretch,
}

# ── 输出文件名生成 ──────────────────────────────────────────────

def derive_output_name(type_key, input_path, name_override):
    """根据类型和输入推导输出文件名。"""
    if name_override:
        base = name_override
    else:
        base = os.path.splitext(os.path.basename(input_path))[0]

    # relic_outline / potion_outline 类型自动加 _outline 后缀
    if type_key in ("relic_outline", "potion_outline") and not base.endswith("_outline"):
        return base + "_outline"

    return base

# ── 主流程 ──────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description="STS2 Mod 图片缩放工具 — 将图片放缩到指定尺寸并输出到项目规范目录"
    )
    parser.add_argument("input", help="输入图片路径")
    parser.add_argument("type", choices=list(TYPE_CONFIG.keys()), help="目标类型")
    parser.add_argument("--name", default=None, help="输出文件名（不含扩展名），默认使用输入文件名")
    parser.add_argument("--mode", choices=list(RESIZE_MODES.keys()), default="fit",
                        help="缩放模式: fit=保持比例居中(默认), fill=裁剪填满, stretch=拉伸")
    parser.add_argument("--output", default=None,
                        help="输出根目录（即 Mod 资源根目录），默认自动检测")

    args = parser.parse_args()

    # 检查 Pillow
    try:
        from PIL import Image
    except ImportError:
        print("错误: 未安装 Pillow，正在安装...")
        os.system(f"{sys.executable} -m pip install Pillow")
        from PIL import Image

    # 检查输入文件
    input_path = args.input
    if not os.path.isfile(input_path):
        print(f"错误: 输入文件不存在: {input_path}")
        sys.exit(1)

    # 获取配置
    type_key = args.type
    width, height, rel_dir = TYPE_CONFIG[type_key]

    # 确定输出根目录
    if args.output:
        output_root = args.output
    else:
        # 自动检测 Mod ID
        # 脚本路径: .codebuddy/skills/sts2-image-resizer/scripts/resize_image.py
        # 向上4级: scripts -> sts2-image-resizer -> skills -> .codebuddy -> 仓库根目录
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", "..", "..", ".."))
        # Godot 项目根目录: 仓库根目录/{{MODID}}/
        godot_root = os.path.join(repo_root, "{{MODID}}")
 
        mod_id = detect_mod_id(godot_root)
        if mod_id:
            # Mod 资源根目录: Godot 项目根目录/{ModId}/
            output_root = os.path.join(godot_root, mod_id)
            print(f"自动检测 Mod ID: {mod_id}")
        else:
            print("警告: 未能自动检测 Mod ID，回退到默认路径 {{MODID}}/{{MODID}}/")
            output_root = os.path.join(godot_root, "{{MODID}}")

    # 构建输出路径
    output_dir = os.path.join(output_root, rel_dir)
    output_name = derive_output_name(type_key, input_path, args.name)
    output_path = os.path.join(output_dir, f"{output_name}.png")

    # 打开图片
    try:
        img = Image.open(input_path).convert("RGBA")
    except Exception as e:
        print(f"错误: 无法打开图片: {e}")
        sys.exit(1)

    print(f"输入: {input_path} ({img.width}x{img.height})")
    print(f"类型: {type_key} -> 目标尺寸: {width}x{height}, 模式: {args.mode}")

    # 缩放
    resize_fn = RESIZE_MODES[args.mode]
    result = resize_fn(img, width, height)

    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)

    # 保存
    result.save(output_path, "PNG")
    print(f"输出: {output_path} ({result.width}x{result.height})")
    print("完成!")


if __name__ == "__main__":
    main()
