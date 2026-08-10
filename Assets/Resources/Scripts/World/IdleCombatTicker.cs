using System;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>Online AFK AutoCombat cycle settlement on MainScene.</summary>
    public sealed class IdleCombatTicker : MonoBehaviour
    {
        public static IdleCombatTicker Instance { get; private set; }

        private float accumulator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!DeckService.IsLoaded)
                return;

            var busy = DeckService.GetBusyDecks();
            if (busy == null || busy.Count == 0)
            {
                accumulator = 0f;
                return;
            }

            accumulator += Time.unscaledDeltaTime;
            if (accumulator < WorldConstants.FarmCycleSeconds)
                return;
            accumulator = 0f;

            foreach (var deck in busy)
            {
                if (deck?.action == null) continue;
                if (deck.action.status != DeckActionStatus.Running) continue;
                if (deck.action.actionType != DeckActionType.AutoCombat) continue;
                TickFarm(deck);
            }
        }

        private void TickFarm(DeckEntity deck)
        {
            var regionId = deck.action.targetId;
            if (string.IsNullOrEmpty(regionId) || !WorldService.IsFarmUnlocked(regionId))
            {
                DeckService.TryStop(deck.deckId);
                return;
            }

            var cards = CardListManager.Instance?.cardEntities;
            var members = DeckService.GetOrderedMembers(deck.deckId, cards);
            float playerPower = 0f;
            var maxWear = false;
            foreach (var m in members)
            {
                if (m == null) continue;
                playerPower += m.power;
                if (m.farmWear >= WorldConstants.FarmWearPauseThreshold)
                    maxWear = true;
            }

            if (maxWear || members.Count == 0)
            {
                ActionScheduler.TryPauseAtCap(
                    DeckService.State, deck.deckId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                DeckService.Save();
                Debug.Log("[FARM] PausedCap due to wear/empty on " + deck.deckId);
                return;
            }

            var region = RegionCatalog.Get(regionId);
            var encounter = EncounterCatalog.Get(region?.farmEncounterId ?? "");
            float enemyPower = FarmCombatResolver.EstimateEncounterPower(encounter);
            long seed = unchecked(regionId.GetHashCode() * 9176L ^ DateTime.UtcNow.Ticks);
            var result = FarmCombatResolver.Resolve(
                playerPower, enemyPower, encounter?.lootScrap ?? WorldConstants.FarmLootQuantity, seed);

            foreach (var m in members)
            {
                if (m == null) continue;
                m.farmWear += WorldConstants.FarmWearPerCycle;
            }

            if (result.Victory && result.LootScrap > 0)
                GrantScrap(result.LootScrap);

            WorldService.MarkFarmTick(regionId);
            DataUtil.Instance?.SaveCardData(cards);
            Debug.Log($"[FARM] cycle region={regionId} win={result.Victory} loot={result.LootScrap}");
        }

        private static void GrantScrap(int qty)
        {
            if (ItemManager.Instance == null || qty <= 0) return;
            var items = ItemManager.Instance.GetItems();
            ItemEntity existing = null;
            foreach (var item in items)
            {
                if (item != null && item.itemName == WorldConstants.FarmLootItemName)
                {
                    existing = item;
                    break;
                }
            }

            if (existing != null)
            {
                existing.quantity += qty;
            }
            else
            {
                var created = new ItemEntity(
                    WorldConstants.FarmLootItemName,
                    "AFK farm scrap",
                    "Steel",
                    0,
                    ItemType.Material).SetQuantity(qty);
                ItemManager.Instance.AddItem(created);
                items = ItemManager.Instance.GetItems();
            }

            DataUtil.Instance?.SaveInventory(InventoryStore.Local, items, touchMeta: true);
        }
    }
}
