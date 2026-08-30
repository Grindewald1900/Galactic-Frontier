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

            BuildProfileSection(root.transform);

            NexusUiFactory.CreateText(
                root.transform,
                "Language Label",
                UiText.Language,
                new Vector2(28f, 360f),
                new Vector2(280f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            bool isZh = LocalizationUtil.IsSimplifiedChinese;
            CreateLanguageButton(root.transform, UiText.LanguageEnglish, new Vector2(28f, 392f), !isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.English));
            CreateLanguageButton(root.transform, UiText.LanguageChinese, new Vector2(320f, 392f), isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.ChineseSimplified));

            NexusUiFactory.CreateButton(
                root.transform,
                "Save",
                UiText.SaveCardData,
                new Vector2(28f, 456f),
                new Vector2(280f, 48f),
                SaveCards,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "Clear",
                UiText.ClearCardsDebug,
                new Vector2(28f, 516f),
                new Vector2(280f, 48f),
                ClearCards,
                NexusTheme.SurfaceRaised,
                NexusTheme.Red,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "MainMenu",
                UiText.ReturnMainMenu,
                new Vector2(28f, 576f),
                new Vector2(280f, 48f),
                () =>
                {
                    DataUtil.Instance?.TrySaveGameData();
                    LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainMenuScene));
                },
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                14f);

            BuildGiftCodeSection(root.transform);
            BuildDebugToggleSection(root.transform);

            return root;
        }

        private static void BuildProfileSection(Transform parent)
        {
            var player = DataUtil.Instance?.currentPlayer;
            NexusUiFactory.CreateText(
                parent,
                "Profile Label",
                UiText.ProfileSection,
                new Vector2(28f, 112f),
                new Vector2(400f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateText(
                parent,
                "Name Label",
                UiText.ProfileNameLabel,
                new Vector2(28f, 140f),
                new Vector2(200f, 20f),
                12f,
                NexusTheme.DimText);

            var nameInput = NexusUiFactory.CreateInputField(
                parent,
                "Name Input",
                UiText.ProfileNamePlaceholder,
                new Vector2(28f, 164f),
                new Vector2(280f, 40f));
            nameInput.text = player?.playerName ?? "";
            nameInput.characterLimit = 16;

            NexusUiFactory.CreateButton(
                parent,
                "Save Name",
                UiText.ProfileSaveName,
                new Vector2(320f, 164f),
                new Vector2(160f, 40f),
                () => SaveDisplayName(nameInput.text),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                13f);

            string id = player?.playerID ?? "—";
            if (id.Length > 18)
                id = id.Substring(0, 18) + "…";
            NexusUiFactory.CreateText(
                parent,
                "Player Id",
                UiText.ProfileIdLabel(id),
                new Vector2(28f, 212f),
                new Vector2(620f, 20f),
                12f,
                NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                parent,
                "Id Hint",
                UiText.ProfileIdHint,
                new Vector2(28f, 232f),
                new Vector2(620f, 18f),
                11f,
                NexusTheme.DimText);

            NexusUiFactory.CreateText(
                parent,
                "Avatar Label",
                UiText.ProfileAvatarLabel,
                new Vector2(28f, 258f),
                new Vector2(200f, 20f),
                12f,
                NexusTheme.MutedText);

            string[] presets = { "Asra_01", "Magki_01", "Sernia_01" };
            for (int i = 0; i < presets.Length; i++)
            {
                string preset = presets[i];
                Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.characterImagePath, preset);
                float x = 28f + i * 88f;
                var icon = NexusUiFactory.CreateIcon(
                    parent,
                    "Avatar " + preset,
                    sprite,
                    new Vector2(x, 282f),
                    new Vector2(72f, 72f),
                    Color.white);
                icon.raycastTarget = true;
                var button = icon.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => SaveAvatar(sprite));
            }
        }

        private static void SaveDisplayName(string raw)
        {
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null || DataUtil.Instance == null)
                return;
            string name = (raw ?? "").Trim();
            if (name.Length < 1 || name.Length > 16)
            {
                NexusSnackbar.Show(UiText.ProfileNameInvalid);
                return;
            }

            player.playerName = name;
            DataUtil.Instance.SavePlayerData(player);
            AppShell.Instance?.RefreshCommanderLabel();
            NexusSnackbar.Show(UiText.ProfileSaved);
        }

        private static void SaveAvatar(Sprite sprite)
        {
            if (sprite == null || DataUtil.Instance?.currentPlayer == null)
                return;
            string path = DataUtil.Instance.GetPlayerAvatarPath(DataUtil.Instance.currentPlayer.playerID);
            if (ImageUtil.TrySaveSpritePng(sprite, path))
                NexusSnackbar.Show(UiText.ProfileAvatarSaved);
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
                    NexusSnackbar.Show(status.text);
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
            NexusSnackbar.Show(UiText.GameSaved);
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
