using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    /// <summary>
    /// App locale helper. Default language is English; Simplified Chinese is optional.
    /// Persists choice in PlayerPrefs under <see cref="PrefsKey"/>.
    /// UI chrome strings load from Resources/Data/Localization/UiStrings.json.
    /// </summary>
    public static class LocalizationUtil
    {
        public const string PrefsKey = "ui_language";
        public const string EnglishCode = "en";
        public const string SimplifiedChineseCode = "zh-CN";
        public const string UiStringsResourcePath = "Data/Localization/UiStrings";

        public static event Action LanguageChanged;

        public static SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.English;

        public static string CurrentLanguageCode =>
            IsSimplifiedChinese ? SimplifiedChineseCode : EnglishCode;

        public static bool IsSimplifiedChinese =>
            CurrentLanguage == SystemLanguage.ChineseSimplified ||
            CurrentLanguage == SystemLanguage.Chinese;

        private static bool initialized;
        private static bool tableLoaded;
        private static readonly Dictionary<string, UiStringEntry> Table =
            new Dictionary<string, UiStringEntry>(StringComparer.Ordinal);

        [Serializable]
        private class UiStringsTable
        {
            public List<UiStringEntry> entries = new List<UiStringEntry>();
        }

        [Serializable]
        private class UiStringEntry
        {
            public string id;
            public string en;
            public string zh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            string saved = PlayerPrefs.GetString(PrefsKey, EnglishCode);
            ApplyLanguageCode(saved, notify: false);
            EnsureTableLoaded();
            initialized = true;
        }

        /// <summary>Resolves a bilingual string. English is the default fallback.</summary>
        public static string T(string english, string simplifiedChinese)
        {
            Initialize();
            if (IsSimplifiedChinese && !string.IsNullOrEmpty(simplifiedChinese))
                return simplifiedChinese;
            return english ?? string.Empty;
        }

        /// <summary>
        /// Looks up a UI string by id. Prefers current locale, then English, then the id itself.
        /// </summary>
        public static string Get(string id)
        {
            Initialize();
            EnsureTableLoaded();

            if (string.IsNullOrEmpty(id))
                return string.Empty;

            if (!Table.TryGetValue(id, out UiStringEntry entry) || entry == null)
                return id;

            if (IsSimplifiedChinese && !string.IsNullOrEmpty(entry.zh))
                return entry.zh;

            if (!string.IsNullOrEmpty(entry.en))
                return entry.en;

            return !string.IsNullOrEmpty(entry.zh) ? entry.zh : id;
        }

        /// <summary>Formats a UI string id with <see cref="string.Format"/> placeholders.</summary>
        public static string Format(string id, params object[] args)
        {
            string template = Get(id);
            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static string GetLocalizedText(LocalizedText localizedText)
        {
            if (localizedText == null)
                return string.Empty;

            Initialize();
            if (IsSimplifiedChinese && !string.IsNullOrEmpty(localizedText.zh))
                return localizedText.zh;
            return string.IsNullOrEmpty(localizedText.en) ? localizedText.zh ?? string.Empty : localizedText.en;
        }

        public static void SetLanguage(SystemLanguage language)
        {
            Initialize();
            if (language == SystemLanguage.Chinese)
                language = SystemLanguage.ChineseSimplified;

            if (language != SystemLanguage.English && language != SystemLanguage.ChineseSimplified)
                language = SystemLanguage.English;

            if (CurrentLanguage == language)
                return;

            CurrentLanguage = language;
            PlayerPrefs.SetString(PrefsKey, CurrentLanguageCode);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void SetLanguageCode(string code)
        {
            ApplyLanguageCode(code, notify: true);
        }

        /// <summary>Editor / tests: force reload of the UI string table.</summary>
        public static void ReloadUiStrings()
        {
            tableLoaded = false;
            Table.Clear();
            EnsureTableLoaded();
        }

        private static void EnsureTableLoaded()
        {
            if (tableLoaded)
                return;

            tableLoaded = true;
            Table.Clear();

            TextAsset asset = UnityEngine.Resources.Load<TextAsset>(UiStringsResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                Debug.LogWarning("[LOC] UiStrings.json missing at Resources/" + UiStringsResourcePath);
                return;
            }

            try
            {
                UiStringsTable table = JsonUtility.FromJson<UiStringsTable>(asset.text);
                if (table?.entries == null)
                    return;

                foreach (UiStringEntry entry in table.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.id))
                        continue;
                    Table[entry.id] = entry;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LOC] Failed to parse UiStrings.json: " + ex.Message);
            }
        }

        private static void ApplyLanguageCode(string code, bool notify)
        {
            SystemLanguage next = string.Equals(code, SimplifiedChineseCode, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(code, "zh", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(code, "zh_cn", StringComparison.OrdinalIgnoreCase)
                ? SystemLanguage.ChineseSimplified
                : SystemLanguage.English;

            bool changed = CurrentLanguage != next;
            CurrentLanguage = next;
            PlayerPrefs.SetString(PrefsKey, CurrentLanguageCode);
            PlayerPrefs.Save();

            if (notify && changed)
                LanguageChanged?.Invoke();
        }
    }
}
