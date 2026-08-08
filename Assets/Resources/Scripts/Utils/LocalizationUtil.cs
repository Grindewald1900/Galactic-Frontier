using System;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    /// <summary>
    /// App locale helper. Default language is English; Simplified Chinese is optional.
    /// Persists choice in PlayerPrefs under <see cref="PrefsKey"/>.
    /// </summary>
    public static class LocalizationUtil
    {
        public const string PrefsKey = "ui_language";
        public const string EnglishCode = "en";
        public const string SimplifiedChineseCode = "zh-CN";

        public static event Action LanguageChanged;

        public static SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.English;

        public static string CurrentLanguageCode =>
            IsSimplifiedChinese ? SimplifiedChineseCode : EnglishCode;

        public static bool IsSimplifiedChinese =>
            CurrentLanguage == SystemLanguage.ChineseSimplified ||
            CurrentLanguage == SystemLanguage.Chinese;

        private static bool initialized;

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
