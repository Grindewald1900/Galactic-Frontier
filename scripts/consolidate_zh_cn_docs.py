#!/usr/bin/env python3
"""Consolidate zh-CN docs: merge redundancies, adopt worldbuilding, renumber."""
from __future__ import annotations

import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path("Documentation/zh-CN")


def read(name: str) -> str:
    return (ROOT / name).read_text(encoding="utf-8")


def write(name: str, text: str) -> None:
    (ROOT / name).write_text(text, encoding="utf-8")
    print(f"write {name} ({len(text.splitlines())} lines)")


def strip_header_body(text: str) -> tuple[str, str]:
    """Return (title_line, body_after_first_hr)."""
    lines = text.splitlines()
    title = lines[0] if lines else ""
    if "\n---\n" in text:
        _, body = text.split("\n---\n", 1)
        return title, body.lstrip("\n")
    # no hr: skip first blank-separated header block
    i = 1
    while i < len(lines) and (lines[i].startswith(">") or lines[i].strip() == ""):
        i += 1
    return title, "\n".join(lines[i:]).lstrip("\n")


def merge_two(out_name: str, title: str, meta: str, parts: list[tuple[str, str]]) -> None:
    """parts: list of (section_heading, filename)."""
    chunks = [f"# {title}\n", meta, "\n---\n"]
    for heading, fname in parts:
        raw = read(fname)
        _, body = strip_header_body(raw)
        chunks.append(f"\n## {heading}\n\n")
        # demote existing ## to ###
        body = re.sub(r"^### ", "#### ", body, flags=re.M)
        body = re.sub(r"^## ", "### ", body, flags=re.M)
        chunks.append(body.rstrip() + "\n")
    write(out_name, "".join(chunks))


def main() -> None:
    # --- 1. Worldbuilding ---
    wb = Path("/tmp/03-worldbuilding.md").read_text(encoding="utf-8")
    # Fix appendix links to final numbers used below
    wb = wb.replace("`19-sector-and-region-content.md`", "`15-sector-and-onboarding.md`")
    wb = wb.replace("`17-characters-and-progression.md`", "`17-characters-and-progression.md`")
    wb = wb.replace("`16-online-and-play-modes.md`", "`16-online-and-play-modes.md`")
    wb = wb.replace("`12-region-and-ship.md`", "`12-region-and-ship.md`")
    wb = wb.replace("`10-deck-and-occupation.md`", "`10-deck-and-occupation.md`")
    wb = wb.replace("`14-economy-production.md`", "`14-economy.md`")
    wb = wb.replace("`15-market-and-trade.md`", "`15-market-and-trade.md`")  # will fix: market is 15? conflict
    write("_tmp_worldbuilding.md", wb)

    # --- 2. Merges into staging names ---
    merge_two(
        "_m_architecture.md",
        "项目结构与架构",
        "> 文档版本：v1.0\n> 文档类型：**工程**\n> 由原 `04-project-structure` + `05-architecture` 合并。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [("项目结构", "04-project-structure.md"), ("架构总览", "05-architecture.md")],
    )
    merge_two(
        "_m_core_systems.md",
        "核心系统实现与类职责",
        "> 文档版本：v1.0\n> 文档类型：**工程**\n> 由原 `06-core-systems` + `12-core-classes` 合并。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [("核心系统实现", "06-core-systems.md"), ("核心类职责与关系", "12-core-classes.md")],
    )
    merge_two(
        "_m_data_save.md",
        "数据、存档与种子契约",
        "> 文档版本：v1.0\n> 文档类型：**工程 / 规则**\n> 由原 `07-data-and-save` + `21-save-and-seed-data` 合并。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [("存档路径与序列化（工程）", "07-data-and-save.md"), ("存档契约与种子（规则）", "21-save-and-seed-data.md")],
    )
    merge_two(
        "_m_dev_guide.md",
        "开发、验证与 Agent 工作指南",
        "> 文档版本：v1.0\n> 文档类型：**工程**\n> 由原 `08-development-guide` + `09-codex-guide` + `28-debug-and-test-mode` 合并。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [
            ("开发与验证", "08-development-guide.md"),
            ("Codex / Cursor 工作边界", "09-codex-guide.md"),
            ("Debug 模式与测试工具", "28-debug-and-test-mode.md"),
        ],
    )
    merge_two(
        "_m_economy.md",
        "经济：生产、品质、资源表与耐久",
        "> 文档版本：v1.0\n> 文档类型：**规则 / 内容契约**\n> 由原 `18-production-and-quality` + `22-resources-and-warehouse` + `19-durability-and-repair` 合并。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n> 世界观解释见 `03-worldbuilding.md` §8。\n",
        [
            ("生产链与品质", "18-production-and-quality.md"),
            ("资源表与仓库", "22-resources-and-warehouse.md"),
            ("装备耐久与维修", "19-durability-and-repair.md"),
        ],
    )
    merge_two(
        "_m_online.md",
        "运行模式与线上多人（方向稿）",
        "> 文档版本：v1.0\n> 文档类型：**方向稿**\n> 由原 `26-play-modes-and-persistence` + `29-online-multiverse-cooperation` 合并。\n> 叙事见 `03-worldbuilding.md` §9。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [
            ("Solo / Online 与存档互通", "26-play-modes-and-persistence.md"),
            ("多元宇宙位面与合作", "29-online-multiverse-cooperation.md"),
        ],
    )
    merge_two(
        "_m_content.md",
        "新手引导与星域内容",
        "> 文档版本：v1.0\n> 文档类型：**规则 / 内容契约**\n> 由原 `23-onboarding-and-missions` + `24-sector-and-region-content` 合并。\n> 叙事主线见 `03-worldbuilding.md` §10。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [
            ("新手引导与任务链", "23-onboarding-and-missions.md"),
            ("星域 / 区域内容与掉落", "24-sector-and-region-content.md"),
        ],
    )
    merge_two(
        "_m_characters.md",
        "角色、抽卡与能级",
        "> 文档版本：v1.0\n> 文档类型：**规则 / 内容契约**\n> 由原 `27-gacha-and-progression` + `30-character-roster-and-lore` + `31-card-energy-rank` 合并。\n> 相位人员档案叙事见 `03-worldbuilding.md` §6。\n> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。\n",
        [
            ("抽卡与成长入口", "27-gacha-and-progression.md"),
            ("卡牌角色设定与背景", "30-character-roster-and-lore.md"),
            ("卡牌能级", "31-card-energy-rank.md"),
        ],
    )

    print("merges staged")


if __name__ == "__main__":
    main()
