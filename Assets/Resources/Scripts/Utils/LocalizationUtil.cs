using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class LocalizationUtil
    {
        public static string GetLocalizedText(LocalizedText localizedText)
        {
            UnityEngine.Debug.Log($"Get Skill localized text: {localizedText}");
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                    return localizedText.zh;
                case SystemLanguage.ChineseSimplified:
                    return localizedText.zh;
                case SystemLanguage.English:
                    return localizedText.en;
                // 添加其他语言支持
                default:
                    return localizedText.en; // 默认返回英文
            }
        }
    }
}