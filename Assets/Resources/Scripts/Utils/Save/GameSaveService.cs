using System;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    public enum AutosaveInterval
    {
        Manual = 0,
        OneMinute = 60,
        FiveMinutes = 300,
        TenMinutes = 600,
        ThirtyMinutes = 1800
    }

    /// <summary>
    /// Batches gameplay mutations into interval or manual flushes so hot paths do not write disk
    /// (or spam save logs) every tick. Quit / pause / explicit save still flush immediately.
    /// </summary>
    public static class GameSaveService
    {
        public const string PrefKey = "gf.autosave.seconds";
        private static int immediateDepth;
        private static bool dirty;
        private static float dirtyForSeconds;

        public static AutosaveInterval Interval
        {
            get
            {
                var seconds = PlayerPrefs.GetInt(PrefKey, (int)AutosaveInterval.OneMinute);
                return seconds switch
                {
                    0 => AutosaveInterval.Manual,
                    300 => AutosaveInterval.FiveMinutes,
                    600 => AutosaveInterval.TenMinutes,
                    1800 => AutosaveInterval.ThirtyMinutes,
                    _ => AutosaveInterval.OneMinute
                };
            }
            set
            {
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool HasPendingChanges => dirty;
        public static bool IsImmediate => immediateDepth > 0;
        public static bool ShouldDeferWrites => immediateDepth <= 0;

        public static void PushImmediate() => immediateDepth++;

        public static void PopImmediate()
        {
            if (immediateDepth > 0)
                immediateDepth--;
        }

        public static void MarkDirty() => dirty = true;

        public static void Tick(float unscaledDelta)
        {
            if (!dirty) return;
            var seconds = (int)Interval;
            if (seconds <= 0) return;
            dirtyForSeconds += Mathf.Max(0f, unscaledDelta);
            if (dirtyForSeconds < seconds) return;
            Flush("interval");
        }

        public static bool Flush(string reason = "manual")
        {
            var util = DataUtil.Instance;
            if (util == null) return false;
            PushImmediate();
            try
            {
                var ok = util.TrySaveGameData();
                if (ok)
                {
                    dirty = false;
                    dirtyForSeconds = 0f;
                    Debug.Log($"[SAVE] Flushed ({reason}).");
                }

                return ok;
            }
            finally
            {
                PopImmediate();
            }
        }
    }
}
