using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Cosmetics;
using Assets.Resources.Scripts.Cosmetics.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                new Vector2(28f, 460f),
                new Vector2(280f, 24f),
                14f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            bool isZh = LocalizationUtil.IsSimplifiedChinese;
            CreateLanguageButton(root.transform, UiText.LanguageEnglish, new Vector2(28f, 492f), !isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.English));
            CreateLanguageButton(root.transform, UiText.LanguageChinese, new Vector2(320f, 492f), isZh,
                () => LocalizationUtil.SetLanguage(SystemLanguage.ChineseSimplified));

            NexusUiFactory.CreateButton(
                root.transform,
                "Save",
                UiText.SaveCardData,
                new Vector2(28f, 556f),
                new Vector2(280f, 48f),
                SaveCards,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "Clear",
                UiText.ClearCardsDebug,
                new Vector2(28f, 616f),
                new Vector2(280f, 48f),
                ClearCards,
                NexusTheme.SurfaceRaised,
                NexusTheme.Red,
                14f);

            NexusUiFactory.CreateButton(
                root.transform,
                "MainMenu",
                UiText.ReturnMainMenu,
                new Vector2(28f, 676f),
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
            AvatarFrameService.EnsurePlayer();
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

            ProfilePortrait.Draw(parent, "Profile", new Vector2(28f, 140f), 80f);

            NexusUiFactory.CreateText(
                parent,
                "Name Label",
                UiText.ProfileNameLabel,
                new Vector2(120f, 140f),
                new Vector2(200f, 20f),
                12f,
                NexusTheme.DimText);

            var nameInput = NexusUiFactory.CreateInputField(
                parent,
                "Name Input",
                UiText.ProfileNamePlaceholder,
                new Vector2(120f, 164f),
                new Vector2(188f, 40f));
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
            if (id.Length > 14)
                id = id.Substring(0, 14) + "…";
            NexusUiFactory.CreateText(
                parent,
                "Player Id",
                UiText.ProfileIdLabel(id),
                new Vector2(120f, 212f),
                new Vector2(230f, 20f),
                12f,
                NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                parent,
                "Id Hint",
                UiText.ProfileIdHint,
                new Vector2(120f, 232f),
                new Vector2(230f, 18f),
                11f,
                NexusTheme.DimText);

            NexusUiFactory.CreateText(
                parent,
                "Avatar Label",
                UiText.ProfileAvatarLabel,
                new Vector2(28f, 258f),
                new Vector2(320f, 20f),
                12f,
                NexusTheme.MutedText);

            BuildAvatarPicker(parent);
            BuildFramePicker(parent);
        }

        private static void BuildAvatarPicker(Transform parent)
        {
            var names = UnlockedCharacterPortraits();
            const float thumb = 64f;
            const float gap = 8f;
            float stripW = 240f;
            var content = CreateHorizontalStrip(
                parent,
                new Vector2(28f, 282f),
                new Vector2(stripW, thumb),
                Mathf.Max(stripW, names.Count * (thumb + gap)));

            if (names.Count == 0)
            {
                NexusUiFactory.CreateText(
                    content,
                    "Avatar Empty",
                    UiText.ProfileAvatarEmpty,
                    Vector2.zero,
                    new Vector2(stripW - 8f, thumb),
                    11f,
                    NexusTheme.DimText);
            }
            else
            {
                for (int i = 0; i < names.Count; i++)
                {
                    string preset = names[i] + "_01";
                    Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.characterImagePath, preset);
                    float x = i * (thumb + gap);
                    var icon = NexusUiFactory.CreateIcon(
                        content,
                        "Avatar " + names[i],
                        sprite,
                        new Vector2(x, 0f),
                        new Vector2(thumb, thumb),
                        Color.white);
                    icon.raycastTarget = true;
                    var button = icon.gameObject.AddComponent<Button>();
                    button.onClick.AddListener(() => SaveAvatar(sprite));
                }
            }

            NexusUiFactory.CreateButton(
                parent,
                "Avatar Upload",
                UiText.ProfileAvatarUpload,
                new Vector2(276f, 282f),
                new Vector2(72f, 64f),
                OpenLocalAvatarPicker,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f),
                NexusTheme.Gold,
                12f);
        }

        private static Transform CreateHorizontalStrip(Transform parent, Vector2 position, Vector2 viewSize, float contentWidth)
        {
            var viewport = new GameObject(
                "Avatar Strip", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = viewSize;

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.35f);
            image.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(Mathf.Max(viewSize.x, contentWidth), viewSize.y);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return content.transform;
        }

        private static List<string> UnlockedCharacterPortraits()
        {
            var names = new List<string>();
            var seen = new HashSet<CharacterName>();
            foreach (var card in CardCollectionUi.AllCards())
            {
                if (card == null || card.characterName == CharacterName.Default)
                    continue;
                if (!seen.Add(card.characterName))
                    continue;
                names.Add(card.characterName.ToString());
            }

            names.Sort(string.CompareOrdinal);
            return names;
        }

        private static void OpenLocalAvatarPicker()
        {
            FileBrowserHelper.OpenFileBrowser(
                SaveAvatar,
                fail =>
                {
                    switch (fail)
                    {
                        case ImagePickFail.InvalidType:
                            NexusSnackbar.Show(UiText.ProfileAvatarInvalidType);
                            break;
                        case ImagePickFail.TooLarge:
                            NexusSnackbar.Show(UiText.ProfileAvatarTooLarge);
                            break;
                        default:
                            NexusSnackbar.Show(UiText.ProfileAvatarBadImage);
                            break;
                    }
                });
        }

        private static void BuildFramePicker(Transform parent)
        {
            NexusUiFactory.CreateText(
                parent,
                "Frame Label",
                UiText.ProfileFrameLabel,
                new Vector2(28f, 362f),
                new Vector2(400f, 20f),
                12f,
                NexusTheme.MutedText);

            string equippedId = AvatarFrameService.Equipped()?.frameId;
            Sprite avatar = ProfilePortrait.LoadAvatarSprite();
            var frames = AvatarFrameCatalog.All;
            for (int i = 0; i < frames.Count; i++)
            {
                var def = frames[i];
                if (def == null) continue;
                float x = 28f + i * 62f;
                bool unlocked = AvatarFrameService.IsUnlocked(def.frameId);
                bool equipped = def.frameId == equippedId;
                Color ring = ProfilePortrait.ParseFrameColor(def.colorHex);
                if (!unlocked)
                    ring = NexusTheme.WithAlpha(ring, 0.35f);

                var chip = NexusUiFactory.CreateBox(
                    parent,
                    "Frame " + def.frameId,
                    new Vector2(x, 386f),
                    new Vector2(56f, 56f),
                    ring,
                    equipped ? NexusTheme.Gold : (Color?)null);
                var image = chip.GetComponent<Image>();
                image.raycastTarget = true;
                var button = chip.AddComponent<Button>();
                string frameId = def.frameId;
                button.onClick.AddListener(() => OnFrameClicked(frameId));

                var inset = NexusUiFactory.CreateIcon(
                    chip.transform,
                    "Thumb",
                    avatar,
                    new Vector2(6f, 6f),
                    new Vector2(44f, 44f),
                    unlocked ? Color.white : new Color(0.45f, 0.45f, 0.5f, 0.85f));
                inset.raycastTarget = false;
            }
        }

        private static void OnFrameClicked(string frameId)
        {
            var def = AvatarFrameCatalog.TryGet(frameId);
            if (def == null) return;
            if (!AvatarFrameService.IsUnlocked(frameId))
            {
                AvatarFrameUi.ShowLocked(def);
                return;
            }

            if (!AvatarFrameService.TryEquip(frameId))
                return;
            AppShell.Instance?.RefreshProfileChrome();
            AppShell.Instance?.RebuildSettingsIfActive();
            NexusSnackbar.Show(UiText.ProfileFrameEquipped);
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
            AppShell.Instance?.RefreshProfileChrome();
            NexusSnackbar.Show(UiText.ProfileSaved);
        }

        private static void SaveAvatar(Sprite sprite)
        {
            if (sprite == null || DataUtil.Instance?.currentPlayer == null)
                return;
            string path = DataUtil.Instance.GetPlayerAvatarPath(DataUtil.Instance.currentPlayer.playerID);
            if (ImageUtil.TrySaveSpritePng(sprite, path))
            {
                NexusSnackbar.Show(UiText.ProfileAvatarSaved);
                AppShell.Instance?.RefreshProfileChrome();
                AppShell.Instance?.RebuildSettingsIfActive();
            }
            else
            {
                NexusSnackbar.Show(UiText.ProfileAvatarSaveFailed);
            }
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
                    string message = LocalizationUtil.IsSimplifiedChinese ? zh : en;
                    NexusSnackbar.Show(message);
                    if (ok)
                    {
                        AppShell.Instance?.RefreshProfileChrome();
                        AppShell.Instance?.RebuildSettingsIfActive();
                    }
                    else
                    {
                        status.text = message;
                        status.color = NexusTheme.Red;
                    }
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
