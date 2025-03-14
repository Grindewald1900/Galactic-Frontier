using Assets.Resources.Scripts.Cards;
using static Assets.Resources.Scripts.Cards.CardDataManager;

namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class PortraitEntity
    {
        public string portraitName = "default_portrait";
        public string portraitFrame = "TierE_Default";
        public CharacterTier characterTier = CharacterTier.None;
        bool isShowFrame = true;

        public PortraitEntity(string name, string frame, CharacterTier tier)
        {
            portraitName = name;
            portraitFrame = frame + "_Default";
            characterTier = tier;
        }

        public PortraitEntity(CardEntity cardEntity)
        {
            portraitName = cardEntity.characterName.ToString();
            portraitFrame = cardEntity.CharacterTier.ToString() + "_Default";
            characterTier = cardEntity.CharacterTier;
        }

        public PortraitEntity SetShowFrame(bool show)
        {
            isShowFrame = show;
            return this;
        }

        public bool IsShowFrame()
        {
            return isShowFrame;
        }

        public PortraitEntity() { }
    }
}