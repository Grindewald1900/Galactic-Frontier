using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;

namespace Assets.Resources.Scripts.Entity
{
    public class ExpertiseEntity
    {
        public Status.AttributeType attributeType;
        // e.g. 0.1f, which will be added to panel attribute 1+0.1 = 1.1f(110%)
        public float value;
        public CharacterTier expertiseTier;

        public ExpertiseEntity(Status.AttributeType attributeType, float value, CharacterTier expertiseTier)
        {
            this.attributeType = attributeType;
            this.value = value;
            this.expertiseTier = expertiseTier;
        }
    }
}