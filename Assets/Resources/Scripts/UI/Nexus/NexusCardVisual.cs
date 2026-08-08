using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Reuses legacy card art paths (character portraits, tier frames, badges) inside AppShell screens.
    /// </summary>
    internal static class NexusCardVisual
    {
        public static Sprite CharacterSprite(CardEntity entity)
        {
            if (entity == null)
                return ImageUtil.GetSpriteByName(ImageUtil.imageDefaultPath, "Default");

            return ImageUtil.GetSpriteByName(
                ImageUtil.characterImagePath,
                entity.characterName + "_01");
        }

        public static Sprite TierFrameSprite(CardEntity entity)
        {
            if (entity == null || entity.CharacterTier == CharacterTier.None)
                return ImageUtil.GetSpriteByName(ImageUtil.cardBkImagePath, "CardBase");

            return ImageUtil.GetSpriteByName(
                ImageUtil.cardBkImagePath,
                entity.CharacterTier + "_Default");
        }

        public static Sprite TierBadgeSprite(CardEntity entity)
        {
            if (entity == null || entity.CharacterTier == CharacterTier.None)
                return null;

            return ImageUtil.GetSpriteByName(
                ImageUtil.badgeImagePath,
                entity.CharacterTier.ToString());
        }

        public static Sprite PlanetSprite(int index)
        {
            return ImageUtil.GetSpriteByName(ImageUtil.planetImagePath, $"Planet_{Mathf.Clamp(index, 0, 16)}");
        }

        public static Sprite EventSprite(int index)
        {
            return ImageUtil.GetSpriteByName(ImageUtil.eventImagePath, $"Event_{Mathf.Clamp(index, 0, 4)}");
        }

        public static Sprite UiIcon(string name)
        {
            return ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, name);
        }

        /// <summary>Builds a compact portrait card using legacy sprites.</summary>
        public static GameObject CreatePortraitCard(
            Transform parent,
            string name,
            CardEntity entity,
            Vector2 position,
            Vector2 size,
            string footer)
        {
            GameObject card = NexusUiFactory.CreateBox(
                parent,
                name,
                position,
                size,
                NexusTheme.SurfaceRaised,
                NexusTheme.BorderSoft);

            Sprite frame = TierFrameSprite(entity);
            if (frame != null)
            {
                NexusUiFactory.CreateIcon(
                    card.transform,
                    "Frame",
                    frame,
                    new Vector2(8f, 8f),
                    new Vector2(size.x - 16f, size.y - 56f),
                    Color.white);
            }

            Sprite portrait = CharacterSprite(entity);
            NexusUiFactory.CreateIcon(
                card.transform,
                "Portrait",
                portrait,
                new Vector2(18f, 28f),
                new Vector2(size.x - 36f, size.y - 90f),
                Color.white);

            Sprite badge = TierBadgeSprite(entity);
            if (badge != null)
            {
                NexusUiFactory.CreateIcon(
                    card.transform,
                    "Badge",
                    badge,
                    new Vector2(size.x - 36f, 12f),
                    new Vector2(24f, 24f),
                    Color.white);
            }

            string title = entity != null
                ? (string.IsNullOrEmpty(entity.cardName) ? entity.characterName.ToString() : entity.cardName)
                : "?";
            NexusUiFactory.CreateText(
                card.transform,
                "Name",
                title,
                new Vector2(10f, size.y - 48f),
                new Vector2(size.x - 20f, 22f),
                13f,
                NexusTheme.Text,
                TMPro.TextAlignmentOptions.Left,
                TMPro.FontStyles.Bold);

            if (!string.IsNullOrEmpty(footer))
            {
                NexusUiFactory.CreateText(
                    card.transform,
                    "Footer",
                    footer,
                    new Vector2(10f, size.y - 26f),
                    new Vector2(size.x - 20f, 18f),
                    11f,
                    NexusTheme.Cyan);
            }

            return card;
        }

        public static void ApplyPortraitToImage(Image target, CardEntity entity)
        {
            if (target == null) return;
            target.sprite = CharacterSprite(entity);
            target.preserveAspect = true;
            target.color = Color.white;
        }
    }
}
