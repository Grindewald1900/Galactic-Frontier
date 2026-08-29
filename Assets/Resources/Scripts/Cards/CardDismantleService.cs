using System.Collections.Generic;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.World;

namespace Assets.Resources.Scripts.Cards
{
    public enum DismantleBlock
    {
        None = 0,
        Missing = 1,
        Bound = 2,
        Busy = 3,
        Slotted = 4,
        Mascot = 5
    }

    public sealed class DismantleResult
    {
        public bool Success;
        public DismantleBlock Block;
        public string Message = "";
        public int Credits;
        public readonly List<PendingLootEntry> Items = new List<PendingLootEntry>();

        public static DismantleResult Fail(DismantleBlock block, string message) =>
            new DismantleResult { Success = false, Block = block, Message = message ?? "" };
    }

    /// <summary>
    /// Destroys an unbound idle card instance for table-driven materials, tickets, and credits.
    /// </summary>
    public static class CardDismantleService
    {
        public static DismantleBlock Evaluate(CardEntity card)
        {
            if (card == null || string.IsNullOrEmpty(card.id))
                return DismantleBlock.Missing;
            if (card.boundReason != CardBoundReason.None)
                return DismantleBlock.Bound;
            if (DeckService.GetOccupation(card.id) != CardOccupationState.Idle)
                return DismantleBlock.Busy;
            if (DeckService.IsCardSlotted(card.id))
                return DismantleBlock.Slotted;
            if (ShipService.MascotCardId == card.id)
                return DismantleBlock.Mascot;
            return DismantleBlock.None;
        }

        public static bool CanDismantle(CardEntity card) => Evaluate(card) == DismantleBlock.None;

        public static DismantleResult TryDismantle(CardEntity card)
        {
            var block = Evaluate(card);
            if (block != DismantleBlock.None)
                return DismantleResult.Fail(block, block.ToString());

            var list = CardListManager.Instance;
            if (list == null)
                return DismantleResult.Fail(DismantleBlock.Missing, "Card list missing.");

            var result = new DismantleResult { Success = true };
            var tier = (int)card.CharacterTier;
            result.Credits = DismantleRules.CreditsForTier(tier);

            DurabilityService.UnequipAllForCard(card.id);
            list.RemoveCardEntity(card);

            foreach (var grant in DismantleRules.ItemGrantsForTier(tier))
            {
                if (grant == null || grant.Quantity <= 0) continue;
                var item = ItemFactory.FromDef(grant.ItemDefId, grant.Quantity, grant.Quality);
                var entry = new PendingLootEntry
                {
                    itemDefId = grant.ItemDefId,
                    quantity = grant.Quantity,
                    quality = grant.Quality,
                    displayName = item != null ? item.itemName : grant.ItemDefId
                };

                if (!ProductionService.TryAddLocal(item))
                    IdleSettlementService.EnqueuePending(
                        grant.ItemDefId, grant.Quantity, grant.Quality, "dismantle");

                result.Items.Add(entry);
            }

            if (result.Credits > 0)
                CurrencyService.AddCredits(result.Credits);

            return result;
        }
    }
}
