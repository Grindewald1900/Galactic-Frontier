using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Cards;
using Assets.Scripts.Utils;
using System.Diagnostics;

namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class SkillEntity
    {
        public CharacterName characterName;
        public string skillImage;
        public LocalizedText skillName;
        public LocalizedText skillDescription;
        public CharacterTier skillTier;

        public SkillEntity()
        {
            characterName = CharacterName.Default;
            skillImage = "Skill-Demo";
            skillTier = CharacterTier.None;
        }

        public SkillEntity SetCharacterName(CharacterName name)
        {
            characterName = name;
            return this;
        }

        public SkillEntity SetSkillImage(string image)
        {
            skillImage = image;
            return this;
        }

        public SkillEntity SetSkillTier(CharacterTier level)
        {
            skillTier = level;
            return this;
        }

        public string GetCharacterName()
        {
            return characterName.ToString();
        }

        public string GetSkillName()
        {
            UnityEngine.Debug.Log("GetSkillName: " + skillName);
            return LocalizationUtil.GetLocalizedText(skillName);
        }

        public string GetSkillImage()
        {
            return skillImage;
        }

        public string GetSkillDescription()
        {
            return LocalizationUtil.GetLocalizedText(skillDescription);
        }

        public CharacterTier GetSkillTier()
        {
            return skillTier;
        }
    }
}