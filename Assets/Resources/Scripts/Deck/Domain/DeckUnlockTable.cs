using System;

namespace Assets.Resources.Scripts.Deck.Domain
{
    /// <summary>
    /// MVP unlock conditions for deck slots / parallel caps (display + gating).
    /// Slot index is 0-based; slots 0–1 start unlocked.
    /// </summary>
    public static class DeckUnlockTable
    {
        public static bool IsSlotUnlockedByDefault(int slotIndex) =>
            slotIndex >= 0 && slotIndex < DeckConstants.DefaultUnlockedDeckSlots;

        public static int ComputeUnlockedSlots(
            int commanderLevel,
            int shipLevel,
            bool chapter1Clear,
            bool chapter2Clear,
            int dispatchHubLevel)
        {
            var slots = DeckConstants.DefaultUnlockedDeckSlots;
            if (commanderLevel >= 5 || chapter1Clear) slots = Math.Max(slots, 3);
            if (commanderLevel >= 10 || shipLevel >= 2) slots = Math.Max(slots, 4);
            if (chapter2Clear || dispatchHubLevel >= 1) slots = Math.Max(slots, 5);
            if (shipLevel >= 4) slots = Math.Max(slots, 6);
            return Math.Min(DeckConstants.MaxDeckSlots, slots);
        }

        public static int ComputeMaxParallel(
            int commanderLevel,
            int shipLevel,
            bool chapter3Clear,
            int automationCoreLevel)
        {
            var parallel = DeckConstants.DefaultMaxParallelActions;
            if (commanderLevel >= 8 || automationCoreLevel >= 1) parallel = Math.Max(parallel, 3);
            if (chapter3Clear || shipLevel >= 5) parallel = Math.Max(parallel, 4);
            return parallel;
        }

        /// <summary>English requirement text for a locked deck slot (0-based).</summary>
        public static string GetSlotUnlockRequirementEn(int slotIndex) => slotIndex switch
        {
            0 => "Unlocked at start",
            1 => "Unlocked at start",
            2 => "Commander Lv.5 or Chapter 1 clear",
            3 => "Commander Lv.10 or Ship Lv.2",
            4 => "Chapter 2 clear or Ship facility Dispatch Hub Lv.1",
            5 => "Ship Lv.4 or research Multi-Deck Protocol",
            _ => "Locked"
        };

        /// <summary>Simplified Chinese requirement text for a locked deck slot (0-based).</summary>
        public static string GetSlotUnlockRequirementZh(int slotIndex) => slotIndex switch
        {
            0 => "开局解锁",
            1 => "开局解锁",
            2 => "舰长等级 ≥ 5 或主线第 1 章通关",
            3 => "舰长等级 ≥ 10 或舰船等级 ≥ 2",
            4 => "主线第 2 章通关或舰船设施「调度中枢」Lv.1",
            5 => "舰船等级 ≥ 4 或研究「多编队协议」",
            _ => "未解锁"
        };

        public static string GetParallelUnlockRequirementEn(int parallelCap) => parallelCap switch
        {
            2 => "Unlocked at start",
            3 => "Commander Lv.8 or Ship facility Automation Core Lv.1",
            4 => "Chapter 3 clear or Ship Lv.5",
            _ => "Locked"
        };

        public static string GetParallelUnlockRequirementZh(int parallelCap) => parallelCap switch
        {
            2 => "开局解锁",
            3 => "舰长等级 ≥ 8 或舰船设施「自动化核心」Lv.1",
            4 => "主线第 3 章通关或舰船等级 ≥ 5",
            _ => "未解锁"
        };
    }
}
