using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;
using static Assets.Resources.Scripts.Cards.CardDataManager;

namespace Assets.Resources.Scripts.Entity
{
    public class ExpertiseEntity
    {
        public Status.AttributeType attributeType;
        // e.g. 0.1f, which will be added to panel attribute 1+0.1 = 1.1f(110%)
        public float value;
        public CharacterTier expertiseTier;

        public ExpertiseEntity()
        {
            attributeType = Status.AttributeType.None;
            value = 0f;
            expertiseTier = CharacterTier.None;
        }

        public ExpertiseEntity(Status.AttributeType attributeType, float value, CharacterTier expertiseTier)
        {
            this.attributeType = attributeType;
            this.value = value;
            this.expertiseTier = expertiseTier;
        }
    }
}