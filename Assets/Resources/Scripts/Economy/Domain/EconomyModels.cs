using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Progression.Domain;

namespace Assets.Resources.Scripts.Economy.Domain
{
    [Serializable]
    public class ItemDef
    {
        public string itemDefId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string descriptionEn = "";
        public string descriptionZh = "";
        public ItemCategory category = ItemCategory.Material;
        public string icon = "Steel";
        public int maxStack = 999;
        public bool stackable = true;
        public int baseMaxDurability;
        public EquipSlot equipSlot = EquipSlot.None;
        public int baseCost;
    }

    [Serializable]
    public class RecipeInput
    {
        public string itemDefId = "";
        public int quantity = 1;
        public int minQuality = 1;
    }

    [Serializable]
    public class RecipeDef
    {
        public string recipeId = "";
        public string chainId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public RecipeKind kind = RecipeKind.Process;
        public RecipeInput[] inputs = Array.Empty<RecipeInput>();
        public string outputDefId = "";
        public int outputQty = 1;
        public int cycleSeconds = EconomyConstants.CraftCycleSeconds;
        public string facilityModuleId = "";
        public bool outputIsEquipment;
        public ProfessionSkill requiredProfession = ProfessionSkill.Craft;
        public int requiredSkillLevel = 1;
    }

    [Serializable]
    public class GatherNodeDef
    {
        public string nodeId = "";
        public string regionId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string outputDefId = "";
        public int outputQty = 2;
        public int outputQuality = EconomyConstants.DefaultQuality;
        public int cycleSeconds = EconomyConstants.GatherCycleSeconds;
        public int riskLevel = 1; // 1 low, 2 mid, 3 high
        public ProfessionSkill requiredProfession = ProfessionSkill.Gather;
        public int requiredSkillLevel = 1;
    }

    [Serializable]
    public class PendingLootEntry
    {
        public string itemDefId = "";
        public int quality = EconomyConstants.DefaultQuality;
        public int quantity = 1;
        public string displayName = "";
        public bool isEquipment;
        public int maxDurability;
    }

    [Serializable]
    public class RecipeMasteryEntry
    {
        public string recipeId = "";
        public int successCount;
    }

    [Serializable]
    public class PlayerIdleState
    {
        public long lastSeenAtUtc;
        public long lastPauseAtUtc;
        public List<PendingLootEntry> pendingLoot = new List<PendingLootEntry>();
        /// <summary>Online gather output held back until the player collects it from Explore.</summary>
        public List<PendingLootEntry> gatherBank = new List<PendingLootEntry>();
        public List<RecipeMasteryEntry> mastery = new List<RecipeMasteryEntry>();
        public int count;
        public List<OfflineProgressNote> lastProgressNotes = new List<OfflineProgressNote>();
    }

    [Serializable]
    public class OfflineProgressNote
    {
        public string textEn = "";
        public string textZh = "";
    }

    public sealed class EconomyCommandResult
    {
        public bool Success;
        public string Message = "";
        public static EconomyCommandResult Ok() => new EconomyCommandResult { Success = true };
        public static EconomyCommandResult Ok(string message) =>
            new EconomyCommandResult { Success = true, Message = message ?? "" };
        public static EconomyCommandResult Fail(string message) =>
            new EconomyCommandResult { Success = false, Message = message ?? "" };
    }

    /// <summary>Serializable inventory row shape used by pure InventoryRules (mirrors ItemEntity fields).</summary>
    [Serializable]
    public class InventoryStack
    {
        public string itemDefId = "";
        public string itemName = "";
        public string itemDescription = "";
        public string itemIcon = "";
        public int quality = EconomyConstants.DefaultQuality;
        public int quantity = 1;
        public string itemInstanceId = "";
        public int durability;
        public int maxDurability;
        public string equippedToCardId = "";
        public int itemType; // legacy ItemType int
        public int itemCost;
        public bool isRemote;
    }
}
