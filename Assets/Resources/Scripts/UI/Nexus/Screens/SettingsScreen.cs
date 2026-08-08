using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Settings screen with save/clear actions and English / Simplified Chinese locale switch.</summary>
    internal static class SettingsScreen
    {
        public static GameObject Build(Transform parent)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Settings Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            NexusUiFactory.CreateText(
                root.transform,
                "Title",
                UiText.SettingsTitle,
                new Vector2(28f, 24f),
                new Vector2(480f, 40f),
                22f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateText(
                root.transform,
                "Hint",
                UiText.SettingsHint,
                new Vector2(28f, 70f),
                new Vector2(900f, 40f),
                13f,
                NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                root.transform,
                "Language Label",
                UiText.Language,
                new Vector2(28f, 120f),
                new Vector2(280f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            bool isZh = LocalizationUtil.IsSimplifiedChinese;
            CreateLanguageButton(root.transform, UiText.LanguageEnglish, new Vector2(28f, 152f), !isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.English));
            CreateLanguageButton(root.transform, UiText.LanguageChinese, new Vector2(320f, 152f), isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.ChineseSimplified));

            NexusUiFactory.CreateButton(
                root.transform,
                "Save",
                UiText.SaveCardData,
                new Vector2(28f, 230f),
                new Vector2(280f, 48f),
                SaveCards,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "Clear",
                UiText.ClearCardsDebug,
                new Vector2(28f, 294f),
                new Vector2(280f, 48f),
                ClearCards,
                NexusTheme.SurfaceRaised,
                NexusTheme.Red,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "MainMenu",
                UiText.ReturnMainMenu,
                new Vector2(28f, 358f),
                new Vector2(280f, 48f),
                () =>
                {
                    if (SceneLoader.Instance != null)
                        SceneLoader.Instance.LoadScene(nameof(SceneLoader.SceneName.MainMenuScene));
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                },
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                14f);

            string player = DataUtil.Instance?.currentPlayer?.playerName ?? "(no player)";
            NexusUiFactory.CreateText(
                root.transform,
                "Player",
                UiText.CurrentPlayer(player),
                new Vector2(28f, 430f),
                new Vector2(600f, 28f),
                13f,
                NexusTheme.Cyan);

            return root;
        }

        private static void CreateLanguageButton(Transform parent, string label, Vector2 position, bool active, UnityEngine.Events.UnityAction onClick)
        {
            Color bg = active
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                : NexusTheme.SurfaceRaised;
            Color fg = active ? NexusTheme.Gold : NexusTheme.Text;
            NexusUiFactory.CreateButton(parent, $"Lang {label}", label, position, new Vector2(280f, 44f), onClick, bg, fg, 14f);
        }

        private static void SaveCards()
        {
            if (CardListManager.Instance == null || DataUtil.Instance == null) return;
            DataUtil.Instance.SaveCardData(CardListManager.Instance.GetCardEntities());
            Debug.Log("SettingsScreen: card data saved.");
        }

        private static void ClearCards()
        {
            if (CardListManager.Instance == null) return;
            CardListManager.Instance.ClearCardEntities();
            DataUtil.Instance?.SaveCardData(CardListManager.Instance.GetCardEntities());
            Debug.Log("SettingsScreen: cards cleared.");
        }
    }
}
