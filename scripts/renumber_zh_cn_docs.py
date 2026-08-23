#!/usr/bin/env python3
"""Flatten Documentation/zh-CN/systems into root and renumber all docs."""
from __future__ import annotations

import re
import subprocess
from pathlib import Path

ROOT = Path("Documentation/zh-CN")

# old relative path (from zh-CN root) -> new filename
RENAMES: dict[str, str] = {
    "01-quick-start.md": "01-quick-start.md",
    "01-core-product-design.md": "02-core-product-design.md",
    "systems/00-setting-and-lore.md": "03-setting-and-lore.md",
    "02-project-structure.md": "04-project-structure.md",
    "03-architecture.md": "05-architecture.md",
    "04-core-systems.md": "06-core-systems.md",
    "05-data-and-save.md": "07-data-and-save.md",
    "06-development-guide.md": "08-development-guide.md",
    "07-codex-guide.md": "09-codex-guide.md",
    "08-known-issues.md": "10-known-issues.md",
    "09-figma-ui.md": "11-figma-ui.md",
    "10-core-classes.md": "12-core-classes.md",
    "11-mvp-development-plan.md": "13-mvp-development-plan.md",
    "systems/01-deck-and-occupation.md": "14-deck-and-occupation.md",
    "systems/02-auto-battle.md": "15-auto-battle.md",
    "systems/03-region-and-ship.md": "16-region-and-ship.md",
    "systems/04-idle-and-offline.md": "17-idle-and-offline.md",
    "systems/05-production-and-quality.md": "18-production-and-quality.md",
    "systems/06-durability-and-repair.md": "19-durability-and-repair.md",
    "systems/07-market-and-card-trade.md": "20-market-and-card-trade.md",
    "systems/08-save-and-seed-data.md": "21-save-and-seed-data.md",
    "systems/09-resources-and-warehouse.md": "22-resources-and-warehouse.md",
    "systems/10-onboarding-and-missions.md": "23-onboarding-and-missions.md",
    "systems/11-sector-and-region-content.md": "24-sector-and-region-content.md",
    "systems/14-play-modes-and-persistence.md": "26-play-modes-and-persistence.md",
    "systems/15-gacha-and-progression.md": "27-gacha-and-progression.md",
    "systems/16-debug-and-test-mode.md": "28-debug-and-test-mode.md",
    "systems/17-online-multiverse-cooperation.md": "29-online-multiverse-cooperation.md",
    "systems/18-character-roster-and-lore.md": "30-character-roster-and-lore.md",
    "systems/19-card-energy-rank.md": "31-card-energy-rank.md",
    "systems/20-stellar-map-and-navigation.md": "32-stellar-map-and-navigation.md",
}

# Placeholder doc id (not in repo yet)
PLACEHOLDER_OLD = "systems/12-sector-special-modes.md"
PLACEHOLDER_NEW = "25-sector-special-modes.md"


def git_mv(src: Path, dst: Path) -> None:
    if not src.exists():
        print(f"skip missing {src}")
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run(["git", "mv", str(src), str(dst)], check=True)
    print(f"mv {src.relative_to(ROOT.parent.parent)} -> {dst.name}")


def move_files() -> None:
    # Phase 1: engineering docs high -> low to avoid collisions
    eng_order = [
        "11-mvp-development-plan.md",
        "10-core-classes.md",
        "09-figma-ui.md",
        "08-known-issues.md",
        "07-codex-guide.md",
        "06-development-guide.md",
        "05-data-and-save.md",
        "04-core-systems.md",
        "03-architecture.md",
        "02-project-structure.md",
        "01-core-product-design.md",
    ]
    for old in eng_order:
        new = RENAMES[old]
        git_mv(ROOT / old, ROOT / new)

    # Phase 2: setting + all systems
    for old, new in RENAMES.items():
        if old.startswith("systems/"):
            git_mv(ROOT / old, ROOT / new)

    systems_dir = ROOT / "systems"
    if systems_dir.exists() and not any(systems_dir.iterdir()):
        systems_dir.rmdir()
        print("removed empty systems/")


def build_replacements() -> list[tuple[str, str]]:
    reps: list[tuple[str, str]] = []

    def add(old: str, new: str) -> None:
        variants = [
            (old, new),
            (f"systems/{old.split('/')[-1]}", new) if "/" not in old else None,
            (old.split("/")[-1], new),
        ]
        for item in variants:
            if item:
                reps.append(item)

    for old, new in RENAMES.items():
        old_name = old.split("/")[-1]
        add(old, new)
        add(old_name, new)
        add(f"Documentation/zh-CN/{old}", f"Documentation/zh-CN/{new}")
        add(f"Documentation/zh-CN/systems/{old_name}", f"Documentation/zh-CN/{new}")
        # markdown links
        reps.append((f"](systems/{old_name})", f"]({new})"))
        reps.append((f"](../systems/{old_name})", f"]({new})"))
        reps.append((f"(`systems/{old_name}`)", f"(`{new}`)"))

    # engineering renames only (basename)
    for old, new in RENAMES.items():
        if not old.startswith("systems/") and old != new:
            reps.append((old, new))
            reps.append((f"Documentation/zh-CN/{old}", f"Documentation/zh-CN/{new}"))

    reps.append((PLACEHOLDER_OLD, PLACEHOLDER_NEW))
    reps.append(("systems/12-sector-special-modes.md", PLACEHOLDER_NEW))
    reps.append(("12-sector-special-modes.md", PLACEHOLDER_NEW))

    # Path cleanup
    reps.extend([
        ("../PRODUCT-STATUS.md", "PRODUCT-STATUS.md"),
        ("Documentation/zh-CN/systems/", "Documentation/zh-CN/"),
        ("`systems/*`", "`14`–`32` 规则文档"),
        ("systems/*", "14–32 规则文档"),
        ("systems/", ""),
        ("见 `14`–`32` 规则文档 已拍板文档为准", "见 `14`–`32` 规则文档为准"),
        ("`Documentation/zh-CN/systems/", "`Documentation/zh-CN/"),
    ])

    # Dedupe longer patterns first
    reps.sort(key=lambda x: len(x[0]), reverse=True)
    seen: set[str] = set()
    unique: list[tuple[str, str]] = []
    for a, b in reps:
        if a not in seen and a != b:
            seen.add(a)
            unique.append((a, b))
    return unique


def patch_file(path: Path, replacements: list[tuple[str, str]]) -> bool:
    text = path.read_text(encoding="utf-8")
    orig = text
    for old, new in replacements:
        text = text.replace(old, new)
    # Fix broken double paths
    text = text.replace("Documentation/zh-CN/Documentation/zh-CN/", "Documentation/zh-CN/")
    text = re.sub(r"\(\s*PRODUCT-STATUS\.md\)", "(PRODUCT-STATUS.md)", text)
    if text != orig:
        path.write_text(text, encoding="utf-8")
        return True
    return False


def patch_all(replacements: list[tuple[str, str]]) -> None:
    for path in ROOT.rglob("*.md"):
        if patch_file(path, replacements):
            print(f"patched {path.relative_to(ROOT.parent.parent)}")
    doc_readme = Path("Documentation/README.md")
    if doc_readme.exists() and patch_file(doc_readme, replacements):
        print("patched Documentation/README.md")
    norm = Path("scripts/normalize_system_docs.py")
    if norm.exists() and patch_file(norm, replacements):
        print("patched scripts/normalize_system_docs.py")


def main() -> None:
    move_files()
    reps = build_replacements()
    patch_all(reps)
    print("done")


if __name__ == "__main__":
    main()
