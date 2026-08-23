#!/usr/bin/env python3
"""Normalize system doc headers and acceptance sections."""
from __future__ import annotations

import re
from pathlib import Path

SYSTEMS = Path("Documentation/zh-CN/systems")

DOC_TYPES: dict[str, str] = {
    "00-setting-and-lore.md": "设定",
    "01-deck-and-occupation.md": "规则",
    "02-auto-battle.md": "规则",
    "03-region-and-ship.md": "规则",
    "04-idle-and-offline.md": "规则",
    "05-production-and-quality.md": "规则",
    "06-durability-and-repair.md": "规则",
    "07-market-and-card-trade.md": "规则",
    "08-save-and-seed-data.md": "规则",
    "09-resources-and-warehouse.md": "内容契约",
    "10-onboarding-and-missions.md": "规则",
    "11-sector-and-region-content.md": "内容契约",
    "14-play-modes-and-persistence.md": "方向稿",
    "15-gacha-and-progression.md": "规则",
    "16-debug-and-test-mode.md": "规则",
    "17-online-multiverse-cooperation.md": "方向稿",
    "18-character-roster-and-lore.md": "内容契约",
    "19-card-energy-rank.md": "规则",
    "20-stellar-map-and-navigation.md": "方向稿",
}

STATUS_NOTE = (
    "> **实现与验收状态**见 [PRODUCT-STATUS.md](../PRODUCT-STATUS.md)；"
    "本文仅描述规则与设计标准。\n"
)


def fix_paths(text: str) -> str:
    text = text.replace("`Documentation/01-core-product-design.md`", "`../01-core-product-design.md`")
    text = text.replace("Documentation/01-core-product-design.md", "../01-core-product-design.md")
    text = text.replace("[01-core-product-design.md](../01-core-product-design.md)", "[01-core-product-design.md](../01-core-product-design.md)")
    return text


def normalize_header(content: str, filename: str) -> str:
    doc_type = DOC_TYPES.get(filename, "规则")
    if not content.startswith("#"):
        return content

    parts = content.split("\n---\n", 1)
    if len(parts) != 2:
        return content

    header_block, body = parts
    lines = header_block.split("\n")

    version = "v0.1"
    for line in lines:
        m = re.search(r"文档版本：\s*(v[\d.]+)", line)
        if m:
            version = m.group(1)
            break

    # Fix known version/changelog mismatches
    if filename == "10-onboarding-and-missions.md":
        version = "v1.2"
    if filename == "07-market-and-card-trade.md" and version == "v1.1":
        # keep v1.1 unless changelog says v1.2 - read changelog
        for line in lines:
            if "v1.2" in line and "变更" in line:
                version = "v1.2"
                break

    kept: list[str] = [lines[0]]  # title
    kept.append(f"> 文档版本：{version}")
    kept.append(f"> 文档类型：**{doc_type}**")
    kept.append(STATUS_NOTE.rstrip())

    for line in lines[1:]:
        if re.match(r">\s*状态：", line):
            continue
        if re.match(r">\s*实现阶段：", line):
            continue
        if re.match(r">\s*文档版本：", line):
            continue
        if re.match(r">\s*文档类型：", line):
            continue
        if "实现与验收状态" in line:
            continue
        if "现有差距：" in line:
            continue
        kept.append(line)

    # Ensure 上级约束 uses relative path
    header = "\n".join(kept)
    header = fix_paths(header)
    body = fix_paths(body)
    return header + "\n---\n" + body


def rename_acceptance_sections(content: str) -> str:
    content = re.sub(
        r"^## (\d+)\.\s*验收清单.*$",
        r"## \1. 设计验收标准",
        content,
        flags=re.MULTILINE,
    )
    content = re.sub(
        r"^## (\d+)\.\s*验收口号.*$",
        r"## \1. 设计验收标准",
        content,
        flags=re.MULTILINE,
    )
    # Remove checkbox markers
    content = re.sub(r"^- \[[ xX]\]\s*", "- ", content, flags=re.MULTILINE)
    return content


def remove_gap_sections(content: str) -> str:
    pattern = re.compile(
        r"\n## \d+\.\s*与现有代码的差距[^\n]*\n.*?(?=\n## \d+\.|\Z)",
        re.DOTALL,
    )
    return pattern.sub("\n", content)


def main() -> None:
    for path in sorted(SYSTEMS.glob("*.md")):
        text = path.read_text(encoding="utf-8")
        text = normalize_header(text, path.name)
        text = rename_acceptance_sections(text)
        text = remove_gap_sections(text)
        text = fix_paths(text)
        path.write_text(text, encoding="utf-8")
        print(f"updated {path.name}")


if __name__ == "__main__":
    main()
