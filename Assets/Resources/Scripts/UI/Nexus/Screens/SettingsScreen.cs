using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Settings: language, save/clear, gift-code rewards, and Debug Mode toggle.
    /// </summary>
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
                new Vector2(28f, 220f),
                new Vector2(280f, 48f),
                SaveCards,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "Clear",
                UiText.ClearCardsDebug,
                new Vector2(28f, 280f),
                new Vector2(280f, 48f),
                ClearCards,
                NexusTheme.SurfaceRaised,
                NexusTheme.Red,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "MainMenu",
                UiText.ReturnMainMenu,
                new Vector2(28f, 340f),
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

            BuildGiftCodeSection(root.transform);
            BuildDebugToggleSection(root.transform);

            string player = DataUtil.Instance?.currentPlayer?.playerName ?? "(no player)";
            NexusUiFactory.CreateText(
                root.transform,
                "Player",
                UiText.CurrentPlayer(player),
                new Vector2(28f, 720f),
                new Vector2(600f, 28f),
                13f,
                NexusTheme.Cyan);

            return root;
        }

        private static void BuildGiftCodeSection(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Gift Label",
                UiText.GiftCodeSection,
                new Vector2(360f, 220f),
                new Vector2(400f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateText(
                parent,
                "Gift Hint",
                UiText.GiftCodeHint,
                new Vector2(360f, 250f),
                new Vector2(560f, 40f),
                12f,
                NexusTheme.MutedText);

            var input = NexusUiFactory.CreateInputField(
                parent,
                "Gift Input",
                UiText.GiftCodePlaceholder,
                new Vector2(360f, 300f),
                new Vector2(360f, 44f));

            var status = NexusUiFactory.CreateText(
                parent,
                "Gift Status",
                string.Empty,
                new Vector2(360f, 360f),
                new Vector2(560f, 48f),
                12f,
                NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                parent,
                "Gift Redeem",
                UiText.GiftCodeRedeem,
                new Vector2(740f, 300f),
                new Vector2(140f, 44f),
                () =>
                {
                    bool ok = GiftCodeService.TryRedeem(input.text, out string en, out string zh);
                    status.text = LocalizationUtil.IsSimplifiedChinese ? zh : en;
                    status.color = ok ? NexusTheme.Gold : NexusTheme.Red;
                    if (ok)
                        input.text = string.Empty;
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);
        }

        private static void BuildDebugToggleSection(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Debug Label",
                UiText.DebugModeSection,
                new Vector2(360f, 430f),
                new Vector2(400f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateText(
                parent,
                "Debug Hint",
                UiText.DebugModeHint,
                new Vector2(360f, 460f),
                new Vector2(560f, 40f),
                12f,
                NexusTheme.MutedText);

            bool enabled = DebugModeController.Instance != null && DebugModeController.Instance.IsEnabled;
            NexusUiFactory.CreateButton(
                parent,
                "Debug Toggle",
                enabled ? UiText.DebugModeToggleOff : UiText.DebugModeToggleOn,
                new Vector2(360f, 510f),
                new Vector2(320f, 48f),
                () =>
                {
                    bool next = !(DebugModeController.Instance?.IsEnabled ?? false);
                    DebugModeController.Instance?.SetEnabled(next);
                },
                enabled
                    ? NexusTheme.WithAlpha(NexusTheme.Red, 0.2f)
                    : NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                enabled ? NexusTheme.Red : NexusTheme.Gold,
                14f);
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
