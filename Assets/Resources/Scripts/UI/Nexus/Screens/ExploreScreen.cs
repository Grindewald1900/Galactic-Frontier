using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Explore / region select using legacy planet sprites.
    /// Starts fully automatic battle (product: no manual turn input).
    /// </summary>
    internal static class ExploreScreen
    {
        private static readonly string[] RegionKeys =
        {
            "VII-A Outer Belt",
            "VII-B Mining Spur",
            "VII-C Quantum Rift",
            "VIII Abyssal Edge",
            "IX Convoy Lane"
        };

        public static GameObject Build(Transform parent, System.Action openFormation)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Explore Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            NexusUiFactory.CreateText(
                root.transform,
                "Title",
                UiText.ExploreTitle,
                new Vector2(28f, 20f),
                new Vector2(640f, 40f),
                22f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var hint = NexusUiFactory.CreateText(
                root.transform,
                "Hint",
                UiText.ExploreHint,
                new Vector2(28f, 64f),
                new Vector2(1100f, 40f),
                13f,
                NexusTheme.MutedText);
            hint.textWrappingMode = TextWrappingModes.Normal;

            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            NexusUiFactory.CreateText(
                root.transform,
                "FleetReady",
                UiText.ExploreFleetReady(inLine),
                new Vector2(28f, 110f),
                new Vector2(600f, 24f),
                13f,
                inLine > 0 ? NexusTheme.Green : NexusTheme.Red);

            for (int i = 0; i < RegionKeys.Length; i++)
            {
                float y = 150f + i * 110f;
                int planetIndex = i * 3;
                int captured = i;

                GameObject row = NexusUiFactory.CreateBox(
                    root.transform,
                    $"Region {i}",
                    new Vector2(28f, y),
                    new Vector2(1200f, 96f),
                    NexusTheme.Surface,
                    NexusTheme.BorderSoft);

                NexusUiFactory.CreateIcon(
                    row.transform,
                    "Planet",
                    NexusCardVisual.PlanetSprite(planetIndex),
                    new Vector2(16f, 8f),
                    new Vector2(80f, 80f),
                    Color.white);

                NexusUiFactory.CreateText(
                    row.transform,
                    "Name",
                    UiText.SectorName(i),
                    new Vector2(120f, 18f),
                    new Vector2(500f, 28f),
                    16f,
                    NexusTheme.Text,
                    TextAlignmentOptions.Left,
                    FontStyles.Bold);

                NexusUiFactory.CreateText(
                    row.transform,
                    "Meta",
                    UiText.ExploreRegionMeta(i),
                    new Vector2(120f, 52f),
                    new Vector2(700f, 28f),
                    12f,
                    NexusTheme.MutedText);

                NexusUiFactory.CreateButton(
                    row.transform,
                    "Start",
                    UiText.StartAutoBattle,
                    new Vector2(980f, 26f),
                    new Vector2(190f, 44f),
                    () => StartBattle(captured, openFormation),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                    NexusTheme.Gold,
                    13f);
            }

            // Right panel: auto-combat rules reminder + formation CTA
            GameObject side = NexusUiFactory.CreateBox(
                root.transform,
                "Side",
                new Vector2(1260f, 150f),
                new Vector2(460f, 620f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                side.transform,
                "SideTitle",
                UiText.ExploreSideTitle,
                new Vector2(20f, 16f),
                new Vector2(400f, 28f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                side.transform,
                "SideBody",
                UiText.ExploreSideBody,
                new Vector2(20f, 56f),
                new Vector2(420f, 280f),
                13f,
                NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                side.transform,
                "Formation",
                UiText.BridgeOpenFormation,
                new Vector2(20f, 360f),
                new Vector2(420f, 48f),
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                14f);

            NexusUiFactory.CreateIcon(
                side.transform,
                "BattleIcon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(180f, 450f),
                new Vector2(100f, 100f),
                NexusTheme.Gold);

            return root;
        }

        private static void StartBattle(int regionIndex, System.Action openFormation)
        {
            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            if (inLine <= 0)
            {
                Debug.LogWarning("ExploreScreen: no lineup — open formation first.");
                openFormation?.Invoke();
                return;
            }

            PlayerPrefs.SetInt("nexus_last_region", regionIndex);
            PlayerPrefs.Save();

            if (CardListManager.Instance != null && !DeckService.IsLoaded && DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || deck.MemberCount < 1)
            {
                Debug.LogWarning("ExploreScreen: active combat deck empty — open formation first.");
                openFormation?.Invoke();
                return;
            }

            var start = DeckService.TryStart(
                deck.deckId,
                DeckActionType.MainCombat,
                "region:" + regionIndex,
                CardListManager.Instance?.cardEntities);
            if (!start.Success)
            {
                Debug.LogWarning(
                    $"[DECK] Cannot start battle: {start.Message} conflicts=[{string.Join(",", start.ConflictCardIds)}]");
                return;
            }

            BattleController.PendingBattleTargetId = "region:" + regionIndex;
            BattleController.PendingBattleSeed =
                unchecked((long)regionIndex * 1_000_003L ^ System.DateTime.UtcNow.Ticks);
            SceneManager.LoadScene("BattleScene");
        }
    }
}
