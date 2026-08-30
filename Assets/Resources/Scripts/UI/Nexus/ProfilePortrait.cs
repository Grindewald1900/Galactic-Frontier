using Assets.Resources.Scripts.Cosmetics;
using Assets.Resources.Scripts.Cosmetics.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Framed commander portrait used in Settings and the AppShell top bar.</summary>
    internal static class ProfilePortrait
    {
        public const string FallbackPreset = "Asra_01";

        public static Color ParseFrameColor(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return new Color(0.39f, 0.45f, 0.55f);
        }

        public static Sprite LoadAvatarSprite()
        {
            var player = DataUtil.Instance?.currentPlayer;
            if (player != null && DataUtil.Instance != null)
            {
                string path = DataUtil.Instance.GetPlayerAvatarPath(player.playerID);
                var fromDisk = ImageUtil.LoadSpriteFromFile(path);
                if (fromDisk != null) return fromDisk;
            }

            return ImageUtil.GetSpriteByName(ImageUtil.characterImagePath, FallbackPreset);
        }

        public static Image Draw(
            Transform parent,
            string name,
            Vector2 position,
            float size,
            Sprite avatar = null,
            Color? frameColor = null)
        {
            var equipped = AvatarFrameService.Equipped();
            Color ring = frameColor ?? ParseFrameColor(equipped?.colorHex);
            var frame = NexusUiFactory.CreateBox(
                parent,
                name + " Frame",
                position,
                new Vector2(size, size),
                ring);
            var frameImage = frame.GetComponent<Image>();
            frameImage.raycastTarget = false;

            float inset = Mathf.Max(3f, size * 0.1f);
            var portrait = NexusUiFactory.CreateIcon(
                frame.transform,
                name + " Avatar",
                avatar ?? LoadAvatarSprite(),
                new Vector2(inset, inset),
                new Vector2(size - inset * 2f, size - inset * 2f),
                Color.white);
            portrait.raycastTarget = false;
            return portrait;
        }

        public static void Apply(Image frame, Image portrait, AvatarFrameDef def = null, Sprite avatar = null)
        {
            if (frame != null)
                frame.color = ParseFrameColor((def ?? AvatarFrameService.Equipped())?.colorHex);
            if (portrait != null)
                portrait.sprite = avatar ?? LoadAvatarSprite();
        }
    }
}
