#!/usr/bin/env python3
"""Normalize rule doc headers (14-32) in Documentation/zh-CN/."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path("Documentation/zh-CN")

DOC_TYPES: dict[str, str] = {
    "03-setting-and-lore.md": "设定",
    "14-deck-and-occupation.md": "规则",
    "15-auto-battle.md": "规则",
    "16-region-and-ship.md": "规则",
    "17-idle-and-offline.md": "规则",
    "18-production-and-quality.md": "规则",
    "19-durability-and-repair.md": "规则",
    "20-market-and-card-trade.md": "规则",
    "21-save-and-seed-data.md": "规则",
    "22-resources-and-warehouse.md": "内容契约",
    "23-onboarding-and-missions.md": "规则",
    "24-sector-and-region-content.md": "内容契约",
    "26-play-modes-and-persistence.md": "方向稿",
    "27-gacha-and-progression.md": "规则",
    "28-debug-and-test-mode.md": "规则",
    "29-online-multiverse-cooperation.md": "方向稿",
    "30-character-roster-and-lore.md": "内容契约",
    "31-card-energy-rank.md": "规则",
    "32-stellar-map-and-navigation.md": "方向稿",
}

STATUS_NOTE = (
    "> **实现与验收状态**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)；"
    "本文仅描述规则与设计标准。\n"
)


def rule_docs() -> list[Path]:
    return sorted(p for name, _ in DOC_TYPES.items() if name != "03-setting-and-lore.md" for p in [ROOT / name] if p.name.startswith(("14", "15", "16", "17", "18", "19", "2", "3")) and (ROOT / name).exists())


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

    kept: list[str] = [lines[0]]
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
        kept.append(line)

    return "\n".join(kept) + "\n---\n" + body


def main() -> None:
    for path in rule_docs():
        text = normalize_header(path.read_text(encoding="utf-8"), path.name)
        path.write_text(text, encoding="utf-8")
        print(f"updated {path.name}")


if __name__ == "__main__":
    main()
