using System;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>Online settlement for AutoCombat, Gather, and craft cycles (P2+P3).</summary>
    public sealed class IdleEconomyTicker : MonoBehaviour
    {
        public static IdleEconomyTicker Instance { get; private set; }

        private float farmAcc;
        private float gatherAcc;
        private float craftAcc;
        private float seenAcc;

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

            IdleSettlementService.EnsureLoaded();
            seenAcc += Time.unscaledDeltaTime;
            if (seenAcc >= 30f)
            {
                seenAcc = 0f;
                IdleSettlementService.TickOnlineSeen();
                IdleSettlementService.Save();
            }

            var busy = DeckService.GetBusyDecks();
            if (busy == null || busy.Count == 0)
            {
                farmAcc = gatherAcc = craftAcc = 0f;
                return;
            }

            farmAcc += Time.unscaledDeltaTime;
            gatherAcc += Time.unscaledDeltaTime;
            craftAcc += Time.unscaledDeltaTime;

            if (farmAcc >= WorldConstants.FarmCycleSeconds)
            {
                farmAcc = 0f;
                foreach (var deck in busy)
                {
                    if (deck?.action == null) continue;
                    if (deck.action.status != DeckActionStatus.Running) continue;
                    if (deck.action.actionType == DeckActionType.AutoCombat)
                        TickFarm(deck);
                }
            }

            if (gatherAcc >= EconomyConstants.GatherCycleSeconds)
            {
                gatherAcc = 0f;
                foreach (var deck in busy)
                {
                    if (deck?.action == null) continue;
                    if (deck.action.status != DeckActionStatus.Running) continue;
                    if (deck.action.actionType == DeckActionType.Gather)
                        TickGather(deck);
                }
            }

            if (craftAcc >= EconomyConstants.CraftCycleSeconds)
            {
                craftAcc = 0f;
                foreach (var deck in busy)
                {
                    if (deck?.action == null) continue;
                    if (deck.action.status != DeckActionStatus.Running) continue;
                    if (deck.action.actionType == DeckActionType.Process
                        || deck.action.actionType == DeckActionType.Manufacture)
                    {
                        Economy.ProductionService.TrySettleCraftCycle(
                            deck, CardListManager.Instance?.cardEntities);
                    }
                }
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

            if (Economy.DurabilityService.HasCriticalBrokenEquipped())
            {
                ActionScheduler.TryPauseBlock(
                    DeckService.State, deck.deckId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                DeckService.Save();
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

            if (result.Victory)
            {
                Economy.DurabilityService.ApplyCombatWearToEquipped(cards);
                var grant = RewardService.GrantForFarmCycle(regionId);
                if (!grant.Success && result.LootScrap > 0)
                    GrantScrap(result.LootScrap);
            }

            WorldService.MarkFarmTick(regionId);
            DataUtil.Instance?.SaveCardData(cards);
        }

        private void TickGather(DeckEntity deck)
        {
            var node = GatherNodeCatalog.Get(deck.action.targetId);
            if (node == null)
            {
                DeckService.TryStop(deck.deckId);
                return;
            }

            Economy.IdleSettlementService.EnsureLoaded();

            // Output is banked on the job rather than dropped into the warehouse, so the player
            // collects it from Explore and sees exactly what the run produced.
            if (Economy.IdleSettlementService.IsGatherBankFull
                || Economy.DurabilityService.HasCriticalBrokenEquipped())
            {
                ActionScheduler.TryPauseBlock(
                    DeckService.State, deck.deckId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                DeckService.Save();
                return;
            }

            Economy.IdleSettlementService.BankGather(node.outputDefId, node.outputQuality, node.outputQty);
            Economy.DurabilityService.ApplyGatherWear(node.riskLevel);
            deck.action.lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            DeckService.Save();
            Assets.Resources.Scripts.Onboarding.OnboardingService.NotifyGatherProgress();
            Debug.Log($"[GATHER] {node.nodeId} +{node.outputQty} {node.outputDefId}");
        }

        private static void GrantScrap(int qty)
        {
            if (qty <= 0) return;
            var created = Economy.ItemFactory.FromDef(EconomyConstants.ScrapDefId, qty, EconomyConstants.DefaultQuality);
            Economy.ProductionService.TryAddLocal(created);
        }
    }
}
