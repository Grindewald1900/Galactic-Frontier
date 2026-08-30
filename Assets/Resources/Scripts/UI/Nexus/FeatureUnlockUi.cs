using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Unlock.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Locked-nav dialogs and just-unlocked celebration (Dialog + Notification).</summary>
    internal static class FeatureUnlockUi
    {
        public static bool CanOpen(AppScreen screen)
        {
            if (screen == AppScreen.Debug)
                return true;
            return FeatureUnlockService.IsScreenUnlocked(screen.ToString());
        }

        public static void ShowLocked(AppScreen screen)
        {
            var def = FeatureUnlockService.DefForScreen(screen.ToString());
            string title = UiText.FeatureLockedTitle;
            string name = def != null ? UiText.T(def.titleEn, def.titleZh) : screen.ToString();
            string hint = def != null ? UiText.T(def.hintEn, def.hintZh) : "";
            string body = string.IsNullOrEmpty(hint)
                ? UiText.FeatureLockedBody(name)
                : UiText.FeatureLockedBody(name) + "\n\n" + hint;
            NexusDialog.Show(title, body, UiText.GotIt, null);
        }

        public static void PresentPending(Action<AppScreen> navigate)
        {
            if (NexusDialog.IsOpen) return;
            var pending = FeatureUnlockService.PendingAnnouncements();
            if (pending == null || pending.Count == 0)
            {
                AvatarFrameUi.PresentPending();
                return;
            }

            PresentAt(pending, 0, navigate);
        }

        private static void PresentAt(List<string> ids, int index, Action<AppScreen> navigate)
        {
            if (ids == null || index >= ids.Count)
            {
                AvatarFrameUi.PresentPending();
                return;
            }
            var def = FeatureUnlockCatalog.Get(ids[index]);
            FeatureUnlockService.MarkAnnounced(ids[index]);
            if (def == null)
            {
                PresentAt(ids, index + 1, navigate);
                return;
            }

            string name = UiText.T(def.titleEn, def.titleZh);
            string title = UiText.FeatureUnlockedTitle(name);
            string body = UiText.T(def.bodyEn, def.bodyZh);
            string notify = UiText.T(def.notifyEn, def.notifyZh);
            if (!string.IsNullOrEmpty(notify))
                NexusNotificationBar.Push(notify);

            AppScreen target = ParseScreen(def.navScreen);
            NexusDialog.Show(
                title,
                body,
                UiText.MissionsGo,
                () => navigate?.Invoke(target),
                UiText.Later,
                () => PresentAt(ids, index + 1, navigate));
        }

        private static AppScreen ParseScreen(string navScreen)
        {
            if (Enum.TryParse(navScreen, out AppScreen screen))
                return screen;
            return AppScreen.Bridge;
        }
    }
}
