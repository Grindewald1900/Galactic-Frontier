using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using TMPro;

namespace Assets.Resources.Scripts.UI.Nexus
{
    internal static class CompactStatusBar
    {
        public static string BuildSummary()
        {
            var player = DataUtil.Instance?.currentPlayer;
            int level = player?.level ?? 1;
            int credits = player?.creditPoints ?? 0;

            DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);
            int running = 0;
            int queue = 0;
            int berths = DeckConstants.DefaultUnlockedDeckSlots;
            if (DeckService.State != null)
            {
                berths = DeckService.State.unlockedDeckSlots;
                if (DeckService.State.decks != null)
                {
                    foreach (var deck in DeckService.State.decks)
                    {
                        if (deck?.action == null) continue;
                        if (deck.action.status == DeckActionStatus.Running)
                        {
                            running++;
                            if (deck.action.actionType is DeckActionType.Process
                                or DeckActionType.Manufacture
                                or DeckActionType.Gather)
                                queue++;
                        }
                    }
                }
            }

            int cargo = ItemManager.Instance?.GetItems()?.Count ?? 0;
            string cruise = running > 0 ? UiText.StatusCruiseActive : UiText.StatusCruiseIdle;

            return UiText.StatusBarSummary(level, credits, running, berths, queue, cargo, cruise);
        }

        public static void Apply(TextMeshProUGUI label)
        {
            if (label == null) return;
            label.text = BuildSummary();
        }
    }
}
