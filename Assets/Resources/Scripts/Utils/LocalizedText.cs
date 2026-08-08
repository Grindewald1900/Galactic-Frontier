namespace Assets.Scripts.Utils
{
    /// <summary>Bilingual text payload used by skill data and other serialized content.</summary>
    [System.Serializable]
    public class LocalizedText
    {
        public string en;
        public string zh;

        public LocalizedText()
        {
        }

        public LocalizedText(string english, string simplifiedChinese)
        {
            en = english;
            zh = simplifiedChinese;
        }

        public string Resolve() => LocalizationUtil.GetLocalizedText(this);
    }
}
