using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class LocalizationUtil
    {
        public static SystemLanguage CurrentLanguage { get; set; } = SystemLanguage.English;

        public static string GetLocalizedText(LocalizedText localizedText)
        {
            UnityEngine.Debug.Log($"Get Skill localized text: {localizedText}");
            switch (CurrentLanguage)
            {
                case SystemLanguage.Chinese:
                    return localizedText.zh;
                case SystemLanguage.ChineseSimplified:
                    return localizedText.zh;
                case SystemLanguage.English:
                    return localizedText.en;
                default:
                    return localizedText.en;
            }
        }
    }
}
