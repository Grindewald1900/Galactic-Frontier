using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.DebugTools
{
    /// <summary>
    /// Gift-code catalog and redemption. Codes grant reward items / credits / cards — not Debug Mode.
    /// </summary>
    public static class GiftCodeService
    {
        public readonly struct GiftReward
        {
            public readonly string ItemName;
            public readonly string ItemIcon;
            public readonly int Quantity;
            public readonly ItemType ItemType;
            public readonly int Credits;
            public readonly bool GrantRandomCard;

            public GiftReward(
                string itemName,
                string itemIcon,
                int quantity,
                ItemType itemType = ItemType.Material,
                int credits = 0,
                bool grantRandomCard = false)
            {
                ItemName = itemName;
                ItemIcon = itemIcon;
                Quantity = quantity;
                ItemType = itemType;
                Credits = credits;
                GrantRandomCard = grantRandomCard;
            }
        }

        public readonly struct GiftCodeDefinition
        {
            public readonly string Code;
            public readonly string DisplayNameEn;
            public readonly string DisplayNameZh;
            public readonly IReadOnlyList<GiftReward> Rewards;

            public GiftCodeDefinition(
                string code,
                string displayNameEn,
                string displayNameZh,
                params GiftReward[] rewards)
            {
                Code = code;
                DisplayNameEn = displayNameEn;
                DisplayNameZh = displayNameZh;
                Rewards = rewards;
            }
        }

        private static readonly GiftCodeDefinition[] Catalog =
        {
            new(
                "WELCOME",
                "Commander Welcome Pack",
                "指挥官欢迎礼包",
                new GiftReward("Copper", "Copper", 40),
                new GiftReward("Water", "Water", 20),
                new GiftReward(null, null, 0, ItemType.Material, credits: 1000)),
            new(
                "COPPER100",
                "Copper Supply",
                "铜材补给",
                new GiftReward("Copper", "Copper", 100)),
            new(
                "STEEL50",
                "Steel Shipment",
                "钢材货运",
                new GiftReward("Steel", "Steel", 50)),
            new(
                "GF-BETA",
                "Galactic Frontier Beta Pack",
                "星际前线内测礼包",
                new GiftReward("Copper", "Copper", 30),
                new GiftReward("Steel", "Steel", 20),
                new GiftReward("GoldBar", "GoldBar", 5),
                new GiftReward(null, null, 0, ItemType.Material, credits: 3000)),
            new(
                "CREDIT5K",
                "Credit Drop",
                "信用点投放",
                new GiftReward(null, null, 0, ItemType.Material, credits: 5000)),
            new(
                "RECRUIT",
                "Emergency Recruit",
                "紧急征召",
                new GiftReward(null, null, 0, ItemType.Material, grantRandomCard: true)),
            new(
                "WATER20",
                "Water Crate",
                "水源箱",
                new GiftReward("Water", "Water", 20)),
            new(
                "WOOD80",
                "Timber Bundle",
                "木材捆",
                new GiftReward("Wood", "Wood", 80)),
        };

        public static IReadOnlyList<GiftCodeDefinition> AllCodes => Catalog;

        public static bool TryRedeem(string rawCode, out string messageEn, out string messageZh)
        {
            messageEn = "Invalid gift code.";
            messageZh = "礼品码无效。";

            if (string.IsNullOrWhiteSpace(rawCode))
                return false;

            string code = rawCode.Trim().ToUpperInvariant();
            GiftCodeDefinition? match = null;
            foreach (var entry in Catalog)
            {
                if (string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    match = entry;
                    break;
                }
            }

            if (match == null)
                return false;

            string playerId = DataUtil.Instance?.currentPlayer?.playerID ?? "local";
            string prefsKey = $"GiftRedeemed_{playerId}_{match.Value.Code}";
            if (PlayerPrefs.GetInt(prefsKey, 0) == 1)
            {
                messageEn = "This gift code was already redeemed on this save.";
                messageZh = "该礼品码已在本存档领取过。";
                return false;
            }

            ApplyRewards(match.Value.Rewards);
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();

            messageEn = $"Redeemed: {match.Value.DisplayNameEn}";
            messageZh = $"已领取：{match.Value.DisplayNameZh}";
            Debug.Log($"[GIFT] Redeemed {match.Value.Code} for player {playerId}");
            return true;
        }

        private static void ApplyRewards(IReadOnlyList<GiftReward> rewards)
        {
            foreach (GiftReward reward in rewards)
            {
                if (reward.Credits > 0)
                    Market.CurrencyService.AddCredits(reward.Credits);

                if (reward.Quantity > 0 && !string.IsNullOrEmpty(reward.ItemName))
                {
                    var item = new ItemEntity(
                        reward.ItemName,
                        $"Gift: {reward.ItemName}",
                        reward.ItemIcon ?? reward.ItemName,
                        0,
                        reward.ItemType).SetQuantity(reward.Quantity);

                    if (ItemManager.Instance != null && ItemManager.Instance.HasLoaded)
                        ItemManager.Instance.AddItem(item);
                    else if (DataUtil.Instance != null)
                    {
                        var list = DataUtil.Instance.LoadInventory(Save.InventoryStore.Local)
                                   ?? new List<ItemEntity>();
                        int idx = list.FindIndex(i =>
                            i != null && string.Equals(i.itemName, item.itemName, StringComparison.Ordinal));
                        if (idx >= 0)
                            list[idx].quantity += item.quantity;
                        else
                            list.Add(item);
                        DataUtil.Instance.SaveInventory(Save.InventoryStore.Local, list);
                    }
                }

                if (reward.GrantRandomCard && CardDataManager.Instance != null && CardListManager.Instance != null)
                {
                    var character = CardDataManager.Instance.GetCharacter();
                    if (character != null)
                    {
                        var card = CardDataManager.Instance.GetCardEntity(character);
                        if (card != null)
                            CardListManager.Instance.AddCardEntity(card);
                    }
                }
            }
        }
    }
}
