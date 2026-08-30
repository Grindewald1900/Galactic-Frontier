using System.Collections.Generic;
using Assets.Resources.Scripts.Cosmetics;
using Assets.Resources.Scripts.Cosmetics.Domain;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Dialog + notification when a new avatar frame is unlocked.</summary>
    internal static class AvatarFrameUi
    {
        public static void PresentPending()
        {
            if (NexusDialog.IsOpen) return;
            var pending = AvatarFrameService.PendingAnnouncements();
            PresentAt(pending, 0);
        }

        public static void ShowLocked(AvatarFrameDef def)
        {
            string name = def != null ? UiText.T(def.titleEn, def.titleZh) : "";
            string hint = def != null ? UiText.T(def.hintEn, def.hintZh) : "";
            NexusDialog.Show(
                UiText.ProfileFrameLockedTitle,
                UiText.ProfileFrameLockedBody(name, hint),
                UiText.GotIt,
                null);
        }

        private static void PresentAt(List<string> ids, int index)
        {
            if (ids == null || index >= ids.Count) return;
            var def = AvatarFrameCatalog.TryGet(ids[index]);
            AvatarFrameService.MarkAnnounced(ids[index]);
            if (def == null)
            {
                PresentAt(ids, index + 1);
                return;
            }

            string name = UiText.T(def.titleEn, def.titleZh);
            string hint = UiText.T(def.hintEn, def.hintZh);
            NexusNotificationBar.Push(UiText.ProfileFrameUnlockedNotify(name));
            NexusDialog.Show(
                UiText.ProfileFrameUnlockedTitle(name),
                UiText.ProfileFrameUnlockedBody(hint),
                UiText.GotIt,
                () => PresentAt(ids, index + 1),
                null,
                () => PresentAt(ids, index + 1));
        }
    }
}
