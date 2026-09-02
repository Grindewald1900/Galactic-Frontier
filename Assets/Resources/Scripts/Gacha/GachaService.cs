using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Gacha.Domain;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Gacha
{
    /// <summary>Standard pool pull: consume recruit tickets, generate Lv1 cards, optional grant.</summary>
    public static class GachaService
    {
        public static PlayerGachaState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null)
            {
                State ??= new PlayerGachaState();
                return;
            }

            if (State != null) return;
            var loaded = util.LoadGachaState();
            State = loaded ?? new PlayerGachaState();
            if (loaded == null)
                util.SaveGachaState(State, touchMeta: false);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            util?.SaveGachaState(State, touchMeta: true);
        }

        public static int TicketCount()
        {
            var stacks = ItemFactory.ToStacks(ProductionService.GetLocalItems());
            return InventoryRules.CountOf(stacks, GachaRules.TicketDefId, 1);
        }

        public static bool CanAfford(int pullCount, out int missing)
        {
            var need = GachaRules.TicketsRequired(pullCount);
            var have = TicketCount();
            missing = Math.Max(0, need - have);
            return missing == 0 && need > 0;
        }

        public static GachaPullResult TryPull(int pullCount, bool grantImmediately, System.Random rng = null)
        {
            EnsureLoaded();
            if (pullCount <= 0)
                return GachaPullResult.Fail("Invalid pull count.");

            var need = GachaRules.TicketsRequired(pullCount);
            if (!CanAfford(pullCount, out var missing))
                return GachaPullResult.Fail($"Need {missing} more recruit ticket(s).");

            var mgr = CardDataManager.Instance;
            if (mgr == null)
                return GachaPullResult.Fail("CardDataManager missing.");

            if (!ProductionService.TryConsumeLocal(GachaRules.TicketDefId, need, 1))
                return GachaPullResult.Fail("Failed to consume recruit tickets.");

            var cards = new List<CardEntity>();
            var forced = 0;
            try
            {
                for (var i = 0; i < pullCount; i++)
                {
                    var pityForce = GachaRules.ShouldForceHighTier(State.pityCounter);
                    if (pityForce) forced++;
                    Character character;
                    if (!State.starterColeGranted)
                    {
                        character = CharacterSkillController.GetCharacter(CharacterName.Cole);
                        State.starterColeGranted = true;
                    }
                    else
                    {
                        character = mgr.GetCharacter(rng);
                    }
                    if (character == null)
                        throw new InvalidOperationException("Empty character pool.");
                    var entity = mgr.GetGachaCardEntity(character, pityForce, rng);
                    cards.Add(entity);
                    var hit = entity.CharacterTier >= CharacterTier.TierA;
                    State.pityCounter = GachaRules.AdvancePity(State.pityCounter, hit);
                    State.totalPulls++;
                }

                State.lastPoolId = GachaRules.StandardPoolId;
                State.count = State.totalPulls;
                Save();
            }
            catch (Exception ex)
            {
                ProductionService.TryAddLocal(ItemFactory.FromDef(GachaRules.TicketDefId, need, EconomyConstants.DefaultQuality));
                Debug.LogError("[GACHA] Generate failed, tickets refunded: " + ex.Message);
                return GachaPullResult.Fail("Generate failed; tickets refunded.");
            }

            var granted = false;
            if (grantImmediately)
            {
                Grant(cards);
                granted = true;
                ChapterQuestService.NotifyEmergencyRecruit();
                ChapterQuestService.Evaluate();
            }

            return GachaPullResult.Ok(cards, need, State.pityCounter, forced, granted);
        }

        public static void Grant(List<CardEntity> cards)
        {
            if (cards == null || cards.Count == 0) return;
            if (CardListManager.Instance != null)
            {
                CardListManager.Instance.AddCardEntity(cards);
                return;
            }

            var util = DataUtil.Instance;
            if (util == null) return;
            var existing = util.LoadCardData() ?? new List<CardEntity>();
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (existing.Exists(c => c != null && c.id == card.id)) continue;
                existing.Add(card);
            }
            util.SaveCardData(existing);
        }
    }
}
